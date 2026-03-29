using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum PerimeterSide { Top,/*Bottom,*/ Left, Right }

// Struktura definiująca pojedynczy trójkąt na obwodzie
[System.Serializable]
public struct PerimeterTriangle
{
    public PerimeterSide side;      // Krawędź mocowania
    public float baseLength;         // Długość podstawy
    public float height;              // Wysokość trójkąta
    public float offsetFromPrevious;  // Odstęp od poprzedniego kształtu na tej samej ścianie (lub od rogu dla pierwszego)
}

[RequireComponent(typeof(CanvasRenderer))]
[ExecuteInEditMode] // Pozwala na aktualizację w edytorze bez uruchamiania gry
public class WeaponSchemeBuilder : Graphic
{
    [Header("Główny Prostokąt")]
    public Vector2 rectSize = new Vector2(200f, 100f);

    [Header("Kształty na Obwodzie")]
    public List<PerimeterTriangle> perimeterTriangles = new List<PerimeterTriangle>();

    // Główna metoda rysująca
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // 1. Obliczamy rogi głównego prostokąta (wyśrodkowanego)
        float halfW = rectSize.x / 2f;
        float halfH = rectSize.y / 2f;

        Vector2 topLeft = new Vector2(-halfW, halfH);
        Vector2 topRight = new Vector2(halfW, halfH);
        Vector2 bottomLeft = new Vector2(-halfW, -halfH);
        Vector2 bottomRight = new Vector2(halfW, -halfH);

        // 2. Rysujemy główny prostokąt
        AddRectangle(vh, topLeft, topRight, bottomRight, bottomLeft, color);

        // 3. Rysujemy trójkąty peryferyjne
        // Potrzebujemy śledzić bieżącą pozycję na każdej krawędzi
        Dictionary<PerimeterSide, float> currentEdgeOffsets = new Dictionary<PerimeterSide, float>
        {
            { PerimeterSide.Top, 0f },
            // { PerimeterSide.Bottom, 0f },
            { PerimeterSide.Left, 0f },
            { PerimeterSide.Right, 0f }
        };

        foreach (PerimeterTriangle tri in perimeterTriangles)
        {
            if (tri.baseLength <= 0 || tri.height <= 0) continue;

            // Zwiększamy odstęp o wartość zadaną w strukturze ("następny za 50")
            currentEdgeOffsets[tri.side] += tri.offsetFromPrevious;

            DrawTriangleOnSide(vh, tri, currentEdgeOffsets[tri.side], topLeft, topRight, bottomLeft, bottomRight);

            // Po narysowaniu, przesuwamy kursor o długość podstawy
            currentEdgeOffsets[tri.side] += tri.baseLength;
        }
    }

    // Pomocnicza metoda do dodawania prostokąta
    private void AddRectangle(VertexHelper vh, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color col)
    {
        int startIndex = vh.currentVertCount;

        UIVertex vert = UIVertex.simpleVert;
        vert.color = col;

        vert.position = v0; vh.AddVert(vert); // 0
        vert.position = v1; vh.AddVert(vert); // 1
        vert.position = v2; vh.AddVert(vert); // 2
        vert.position = v3; vh.AddVert(vert); // 3

        vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
    }

    // Pomocnicza metoda do obliczania i rysowania trójkąta na danej krawędzi
    private void DrawTriangleOnSide(VertexHelper vh, PerimeterTriangle triDef, float currentOffset, Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
    {
        Vector2 pBaseStart = Vector2.zero;
        Vector2 pBaseEnd = Vector2.zero;
        Vector2 pPeak = Vector2.zero;

        int vertStartIndex = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;

        switch (triDef.side)
        {
            case PerimeterSide.Top:
                // Podstawa na górnej krawędzi, szczyt idzie w górę (+Y)
                pBaseStart = topLeft + new Vector2(currentOffset, 0);
                pBaseEnd = pBaseStart + new Vector2(triDef.baseLength, 0);
                pPeak = pBaseStart + new Vector2(triDef.baseLength / 2f, triDef.height);
                break;
            // case PerimeterSide.Bottom:
            //     // Podstawa na dolnej, szczyt w dół (-Y)
            //     pBaseStart = bottomLeft + new Vector2(currentOffset, 0);
            //     pBaseEnd = pBaseStart + new Vector2(triDef.baseLength, 0);
            //     pPeak = pBaseStart + new Vector2(triDef.baseLength / 2f, -triDef.height);
            //     break;
            case PerimeterSide.Left:
                // Podstawa na lewej (Y rośnie w górę), szczyt w lewo (-X)
                pBaseStart = bottomLeft + new Vector2(0, currentOffset);
                pBaseEnd = pBaseStart + new Vector2(0, triDef.baseLength);
                pPeak = pBaseStart + new Vector2(-triDef.height, triDef.baseLength / 2f);
                break;
            case PerimeterSide.Right:
                // Podstawa na prawej (Y rośnie w górę), szczyt w prawo (+X)
                pBaseStart = bottomRight + new Vector2(0, currentOffset);
                pBaseEnd = pBaseStart + new Vector2(0, triDef.baseLength);
                pPeak = pBaseStart + new Vector2(triDef.height, triDef.baseLength / 2f);
                break;
        }

        // Dodawanie wierzchołków
        vert.position = pBaseStart; vh.AddVert(vert);
        vert.position = pBaseEnd; vh.AddVert(vert);
        vert.position = pPeak; vh.AddVert(vert);

        // Dodawanie trójkąta (ważna kolejność dla kierunku rysowania)
        vh.AddTriangle(vertStartIndex, vertStartIndex + 1, vertStartIndex + 2);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}