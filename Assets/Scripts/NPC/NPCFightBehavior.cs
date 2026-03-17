using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(npcPathFinding))]
[RequireComponent(typeof(NPCCombat))]
public class NPCFightBehavior : MonoBehaviour
{
    [Header("Wykrywanie")]
    public float detectionRadius = 10f; // w jakiej odlegosci znajduje enemy
    public float attackRange = 2f; // w jakiej odlegosci atakuje enemy
    public float checkInterval = 0.25f; // co ile sprawdza

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
    }

    void Update()
    {
        if (!pathFinding.isInTeam) return;

        bool isArmed = weaponSocket != null && weaponSocket.GetEquippedWeapon() != null;
        if (!isArmed)
        {
            if (currentTarget != null) LoseTarget();
            return;
        }

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            FindNearestEnemy();
        }

        // Target mógł zostać zniszczony między skanami
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

            if (combat.CurrentMode != NPCCombatMode.Attacking)
                combat.SetMode(NPCCombatMode.Attacking);
        }
        else
        {
            agent.SetDestination(currentTarget.position);

            if (combat.CurrentMode != NPCCombatMode.ArmedIdle)
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
            agent.stoppingDistance = 0f; // NPCFightBehavior sam zatrzymuje gdy dist < attackRange
    }

    void LoseTarget()
    {
        currentTarget = null;
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
