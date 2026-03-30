using UnityEngine;
using TMPro;

public class TransactionResultUI : MonoBehaviour
{
    public static TransactionResultUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Tekst wyniku")]
    public TextMeshProUGUI resultText;

    private System.Action onClose;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(float matchPercent, int payment, System.Action callback)
    {
        onClose = callback;

        string color = matchPercent >= 90f ? "#00FF00" : matchPercent >= 50f ? "#FFFF00" : "#FF0000";
        resultText.text = $"Dopasowanie: <color={color}>{matchPercent:F0}%</color>\nZapłata: {payment} złota";

        panel.SetActive(true);

        BlacksmithInteraction bi = BlacksmithInteraction.Instance;
        if (bi != null) bi.SetTransactionUIOpen(true);
    }

    public void Close()
    {
        panel.SetActive(false);

        BlacksmithInteraction bi = BlacksmithInteraction.Instance;
        if (bi != null) bi.SetTransactionUIOpen(false);

        onClose?.Invoke();
        onClose = null;
    }

    public bool IsVisible => panel != null && panel.activeSelf;
}
