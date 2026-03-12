using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class IronPiece : MonoBehaviour
{
    [Header("Ustawienia Temperatury")]
    public float currentTemperature = 20f;
    public float maxTemperature = 1000f;
    public float coolingRate = 10f;
    public float forgingTemperature = 500f;

    [Header("Ustawienia Deformacji (Nowe!)")]
    public float deformRadius = 0.01f; // Jak szeroki jest m³ot
    public float deformForce = 0.05f;  // Jak mocno jedno uderzenie wgniata metal
    public float minThickness = 0.15f; // Maksymalna deformacja (¿eby nie zrobiæ z tego naleœnika)
    public float grindRadius = 0.30f;

    [Header("Ustawienia Szpikulca (Zaktualizowane)")]
    public float tipLength = 0.15f;      // Jak d³uga jest strefa czubka (Np. 15 centymetrów)
    public float grindSpeed = 0.05f;     // GLOBALNA szybkoœæ - ¿eby nie by³o "natychmiastowo"
    public float maxHalfWidth = 0.1f;    // Po³owa szerokoœci Twojej sztaby

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    private Vector3[] vertices;
    private MeshRenderer meshRenderer;
    private bool isInForge = false;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Klonujemy siatkê, ¿eby nie zepsuæ oryginalnego pliku na dysku!
        mesh = meshFilter.mesh;
        vertices = mesh.vertices;
    }

    void Update()
    {
        if (!isInForge && currentTemperature > 20f)
        {
            currentTemperature -= coolingRate * Time.deltaTime;
        }
        UpdateVisuals();
    }

    // Nowa funkcja HitMetal przyjmuje teraz DOK£ADNY punkt i k¹t uderzenia
    public void HitMetal(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (currentTemperature >= forgingTemperature)
        {
            Debug.Log("Kucie! Deformacja siatki...");
            DeformMesh(hitPoint, hitNormal);
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go kuæ!");
        }
    }

    // G£ÓWNA MATEMATYKA DEFORMACJI
    // G£ÓWNA MATEMATYKA ROZLEWANIA (Rozp³aszczanie na boki)
    // G£ÓWNA MATEMATYKA KUCIA (Sp³aszczanie i rozlewanie)
    // G£ÓWNA MATEMATYKA KUCIA (Kontrolowane wyd³u¿anie - zero bananów!)
    // G£ÓWNA MATEMATYKA KUCIA (Z zachowaniem masy / oporem materia³u)
    // G£ÓWNA MATEMATYKA KUCIA (Naprawa stoj¹cych œcian i symetrii!)
    private void DeformMesh(Vector3 hitPoint, Vector3 hitNormal)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);

        // 1. OBLICZAMY OPÓR DLA CA£EGO UDERZENIA (A nie dla ka¿dego wierzcho³ka osobno!)
        // Sprawdzamy, jak gruba jest sztabka w miejscu uderzenia m³ota.
        float currentThickness = Mathf.Abs(localHitPoint.y) * 2f;

        // Zabezpieczenie na wypadek uderzenia idealnie z boku
        if (currentThickness < 0.005f) currentThickness = minThickness + 0.05f;

        float resistanceFactor = Mathf.Clamp01((currentThickness - minThickness) / 0.02f);

        if (resistanceFactor <= 0.01f) return; // Jeœli uderzy³eœ w p³askie miejsce, nic siê nie dzieje

        bool wasDeformed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localHitPoint, vertices[i]);

            if (distance < deformRadius)
            {
                float baseForce = (deformRadius - distance) / deformRadius;
                float finalForce = baseForce * deformForce * resistanceFactor;

                // 2. BEZPIECZNE KIERUNKI (Naprawa wierzcho³ków uciekaj¹cych i stoj¹cych w miejscu)
                // Zastêpujemy felerne Mathf.Sign w³asn¹, bezpieczn¹ logik¹ (zwracaj¹c¹ 0 dla œrodka)
                float dirY = vertices[i].y > 0.001f ? 1f : (vertices[i].y < -0.001f ? -1f : 0f);
                float dirZ = vertices[i].z > 0.001f ? 1f : (vertices[i].z < -0.001f ? -1f : 0f);
                float dirX = vertices[i].x > 0.001f ? 1f : (vertices[i].x < -0.001f ? -1f : 0f);

                // SP£ASZCZANIE 
                float targetY = dirY * (minThickness / 2f);
                vertices[i].y = Mathf.Lerp(vertices[i].y, targetY, finalForce);

                // WYD£U¯ANIE I POSZERZANIE (Teraz ca³e œciany boczne id¹ równo!)
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
    // Zaktualizowana metoda GrindPerfectEdge - ostrzenie szpikulca jednostronnie i póŸniej z drugiej strony
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
                // 1. OSTRZENIE KRAWÊDZI BOKU (Wybieramy stronê za pomoc¹ obrotu isFlipped)
                if ((!isFlipped && vertices[i].x > 0.001f) || (isFlipped && vertices[i].x < -0.001f))
                {
                    float edgeFactor = Mathf.Abs(vertices[i].x) / maxHalfWidth;
                    edgeFactor = Mathf.Clamp01(edgeFactor);

                    // Œcinamy krawêdŸ do zera
                    vertices[i].y = Mathf.Lerp(vertices[i].y, 0.01f, edgeFactor * grindSpeed);
                    wasDeformed = true;
                }

                // 2. OSTRZENIE SZPIKULCA (Ca³kowita wolnoœæ kszta³tu)
                if (vertices[i].z > tipStartPoint)
                {
                    float tipFactor = (vertices[i].z - tipStartPoint) / tipLength;
                    tipFactor = Mathf.Clamp01(tipFactor);

                    // CELUJEMY W ŒRODEK (0). Dziêki temu boki nigdy nie zamieni¹ siê miejscami!
                    float targetWidth = Mathf.Lerp(maxHalfWidth, 0f, tipFactor);

                    if (!isFlipped && vertices[i].x > 0.001f) // Szlifujemy PRAW¥ krawêdŸ
                    {
                        // ZASADA SUBTRAKTYWNA: Œcinamy TYLKO wtedy, gdy metal wystaje.
                        // Dziêki temu raz zeszlifowany szpikulec "zastyga" i nie da siê go cofn¹æ!
                        if (vertices[i].x > targetWidth)
                        {
                            vertices[i].x = Mathf.Lerp(vertices[i].x, targetWidth, grindSpeed * 0.1f);
                            wasDeformed = true;
                        }
                    }
                    else if (isFlipped && vertices[i].x < -0.001f) // Szlifujemy LEW¥ krawêdŸ
                    {
                        // To samo dla lewej strony (-targetWidth)
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

    /// OSTATECZNE SZLIFOWANIE: Precyzyjne ³apanie tylko najbli¿szych wierzcho³ków
    public void SharpenEdge(Vector3 hitPoint)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        bool wasDeformed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localHitPoint, vertices[i]);

            // U¿ywamy dedykowanego, ma³ego promienia z Inspektora!
            if (distance < grindRadius)
            {
                float force = (grindRadius - distance) / grindRadius;

                // Œcinamy krawêdŸ. Zwiêkszy³em mno¿nik (0.2f), ¿eby dzia³a³o szybciej na ma³ym obszarze.
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
            if (currentTemperature < maxTemperature) currentTemperature += 50f * Time.deltaTime;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Forge")) isInForge = false;
    }

    void UpdateVisuals()
    {
        float tempNormalized = (currentTemperature - 20f) / (maxTemperature - 20f);
        meshRenderer.material.color = Color.Lerp(Color.gray, new Color(1f, 0.4f, 0f), tempNormalized);
    }
}