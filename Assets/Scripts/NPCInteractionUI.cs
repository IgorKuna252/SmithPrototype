using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCInteractionUI : MonoBehaviour
{
    public static NPCInteractionUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Statystyki NPC")]
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI npcStatsText;

    [Header("Przyciski")]
    public Button acceptButton;
    public Button rejectButton;

    private npcPathFinding currentNPC;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(npcPathFinding npc)
    {
        currentNPC = npc;
        panel.SetActive(true);

        // Statyczne statystyki na razie
        npcNameText.text = "Wędrowiec";
        npcStatsText.text = npc.ShowStats();

        acceptButton.onClick.RemoveAllListeners();
        rejectButton.onClick.RemoveAllListeners();

        acceptButton.onClick.AddListener(OnAccept);
        rejectButton.onClick.AddListener(OnReject);
    }

    void OnAccept()
    {
        currentNPC.Interact(KeyCode.Mouse1);
        BlacksmithInteraction blacksmith = FindObjectOfType<BlacksmithInteraction>();
        blacksmith.CloseNPCInteraction();
    }

    void OnReject()
    {
        currentNPC.Interact(KeyCode.Mouse0);
        BlacksmithInteraction blacksmith = FindObjectOfType<BlacksmithInteraction>();
        blacksmith.CloseNPCInteraction();
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentNPC = null;
    }
}