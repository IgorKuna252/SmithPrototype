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
        
        // Dynamiczne wyliczenie punktacji na żywo ("czy klient to lubi")
        AssignedTask task = npc.GetComponent<ExiledCitizen>()?.GetAssignedTask();
        WeaponData wpn = npc.GetWeaponData();
        
        if (task != null && wpn != null && wpn.type != WeaponType.None)
        {
            float completion = task.CalculateTaskCompletion(wpn);
            int percent = Mathf.RoundToInt(completion * 100f);
            
            // Kolorujemy tekst dla fajnego efektu - na zielono przy 100%
            string colorHex = percent >= 100 ? "#00FF00" : (percent > 50 ? "#FFFF00" : "#FF0000");
            npcTaskText.text += $"\nZgodność: <color={colorHex}>{percent}%</color>";
        }
        else
        {
            npcTaskText.text += "\n[Brak założonej wytycznej lub brak wręczonego przedmiotu]";
        }
        
        Debug.Log($"[Task] {npc.GetAsssignedTask()}\n{npc.GetTaskComparison()}");
        wheel.UpdateWheel(npc.GetNormalizedStrength(), npc.GetNormalizedSpeed(), npc.GetNormalizedIntelligence());

        // Koło broni — pokaż tylko jeśli NPC ma broń
        if (weaponWheel != null)
        {
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
