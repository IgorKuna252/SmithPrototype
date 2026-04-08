using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu pauzy (ESC). Tworzy sobie UI automatycznie jeśli nie podano panelu ręcznie.
/// Podepnij na dowolny GameObject na scenie.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panel Menu (opcjonalnie — stworzy się sam)")]
    public GameObject panel;

    private bool isPaused = false;
    private BlacksmithInteraction _blacksmith;

    void Start()
    {
        _blacksmith = Object.FindFirstObjectByType<BlacksmithInteraction>();

        if (panel == null)
            BuildUI();
        else
            panel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Jeśli menu pauzy jest otwarte — zamknij je
        if (isPaused)
        {
            Resume();
            return;
        }

        // Nie otwieraj menu jeśli gracz jest w interakcji — ESC ma tam inną funkcję
        if (_blacksmith != null && _blacksmith.IsBusy) return;

        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        panel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public void Resume()
    {
        isPaused = true; // Toggle odwróci na false
        TogglePause();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // --- Auto-budowanie UI gdy nie podano panelu w Inspektorze ---
    private void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("PauseMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.transform.SetParent(transform);

        // Ciemne tło
        panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // Tytuł
        CreateText(panel.transform, "PAUZA", 48, new Vector2(0, 80));

        // Przycisk WZNÓW
        CreateButton(panel.transform, "Wznów", new Vector2(0, 0), Resume);

        // Przycisk WYJDŹ
        CreateButton(panel.transform, "Wyjdź z gry", new Vector2(0, -80), QuitGame);

        panel.SetActive(false);
    }

    private void CreateText(Transform parent, string text, int fontSize, Vector2 pos)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 60);
        rt.anchoredPosition = pos;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 50);
        rt.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        cb.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        CreateText(btnObj.transform, label, 24, Vector2.zero);
    }
}
