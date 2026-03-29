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
        forgedMetal.transform.position = hiddenPosition;
        forgedMetal.transform.rotation = Quaternion.identity;
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

    private void ChangeLayerRecursive(Transform obj, int newLayer)
    {
        obj.gameObject.layer = newLayer;
        foreach (Transform child in obj) ChangeLayerRecursive(child, newLayer);
    }
}