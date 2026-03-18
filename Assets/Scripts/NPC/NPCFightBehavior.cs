using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(npcPathFinding))]
[RequireComponent(typeof(NPCCombat))]
public class NPCFightBehavior : MonoBehaviour
{
    [Header("Wykrywanie")]
    public float detectionRadius = 100f;
    public float attackRange = 2f;
    public float checkInterval = 0.25f;

    NavMeshAgent agent;
    NPCCombat combat;
    npcPathFinding pathFinding;
    WeaponSocket weaponSocket;

    Transform currentTarget;
    float checkTimer;
    float defaultStoppingDistance;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        combat = GetComponent<NPCCombat>();
        pathFinding = GetComponent<npcPathFinding>();
        weaponSocket = GetComponent<WeaponSocket>();
    }

    void Start()
    {
        defaultStoppingDistance = agent != null ? agent.stoppingDistance : 0.5f;

        // Wymuszamy minimalny zasięg — Unity serializacja może trzymać stare wartości z prefaba
        if (detectionRadius < 50f)
            detectionRadius = 100f;
    }

    void Update()
    {
        if (!pathFinding.isInTeam) return;
        if (agent == null || !agent.isOnNavMesh) return;

        pathFinding.isManagedByCombat = true;

        bool isArmed = weaponSocket != null && weaponSocket.GetEquippedWeapon() != null;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            FindNearestEnemy();
        }

        if (currentTarget == null)
        {
            LoseTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (dist <= attackRange)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero;
            FaceTarget(currentTarget.position);

            if (isArmed && combat.CurrentMode != NPCCombatMode.Attacking)
                combat.SetMode(NPCCombatMode.Attacking);
        }
        else
        {
            agent.SetDestination(currentTarget.position);

            if (isArmed && combat.CurrentMode != NPCCombatMode.ArmedIdle)
                combat.SetMode(NPCCombatMode.ArmedIdle);
        }
    }

    void FindNearestEnemy()
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        float nearest = float.MaxValue;
        Transform found = null;

        foreach (Enemy e in enemies)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < nearest && d <= detectionRadius)
            {
                nearest = d;
                found = e.transform;
            }
        }

        if (found == currentTarget) return;

        currentTarget = found;

        if (currentTarget == null)
            LoseTarget();
        else
            agent.stoppingDistance = 0f;
    }

    void LoseTarget()
    {
        currentTarget = null;
        if (agent.isOnNavMesh)
            agent.stoppingDistance = defaultStoppingDistance;
        bool hasWeapon = weaponSocket != null && weaponSocket.GetEquippedWeapon() != null;
        combat.SetMode(hasWeapon ? NPCCombatMode.ArmedIdle : NPCCombatMode.Unarmed);
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
