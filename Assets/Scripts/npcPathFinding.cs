using System;
using UnityEngine;
using UnityEngine.AI;

public class npcPathFinding : MonoBehaviour, IInteractable
{
    NavMeshAgent agentNPC;
    public Transform rejectObject;
    public Transform acceptObject;

    void Start()
    {
        agentNPC = GetComponent<NavMeshAgent>();
        if (agentNPC == null)
        {
            Debug.Log("Couldn't find agent component");
        }
    }

    void SetDestination(Transform target)
    {
        if (target != null)
        {
            agentNPC.SetDestination(target.position);
        }
    }

    public void Interact(KeyCode key)
    {
        if (key == KeyCode.Mouse0)
        {
            SetDestination(rejectObject);
        }
        else if (key == KeyCode.Mouse1)
        {
            SetDestination(acceptObject);
        }
    }
}