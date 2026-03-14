using UnityEngine;
using UnityEngine.AI;

public class npcPathFinding : MonoBehaviour, IInteractable
{
    NavMeshAgent agentNPC;
    ExiledCitizen citizenStats;
    WeaponIKHandler weaponIK;
    public Transform rejectObject;
    public Transform acceptObject;

    void Start()
    {
        agentNPC = GetComponent<NavMeshAgent>();
        citizenStats = GetComponent<ExiledCitizen>();
        weaponIK = GetComponent<WeaponIKHandler>();
        if (citizenStats == null || agentNPC == null)
        {
            Debug.LogWarning("There is no NavMeshAgent or ExiledCitizen attached to " + gameObject.name);
        }
    }

    /// <summary>
    /// Przekaż obiekt NPC-owi do trzymania przez IK.
    /// </summary>
    public void GiveItem(GameObject item)
    {
        if (weaponIK == null)
            weaponIK = GetComponent<WeaponIKHandler>();

        if (weaponIK == null)
        {
            Debug.LogWarning("Brak WeaponIKHandler na " + gameObject.name);
            return;
        }
        weaponIK.SetHeldObject(item);
    }

    /// <summary>
    /// Zabierz obiekt z rąk NPC.
    /// </summary>
    public void TakeItem()
    {
        if (weaponIK != null)
            weaponIK.ClearHeldObject();
    }

    void SetDestination(Transform target)
    {
        if (target != null)
            agentNPC.SetDestination(target.position);
    }

    public bool Interact(KeyCode key)
    {
        if (key == KeyCode.Mouse0)
            SetDestination(rejectObject);
        else if (key == KeyCode.Mouse1)
            SetDestination(acceptObject);
        return true;
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