using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class IronPiece : MonoBehaviour, IInteractable, IPickable
{
    [Header("Ustawienia Temperatury")]
    public float currentTemperature = 20f;
    public float maxTemperature = 1000f;
    public float coolingRate = 10f;
    public float forgingTemperature = 500f;

    [Header("Ustawienia Deformacji (Nowe!)")]
    public float deformRadius = 0.01f; // Jak szeroki jest m�ot
    public float deformForce = 0.05f;  // Jak mocno jedno uderzenie wgniata metal
    public float minThickness = 0.15f; // Maksymalna deformacja (�eby nie zrobi� z tego nale�nika)
    public float grindRadius = 0.30f;

    [Header("Ustawienia Szpikulca (Zaktualizowane)")]
    public float tipLength = 0.15f;      // Jak d�uga jest strefa czubka (Np. 15 centymetr�w)
    public float grindSpeed = 0.05f;     // GLOBALNA szybko�� - �eby nie by�o "natychmiastowo"
    public float maxHalfWidth = 0.1f;    // Po�owa szeroko�ci Twojej sztaby

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
        meshCollider.convex = true;

        // Klonujemy siatk�, �eby nie zepsu� oryginalnego pliku na dysku!
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

    public bool Interact(KeyCode key)
    {
        // Interact bez punktu uderzenia — zwraca true jeśli gorący (blokuje pickup)
        return currentTemperature >= forgingTemperature;
    }

    public void OnPickUp()
    {
        isInForge = false;
    }

    public void OnDrop()
    {
        // możesz tu dodać logikę np. efekt iskier przy upuszczeniu
    }

    // Nowa funkcja HitMetal przyjmuje teraz DOK�ADNY punkt i k�t uderzenia
    public void HitMetal(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (currentTemperature >= forgingTemperature)
        {
            Debug.Log("Kucie! Deformacja siatki...");
            DeformMesh(hitPoint, hitNormal);
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go ku�!");
        }
    }

    // G��WNA MATEMATYKA DEFORMACJI
    // G��WNA MATEMATYKA ROZLEWANIA (Rozp�aszczanie na boki)
    // G��WNA MATEMATYKA KUCIA (Sp�aszczanie i rozlewanie)
    // G��WNA MATEMATYKA KUCIA (Kontrolowane wyd�u�anie - zero banan�w!)
    // G��WNA MATEMATYKA KUCIA (Z zachowaniem masy / oporem materia�u)
    // G��WNA MATEMATYKA KUCIA (Naprawa stoj�cych �cian i symetrii!)
    private void DeformMesh(Vector3 hitPoint, Vector3 hitNormal)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);

        // 1. OBLICZAMY OP�R DLA CA�EGO UDERZENIA (A nie dla ka�dego wierzcho�ka osobno!)
        // Sprawdzamy, jak gruba jest sztabka w miejscu uderzenia m�ota.
        float currentThickness = Mathf.Abs(localHitPoint.y) * 2f;

        // Zabezpieczenie na wypadek uderzenia idealnie z boku
        if (currentThickness < 0.005f) currentThickness = minThickness + 0.05f;

        float resistanceFactor = Mathf.Clamp01((currentThickness - minThickness) / 0.02f);

        if (resistanceFactor <= 0.01f) return; // Je�li uderzy�e� w p�askie miejsce, nic si� nie dzieje

        bool wasDeformed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localHitPoint, vertices[i]);

            if (distance < deformRadius)
            {
                float baseForce = (deformRadius - distance) / deformRadius;
                float finalForce = baseForce * deformForce * resistanceFactor;

                // 2. BEZPIECZNE KIERUNKI (Naprawa wierzcho�k�w uciekaj�cych i stoj�cych w miejscu)
                // Zast�pujemy felerne Mathf.Sign w�asn�, bezpieczn� logik� (zwracaj�c� 0 dla �rodka)
                float dirY = vertices[i].y > 0.001f ? 1f : (vertices[i].y < -0.001f ? -1f : 0f);
                float dirZ = vertices[i].z > 0.001f ? 1f : (vertices[i].z < -0.001f ? -1f : 0f);
                float dirX = vertices[i].x > 0.001f ? 1f : (vertices[i].x < -0.001f ? -1f : 0f);

                // SP�ASZCZANIE 
                float targetY = dirY * (minThickness / 2f);
                vertices[i].y = Mathf.Lerp(vertices[i].y, targetY, finalForce);

                // WYD�U�ANIE I POSZERZANIE (Teraz ca�e �ciany boczne id� r�wno!)
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
    // Zaktualizowana metoda GrindPerfectEdge - ostrzenie szpikulca jednostronnie i p�niej z drugiej strony
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
                // 1. OSTRZENIE KRAW�DZI BOKU (Wybieramy stron� za pomoc� obrotu isFlipped)
                if ((!isFlipped && vertices[i].x > 0.001f) || (isFlipped && vertices[i].x < -0.001f))
                {
                    float edgeFactor = Mathf.Abs(vertices[i].x) / maxHalfWidth;
                    edgeFactor = Mathf.Clamp01(edgeFactor);

                    // �cinamy kraw�d� do zera
                    vertices[i].y = Mathf.Lerp(vertices[i].y, 0.01f, edgeFactor * grindSpeed);
                    wasDeformed = true;
                }

                // 2. OSTRZENIE SZPIKULCA (Ca�kowita wolno�� kszta�tu)
                if (vertices[i].z > tipStartPoint)
                {
                    float tipFactor = (vertices[i].z - tipStartPoint) / tipLength;
                    tipFactor = Mathf.Clamp01(tipFactor);

                    // CELUJEMY W �RODEK (0). Dzi�ki temu boki nigdy nie zamieni� si� miejscami!
                    float targetWidth = Mathf.Lerp(maxHalfWidth, 0f, tipFactor);

                    if (!isFlipped && vertices[i].x > 0.001f) // Szlifujemy PRAW� kraw�d�
                    {
                        // ZASADA SUBTRAKTYWNA: �cinamy TYLKO wtedy, gdy metal wystaje.
                        // Dzi�ki temu raz zeszlifowany szpikulec "zastyga" i nie da si� go cofn��!
                        if (vertices[i].x > targetWidth)
                        {
                            vertices[i].x = Mathf.Lerp(vertices[i].x, targetWidth, grindSpeed * 0.1f);
                            wasDeformed = true;
                        }
                    }
                    else if (isFlipped && vertices[i].x < -0.001f) // Szlifujemy LEW� kraw�d�
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

    /// OSTATECZNE SZLIFOWANIE: Precyzyjne �apanie tylko najbli�szych wierzcho�k�w
    public void SharpenEdge(Vector3 hitPoint)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        bool wasDeformed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localHitPoint, vertices[i]);

            // U�ywamy dedykowanego, ma�ego promienia z Inspektora!
            if (distance < grindRadius)
            {
                float force = (grindRadius - distance) / grindRadius;

                // �cinamy kraw�d�. Zwi�kszy�em mno�nik (0.2f), �eby dzia�a�o szybciej na ma�ym obszarze.
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