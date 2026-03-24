using UnityEngine;
using TMPro;

public class NPCInteractionUI : MonoBehaviour
{
    public static NPCInteractionUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Statystyki NPC")]
    public TextMeshProUGUI npcStatsText;
    public TextMeshProUGUI npcTaskText;

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
        npcTaskText.text = npc.GetAsssignedTask();
        bool fulfilled = npc.IsTaskFulfilled();
        npcTaskText.text += fulfilled ? "\nTAK" : "\nNIE";
        Debug.Log($"[Task] {npc.GetAsssignedTask()} | {(fulfilled ? "TAK" : "NIE")}\n{npc.GetTaskComparison()}");
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
    }

    public void Hide()
    {
        panel.SetActive(false);
        if (weaponWheel != null) weaponWheel.SetWheel(false);
        currentNPC = null;
    }
}
