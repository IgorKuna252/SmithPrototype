using UnityEngine;
using TMPro;

public class StationUIManager : MonoBehaviour
{
    public static StationUIManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject instructionsPanel;
    public TextMeshProUGUI instructionsText;

    private bool isStationActive = false;

    private string defaultTutorialText = "<b>WSKAZOWKI</b>\n" +
                                         "WSAD - Poruszanie sie\n" +
                                         "Mysz - Rozgladanie sie\n" +
                                         "LPM - Chwyc przedmiot\n" +
                                         "PPM - Upusc przedmiot\n" +
                                         "E - Wejdz w interakcje\n" +
                                         "Q - Wyrzuc trzymany przedmiot\n" +
                                         "ESC - Pauza gry";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ShowDefaultInstructions();
    }

    private void Update()
    {
        if (!isStationActive)
        {
            if (BlacksmithInteraction.Instance != null && BlacksmithInteraction.Instance.IsBusy)
            {
                if (instructionsPanel.activeSelf) 
                {
                    instructionsPanel.SetActive(false);
                }
            }
            else
            {
                if (!instructionsPanel.activeSelf)
                {
                    ShowDefaultInstructions();
                }
            }
        }
    }

    public void ShowInstructions(string text)
    {
        isStationActive = true;
        instructionsText.text = text;
        instructionsPanel.SetActive(true);
    }

    public void HideInstructions()
    {
        isStationActive = false;
        ShowDefaultInstructions();
    }

    private void ShowDefaultInstructions()
    {
        if (BlacksmithInteraction.Instance != null && BlacksmithInteraction.Instance.IsBusy)
        {
            instructionsPanel.SetActive(false);
            return;
        }
        instructionsText.text = defaultTutorialText;
        instructionsPanel.SetActive(true);
    }
}
