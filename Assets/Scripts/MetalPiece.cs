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

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Klonujemy siatkę, żeby nie zepsuć oryginalnego pliku na dysku!
        mesh = meshFilter.mesh;
        vertices = mesh.vertices;

        SetBaseColor(); // Ustawiamy startowy kolor
    }

    void OnValidate()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            SetBaseColor();

            // Kolorowanie w edytorze (używamy "_Color" dla Standard/ProBuilder Shadera)
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

    public void HitMetal(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (currentTemperature >= forgingTemperature)
        {
            Debug.Log("Kucie! Deformacja siatki...");
            DeformMesh(hitPoint, hitNormal);
            isFinished = true; // Stół montażowy to zaakceptuje
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go kuć!");
        }
    }

    // --- TWOJA SPRAWDZONA MATEMATYKA DEFORMACJI ---
    private void DeformMesh(Vector3 hitPoint, Vector3 hitNormal)

    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);

        // OBLICZAMY GRUBOŚĆ
        float currentThickness = Mathf.Abs(localHitPoint.y) * 2f;

        // --- DEBUG LOGI ---
        Debug.Log($"[DEBUG KUCIA] Wykryta grubość: {currentThickness} | Limit minThickness: {minThickness}");

        if (currentThickness < minThickness)
        {
            Debug.LogWarning("<color=red>[BLOKADA]</color> Metal jest już zbyt cienki! Kucie przerwane.");
            return;
        }
        // ------------------

        if (currentThickness < 0.005f) currentThickness = minThickness + 0.05f;

        float resistanceFactor = Mathf.Clamp01((currentThickness - minThickness) / 0.02f);

        Debug.Log($"[DEBUG KUCIA] Resistance Factor: {resistanceFactor}");

        if (resistanceFactor <= 0.01f)
        {
            Debug.LogWarning("<color=orange>[BLOKADA]</color> Resistance Factor za niski! Kucie przerwane.");
            return;
        }

        bool wasDeformed = false;

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

                float targetY = dirY * (minThickness / 2f);
                vertices[i].y = Mathf.Lerp(vertices[i].y, targetY, finalForce);

                vertices[i].z += dirZ * (finalForce * 0.08f);
                vertices[i].x += dirX * (finalForce * 0.01f);

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

    public void GrindPerfectEdge(float localZPosition, bool isFlipped)
    {
        bool wasDeformed = false;
        float stoneWidth = 0.05f;

        float actualBladeLength = mesh.bounds.max.z;
        float tipStartPoint = actualBladeLength - tipLength;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (Mathf.Abs(vertices[i].z - localZPosition) < stoneWidth)
            {
                if ((!isFlipped && vertices[i].x > 0.001f) || (isFlipped && vertices[i].x < -0.001f))
                {
                    float edgeFactor = Mathf.Abs(vertices[i].x) / maxHalfWidth;
                    edgeFactor = Mathf.Clamp01(edgeFactor);
                    vertices[i].y = Mathf.Lerp(vertices[i].y, 0.01f, edgeFactor * grindSpeed);
                    wasDeformed = true;
                }

                if (vertices[i].z > tipStartPoint)
                {
                    float tipFactor = (vertices[i].z - tipStartPoint) / tipLength;
                    tipFactor = Mathf.Clamp01(tipFactor);
                    float targetWidth = Mathf.Lerp(maxHalfWidth, 0f, tipFactor);

                    if (!isFlipped && vertices[i].x > 0.001f)
                    {
                        if (vertices[i].x > targetWidth)
                        {
                            vertices[i].x = Mathf.Lerp(vertices[i].x, targetWidth, grindSpeed * 0.1f);
                            wasDeformed = true;
                        }
                    }
                    else if (isFlipped && vertices[i].x < -0.001f)
                    {
                        if (vertices[i].x < -targetWidth)
                        {
                            vertices[i].x = Mathf.Lerp(vertices[i].x, -targetWidth, grindSpeed * 0.1f);
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
}