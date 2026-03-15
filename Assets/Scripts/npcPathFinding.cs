using UnityEngine;
using UnityEngine.AI;

public class npcPathFinding : MonoBehaviour
{
    NavMeshAgent agentNPC;
    ExiledCitizen citizenStats;
    gameManager manager;
    public Transform rejectObject;
    public Transform acceptObject;

    void Start()
    {
        agentNPC = GetComponent<NavMeshAgent>();
        citizenStats = GetComponent<ExiledCitizen>();
        manager = FindObjectOfType<gameManager>();
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
        SetDestination(acceptObject);
        manager.addTeamMember(this.gameObject);
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