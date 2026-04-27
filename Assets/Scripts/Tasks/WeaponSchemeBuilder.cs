using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum PerimeterSide { Top, Left, Right }

[System.Serializable]
public struct PerimeterTriangle
{
    public PerimeterSide side;
    public float baseLength;
    public float height;
    public float offsetFromPrevious;
    // Pozycja wierzchołka wzdłuż podstawy: 0 = lewo/dół, 0.5 = środek, 1 = prawo/góra
    public float peakOffset;
}

[RequireComponent(typeof(CanvasRenderer))]
[ExecuteInEditMode]
public class WeaponSchemeBuilder : Graphic
{
    [Header("Tło (opcjonalne — wyłączane razem ze schematem)")]
    public GameObject background;

    [Header("Główny Prostokąt")]
    public Vector2 rectSize = new Vector2(200f, 100f);

    [Header("Kształty na Obwodzie")]
    public List<PerimeterTriangle> perimeterTriangles = new List<PerimeterTriangle>();

    [Header("Outline")]
    public bool drawOutline = true;
    public Color outlineColor = Color.black;
    public float outlineThickness = 4f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // Najpierw outline (rozciągnięty z centroidu każdego kształtu — żeby ostre szpice też miały outline),
        // potem właściwe wypełnienie na wierzchu.
        if (drawOutline && outlineThickness > 0f)
            PopulateGeometry(vh, outlineColor, outlineThickness);

        PopulateGeometry(vh, color, 0f);
    }

    private void PopulateGeometry(VertexHelper vh, Color c, float expand)
    {
        float halfW = rectSize.x / 2f + expand;
        float halfH = rectSize.y / 2f + expand;

        Vector2 topLeft     = new Vector2(-halfW,  halfH);
        Vector2 topRight    = new Vector2( halfW,  halfH);
        Vector2 bottomLeft  = new Vector2(-halfW, -halfH);
        Vector2 bottomRight = new Vector2( halfW, -halfH);

        AddQuad(vh, c, topLeft, topRight, bottomRight, bottomLeft);

        // Trójkąty liczymy z oryginalnych wymiarów (nierozciągniętych), a potem rozciągamy z centroidu
        float origHalfW = rectSize.x / 2f;
        float origHalfH = rectSize.y / 2f;
        Vector2 oTopLeft     = new Vector2(-origHalfW,  origHalfH);
        Vector2 oBottomLeft  = new Vector2(-origHalfW, -origHalfH);
        Vector2 oBottomRight = new Vector2( origHalfW, -origHalfH);

        float[] sideOffsets = new float[3];

        foreach (PerimeterTriangle tri in perimeterTriangles)
        {
            if (tri.baseLength <= 0 || tri.height <= 0) continue;

            int s = (int)tri.side;
            sideOffsets[s] += tri.offsetFromPrevious;
            DrawTriangle(vh, c, tri, sideOffsets[s], oTopLeft, oBottomLeft, oBottomRight, expand);
            sideOffsets[s] += tri.baseLength;
        }
    }

    private void AddQuad(VertexHelper vh, Color c, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        int i = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = c;
        vert.position = v0; vh.AddVert(vert);
        vert.position = v1; vh.AddVert(vert);
        vert.position = v2; vh.AddVert(vert);
        vert.position = v3; vh.AddVert(vert);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }

    private void DrawTriangle(VertexHelper vh, Color c, PerimeterTriangle tri, float offset,
                              Vector2 topLeft, Vector2 bottomLeft, Vector2 bottomRight, float expand)
    {
        Vector2 baseStart, baseEnd, peak;

        switch (tri.side)
        {
            case PerimeterSide.Top:
                baseStart = topLeft      + new Vector2(offset, 0);
                baseEnd   = baseStart    + new Vector2(tri.baseLength, 0);
                peak      = baseStart    + new Vector2(tri.baseLength * tri.peakOffset, tri.height);
                break;
            case PerimeterSide.Left:
                baseStart = bottomLeft   + new Vector2(0, offset);
                baseEnd   = baseStart    + new Vector2(0, tri.baseLength);
                peak      = baseStart    + new Vector2(-tri.height, tri.baseLength * tri.peakOffset);
                break;
            default: // Right
                baseStart = bottomRight  + new Vector2(0, offset);
                baseEnd   = baseStart    + new Vector2(0, tri.baseLength);
                peak      = baseStart    + new Vector2(tri.height, tri.baseLength * tri.peakOffset);
                break;
        }

        // Mitered outline: każda krawędź odsunięta prostopadle o `expand`,
        // wierzchołki przesunięte po dwusiecznej (t / cos(pół_kąta)).
        if (expand > 0f)
        {
            Vector2[] expanded = ExpandPolygon(new[] { baseStart, baseEnd, peak }, expand);
            baseStart = expanded[0];
            baseEnd   = expanded[1];
            peak      = expanded[2];
        }

        int i = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = c;
        vert.position = baseStart; vh.AddVert(vert);
        vert.position = baseEnd;   vh.AddVert(vert);
        vert.position = peak;      vh.AddVert(vert);
        vh.AddTriangle(i, i + 1, i + 2);
    }

    // Rozsuwa wielokąt o `t` prostopadle do każdej krawędzi (mitered offset).
    // Działa dla dowolnego windingu — wykrywany z signed-area.
    private static Vector2[] ExpandPolygon(Vector2[] verts, float t)
    {
        int n = verts.Length;
        Vector2[] result = new Vector2[n];

        // Signed area (>0 = CCW w Y-up)
        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % n];
            area += a.x * b.y - b.x * a.y;
        }
        bool isCCW = area > 0f;

        const float MITER_LIMIT = 6f; // ogranicza długość spike'a na bardzo ostrych szpicach

        for (int i = 0; i < n; i++)
        {
            Vector2 prev = verts[(i - 1 + n) % n];
            Vector2 cur  = verts[i];
            Vector2 next = verts[(i + 1) % n];

            Vector2 dPrev = (cur - prev).normalized;
            Vector2 dNext = (next - cur).normalized;

            // Prostopadła "na zewnątrz" zależnie od windingu
            Vector2 nPrev, nNext;
            if (isCCW)
            {
                nPrev = new Vector2( dPrev.y, -dPrev.x);
                nNext = new Vector2( dNext.y, -dNext.x);
            }
            else
            {
                nPrev = new Vector2(-dPrev.y,  dPrev.x);
                nNext = new Vector2(-dNext.y,  dNext.x);
            }

            Vector2 sum = nPrev + nNext;
            float sumLen = sum.magnitude;
            if (sumLen < 0.0001f)
            {
                result[i] = cur + nPrev * t;
                continue;
            }
            Vector2 bis = sum / sumLen;
            float cosHalf = Vector2.Dot(bis, nPrev);
            float miter = cosHalf > 0.0001f ? t / cosHalf : t * MITER_LIMIT;
            miter = Mathf.Min(miter, t * MITER_LIMIT);

            result[i] = cur + bis * miter;
        }
        return result;
    }

    public void SetTriangles(PerimeterTriangle[] newTriangles)
    {
        perimeterTriangles = new List<PerimeterTriangle>(newTriangles);
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
