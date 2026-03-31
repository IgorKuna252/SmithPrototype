using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum MetalType { Copper, Bronze, Iron, Steel, Gold, Platinum, BlueSteel, Vibranium }
public enum HitType { Lengthen, Widen }

// --- EWOLUCJA: 6-punktowy profil (Niezależne krawędzie i środek!) ---
[System.Serializable]
public class MetalProfile
{
    public float z;               // Pozycja na długości
    public float leftX;           // Lewa krawędź (ujemna)
    public float rightX;          // Prawa krawędź (dodatnia)

    public float centerHalfHeight; // Grubość na samym środku miecza (Rdzeń)
    public float leftHalfHeight;   // Grubość lewej krawędzi (Do ostrzenia!)
    public float rightHalfHeight;  // Grubość prawej krawędzi (Do ostrzenia!)
}

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
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

    [Header("Ustawienia Wstążki (Wymiary)")]
    public int initialSegments = 40;
    public float startLength = 1.0f;
    public float startWidth = 0.12f;      // Poszerzona sztaba!
    public float startThickness = 0.04f;  // Pogrubiona sztaba!

    [Header("Narzędzia")]
    public float minThickness = 0.005f;
    public float grindSpeed = 0.05f;
    public float sharpenMultiplier = 20f;
    public float eatMultiplier = 0.8f;

    [Header("Młotek (Tuning)")]
    public float hammerRadius = 0.15f;   // Promień rażenia młotka
    public float squishSpeed = 0.004f;   // ZMniejszone z 0.015f (wolniejsze spłaszczanie)
    public float spreadSpeed = 0.006f;   // Jak mocno rozlewa na boki/długość

    [SerializeField]
    public List<MetalProfile> metalSpine = new List<MetalProfile>();

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;
    private bool isInForge = false;
    private Color baseColdColor;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshRenderer != null) meshRenderer.SetPropertyBlock(null);

        SetBaseColor();
        InitializeSpine();
        BuildMeshFromSpine();
    }

    void Update()
    {
        if (!isInForge && currentTemperature > 20f) currentTemperature -= coolingRate * Time.deltaTime;
        UpdateVisuals();
    }

    // =================================================================
    // GENEROWANIE DANYCH I SIATKI (TKACZ)
    // =================================================================

    private void InitializeSpine()
    {
        metalSpine.Clear();
        float startZ = -startLength / 2f;
        float segmentLength = startLength / initialSegments;

        for (int i = 0; i <= initialSegments; i++)
        {
            metalSpine.Add(new MetalProfile
            {
                z = startZ + (i * segmentLength),
                leftX = -startWidth / 2f,
                rightX = startWidth / 2f,
                centerHalfHeight = startThickness / 2f,
                leftHalfHeight = startThickness / 2f,
                rightHalfHeight = startThickness / 2f
            });
        }
    }

    public void BuildMeshFromSpine()
    {
        if (metalSpine.Count < 2) return;

        Mesh mesh = new Mesh();
        mesh.name = "RibbonSwordMesh";

        int segments = metalSpine.Count - 1;
        // Mamy teraz 6 wierzchołków na profil! (Lewy, Środek, Prawy) * (Góra, Dół)
        Vector3[] vertices = new Vector3[(segments + 1) * 6];
        int[] triangles = new int[segments * 36 + 24]; // Złożona topologia dla ostrza

        int v = 0;
        int t = 0;

        // 1. Ustawiamy wierzchołki
        for (int i = 0; i < metalSpine.Count; i++)
        {
            MetalProfile p = metalSpine[i];

            // Górna połowa (Y na plusie)
            vertices[v++] = new Vector3(p.leftX, p.leftHalfHeight, p.z);    // 0: Lewy Górny
            vertices[v++] = new Vector3(0f, p.centerHalfHeight, p.z);       // 1: Środek Górny (Rdzeń)
            vertices[v++] = new Vector3(p.rightX, p.rightHalfHeight, p.z);  // 2: Prawy Górny

            // Dolna połowa (Y na minusie)
            vertices[v++] = new Vector3(p.leftX, -p.leftHalfHeight, p.z);   // 3: Lewy Dolny
            vertices[v++] = new Vector3(0f, -p.centerHalfHeight, p.z);      // 4: Środek Dolny
            vertices[v++] = new Vector3(p.rightX, -p.rightHalfHeight, p.z); // 5: Prawy Dolny
        }

        // 2. Łączymy w trójkąty
        for (int i = 0; i < segments; i++)
        {
            int c = i * 6;       // Bieżący profil
            int n = (i + 1) * 6; // Następny profil

            // Ściana Górna Lewa
            triangles[t++] = c; triangles[t++] = n; triangles[t++] = c + 1;
            triangles[t++] = c + 1; triangles[t++] = n; triangles[t++] = n + 1;
            // Ściana Górna Prawa
            triangles[t++] = c + 1; triangles[t++] = n + 1; triangles[t++] = c + 2;
            triangles[t++] = c + 2; triangles[t++] = n + 1; triangles[t++] = n + 2;
            // Ściana Dolna Lewa
            triangles[t++] = c + 3; triangles[t++] = c + 4; triangles[t++] = n + 3;
            triangles[t++] = c + 4; triangles[t++] = n + 4; triangles[t++] = n + 3;
            // Ściana Dolna Prawa
            triangles[t++] = c + 4; triangles[t++] = c + 5; triangles[t++] = n + 4;
            triangles[t++] = c + 5; triangles[t++] = n + 5; triangles[t++] = n + 4;
            // Bok Lewy (Zamyka krawędź jeśli nie jest w pełni ostra)
            triangles[t++] = c; triangles[t++] = c + 3; triangles[t++] = n;
            triangles[t++] = c + 3; triangles[t++] = n + 3; triangles[t++] = n;
            // Bok Prawy
            triangles[t++] = c + 2; triangles[t++] = n + 2; triangles[t++] = c + 5;
            triangles[t++] = c + 5; triangles[t++] = n + 2; triangles[t++] = n + 5;
        }

        // Zaślepka Tył (Z-min)
        triangles[t++] = 0; triangles[t++] = 1; triangles[t++] = 4;
        triangles[t++] = 4; triangles[t++] = 3; triangles[t++] = 0;
        triangles[t++] = 1; triangles[t++] = 2; triangles[t++] = 5;
        triangles[t++] = 5; triangles[t++] = 4; triangles[t++] = 1;

        // Zaślepka Przód (Z-max)
        int L = segments * 6;
        triangles[t++] = L + 1; triangles[t++] = L + 0; triangles[t++] = L + 3;
        triangles[t++] = L + 3; triangles[t++] = L + 4; triangles[t++] = L + 1;
        triangles[t++] = L + 2; triangles[t++] = L + 1; triangles[t++] = L + 4;
        triangles[t++] = L + 4; triangles[t++] = L + 5; triangles[t++] = L + 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    // =================================================================
    // INTERAKCJE (Młotek i Szlifierka)
    // =================================================================

    public bool HitMetal(Vector3 hitPoint, Vector3 hitNormal, HitType hitType = HitType.Lengthen)
    {
        if (currentTemperature < forgingTemperature) return false;

        Vector3 localHit = transform.InverseTransformPoint(hitPoint);
        float localZ = localHit.z;
        float localX = localHit.x;

        if (metalSpine.Count > 0)
        {
            float minZ = metalSpine[0].z;
            float maxZ = metalSpine[metalSpine.Count - 1].z;
            localZ = Mathf.Clamp(localZ, minZ, maxZ);
        }

        bool wasDeformed = false;

        // TUNING SIŁY (Możesz to też wystawić do Inspektora)
        float powerSquish = 0.007f; // ZMNIEJSZONE: Metal wolniej się spłaszcza (ok. 20-30 uderzeń do min)
        float powerStretch = 0.04f; // ZWIĘKSZONE: Metal mocniej ucieka na długość/szerokość

        for (int i = 0; i < metalSpine.Count; i++)
        {
            float distZ = Mathf.Abs(metalSpine[i].z - localZ);
            float profileCenterX = (metalSpine[i].leftX + metalSpine[i].rightX) / 2f;
            float distX = Mathf.Abs(profileCenterX - localX);
            float trueDistance = Mathf.Sqrt(distZ * distZ + distX * distX);

            if (trueDistance < hammerRadius)
            {
                // Obliczamy opór: im cieńszy metal, tym trudniej go dalej zgnieść
                float currentThickness = metalSpine[i].centerHalfHeight * 2f;
                float resistance = Mathf.Clamp01((currentThickness - minThickness) / 0.02f);

                if (resistance > 0.01f)
                {
                    float force = (1f - (trueDistance / hammerRadius)) * resistance;
                    float targetY = minThickness / 2f;

                    // 1. SPŁASZCZANIE (Bardzo subtelne)
                    metalSpine[i].centerHalfHeight = Mathf.MoveTowards(metalSpine[i].centerHalfHeight, targetY, powerSquish * force);
                    metalSpine[i].leftHalfHeight = Mathf.MoveTowards(metalSpine[i].leftHalfHeight, targetY, powerSquish * force);
                    metalSpine[i].rightHalfHeight = Mathf.MoveTowards(metalSpine[i].rightHalfHeight, targetY, powerSquish * force);

                    // 2. WYDŁUŻANIE / POSZERZANIE (Agresywne)
                    if (hitType == HitType.Widen)
                    {
                        metalSpine[i].leftX -= powerStretch * force;
                        metalSpine[i].rightX += powerStretch * force;
                    }
                    else if (hitType == HitType.Lengthen)
                    {
                        float pushDirection = Mathf.Sign(metalSpine[i].z - localZ);
                        if (pushDirection == 0) pushDirection = (i > metalSpine.Count / 2) ? 1f : -1f;

                        // Przesuwamy kręgi znacznie mocniej
                        metalSpine[i].z += pushDirection * powerStretch * force;
                    }
                    wasDeformed = true;
                }
            }
        }

        if (wasDeformed)
        {
            metalSpine = metalSpine.OrderBy(p => p.z).ToList();
            SubdivideSpine();
            BuildMeshFromSpine();
        }
        return wasDeformed;
    }

    // NOWOŚĆ: Automatyczne dodawanie nowych kręgów po rozciągnięciu
    private void SubdivideSpine()
    {
        float maxSegmentLength = (startLength / initialSegments) * 1.5f;

        for (int i = 0; i < metalSpine.Count - 1; i++)
        {
            if (Mathf.Abs(metalSpine[i + 1].z - metalSpine[i].z) > maxSegmentLength)
            {
                MetalProfile p1 = metalSpine[i];
                MetalProfile p2 = metalSpine[i + 1];

                // Wstawiamy nowy idealnie pośrodku
                MetalProfile mid = new MetalProfile
                {
                    z = (p1.z + p2.z) / 2f,
                    leftX = (p1.leftX + p2.leftX) / 2f,
                    rightX = (p1.rightX + p2.rightX) / 2f,
                    centerHalfHeight = (p1.centerHalfHeight + p2.centerHalfHeight) / 2f,
                    leftHalfHeight = (p1.leftHalfHeight + p2.leftHalfHeight) / 2f,
                    rightHalfHeight = (p1.rightHalfHeight + p2.rightHalfHeight) / 2f
                };

                metalSpine.Insert(i + 1, mid);
                i++; // Przeskakujemy go, żeby nie wpaść w nieskończoną pętlę!
            }
        }
    }

    public void GrindPerfectEdge(float localZPosition, bool isFlipped)
    {
        float coreWidth = 0.02f;
        float falloffRadius = 0.015f;
        bool wasDeformed = false;

        float baseSharpenSpeed = grindSpeed * sharpenMultiplier * 0.01f * Time.deltaTime;
        float baseEatSpeed = grindSpeed * eatMultiplier * Time.deltaTime;

        for (int i = 0; i < metalSpine.Count; i++)
        {
            float distance = Mathf.Abs(metalSpine[i].z - localZPosition);

            if (distance < falloffRadius)
            {
                float forceMultiplier = distance < coreWidth ? 1f : 1f - ((distance - coreWidth) / (falloffRadius - coreWidth));
                float curSharpenSpeed = baseSharpenSpeed * forceMultiplier;
                float curEatSpeed = baseEatSpeed * forceMultiplier;

                if (!isFlipped) // PRAWA KRAWĘDŹ
                {
                    // 1. Najpierw OSTRZENIE (Ścina do płaskiego zera na brzegu, robi trójkąt!)
                    if (metalSpine[i].rightHalfHeight > 0.001f)
                    {
                        metalSpine[i].rightHalfHeight = Mathf.MoveTowards(metalSpine[i].rightHalfHeight, 0f, curSharpenSpeed);
                        wasDeformed = true;
                    }
                    // 2. Potem WŻERANIE (Kiedy jest już ostre jak brzytwa)
                    else if (metalSpine[i].rightX > 0f)
                    {
                        metalSpine[i].rightX = Mathf.MoveTowards(metalSpine[i].rightX, 0f, curEatSpeed);
                        wasDeformed = true;
                    }
                }
                else // LEWA KRAWĘDŹ
                {
                    // 1. Najpierw OSTRZENIE
                    if (metalSpine[i].leftHalfHeight > 0.001f)
                    {
                        metalSpine[i].leftHalfHeight = Mathf.MoveTowards(metalSpine[i].leftHalfHeight, 0f, curSharpenSpeed);
                        wasDeformed = true;
                    }
                    // 2. Potem WŻERANIE
                    else if (metalSpine[i].leftX < 0f)
                    {
                        metalSpine[i].leftX = Mathf.MoveTowards(metalSpine[i].leftX, 0f, curEatSpeed);
                        wasDeformed = true;
                    }
                }
            }
        }

        // --- DETEKCJA ODCIĘCIA (Jeśli krawędzie się spotkały) ---
        if (wasDeformed)
        {
            for (int i = 1; i < metalSpine.Count - 1; i++)
            {
                if (Mathf.Abs(metalSpine[i].z - localZPosition) < coreWidth)
                {
                    // Jeśli Prawy styk spotkał się z Lewym, miecz przerwany!
                    if (metalSpine[i].rightX <= metalSpine[i].leftX + 0.005f)
                    {
                        Debug.Log("<color=red>AMPUTACJA! Miecz przecięty!</color>");
                        metalSpine.RemoveRange(i, metalSpine.Count - i);
                        break;
                    }
                }
            }
            BuildMeshFromSpine();
        }
    }

    public float GetEdgeWidthAt(float localZPosition, bool isFlipped)
    {
        float closestDist = float.MaxValue;
        float edgeDistance = 0.05f;

        foreach (var profile in metalSpine)
        {
            float dist = Mathf.Abs(profile.z - localZPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                // Jeśli niezwrócony, szlifierka czyta prawy X. Jeśli zwrócony, czyta lewy X (jako wartość dodatnią do kamienia)
                edgeDistance = !isFlipped ? profile.rightX : Mathf.Abs(profile.leftX);
            }
        }
        return edgeDistance * transform.localScale.x;
    }

    // =================================================================
    // RESZTA FUNKCJI
    // =================================================================

    public bool Interact() => currentTemperature >= forgingTemperature;
    public void OnPickUp() => isInForge = false;
    public void OnDrop() { }
    public void ForceCoolDown() { currentTemperature = 20f; isInForge = false; UpdateVisuals(); }

    void SetBaseColor()
    {
        switch (metalTier)
        {
            case MetalType.Copper: baseColdColor = new Color(0.8f, 0.4f, 0.2f); break;
            case MetalType.Iron: baseColdColor = new Color(0.5f, 0.5f, 0.5f); break;
            default: baseColdColor = Color.gray; break;
        }
    }

    void UpdateVisuals()
    {
        if (meshRenderer == null) return;
        float tempNormalized = Mathf.Clamp01((currentTemperature - 20f) / (maxTemperature - 20f));
        Color hotColor = new Color(0.8f, 0.25f, 0f);
        Color currentColor = Color.Lerp(baseColdColor, hotColor, tempNormalized);

        Material mat = meshRenderer.material;
        mat.color = currentColor;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", currentColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", currentColor * (1f + tempNormalized * 4f));
        }
    }
}