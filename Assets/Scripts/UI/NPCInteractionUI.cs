using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCInteractionUI : MonoBehaviour
{
    public static NPCInteractionUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Statystyki NPC")]
    public TextMeshProUGUI npcStatsText;

    [Header("Przyciski")]
    public Button acceptButton;
    public Button rejectButton;

    [Header("Koło broni NPC")]
    public WheelController weaponWheel;

    private npcPathFinding currentNPC;
    private BlacksmithInteraction blacksmith;
    private prefabSpawning queue;
    private WheelController wheel;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        blacksmith = Object.FindFirstObjectByType<BlacksmithInteraction>();
        queue = Object.FindFirstObjectByType<prefabSpawning>();
        wheel = GetComponent<WheelController>();
    }

    public void Show(npcPathFinding npc)
    {
        currentNPC = npc;
        panel.SetActive(true);

        npcStatsText.text = npc.ShowStats();
        wheel.UpdateWheel(npc.GetNormalizedStrength(), npc.GetNormalizedSpeed(), npc.GetNormalizedIntelligence());

        // Koło broni — pokaż tylko jeśli NPC ma broń
        if (weaponWheel != null)
        {
            WeaponData wpn = npc.GetWeaponData();
            if (wpn != null && wpn.type != WeaponType.None)
            {
                weaponWheel.SetWheel(true);
                weaponWheel.UpdateWheel(wpn.GetNormalizedDamage(), wpn.GetNormalizedSpeed(), wpn.GetNormalizedAoE());
            }
            else
            {
                weaponWheel.SetWheel(false);
            }
        }

        if (npc.isInTeam)
        {
            acceptButton.gameObject.SetActive(false);
            rejectButton.gameObject.SetActive(false);
        }
        else
        {
            acceptButton.gameObject.SetActive(true);
            rejectButton.gameObject.SetActive(true);

            bool teamFull = gameManager.Instance.team.Count >= gameManager.teamSize;
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
        if (weaponWheel != null) weaponWheel.SetWheel(false);
        currentNPC = null;
    }
}