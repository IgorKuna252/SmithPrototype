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
    [Header("Główny Prostokąt")]
    public Vector2 rectSize = new Vector2(200f, 100f);

    [Header("Kształty na Obwodzie")]
    public List<PerimeterTriangle> perimeterTriangles = new List<PerimeterTriangle>();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float halfW = rectSize.x / 2f;
        float halfH = rectSize.y / 2f;

        Vector2 topLeft     = new Vector2(-halfW,  halfH);
        Vector2 topRight    = new Vector2( halfW,  halfH);
        Vector2 bottomLeft  = new Vector2(-halfW, -halfH);
        Vector2 bottomRight = new Vector2( halfW, -halfH);

        AddQuad(vh, topLeft, topRight, bottomRight, bottomLeft);

        // Śledzenie offsetu na każdej krawędzi — indeks = (int)PerimeterSide
        float[] sideOffsets = new float[3];

        foreach (PerimeterTriangle tri in perimeterTriangles)
        {
            if (tri.baseLength <= 0 || tri.height <= 0) continue;

            int s = (int)tri.side;
            sideOffsets[s] += tri.offsetFromPrevious;
            DrawTriangle(vh, tri, sideOffsets[s], topLeft, bottomLeft, bottomRight);
            sideOffsets[s] += tri.baseLength;
        }
    }

    private void AddQuad(VertexHelper vh, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        int i = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;
        vert.position = v0; vh.AddVert(vert);
        vert.position = v1; vh.AddVert(vert);
        vert.position = v2; vh.AddVert(vert);
        vert.position = v3; vh.AddVert(vert);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }

    private void DrawTriangle(VertexHelper vh, PerimeterTriangle tri, float offset,
                              Vector2 topLeft, Vector2 bottomLeft, Vector2 bottomRight)
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

        int i = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;
        vert.position = baseStart; vh.AddVert(vert);
        vert.position = baseEnd;   vh.AddVert(vert);
        vert.position = peak;      vh.AddVert(vert);
        vh.AddTriangle(i, i + 1, i + 2);
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
