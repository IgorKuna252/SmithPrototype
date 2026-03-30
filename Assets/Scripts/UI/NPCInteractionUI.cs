using UnityEngine;
using TMPro;

public class NPCInteractionUI : MonoBehaviour
{
    public static NPCInteractionUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Opis zadania")]
    public TextMeshProUGUI taskDescriptionText;

    [Header("Schemat broni (zadanie)")]
    public WeaponSchemeBuilder taskSchemeBuilder;

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

        AssignedTask task = npc.GetComponent<ExiledCitizen>()?.GetAssignedTask();
        WeaponData wpn = npc.GetWeaponData();

        if (taskDescriptionText != null)
            taskDescriptionText.text = task?.description ?? "";

        if (taskSchemeBuilder != null && task != null)
            taskSchemeBuilder.SetTriangles(task.triangles);

        wheel.UpdateWheel(npc.GetNormalizedStrength(), npc.GetNormalizedSpeed(), npc.GetNormalizedIntelligence());

        if (weaponWheel != null)
        {
            if (wpn != null && wpn.isValid)
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
