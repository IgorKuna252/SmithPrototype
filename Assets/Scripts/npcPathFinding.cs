using UnityEngine;
using UnityEngine.AI;

public class npcPathFinding : MonoBehaviour
{
    NavMeshAgent agentNPC;
    ExiledCitizen citizenStats;
    gameManager manager;
    Animator animator;
    public Transform rejectObject;
    public Transform acceptObject;
    [SerializeField] float teamSpacing = 1.5f;
    [HideInInspector] public bool isInTeam = false;

    void Start()
    {
        agentNPC = GetComponent<NavMeshAgent>();
        citizenStats = GetComponent<ExiledCitizen>();
        animator = GetComponentInChildren<Animator>();
        manager = gameManager.Instance;

        if (animator != null)
            animator.applyRootMotion = false;

        if (agentNPC != null)
            agentNPC.stoppingDistance = 0.5f;

        if (citizenStats == null || agentNPC == null || manager == null)
        {
            Debug.LogWarning("There is no NavMeshAgent or ExiledCitizen attached to " + gameObject.name);
        }
    }

    void Update()
    {
        if (agentNPC == null) return;

        // Gdy dotarł do celu — zatrzymaj
        if (!agentNPC.pathPending && agentNPC.hasPath && agentNPC.remainingDistance <= agentNPC.stoppingDistance)
        {
            agentNPC.ResetPath();
            agentNPC.velocity = Vector3.zero;
        }

        // Brak ścieżki = stój
        if (!agentNPC.hasPath)
            agentNPC.velocity = Vector3.zero;

        float speed = agentNPC.velocity.magnitude;
        if (speed < 0.15f) speed = 0f;

        if (animator != null)
            animator.SetFloat("Speed", speed);
    }

    void SetDestination(Transform target)
    {
        if (target != null)
            agentNPC.SetDestination(target.position);
    }

    public void Accept()
    {
        int index = manager.team.Count;
        Vector3 offset = acceptObject.right * (index * teamSpacing);
        agentNPC.SetDestination(acceptObject.position + offset);
        manager.addTeamMember(this.gameObject);
        isInTeam = true;
    }

    public void Reject()
    {
        SetDestination(rejectObject);
    }

    public string ShowStats()
    {
        if (citizenStats == null) return "Brak danych";
        return citizenStats.GetStats();
    }

    public void MoveToQueuePosition(Vector3 position)
    {
        agentNPC.updateRotation = true;
        agentNPC.SetDestination(position);
    }
}
