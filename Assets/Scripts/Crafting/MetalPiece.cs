using UnityEngine;

// Definiujemy nasze tiery metali
public enum MetalType
{
    Copper,      // Miedź
    Bronze,      // Brąz
    Iron,        // Żelazo
    Steel,       // Stal
    Gold,        // Złoto
    Platinum,    // Platyna
    BlueSteel,   // Niebieska Stal
    Vibranium    // Wibranium
}

public enum HitType
{
    Lengthen, // Wydłużanie (na osi Z)
    Widen     // Poszerzanie (na osi X)
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class MetalPiece : MonoBehaviour, IInteractable, IPickable
{

    public enum MetalPartType { SwordBlade, AxeHead }
    public MetalPartType partType;
    
    [Header("Dane dla Stołu Montażowego")]
    public bool isFinished = false;
    public MetalType metalTier = MetalType.Iron;

    [Header("Ustawienia Temperatury")]
    public float currentTemperature = 20f;
    public float maxTemperature = 1000f;
    public float coolingRate = 10f;
    public float forgingTemperature = 500f;

    [Header("Ustawienia Deformacji (Bezpieczne wartości!)")]
    public float deformRadius = 0.15f; // Zwiększone domyślnie, żeby trafiało w siatkę!
    public float deformForce = 0.15f;
    public float minThickness = 0.01f;
    public float grindRadius = 0.30f;

    [Header("Ustawienia Szpikulca")]
    public float tipLength = 0.15f;
    public float grindSpeed = 0.05f;
    public float maxHalfWidth = 0.1f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    private Vector3[] vertices;
    private MeshRenderer meshRenderer;
    private bool isInForge = false;
    private Color baseColdColor; // Zmienna na nasz kolor

   

    void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshFilter = GetComponentInChildren<MeshFilter>();
        meshCollider = GetComponentInChildren<MeshCollider>();

        // Klonujemy siatkę, żeby nie zepsuć oryginalnego pliku na dysku!
        mesh = meshFilter.mesh;
        vertices = mesh.vertices;

        SetBaseColor(); // Ustawiamy startowy kolor
    }



    void OnValidate()
    {
        // Szukamy w dzieciach również w edytorze
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            SetBaseColor();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", baseColdColor);
            meshRenderer.SetPropertyBlock(block);
        }
    }

    void Update()
    {
        if (!isInForge && currentTemperature > 20f)
        {
            currentTemperature -= coolingRate * Time.deltaTime;
        }
        UpdateVisuals();
    }

    public bool Interact()
    {
        return currentTemperature >= forgingTemperature;
    }

    public void OnPickUp()
    {
        isInForge = false;
    }

    public void OnDrop()
    {
        // iskry itp.
    }

    public void ForceCoolDown()
    {
        currentTemperature = 20f;
        isInForge = false;
        UpdateVisuals();
    }

    // --- NOWA FUNKCJA: Skanuje szerokość metalu w danym punkcie ---
    // --- NOWA FUNKCJA: Gładko interpoluje szerokość metalu między wierzchołkami ---
    public float GetEdgeWidthAt(float localZPosition, bool isFlipped)
    {
        float prevZ = float.MinValue;
        float nextZ = float.MaxValue;

        float prevX = 0.005f; // Domyślna bezpieczna grubość
        float nextX = 0.005f;

        bool foundPrev = false;
        bool foundNext = false;

        // Szukamy dwóch punktów brzegowych, między którymi aktualnie znajduje się kursor/kamień
        for (int i = 0; i < vertices.Length; i++)
        {
            // Filtrujemy tylko wierzchołki zewnętrzne (prawą lub lewą krawędź)
            bool isEdgeVertex = (!isFlipped && vertices[i].x > 0.001f) || (isFlipped && vertices[i].x < -0.001f);

            if (isEdgeVertex)
            {
                float currentX = Mathf.Abs(vertices[i].x); // Zawsze chcemy wartość dodatnią jako grubość

                // 1. Najbliższy wierzchołek ZA kamieniem (mniejsze Z)
                if (vertices[i].z <= localZPosition && vertices[i].z > prevZ)
                {
                    prevZ = vertices[i].z;
                    prevX = currentX;
                    foundPrev = true;
                }
                // 2. Najbliższy wierzchołek PRZED kamieniem (większe Z)
                else if (vertices[i].z > localZPosition && vertices[i].z < nextZ)
                {
                    nextZ = vertices[i].z;
                    nextX = currentX;
                    foundNext = true;
                }
            }
        }

        float exactWidth = 0.005f;

        // --- MATEMATYCZNA LINIA (INTERPOLACJA) ---
        if (foundPrev && foundNext)
        {
            // Obliczamy w jakim procencie drogi (0.0 do 1.0) między wierzchołkami jest kamień
            float t = (localZPosition - prevZ) / (nextZ - prevZ);

            // Tworzymy linię prostą między grubościami i pobieramy z niej punkt
            exactWidth = Mathf.Lerp(prevX, nextX, t);
        }
        else if (foundPrev)
        {
            exactWidth = prevX; // Jesteśmy na samej górze miecza (brak punktów przed nami)
        }
        else if (foundNext)
        {
            exactWidth = nextX; // Jesteśmy na samym dole (brak punktów za nami)
        }

        // Zwracamy idealnie gładką szerokość, z uwzględnieniem skali
        return exactWidth * transform.localScale.x;
    }

    // Zmieniamy void na bool!
    public bool HitMetal(Vector3 hitPoint, Vector3 hitNormal, HitType hitType = HitType.Lengthen)
    {
        if (currentTemperature >= forgingTemperature)
        {
            Debug.Log($"Kucie! Typ: {hitType}");

            // Przekazujemy typ uderzenia dalej
            bool success = DeformMesh(hitPoint, hitNormal, hitType);

            if (success) isFinished = true;
            return success;
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go kuć!");
            return false;
        }
    }

    // Zmieniamy void na bool!
    private bool DeformMesh(Vector3 hitPoint, Vector3 hitNormal, HitType hitType)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);

        float currentThickness = Mathf.Abs(localHitPoint.y) * 2f;
        if (currentThickness < 0.005f) currentThickness = minThickness + 0.05f;

        float resistanceFactor = Mathf.Clamp01((currentThickness - minThickness) / 0.02f);
        if (resistanceFactor <= 0.01f) return false;

        bool wasDeformed = false;

        // --- USTALAMY SIŁĘ ROZLEWANIA W ZALEŻNOŚCI OD MŁOTA ---
        float spreadZ = (hitType == HitType.Lengthen) ? 0.08f : 0.01f;
        float spreadX = (hitType == HitType.Widen) ? 0.08f : 0.01f;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localHitPoint, vertices[i]);

            if (distance < deformRadius)
            {
                float baseForce = (deformRadius - distance) / deformRadius;
                float finalForce = baseForce * deformForce * resistanceFactor;

                float dirY = vertices[i].y > 0.001f ? 1f : (vertices[i].y < -0.001f ? -1f : 0f);
                float dirZ = vertices[i].z > 0.001f ? 1f : (vertices[i].z < -0.001f ? -1f : 0f);
                float dirX = vertices[i].x > 0.001f ? 1f : (vertices[i].x < -0.001f ? -1f : 0f);

                // Zawsze spłaszczamy tak samo
                float targetY = dirY * (minThickness / 2f);
                vertices[i].y = Mathf.Lerp(vertices[i].y, targetY, finalForce);

                // Zmieniamy kształt kierunkowo!
                vertices[i].z += dirZ * (finalForce * spreadZ);
                vertices[i].x += dirX * (finalForce * spreadX);

                wasDeformed = true;
            }
        }

        if (wasDeformed)
        {
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }

        return wasDeformed;
    }

    public void GrindPerfectEdge(float localZPosition, bool isFlipped)
    {
        bool wasDeformed = false;
        float stoneWidth = 0.05f;

        // Zmniejszona prędkość zjadania metalu, żeby dać graczowi czas na reakcję
        float eatSpeed = grindSpeed * 0.01f;

        // Grubość (oś Y), przy której ostrze uznajemy za "naostrzone" i zaczyna się psuć
        float perfectThickness = 0.015f;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Sprawdzamy, czy wierzchołek jest pod kamieniem
            if (Mathf.Abs(vertices[i].z - localZPosition) < stoneWidth)
            {
                // Sprawdzamy, czy to prawa krawędź (nieodwrócony) lub lewa (odwrócony)
                bool isRightEdge = !isFlipped && vertices[i].x > 0.001f;
                bool isLeftEdge = isFlipped && vertices[i].x < -0.001f;

                if (isRightEdge || isLeftEdge)
                {
                    // ==========================================
                    // FAZA 1: OSTRZENIE (Spłaszczanie osi Y)
                    // ==========================================
                    float edgeFactor = Mathf.Abs(vertices[i].x) / maxHalfWidth;
                    edgeFactor = Mathf.Clamp01(edgeFactor);

                    // Płynnie schodzimy do docelowej grubości (0.01f)
                    if (vertices[i].y > 0.01f)
                    {
                        vertices[i].y = Mathf.Lerp(vertices[i].y, 0.01f, edgeFactor * grindSpeed);
                        wasDeformed = true;
                    }

                    // ==========================================
                    // FAZA 2: ZJADANIE METALU (Zmniejszanie osi X)
                    // ==========================================
                    // Odpala się TYLKO wtedy, gdy krawędź jest już cienka!
                    if (vertices[i].y <= perfectThickness)
                    {
                        if (isRightEdge)
                        {
                            vertices[i].x -= eatSpeed;
                            if (vertices[i].x < 0f) vertices[i].x = 0f; // Blokada przed przejściem na drugą stronę
                            wasDeformed = true;
                        }
                        else if (isLeftEdge)
                        {
                            vertices[i].x += eatSpeed;
                            if (vertices[i].x > 0f) vertices[i].x = 0f; // Blokada przed przejściem na drugą stronę
                            wasDeformed = true;
                        }
                    }
                }
            }
        }

        if (wasDeformed)
        {
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    public void SharpenEdge(Vector3 hitPoint)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        bool wasDeformed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localHitPoint, vertices[i]);

            if (distance < grindRadius)
            {
                float force = (grindRadius - distance) / grindRadius;
                vertices[i].y = Mathf.Lerp(vertices[i].y, 0f, force * 0.2f);
                wasDeformed = true;
            }
        }

        if (wasDeformed)
        {
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Forge"))
        {
            isInForge = true;
            if (currentTemperature < maxTemperature) currentTemperature += 250f * Time.deltaTime; // Zostawiłem szybkie nagrzewanie!
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Forge")) isInForge = false;
    }

    // --- SYSTEM KOLORÓW ---
    void SetBaseColor()
    {
        switch (metalTier)
        {
            case MetalType.Copper: baseColdColor = new Color(0.8f, 0.4f, 0.2f); break;
            case MetalType.Bronze: baseColdColor = new Color(0.6f, 0.5f, 0.2f); break;
            case MetalType.Iron: baseColdColor = new Color(0.5f, 0.5f, 0.5f); break;
            case MetalType.Steel: baseColdColor = new Color(0.7f, 0.75f, 0.8f); break;
            case MetalType.Gold: baseColdColor = new Color(1f, 0.84f, 0f); break;
            case MetalType.Platinum: baseColdColor = new Color(0.9f, 0.9f, 0.95f); break;
            case MetalType.BlueSteel: baseColdColor = new Color(0.2f, 0.4f, 0.6f); break;
            case MetalType.Vibranium: baseColdColor = new Color(0.6f, 0.2f, 0.8f); break;
            default: baseColdColor = Color.gray; break;
        }
    }

    void UpdateVisuals()
    {
        float tempNormalized = Mathf.Clamp01((currentTemperature - 20f) / (maxTemperature - 20f));

        Color hotColor = new Color(1f, 0.2f, 0f);
        Color currentColor = Color.Lerp(baseColdColor, hotColor, tempNormalized);

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(block);
        block.SetColor("_Color", currentColor);
        meshRenderer.SetPropertyBlock(block);
    }

    public float GetActualBackOfBlade()
    {
        float minY = float.MaxValue;
        // Przeszukujemy naszą zmodyfikowaną listę wierzchołków
        foreach (Vector3 v in vertices)
        {
            if (v.z < minY) 
            {
                minY = v.z;
            }
        }
        // Zwracamy najmniejsze Z (tył), uwzględniając skalę obiektu
        return minY * transform.localScale.z;
    }
}