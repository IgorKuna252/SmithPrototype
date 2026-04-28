using UnityEngine;
using TMPro;

public class NPCInteractionUI : MonoBehaviour
{
    public static NPCInteractionUI Instance;

    [Header("Panel")]
    public GameObject panel;

    [Header("Opis zadania")]
    public TextMeshProUGUI taskDescriptionText;

    [Header("Nagroda za zlecenie")]
    public TextMeshProUGUI rewardText;

    [Header("Schemat broni (zadanie)")]
    public WeaponSchemeBuilder taskSchemeBuilder;

    private npcPathFinding currentNPC;
    private AssignedTask currentTask;

    // NPC od którego gracz aktualnie ma przyjęte zadanie (jego schemat widnieje w PlayerUI).
    public static npcPathFinding ActiveTaskNPC { get; private set; }
    private BlacksmithInteraction blacksmith;
    private prefabSpawning queue;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        blacksmith = FindFirstObjectByType<BlacksmithInteraction>();
        queue = FindFirstObjectByType<prefabSpawning>();
    }

    public void Show(npcPathFinding npc)
    {
        currentNPC = npc;
        panel.SetActive(true);

        npc.StopAllCoroutines();
        UnityEngine.AI.NavMeshAgent agent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) { agent.ResetPath(); agent.isStopped = true; }

        AssignedTask task = npc.GetComponent<ExiledCitizen>()?.GetAssignedTask();
        currentTask = task;
        ExiledCitizen citizen = npc.GetComponent<ExiledCitizen>();

        if (taskDescriptionText)
            taskDescriptionText.text = task?.description ?? "";

        if (rewardText && citizen)
            rewardText.text = $"Nagroda: {citizen.rewardResource}";
        else if (rewardText)
            rewardText.text = "";

        if (taskSchemeBuilder)
        {
            bool hasScheme = task != null && task.triangles != null && task.triangles.Length > 0;
            taskSchemeBuilder.gameObject.SetActive(hasScheme);
            if (taskSchemeBuilder.background)
                taskSchemeBuilder.background.SetActive(hasScheme);
            if (hasScheme)
            {
                taskSchemeBuilder.SetTriangles(task.triangles);
                taskSchemeBuilder.color = task.checkMetal ? MetalPiece.GetMetalColor(task.requiredMetal) : Color.white;
            }
        }
    }

    public void Hide()
    {
        panel.SetActive(false);

        if (currentNPC)
        {
            UnityEngine.AI.NavMeshAgent agent = currentNPC.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent) agent.isStopped = false;
        }

        bool hasScheme = currentTask != null && currentTask.triangles != null && currentTask.triangles.Length > 0;
        if (hasScheme)
        {
            ForgeShapeEvaluator evaluator = FindFirstObjectByType<ForgeShapeEvaluator>();
            if (evaluator && evaluator.uiShapeObject)
            {
                WeaponSchemeBuilder evalScheme = evaluator.uiShapeObject.GetComponent<WeaponSchemeBuilder>();
                if (evalScheme)
                {
                    evalScheme.SetTriangles(currentTask.triangles);
                    evalScheme.color = currentTask.checkMetal ? MetalPiece.GetMetalColor(currentTask.requiredMetal) : Color.white;
                }
                evaluator.expectedMetal = currentTask.requiredMetal;
                evaluator.checkMetalColor = currentTask.checkMetal;

                PlayerUIScript playerUI = FindFirstObjectByType<PlayerUIScript>();
                playerUI?.CopyScheme();

                if (ActiveTaskNPC && ActiveTaskNPC != currentNPC)
                    ActiveTaskNPC.SetTaskMarker(false);
                ActiveTaskNPC = currentNPC;
                if (ActiveTaskNPC) ActiveTaskNPC.SetTaskMarker(true);
            }
        }

        currentNPC = null;
        currentTask = null;
    }

    public static void ClearActiveTaskNPC(npcPathFinding npc)
    {
        if (ActiveTaskNPC == npc)
        {
            if (ActiveTaskNPC) ActiveTaskNPC.SetTaskMarker(false);
            ActiveTaskNPC = null;
        }
    }
}
