using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum MetalType { Copper, Bronze, Iron, Steel, Gold, Platinum, BlueSteel, Vibranium }
public enum HitType { Lengthen, Widen }

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

    [Header("Ustawienia SDF (Rozdzielczość Vokseli)")]
    public float voxelSize = 0.012f;
    public Vector3 gridExtents = new Vector3(0.2f, 0.06f, 0.7f); // Pół-wymiar całego pola (X=0.4m, Y=0.12m, Z=1.4m)

    [Header("Młotek (Tuning dla SDF)")]
    public float hammerRadius = 0.06f; 

    private float[,,] sdfGrid;
    private int gridX, gridY, gridZ;

    // The 8 corners of a cube and their offsets
    private static readonly Vector3[] cornerOffsets = {
        new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1),
        new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1)
    };

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
        SetMoldAndRebuild(partType);
    }

    void Update()
    {
        if (!isInForge && currentTemperature > 20f) currentTemperature -= coolingRate * Time.deltaTime;
        UpdateVisuals();
    }

    // =================================================================
    // SDF GENERATION
    // =================================================================

    public void SetMoldAndRebuild(MetalPartType newMoldType)
    {
        partType = newMoldType;
        InitializeSDFGrid();
        RunMarchingCubes();
        SetBaseColor();
        UpdateVisuals();
    }

    private void InitializeSDFGrid()
    {
        gridX = Mathf.CeilToInt(gridExtents.x * 2f / voxelSize);
        gridY = Mathf.CeilToInt(gridExtents.y * 2f / voxelSize);
        gridZ = Mathf.CeilToInt(gridExtents.z * 2f / voxelSize);
        sdfGrid = new float[gridX, gridY, gridZ];

        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int z = 0; z < gridZ; z++)
                {
                    Vector3 pos = GetLocalPosition(x, y, z);
                    float dist = 100f; // very far away outside

                    if (partType == MetalPartType.Ingot)
                    {
                        dist = SDFBox(pos, new Vector3(0.05f, 0.04f, 0.175f));
                    }
                    else if (partType == MetalPartType.SwordMold)
                    {
                        // Kombinacja ostrza: gruby trzon, zwężający się w Z
                        float szpic = Mathf.Max(0, pos.z - 0.2f); // tapering for sword tip
                        float bladeWidth = Mathf.Lerp(0.075f, 0.01f, szpic * 2f);
                        dist = SDFBox(pos, new Vector3(bladeWidth, 0.032f, 0.55f));
                    }
                    else if (partType == MetalPartType.AxeMold)
                    {
                        // Siekiera - szersza po lewej stronie, niesymetrycznie
                        Vector3 offset = new Vector3(-0.06f, 0f, 0f);
                        dist = SDFBox(pos - offset, new Vector3(0.12f, 0.03f, 0.175f));
                    }

                    sdfGrid[x, y, z] = dist;
                }
            }
        }
    }

    private Vector3 GetLocalPosition(int x, int y, int z)
    {
        return new Vector3(
            x * voxelSize - gridExtents.x,
            y * voxelSize - gridExtents.y,
            z * voxelSize - gridExtents.z
        );
    }

    private float SDFBox(Vector3 p, Vector3 size)
    {
        Vector3 q = new Vector3(Mathf.Abs(p.x) - size.x, Mathf.Abs(p.y) - size.y, Mathf.Abs(p.z) - size.z);
        return Vector3.Magnitude(Vector3.Max(q, Vector3.zero)) + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0.0f);
    }

    private float SDFCapsule(Vector3 p, Vector3 a, Vector3 b, float r)
    {
        Vector3 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
        return Vector3.Magnitude(pa - ba * h) - r;
    }

    // =================================================================
    // MARCHING CUBES 
    // =================================================================

    public void RunMarchingCubes()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float isolevel = 0f;

        for (int x = 0; x < gridX - 1; x++)
        {
            for (int y = 0; y < gridY - 1; y++)
            {
                for (int z = 0; z < gridZ - 1; z++)
                {
                    float[] cubeValues = new float[8];
                    Vector3[] cubePositions = new Vector3[8];

                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int cx = x + (int)cornerOffsets[i].x;
                        int cy = y + (int)cornerOffsets[i].y;
                        int cz = z + (int)cornerOffsets[i].z;

                        cubeValues[i] = sdfGrid[cx, cy, cz];
                        cubePositions[i] = GetLocalPosition(cx, cy, cz);

                        if (cubeValues[i] < isolevel) cubeIndex |= (1 << i);
                    }

                    int edgeBits = MarchingCubesTables.edgeTable[cubeIndex];
                    if (edgeBits == 0) continue;

                    Vector3[] edgeVertices = new Vector3[12];
                    if ((edgeBits & 1) != 0) edgeVertices[0] = VertexInterp(isolevel, cubePositions[0], cubePositions[1], cubeValues[0], cubeValues[1]);
                    if ((edgeBits & 2) != 0) edgeVertices[1] = VertexInterp(isolevel, cubePositions[1], cubePositions[2], cubeValues[1], cubeValues[2]);
                    if ((edgeBits & 4) != 0) edgeVertices[2] = VertexInterp(isolevel, cubePositions[2], cubePositions[3], cubeValues[2], cubeValues[3]);
                    if ((edgeBits & 8) != 0) edgeVertices[3] = VertexInterp(isolevel, cubePositions[3], cubePositions[0], cubeValues[3], cubeValues[0]);
                    if ((edgeBits & 16) != 0) edgeVertices[4] = VertexInterp(isolevel, cubePositions[4], cubePositions[5], cubeValues[4], cubeValues[5]);
                    if ((edgeBits & 32) != 0) edgeVertices[5] = VertexInterp(isolevel, cubePositions[5], cubePositions[6], cubeValues[5], cubeValues[6]);
                    if ((edgeBits & 64) != 0) edgeVertices[6] = VertexInterp(isolevel, cubePositions[6], cubePositions[7], cubeValues[6], cubeValues[7]);
                    if ((edgeBits & 128) != 0) edgeVertices[7] = VertexInterp(isolevel, cubePositions[7], cubePositions[4], cubeValues[7], cubeValues[4]);
                    if ((edgeBits & 256) != 0) edgeVertices[8] = VertexInterp(isolevel, cubePositions[0], cubePositions[4], cubeValues[0], cubeValues[4]);
                    if ((edgeBits & 512) != 0) edgeVertices[9] = VertexInterp(isolevel, cubePositions[1], cubePositions[5], cubeValues[1], cubeValues[5]);
                    if ((edgeBits & 1024) != 0) edgeVertices[10] = VertexInterp(isolevel, cubePositions[2], cubePositions[6], cubeValues[2], cubeValues[6]);
                    if ((edgeBits & 2048) != 0) edgeVertices[11] = VertexInterp(isolevel, cubePositions[3], cubePositions[7], cubeValues[3], cubeValues[7]);

                    for (int i = 0; MarchingCubesTables.triTable[cubeIndex, i] != -1; i += 3)
                    {
                        int a0 = MarchingCubesTables.triTable[cubeIndex, i];
                        int a1 = MarchingCubesTables.triTable[cubeIndex, i + 1];
                        int a2 = MarchingCubesTables.triTable[cubeIndex, i + 2];

                        // Rozbicie trójkątów dla efektu HARD-EDGES (Flat Shading)
                        verts.Add(edgeVertices[a0]);
                        verts.Add(edgeVertices[a1]);
                        verts.Add(edgeVertices[a2]);

                        int idx = tris.Count;
                        tris.Add(idx);
                        tris.Add(idx + 1);
                        tris.Add(idx + 2);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "SDFMetalMesh";
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);

        mesh.RecalculateNormals(); // Dadzą twarde normalki dzięki powielonym wierzchołkom
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private Vector3 VertexInterp(float isolevel, Vector3 p1, Vector3 p2, float valp1, float valp2)
    {
        if (Mathf.Abs(isolevel - valp1) < 0.00001f) return p1;
        if (Mathf.Abs(isolevel - valp2) < 0.00001f) return p2;
        if (Mathf.Abs(valp1 - valp2) < 0.00001f) return p1;
        float mu = (isolevel - valp1) / (valp2 - valp1);
        return Vector3.Lerp(p1, p2, mu);
    }

    // =================================================================
    // INTERAKCJE KOWALSKIE (SDF MATH)
    // =================================================================

    public bool HitMetal(Vector3 hitPoint, Vector3 hitNormal, HitType hitType = HitType.Lengthen)
    {
        if (currentTemperature < forgingTemperature) return false;

        Vector3 localHit = transform.InverseTransformPoint(hitPoint);
        bool wasDeformed = false;

        // Optymalizacja pętli uderzenia (Bounding Box Młotka)
        int startX = Mathf.Max(0, Mathf.FloorToInt((localHit.x - hammerRadius + gridExtents.x) / voxelSize));
        int endX = Mathf.Min(gridX, Mathf.CeilToInt((localHit.x + hammerRadius + gridExtents.x) / voxelSize) + 1);
        
        int startY = 0;
        int endY = gridY;

        int startZ = Mathf.Max(0, Mathf.FloorToInt((localHit.z - hammerRadius + gridExtents.z) / voxelSize));
        int endZ = Mathf.Min(gridZ, Mathf.CeilToInt((localHit.z + hammerRadius + gridExtents.z) / voxelSize) + 1);

        // Młotek działa jak wgniatający walec od góry
        Vector3 hammerTop = localHit + Vector3.up * 0.1f;
        Vector3 hammerBot = localHit - Vector3.up * 0.1f;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    Vector3 p = GetLocalPosition(x, y, z);
                    
                    // Odległość wertykalna żeby wgnieciona objętość rozpychała siatkę
                    float dx = p.x - localHit.x;
                    float dz = p.z - localHit.z;
                    float distCenter2D = Mathf.Sqrt(dx * dx + dz * dz);

                    if (distCenter2D < hammerRadius + 0.03f) // Nieco powiększony promień dla efektu rozpychania
                    {
                        float currentSdf = sdfGrid[x, y, z];

                        // 1. WGNIECENIE (Subtract SDF)
                        float hammerSDF = SDFCapsule(p, hammerTop, hammerBot, hammerRadius * 0.4f);
                        float indentedSdf = Mathf.Max(currentSdf, -hammerSDF);
                        
                        // 2. ROZPYCHANIE (Add SDF - dodajemy na bokach)
                        float spreadRadius = hammerRadius * 0.5f;
                        Vector3 spreadCenter;
                        
                        if (hitType == HitType.Lengthen) {
                            float zDir = dz > 0 ? 1 : -1;
                            spreadCenter = localHit + new Vector3(0, 0, zDir * hammerRadius * 0.5f);
                        } else {
                            float xDir = dx > 0 ? 1 : -1;
                            spreadCenter = localHit + new Vector3(xDir * hammerRadius * 0.5f, 0, 0);
                        }
                        
                        // Aby nie było za grube, mocno spłaszczamy dodawaną masę w osi Y
                        Vector3 pSpread = new Vector3(p.x, p.y * 1.5f, p.z);
                        float spreadSDF = Vector3.Distance(pSpread, spreadCenter) - spreadRadius;

                        float newSdf = Mathf.Min(indentedSdf, spreadSDF);

                        if (Mathf.Abs(currentSdf - newSdf) > 0.001f)
                        {
                            sdfGrid[x, y, z] = newSdf;
                            wasDeformed = true;
                        }
                    }
                }
            }
        }

        if (wasDeformed)
        {
            RunMarchingCubes();
        }

        return wasDeformed;
    }

    public void GrindPerfectEdge(float localZPosition, bool isFlipped)
    {
        // SDF Grinding - zamiast 2D plastrowania, subtrakcja kuli na krawędzi
        float eatRadius = 0.02f;
        Vector3 stonePos = new Vector3(isFlipped ? -0.06f : 0.06f, 0, localZPosition);
        
        bool changed = false;

        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int z = 0; z < gridZ; z++)
                {
                    Vector3 p = GetLocalPosition(x, y, z);
                    float dist = Vector3.Distance(p, stonePos);
                    
                    if (dist < eatRadius * 2f) 
                    {
                        float stoneSdf = dist - eatRadius;
                        float current = sdfGrid[x, y, z];
                        float newSdf = Mathf.Max(current, -stoneSdf);

                        if (newSdf > current)
                        {
                            sdfGrid[x, y, z] = newSdf;
                            changed = true;
                        }
                    }
                }
            }
        }
        if (changed) RunMarchingCubes();
    }

    public void SmoothPerfectEdge(float localZPosition, bool isFlipped)
    {
        // Smooth (Pilnik) usuwa materiał trochę podobnie jak szlifierka w tym wariancie
        GrindPerfectEdge(localZPosition, isFlipped);
    }

    public float GetEdgeWidthAt(float localZPosition, bool isFlipped)
    {
        // Uproszczony śledzik grawędzi SDF dla narzędzi
        int zIndex = Mathf.Clamp(Mathf.RoundToInt((localZPosition + gridExtents.z) / voxelSize), 0, gridZ - 1);
        int yIndex = gridY / 2;
        int centerIdx = gridX / 2;
        int dir = isFlipped ? -1 : 1;

        int edgeX = centerIdx;
        for (int i = 0; i < gridX / 2; i++)
        {
            int x = centerIdx + dir * i;
            if (x >= 0 && x < gridX)
            {
                if (sdfGrid[x, yIndex, zIndex] < 0f) edgeX = x; else break;
            }
        }
        return Mathf.Abs(GetLocalPosition(edgeX, yIndex, zIndex).x) * transform.localScale.x;
    }

    public void ResetEdgeIntegrity() 
    { 
        // W SDF nie mamy bariery integralności per-krąg, system automatyczne trawi woksele
    }

    // =================================================================
    // RESZTA FUNKCJI
    // =================================================================

    public bool Interact() => currentTemperature >= forgingTemperature;
    public void OnPickUp() => isInForge = false;
    public void OnDrop() { }
    public void ForceCoolDown() { currentTemperature = 20f; isInForge = false; UpdateVisuals(); }

    public void SetBaseColor()
    {
        switch (metalTier)
        {
            case MetalType.Copper: baseColdColor = new Color(0.8f, 0.4f, 0.2f); break;
            case MetalType.Bronze: baseColdColor = new Color(0.7f, 0.5f, 0.1f); break;
            case MetalType.Iron: baseColdColor = new Color(0.15f, 0.15f, 0.15f); break;
            case MetalType.Steel: baseColdColor = new Color(0.35f, 0.35f, 0.4f); break;
            case MetalType.Gold: baseColdColor = new Color(1.0f, 0.8f, 0.0f); break;
            case MetalType.Platinum: baseColdColor = new Color(0.9f, 0.9f, 0.95f); break;
            case MetalType.BlueSteel: baseColdColor = new Color(0.2f, 0.3f, 0.5f); break;
            case MetalType.Vibranium: baseColdColor = new Color(0.5f, 0.2f, 0.8f); break;
            default: baseColdColor = new Color(0.15f, 0.15f, 0.15f); break;
        }

        if (meshRenderer != null)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            if (litShader != null)
            {
                Material mat = new Material(litShader);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColdColor);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColdColor);
                meshRenderer.material = mat;
            }
        }
    }

    private void UpdateVisuals()
    {
        if (meshRenderer == null || meshRenderer.material == null) return;

        float t = Mathf.Clamp01((currentTemperature - 20f) / (maxTemperature - 20f));
        Color hotColor = new Color(0.8f, 0.25f, 0f);
        Color targetColor = Color.Lerp(baseColdColor, hotColor, t);

        Material mat = meshRenderer.material;
        mat.color = targetColor;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", targetColor);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", targetColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", targetColor * (t * 1.5f));
        }
    }
}