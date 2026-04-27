using UnityEngine;

public class PlayerUIScript : MonoBehaviour
{
    public Transform playerPanelUI;
    public GameObject bgScheme;

    private GameObject _lastScheme;
    

    public void CopyScheme()
    {
        ForgeShapeEvaluator evaluator = FindFirstObjectByType<ForgeShapeEvaluator>();

        if (!evaluator)
            return;
        if (!evaluator.uiShapeObject)
            return;
        if (!playerPanelUI)
            return;
        
        ClearScheme();

        // Clone new scheme
        _lastScheme = Instantiate(evaluator.uiShapeObject, playerPanelUI, false);
        _lastScheme.name = "LastPlayerScheme";
        _lastScheme.SetActive(true);
        if (bgScheme) bgScheme.SetActive(true);
        
        // Size and centering image.
        RectTransform rectTransform = _lastScheme.GetComponent<RectTransform>();
        if (rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = new Vector3(0.75f, 0.75f, 1f);
        }
    }
    
    public void ClearScheme()
    {
        if (_lastScheme)
        {
            Destroy(_lastScheme);
        }
        if (bgScheme) bgScheme.SetActive(false);
        
    }
}