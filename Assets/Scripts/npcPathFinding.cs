using UnityEngine;
using UnityEngine.AI;

public class npcPathFinding : MonoBehaviour
{
    NavMeshAgent agentNPC;
    ExiledCitizen citizenStats;
    gameManager manager;
    public Transform rejectObject;
    public Transform acceptObject;
    [SerializeField] float teamSpacing = 1.5f;
    [HideInInspector] public bool isInTeam = false;

    void Start()
    {
        agentNPC = GetComponent<NavMeshAgent>();
        citizenStats = GetComponent<ExiledCitizen>();
        manager = gameManager.Instance; 

        if (citizenStats == null || agentNPC == null || manager == null)
        {
            Debug.LogWarning("There is no NavMeshAgent or ExiledCitizen attached to " + gameObject.name);
        }
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
        agentNPC.updateRotation = false;
        agentNPC.SetDestination(position);
    }
}