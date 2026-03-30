using System;
using UnityEngine;

public class ForgeShapeEvaluator : MonoBehaviour
{
    [Header("Ustawienia Kamery, która 'robi zdjęcie' ")]
    public float cameraOrthoSize = 2f;
    public int resolution = 256;

    [Header("Ustawienia Kształtu UI")]
    public GameObject uiShapeObject;
    public float uiToWorldScale = 0.01f;

    private Texture2D targetShapeMask;
    private Texture2D forgedSilhouette;

    [Header("Ustawienia Oceny")]
    [Range(0f, 2f)]
    public float overspillPenalty = 1.0f;

    // Subskrybuj żeby otrzymać znormalizowane tekstury do debugowania (schemat, broń, nakładka)
    public event Action<Texture2D, Texture2D, Texture2D> OnDebugReady;

    // Przechowuje ostatnio znormalizowane tekstury między wywołaniami
    private Texture2D _lastNormScheme;
    private Texture2D _lastNormWeapon;

    public float EvaluateForgingAccuracy(GameObject forgedMetal)
    {
        if (uiShapeObject == null || forgedMetal == null)
        {
            Debug.LogError("[ForgeShapeEvaluator] Brak uiShapeObject lub forgedMetal!");
            return 0f;
        }

        int tempLayer = LayerMask.NameToLayer("Hidden");
        if (tempLayer < 0)
        {
            Debug.LogError("[ForgeShapeEvaluator] Warstwa 'Hidden' nie istnieje! Dodaj ją: Edit → Project Settings → Tags and Layers");
            return 0f;
        }

        Vector3 originalPos   = forgedMetal.transform.position;
        Quaternion originalRot = forgedMetal.transform.rotation;
        int originalLayer      = forgedMetal.layer;

        Vector3 hiddenPosition = new Vector3(0, -5000, 0);

        // Kamera oceniająca
        GameObject tempCamObj = new GameObject("TempPhotoboothCamera");
        tempCamObj.transform.position = hiddenPosition + new Vector3(0, 0, -10);
        Camera tempCam = tempCamObj.AddComponent<Camera>();
        tempCam.orthographic = true;
        tempCam.orthographicSize = cameraOrthoSize;
        tempCam.clearFlags = CameraClearFlags.SolidColor;
        tempCam.backgroundColor = Color.black;
        tempCam.cullingMask = 1 << tempLayer;

        // ZDJĘCIE 1: KSZTAŁT UI
        GameObject tempCanvasObj = new GameObject("TempPhotoboothCanvas");
        tempCanvasObj.transform.position = hiddenPosition;
        Canvas tempCanvas = tempCanvasObj.AddComponent<Canvas>();
        tempCanvas.renderMode = RenderMode.WorldSpace;
        tempCanvasObj.GetComponent<RectTransform>().localScale = new Vector3(uiToWorldScale, uiToWorldScale, 1f);

        GameObject shapeCopy = Instantiate(uiShapeObject, tempCanvas.transform);
        shapeCopy.transform.localPosition = Vector3.zero;

        UnityEngine.UI.Graphic graphic = shapeCopy.GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null) graphic.color = Color.white;

        tempCamObj.layer = tempLayer;
        tempCanvasObj.layer = tempLayer;
        ChangeLayerRecursive(shapeCopy.transform, tempLayer);
        shapeCopy.SetActive(true);

        // Wymuszamy odbudowę meshy Canvas przed renderem — bez tego nowe Graphici są puste
        Canvas.ForceUpdateCanvases();

        targetShapeMask = CaptureCameraToTexture(tempCam, resolution, resolution);

        int schemePixels = CountWhitePixels(targetShapeMask);
        Debug.Log($"[ForgeShapeEvaluator] Schemat: {schemePixels} białych px / {resolution * resolution} total");

        DestroyImmediate(tempCanvasObj);

        // ZDJĘCIE 2: WYKUTY METAL
        // Obrót: ostrze biegnie wzdłuż Z, kamera patrzy w +Z → widzimy tylko przekrój.
        // Euler(-90, 0, 90): Z→X (długość poziomo), X→Y (szerokość pionowo), Y→Z (grubość = głębokość, niewidoczna)
        forgedMetal.transform.position = hiddenPosition;
        forgedMetal.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ChangeLayerRecursive(forgedMetal.transform, tempLayer);

        forgedSilhouette = CaptureCameraToTexture(tempCam, resolution, resolution);

        int weaponPixels = CountWhitePixels(forgedSilhouette);
        Debug.Log($"[ForgeShapeEvaluator] Broń: {weaponPixels} białych px");

        DestroyImmediate(tempCamObj);
        forgedMetal.transform.position = originalPos;
        forgedMetal.transform.rotation = originalRot;
        ChangeLayerRecursive(forgedMetal.transform, originalLayer);

        float result = CompareTextures(forgedSilhouette, targetShapeMask);
        Debug.Log($"[ForgeShapeEvaluator] Wynik porównania: {result:F1}%");

        // Powiadamiamy UI debugowania (jeśli ktoś subskrybuje)
        if (OnDebugReady != null && _lastNormScheme != null && _lastNormWeapon != null)
        {
            Texture2D overlay = BuildOverlay(_lastNormScheme, _lastNormWeapon);
            OnDebugReady.Invoke(_lastNormScheme, _lastNormWeapon, overlay);
        }

        return result;
    }

    private int CountWhitePixels(Texture2D tex)
    {
        int count = 0;
        Color[] pixels = tex.GetPixels();
        foreach (Color c in pixels)
            if (c.r > 0.1f) count++;
        return count;
    }

    private Texture2D CaptureCameraToTexture(Camera cam, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        return tex;
    }

    private float CompareTextures(Texture2D forgedTex, Texture2D targetTex)
    {
        Color[] forgedPixels = forgedTex.GetPixels();
        Color[] targetPixels = targetTex.GetPixels();

        if (forgedPixels.Length != targetPixels.Length) return 0f;

        // Normalizacja: skalujemy oba zestawy pikseli do pełnej klatki na podstawie bounding boxów,
        // żeby różnice skali między bronią a schematem nie dawały 0%.
        // Przechowujemy je w polach, żeby debug UI mógł je wyświetlić.
        if (_lastNormScheme != null) Destroy(_lastNormScheme);
        if (_lastNormWeapon != null) Destroy(_lastNormWeapon);
        _lastNormScheme = NormalizeToFullFrame(targetTex, resolution, resolution, 0.5f);
        _lastNormWeapon = NormalizeToFullFrame(forgedTex,  resolution, resolution, 0.1f);

        forgedPixels = _lastNormWeapon.GetPixels();
        targetPixels = _lastNormScheme.GetPixels();

        int idealShapeArea = 0;
        int matchedArea = 0;
        int overspillArea = 0;

        for (int i = 0; i < targetPixels.Length; i++)
        {
            bool isTargetMetal = targetPixels[i].r > 0.5f;
            bool isForgedMetal = forgedPixels[i].r > 0.1f;

            if (isTargetMetal) idealShapeArea++;

            if (isForgedMetal)
            {
                if (isTargetMetal) matchedArea++;
                else overspillArea++;
            }
        }

        if (idealShapeArea == 0) return 0f;

        float accuracy = (float)matchedArea / idealShapeArea;
        float penalty = ((float)overspillArea / idealShapeArea) * overspillPenalty;
        float finalScore = Mathf.Clamp01(accuracy - penalty);

        return finalScore * 100f;
    }

    // Buduje nakładkę kolorową:
    //   Zielony  = trafiony (schemat i broń się zgadzają)
    //   Czerwony = nadmiar broni (poza schematem)
    //   Niebieski = brakuje (schemat bez broni)
    private Texture2D BuildOverlay(Texture2D normScheme, Texture2D normWeapon)
    {
        Color[] schemePixels = normScheme.GetPixels();
        Color[] weaponPixels = normWeapon.GetPixels();
        int n = schemePixels.Length;

        Texture2D overlay = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] overlayPixels = new Color[n];

        for (int i = 0; i < n; i++)
        {
            bool inScheme = schemePixels[i].r > 0.5f;
            bool inWeapon = weaponPixels[i].r > 0.1f;

            if (inScheme && inWeapon)
                overlayPixels[i] = new Color(0f, 0.85f, 0.2f, 1f);   // zielony = trafienie
            else if (inWeapon)
                overlayPixels[i] = new Color(0.9f, 0.1f, 0.1f, 1f);  // czerwony = nadmiar
            else if (inScheme)
                overlayPixels[i] = new Color(0.15f, 0.4f, 0.9f, 1f); // niebieski = brakuje
            else
                overlayPixels[i] = new Color(0f, 0f, 0f, 0.6f);      // czarne tło
        }

        overlay.SetPixels(overlayPixels);
        overlay.Apply();
        return overlay;
    }

    // Wycina bounding box białych pikseli i skaluje do ramki zachowując proporcje (letterbox)
    private Texture2D NormalizeToFullFrame(Texture2D src, int w, int h, float threshold)
    {
        Color[] pixels = src.GetPixels();
        int minX = w, maxX = 0, minY = h, maxY = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].r > threshold)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (minX > maxX || minY > maxY)
            return new Texture2D(w, h, TextureFormat.RGB24, false);

        int srcW = maxX - minX + 1;
        int srcH = maxY - minY + 1;

        // Skalowanie z zachowaniem proporcji — fit-to-frame z czarnym marginesem
        float scaleX = (float)w / srcW;
        float scaleY = (float)h / srcH;
        float scale  = Mathf.Min(scaleX, scaleY) * 0.9f; // 0.9 = mały margines przy krawędzi

        int dstW = Mathf.RoundToInt(srcW * scale);
        int dstH = Mathf.RoundToInt(srcH * scale);
        int offX = (w - dstW) / 2;
        int offY = (h - dstH) / 2;

        Texture2D result = new Texture2D(w, h, TextureFormat.RGB24, false);
        Color[] resultPixels = new Color[w * h]; // domyślnie czarne

        for (int y = 0; y < dstH; y++)
        {
            for (int x = 0; x < dstW; x++)
            {
                int srcX = minX + Mathf.RoundToInt((float)x / dstW * srcW);
                int srcY = minY + Mathf.RoundToInt((float)y / dstH * srcH);
                srcX = Mathf.Clamp(srcX, 0, w - 1);
                srcY = Mathf.Clamp(srcY, 0, h - 1);
                resultPixels[(y + offY) * w + (x + offX)] = pixels[srcY * w + srcX];
            }
        }

        result.SetPixels(resultPixels);
        result.Apply();
        return result;
    }

    private void ChangeLayerRecursive(Transform obj, int newLayer)
    {
        obj.gameObject.layer = newLayer;
        foreach (Transform child in obj) ChangeLayerRecursive(child, newLayer);
    }
}
