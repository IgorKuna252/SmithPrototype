using UnityEngine;
using TMPro; // Wymagane dla TextMeshPro

public class StationUIManager : MonoBehaviour
{
    // Wzorzec Singleton - pozwala na ³atwy dostêp z ka¿dego innego skryptu bez u¿ycia GetComponent
    public static StationUIManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject instructionsPanel;
    public TextMeshProUGUI instructionsText;

    private void Awake()
    {
        // Konfiguracja Singletona
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Upewniamy siê, ¿e na starcie gry panel instrukcji jest ukryty
        HideInstructions();
    }

    // Wywo³uj tê funkcjê wchodz¹c na stanowisko
    public void ShowInstructions(string text)
    {
        instructionsText.text = text;
        instructionsPanel.SetActive(true);
    }

    // Wywo³uj tê funkcjê wychodz¹c ze stanowiska
    public void HideInstructions()
    {
        instructionsPanel.SetActive(false);
        instructionsText.text = "";
    }
}