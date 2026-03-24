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
    [HideInInspector] public bool isInTeam = false;
    [HideInInspector] public bool isManagedByCombat = false; // NPCFightBehavior ustawia to na true

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

        // Jeśli walka kontroluje tego NPC — nie ingeruj w ruch!
        if (!isManagedByCombat)
        {
            // Gdy dotarł do celu — zatrzymaj
            if (!agentNPC.pathPending && agentNPC.hasPath && agentNPC.remainingDistance <= agentNPC.stoppingDistance)
            {
                agentNPC.ResetPath();
                agentNPC.velocity = Vector3.zero;
            }

            // Brak ścieżki = stój
            if (!agentNPC.hasPath)
            {
                agentNPC.velocity = Vector3.zero;

                // Członek drużyny patrzy w kierunku acceptObject
                if (isInTeam && acceptObject != null)
                    transform.rotation = Quaternion.Slerp(transform.rotation, acceptObject.rotation, Time.deltaTime * 5f);
            }
        }

        // Animacja prędkości — tylko gdy walka nie zarządza NPC
        if (!isManagedByCombat)
        {
            float speed = agentNPC.velocity.magnitude;
            if (speed < 0.15f) speed = 0f;

            if (animator != null)
                animator.SetFloat("Speed", speed);
        }
    }

    void SetDestination(Transform target)
    {
        if (target != null)
            agentNPC.SetDestination(target.position);
    }

    public void Accept()
    {
        if (isInTeam) return;

        isInTeam = true;
        SetDestination(acceptObject);
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

    public float GetSpeed()             { return citizenStats.GetSpeed(); }
    public float GetIntelligence()      { return citizenStats.GetIntelligence(); }
    public float GetStrengh()           { return citizenStats.GetStrength(); }

    public float GetNormalizedStrength()     { return citizenStats.GetNormalizedStrength(); }
    public float GetNormalizedSpeed()        { return citizenStats.GetNormalizedSpeed(); }
    public float GetNormalizedIntelligence() { return citizenStats.GetNormalizedIntelligence(); }

    public string GetAsssignedTask()
    {
        AssignedTask task = citizenStats.GetAssignedTask();
        if (task == null) return "Brak danych";
        return task.description;
    }

    public string GetTaskRequirements()
    {
        AssignedTask task = citizenStats.GetAssignedTask();
        if (task == null) return "Brak wymagań";
        return task.GetRequirementsText();
    }

    public string GetTaskComparison()
    {
        AssignedTask task = citizenStats.GetAssignedTask();
        WeaponData wpn = GetWeaponData();
        if (task == null) return "Brak tasku";
        if (wpn == null || wpn.type == WeaponType.None) return "Brak broni";

        var sb = new System.Text.StringBuilder();
        if (task.requiredDamage >= 0f)
        {
            float has = wpn.GetNormalizedDamage();
            bool ok = has >= task.requiredDamage;
            sb.AppendLine($"DMG: {has:F0}% / {task.requiredDamage:F0}% {(ok ? "OK" : "X")}");
        }
        if (task.requiredSpeed >= 0f)
        {
            float has = wpn.GetNormalizedSpeed();
            bool ok = has >= task.requiredSpeed;
            sb.AppendLine($"SPD: {has:F0}% / {task.requiredSpeed:F0}% {(ok ? "OK" : "X")}");
        }
        if (task.requiredAoe >= 0f)
        {
            float has = wpn.GetNormalizedAoE();
            bool ok = has >= task.requiredAoe;
            sb.AppendLine($"AOE: {has:F0}% / {task.requiredAoe:F0}% {(ok ? "OK" : "X")}");
        }
        return sb.ToString().TrimEnd();
    }

    public bool IsTaskFulfilled()
    {
        AssignedTask task = citizenStats.GetAssignedTask();
        WeaponData wpn = GetWeaponData();
        if (task == null || wpn == null || wpn.type == WeaponType.None) return false;
        return task.CheckWeapon(wpn);
    }

    public WeaponData GetWeaponData()
    {
        WeaponSocket socket = GetComponentInChildren<WeaponSocket>();
        return socket?.ownerData?.equippedWeapon;
    }

    public void MoveToQueuePosition(Vector3 position)
    {
        agentNPC.updateRotation = true;
        agentNPC.SetDestination(position);
    }
}
