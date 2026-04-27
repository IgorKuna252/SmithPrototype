using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum MetalType { Copper, Bronze, Iron, Steel, Gold, Platinum, BlueSteel, Vibranium }
public enum HitType { Lengthen, Widen }

// 6punktowy profil
[System.Serializable]
public class MetalProfile
{
    public float z;
    public float leftX;
    public float rightX;

    public float centerHalfHeight;
    public float leftHalfHeight;
    public float rightHalfHeight;

    //pauza dla lepszego szlifowania
    public float leftEdgeIntegrity;
    public float rightEdgeIntegrity;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
public class MetalPiece : MonoBehaviour, IInteractable, IPickable
{
    public enum MetalPartType { Ingot, SwordMold, AxeMold }
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
    public float grindSpeed = 0.09f;
    public float sharpenMultiplier = 20f;
    public float eatMultiplier = 0.03f;
    public float safetyPauseSeconds = 1.5f;

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

    void Awake()
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

        float length = startLength;
        float width = startWidth;
        float thickness = startThickness;
        int segments = initialSegments;

        // --- DEFINIUJEMY WŁAŚCIWOŚCI STARTOWE BAZUJĄC NA FORMIE (MOLD) ---
        switch (partType)
        {
            case MetalPartType.Ingot:
                // Zwykła Sztabka - gruby, krótki i zwarty blok (bardzo bazowy)
                length = 0.35f;
                width = 0.1f;
                thickness = 0.08f;
                segments = 15;
                break;

            case MetalPartType.SwordMold:
                // Foremka miecza - długa wstążka
                length = 1.1f;    // Odrobinkę dłuższa sztaba ogólna
                width = 0.15f;    // Szerszy miecz!
                thickness = 0.065f; // Wyraźnie pogrubiony
                segments = 40;
                break;

            case MetalPartType.AxeMold:
                // Foremka siekiery - krótka, ale od razu wyprofilowana w szerszy, asymetryczny blok (w X)
                length = 0.35f;
                width = 0.25f;  // Dużo szersza, by było miejsce na zakucie ostrza
                thickness = 0.06f;
                segments = 15;
                break;
        }

        float startZ = -length / 2f;
        float segmentLength = length / segments;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments; // Postęp długości od 0.0 (dół) do 1.0 (góra formy)

            float currentZ = startZ + (i * segmentLength);
            float leftX = -width / 2f;
            float rightX = width / 2f;
            float halfThickness = thickness / 2f;

            // --- PROCEDURALNE KSZTAŁTOWANIE BRYŁ ODLANYCH ---
            if (partType == MetalPartType.SwordMold)
            {
                // Miecz: Szpic zaczyna się o wiele wcześniej, tworząc znacznie dłuższe ostrze tnące
                if (t > 0.70f) // Z 85% zeszło na 70% (ostatnie 30% długości zwęży się płynnie w szpic)
                {
                    float taper = 1f - ((t - 0.70f) / 0.30f); // Wartość schodzi od 1.0 do 0.0
                    leftX *= taper;     
                    rightX *= taper;    
                    halfThickness *= (0.15f + 0.85f * taper); 
                }
            }
            else if (partType == MetalPartType.AxeMold)
            {
                // Siekiera: Obrócona na drugą stronę
                // Teraz twardy ucinany obuch jest pod prawą dłonią (płaski)
                rightX = 0.05f; 
                
                // A brzuch siekiery celuje potężnie na lewo
                float bowCurve = Mathf.Sin(t * Mathf.PI); 
                leftX = -0.03f - (0.22f * bowCurve);
                
                // Modyfikacja grubości na podobnych zasadach
                halfThickness = (thickness / 2f) * (0.6f + 0.4f * bowCurve);
            }

            metalSpine.Add(new MetalProfile
            {
                z = currentZ,
                leftX = leftX,
                rightX = rightX,
                centerHalfHeight = halfThickness,
                leftHalfHeight = halfThickness,
                rightHalfHeight = halfThickness,
                leftEdgeIntegrity = 1f,
                rightEdgeIntegrity = 1f
            });
        }
    }

    // Dodana funkcja dla Twojego 'Mold Table':
    public void SetMoldAndRebuild(MetalPartType newMoldType)
    {
        partType = newMoldType;
        InitializeSpine();
        BuildMeshFromSpine();
        SetBaseColor(); // Ustawiamy od nowa materiał na bazie metalTier
        UpdateVisuals(); // Przerysowujemy ze zaktualizowanym kolorem bazowym i aktualną temperaturą
        
        // Zależnie jak robisz chłodzenie, być może warto przywrócić mu temperaturę przy wyjmowaniu z formy?
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
        
        // NOWOŚĆ: Wymuszenie twardych krawędzi (Flat Shading) PRZED obliczaniem odbić
        FlatShading(mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    // Dodana funkcja, która rozbija połączoną siatkę uwalniając cienie spod wygładzania Unity
    private void FlatShading(Mesh targetMesh)
    {
        Vector3[] oldVerts = targetMesh.vertices;
        int[] triangles = targetMesh.triangles;
        Vector3[] newVerts = new Vector3[triangles.Length];
        
        for (int i = 0; i < triangles.Length; i++)
        {
            newVerts[i] = oldVerts[triangles[i]];
            // Przypisanie unikalnego wierzchołka dla KAŻDEGO rogu KAŻDEGO trójkąta!
            triangles[i] = i; 
        }
        
        targetMesh.vertices = newVerts;
        targetMesh.triangles = triangles;
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

        // UWAGA: Usunąłem tutaj 'Mathf.Clamp'! 
        // Teraz młotek uderza DOKŁADNIE tam gdzie myszka. Nie ma magnesowania do krawędzi.

        bool wasDeformed = false;

        float powerSquish = 0.005f;
        float powerWiden = 0.015f;
        float powerLengthen = 0.035f;

        float swordCenterZ = 0f;
        if (metalSpine.Count > 0)
        {
            swordCenterZ = (metalSpine[0].z + metalSpine[metalSpine.Count - 1].z) / 2f;
        }

        for (int i = 0; i < metalSpine.Count; i++)
        {
            // Prawdziwa odległość od faktycznego kliknięcia myszką
            float distZ = Mathf.Abs(metalSpine[i].z - localZ);
            float profileCenterX = (metalSpine[i].leftX + metalSpine[i].rightX) / 2f;
            float distX = Mathf.Abs(profileCenterX - localX);
            float trueDistance = Mathf.Sqrt(distZ * distZ + distX * distX);

            // Jeśli uderzyłeś w powietrze (np. za sztabą), trueDistance będzie większe niż hammerRadius
            // i ten if po prostu zignoruje uderzenie!
            if (trueDistance < hammerRadius)
            {
                float force = 1f - (trueDistance / hammerRadius);
                float targetY = minThickness / 2f;

                // 1. SPŁASZCZANIE W DÓŁ
                if (metalSpine[i].centerHalfHeight > targetY)
                {
                    metalSpine[i].centerHalfHeight = Mathf.MoveTowards(metalSpine[i].centerHalfHeight, targetY, powerSquish * force);
                    metalSpine[i].leftHalfHeight = Mathf.MoveTowards(metalSpine[i].leftHalfHeight, targetY, powerSquish * force);
                    metalSpine[i].rightHalfHeight = Mathf.MoveTowards(metalSpine[i].rightHalfHeight, targetY, powerSquish * force);
                    wasDeformed = true;
                }

                // 2. ASYMETRYCZNE ROZBIJANIE NA BOKI
                if (hitType == HitType.Widen)
                {
                    if (localX < profileCenterX)
                    {
                        metalSpine[i].leftX -= powerWiden * force;
                        metalSpine[i].rightX += powerWiden * force * 0.2f;
                    }
                    else
                    {
                        metalSpine[i].rightX += powerWiden * force;
                        metalSpine[i].leftX -= powerWiden * force * 0.2f;
                    }
                    wasDeformed = true;
                }
                // 3. WYDŁUŻANIE
                else if (hitType == HitType.Lengthen)
                {
                    float pushDirection = (metalSpine[i].z >= swordCenterZ) ? 1f : -1f;

                    metalSpine[i].z += pushDirection * powerLengthen * force;
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
                    rightHalfHeight = (p1.rightHalfHeight + p2.rightHalfHeight) / 2f,

                    // --- POPRAWKA BŁĘDU: Uśredniamy barierę sąsiadów, zamiast zostawiać 0! ---
                    leftEdgeIntegrity = (p1.leftEdgeIntegrity + p2.leftEdgeIntegrity) / 2f,
                    rightEdgeIntegrity = (p1.rightEdgeIntegrity + p2.rightEdgeIntegrity) / 2f
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

        // NOWOŚĆ: Szybkość zjadania pauzy. 1f oznacza około 1 sekundę piłowania w jednym miejscu
        // zanim kamień zacznie wżerać się w szerokość!
        float integrityDrainSpeed = (1f / safetyPauseSeconds) * Time.deltaTime;

        for (int i = 0; i < metalSpine.Count; i++)
        {
            float distance = Mathf.Abs(metalSpine[i].z - localZPosition);

            if (distance < falloffRadius)
            {
                float forceMultiplier = distance < coreWidth ? 1f : 1f - ((distance - coreWidth) / (falloffRadius - coreWidth));
                float curSharpenSpeed = baseSharpenSpeed * forceMultiplier;
                float curEatSpeed = baseEatSpeed * forceMultiplier;
                float curIntegrityDrain = integrityDrainSpeed * forceMultiplier;

                if (!isFlipped) // PRAWA KRAWĘDŹ
                {
                    // ETAP 1: OSTRZENIE (Ścina góra-dół)
                    if (metalSpine[i].rightHalfHeight > 0.001f)
                    {
                        metalSpine[i].rightHalfHeight = Mathf.MoveTowards(metalSpine[i].rightHalfHeight, 0f, curSharpenSpeed);
                        wasDeformed = true;
                    }
                    // ETAP 2: PAUZA BEZPIECZEŃSTWA (Zbiera niewidzialne punkty, wizualnie nic się nie psuje!)
                    else if (metalSpine[i].rightEdgeIntegrity > 0f)
                    {
                        metalSpine[i].rightEdgeIntegrity -= curIntegrityDrain;
                        wasDeformed = true; // Zmieniamy dane, więc odświeżamy siatkę
                    }
                    // ETAP 3: WŻERANIE (Bariera pękła, kamień zjada szerokość miecza - idealne do ząbków)
                    else if (metalSpine[i].rightX > 0f)
                    {
                        metalSpine[i].rightX = Mathf.MoveTowards(metalSpine[i].rightX, 0f, curEatSpeed);
                        wasDeformed = true;
                    }
                }
                else // LEWA KRAWĘDŹ
                {
                    // ETAP 1: OSTRZENIE
                    if (metalSpine[i].leftHalfHeight > 0.001f)
                    {
                        metalSpine[i].leftHalfHeight = Mathf.MoveTowards(metalSpine[i].leftHalfHeight, 0f, curSharpenSpeed);
                        wasDeformed = true;
                    }
                    // ETAP 2: PAUZA BEZPIECZEŃSTWA
                    else if (metalSpine[i].leftEdgeIntegrity > 0f)
                    {
                        metalSpine[i].leftEdgeIntegrity -= curIntegrityDrain;
                        wasDeformed = true;
                    }
                    // ETAP 3: WŻERANIE
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

    public static Color GetMetalColor(MetalType type)
    {
        switch (type)
        {
            case MetalType.Copper:    return new Color(0.8f, 0.4f, 0.2f);
            case MetalType.Bronze:    return new Color(0.7f, 0.5f, 0.1f);
            case MetalType.Iron:      return new Color(0.15f, 0.15f, 0.15f);
            case MetalType.Steel:     return new Color(0.35f, 0.35f, 0.4f);
            case MetalType.Gold:      return new Color(1.0f, 0.8f, 0.0f);
            case MetalType.Platinum:  return new Color(0.9f, 0.9f, 0.95f);
            case MetalType.BlueSteel: return new Color(0.2f, 0.3f, 0.5f);
            case MetalType.Vibranium: return new Color(0.5f, 0.2f, 0.8f);
            default:                  return new Color(0.15f, 0.15f, 0.15f);
        }
    }

    public void SetBaseColor()
    {
        baseColdColor = GetMetalColor(metalTier);

        if (meshRenderer != null)
        {
            // CAŁKOWICIE ODZINAMY SIĘ OD ORYGINALNEGO KOLORU (CZERWONEGO) PREFABU:
            // Szukamy standardowego shadera, by zapewnić poprawne działanie odbić i emisji.
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            
            if (litShader != null)
            {
                Material freshMetallicMat = new Material(litShader);
                
                // Ustawiamy właściwości twardego materiału szarości i przypinamy na stałe do obiektu
                if (freshMetallicMat.HasProperty("_Color")) freshMetallicMat.SetColor("_Color", baseColdColor);
                if (freshMetallicMat.HasProperty("_BaseColor")) freshMetallicMat.SetColor("_BaseColor", baseColdColor);
                
                meshRenderer.material = freshMetallicMat;
            }
        }
    }


    private void UpdateVisuals()
    {
        if (meshRenderer == null || meshRenderer.material == null) return;
        
        float t = Mathf.Clamp01((currentTemperature - 20f) / (maxTemperature - 20f));
        
        // Zgodnie z oryginałem - oryginalny gorący i stara matematyka
        Color hotColor = new Color(0.8f, 0.25f, 0f);
        Color targetColor = Color.Lerp(baseColdColor, hotColor, t);

        Material mat = meshRenderer.material;
        
        mat.color = targetColor; // Dopiska z oryginału dla niektórych starych shaderów
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", targetColor);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", targetColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            // Znaczące osłabienie emisji: t=0 daje czystą czerń (brak świecenia w cieniu), a t=1 daje łagodne x1.5 podbicie zamiast starego x5.
            mat.SetColor("_EmissionColor", targetColor * (t * 1.5f));
        }
    }

    // reset bariery
    public void ResetEdgeIntegrity()
    {
        for (int i = 0; i < metalSpine.Count; i++)
        {
            metalSpine[i].leftEdgeIntegrity = 1f;
            metalSpine[i].rightEdgeIntegrity = 1f;
        }
    }

    // NOWOŚĆ: Ukośny Pilnik (Zachowuje i wygładza skosy/szpice!)
    public void SmoothPerfectEdge(float localZPosition, bool isFlipped)
    {
        float fileRadius = 0.15f; // Powiększyłem zasięg, żeby łapał fajne, długie skosy
        float smoothForce = 15f * Time.deltaTime; // Prasuje dużo szybciej!
        bool wasDeformed = false;

        // Szukamy granic działania pilnika na osi Z
        float minZ = localZPosition - fileRadius;
        float maxZ = localZPosition + fileRadius;

        // Zmienne do zapisania prawdziwych punktów na krawędziach pilnika
        float startX = 0f;
        float endX = 0f;
        float actualMinZ = float.MaxValue;
        float actualMaxZ = float.MinValue;

        // 1. ZNAJDUJEMY SKRAJNE PUNKTY (Góra i dół obszaru szlifowania)
        for (int i = 0; i < metalSpine.Count; i++)
        {
            float z = metalSpine[i].z;
            if (z >= minZ && z <= maxZ)
            {
                if (z < actualMinZ)
                {
                    actualMinZ = z;
                    startX = isFlipped ? metalSpine[i].leftX : metalSpine[i].rightX;
                }
                if (z > actualMaxZ)
                {
                    actualMaxZ = z;
                    endX = isFlipped ? metalSpine[i].leftX : metalSpine[i].rightX;
                }
            }
        }

        if (actualMinZ == float.MaxValue) return; // Zabezpieczenie

        // 2. PRASUJEMY DO IDEALNEJ LINI UKOŚNEJ (DIAGONALNEJ)
        for (int i = 0; i < metalSpine.Count; i++)
        {
            float z = metalSpine[i].z;
            if (z >= actualMinZ && z <= actualMaxZ)
            {
                // Obliczamy "procent" dystansu między początkiem a końcem pilnika (0.0 do 1.0)
                float t = Mathf.InverseLerp(actualMinZ, actualMaxZ, z);

                // Wyliczamy idealny punkt X na skośnej linii
                float targetDiagonalX = Mathf.Lerp(startX, endX, t);

                // Im bliżej środka pilnika, tym mocniej działa
                float distance = Mathf.Abs(z - localZPosition);
                float force = 1f - (distance / fileRadius);

                if (!isFlipped)
                {
                    metalSpine[i].rightX = Mathf.Lerp(metalSpine[i].rightX, targetDiagonalX, smoothForce * force);
                }
                else
                {
                    metalSpine[i].leftX = Mathf.Lerp(metalSpine[i].leftX, targetDiagonalX, smoothForce * force);
                }
                wasDeformed = true;
            }
        }

        if (wasDeformed) BuildMeshFromSpine();
    }
}