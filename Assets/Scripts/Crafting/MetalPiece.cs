using System.Collections.Generic;
using System.Linq;
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

    [Header("Tuning Szlifierki (Prędkość)")]
    [Tooltip("Jak szybko miecz robi się płaski (Faza 1)")]
    public float sharpenMultiplier = 20f;
    [Tooltip("Jak szybko kamień zjada zepsute ostrze (Faza 2)")]
    public float eatMultiplier = 0.8f;



    void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshFilter = GetComponentInChildren<MeshFilter>();
        meshCollider = GetComponentInChildren<MeshCollider>();

        // Klonujemy siatkę, żeby nie zepsuć oryginalnego pliku na dysku!
        mesh = meshFilter.mesh;
        vertices = mesh.vertices;

        // BARDZO WAŻNE: Czyścimy widmo ze starych bloków z Inspektora (OnValidate), które zamrażały kolor "na twardo" i blokowały nagrzewanie!
        if (meshRenderer != null)
        {
            meshRenderer.SetPropertyBlock(null);
        }

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

        // --- NOWOŚĆ: MIĘKKI PĘDZEL SZLIFIERKI ---
        float coreStoneWidth = 0.02f; // Płaski środek kamienia (100% siły wcinania)
        float falloffRadius = 0.08f;  // Miękki brzeg (siła spada płynnie do 0%)

        // Zapisujemy bazowe prędkości
        float baseSharpenSpeed = grindSpeed * sharpenMultiplier * 0.01f * Time.deltaTime;
        float baseEatSpeed = grindSpeed * eatMultiplier * Time.deltaTime;

        float perfectThickness = 0.002f;

        // ==========================================
        // FAZA 1 i 2: OSTRZENIE I WŻERANIE Z GRADIENTEM
        // ==========================================
        for (int i = 0; i < vertices.Length; i++)
        {
            // Sprawdzamy odległość wierzchołka od samego środka kamienia
            float distance = Mathf.Abs(vertices[i].z - localZPosition);

            // Jeśli wierzchołek jest w całkowitej strefie rażenia (nawet tej miękkiej)
            if (distance < falloffRadius)
            {
                bool isRightEdge = !isFlipped && vertices[i].x > 0.001f;
                bool isLeftEdge = isFlipped && vertices[i].x < -0.001f;

                if (isRightEdge || isLeftEdge)
                {
                    // Obliczamy MNOŻNIK SIŁY (od 0.0 do 1.0)
                    float forceMultiplier = 1f;
                    if (distance > coreStoneWidth)
                    {
                        // Płynnie zmniejszamy siłę dla wierzchołków na obrzeżach
                        forceMultiplier = 1f - ((distance - coreStoneWidth) / (falloffRadius - coreStoneWidth));
                    }

                    // Aplikujemy mnożnik do prędkości
                    float currentSharpenSpeed = baseSharpenSpeed * forceMultiplier;
                    float currentEatSpeed = baseEatSpeed * forceMultiplier;

                    // TWARDE ścinanie do zera
                    if (Mathf.Abs(vertices[i].y) > perfectThickness)
                    {
                        vertices[i].y = Mathf.MoveTowards(vertices[i].y, 0f, currentSharpenSpeed);
                        wasDeformed = true;
                    }
                    else
                    {
                        if (isRightEdge)
                        {
                            vertices[i].x = Mathf.MoveTowards(vertices[i].x, 0f, currentEatSpeed);
                            wasDeformed = true;
                        }
                        else if (isLeftEdge)
                        {
                            vertices[i].x = Mathf.MoveTowards(vertices[i].x, 0f, currentEatSpeed);
                            wasDeformed = true;
                        }
                    }
                }
            }
        }

        // ==========================================
        // FAZA 3: DETEKCJA ODCIĘCIA (AMPUTACJA)
        // ==========================================
        float maxRemainingWidth = 0f;
        int verticesInZone = 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Detekcję odcięcia sprawdzamy TYLKO w wąskim rdzeniu kamienia!
            if (Mathf.Abs(vertices[i].z - localZPosition) < coreStoneWidth)
            {
                verticesInZone++;
                if (Mathf.Abs(vertices[i].x) > maxRemainingWidth)
                {
                    maxRemainingWidth = Mathf.Abs(vertices[i].x);
                }
            }
        }

        if (verticesInZone > 0 && maxRemainingWidth <= 0.001f)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].z >= localZPosition - 0.01f)
                {
                    vertices[i].z = localZPosition - 0.02f;
                    vertices[i].x = 0f;
                    vertices[i].y = 0f;
                    wasDeformed = true;
                }
            }
            Debug.Log("<color=red>KRYTYCZNE USZKODZENIE! Ostrze przecięte!</color>");
        }

        // ==========================================
        // ZAKOŃCZENIE I ZAPISANIE SIATKI
        // ==========================================
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
        // Usunięto stare automatyczne grzanie przez Trigger. Teraz zarządza tym Minigra w FurnaceStation.
    }

    void OnTriggerExit(Collider other)
    {
        // Puste
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
        // Sprawdzamy postęp temperatury
        float tempNormalized = Mathf.Clamp01((currentTemperature - 20f) / (maxTemperature - 20f));

        // Ten sam płomień co w piecu
        Color hotColor = new Color(0.8f, 0.25f, 0f);
        Color currentColor = Color.Lerp(baseColdColor, hotColor, tempNormalized);

        // Kuloodporna zmiana - bezpośrednio na materiale! (Instancjonuje go, by ominąć ewentualne blokady globalne)
        Material mat = meshRenderer.material;
        
        // Twarda modyfikacja głównego koloru uderzając po wszystkich znanych flagach silnika
        mat.color = currentColor; 
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", currentColor);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", currentColor);

        // Zapalenie emisji
        if (mat.HasProperty("_EmissionColor")) 
        {
            mat.EnableKeyword("_EMISSION"); 
            mat.SetColor("_EmissionColor", currentColor * (1f + tempNormalized * 4f)); 
        }

        // Pokaż wynik pierwszych zmian z logu w konsoli gdy metal już wyjdzie poza letnią wodę
        if (tempNormalized > 0.5f && tempNormalized < 0.55f)
        {
            Debug.Log($"[MetalPiece] Nagrzałem się w ponad połowie!! Moja obecna temp: {currentTemperature}");
        }
    }


    public float GetBladeLength()
    {
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        foreach (Vector3 v in vertices)
        {
            if (v.z < minZ) minZ = v.z;
            if (v.z > maxZ) maxZ = v.z;
        }
        return (maxZ - minZ) * transform.localScale.z;
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
    
    public float[] GetEdgeVertexPositionsZ()
    {
        HashSet<float> positions = new HashSet<float>();

        for (int i = 0; i < vertices.Length; i++)
        {
            // Ten sam filtr co w GrindPerfectEdge — wierzchołki krawędziowe
            if (vertices[i].x > 0.001f || vertices[i].x < -0.001f)
            {
                float snapped = Mathf.Round(vertices[i].z * 1000f) / 1000f;
                positions.Add(snapped);
            }
        }

        float[] sorted = positions.OrderBy(z => z).ToArray();
        return sorted;
    }
}