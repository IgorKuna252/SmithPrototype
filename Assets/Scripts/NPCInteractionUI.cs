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
    private BlacksmithInteraction blacksmith;
    private prefabSpawning queue;
    private gameManager manager;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        blacksmith = FindObjectOfType<BlacksmithInteraction>();
        queue = FindObjectOfType<prefabSpawning>();
        manager = FindObjectOfType<gameManager>();
    }

    public void Show(npcPathFinding npc)
    {
        currentNPC = npc;
        panel.SetActive(true);

        npcStatsText.text = npc.ShowStats();

        if (npc.isInTeam)
        {
            npcNameText.text = "Członek drużyny";
            acceptButton.gameObject.SetActive(false);
            rejectButton.gameObject.SetActive(false);
        }
        else
        {
            npcNameText.text = "Wędrowiec";
            acceptButton.gameObject.SetActive(true);
            rejectButton.gameObject.SetActive(true);

            bool teamFull = manager.team.Count >= gameManager.teamSize;
            acceptButton.interactable = !teamFull;

            acceptButton.onClick.RemoveAllListeners();
            rejectButton.onClick.RemoveAllListeners();

            acceptButton.onClick.AddListener(OnAccept);
            rejectButton.onClick.AddListener(OnReject);
        }
    }

    void OnAccept()
    {
        currentNPC.Accept();
        queue.OnNPCProcessed();
        blacksmith.CloseNPCInteraction();
    }

    void OnReject()
    {
        currentNPC.Reject();
        queue.OnNPCProcessed();
        blacksmith.CloseNPCInteraction();
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentNPC = null;
    }
}