using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

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
    Animator animator;

    Transform currentTarget;
    float checkTimer;
    float defaultStoppingDistance;
    Vector3 prevPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        combat = GetComponent<NPCCombat>();
        pathFinding = GetComponent<npcPathFinding>();
        weaponSocket = GetComponentInChildren<WeaponSocket>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        defaultStoppingDistance = agent != null ? agent.stoppingDistance : 0.5f;
        prevPosition = transform.position;

        // Wymuszamy minimalny zasięg — Unity serializacja może trzymać stare wartości z prefaba
        if (detectionRadius < 50f)
            detectionRadius = 100f;
    }

    void Update()
    {
        if (!pathFinding.isInTeam) return;
        if (agent == null || !agent.isOnNavMesh) return;

        bool isArmed = weaponSocket != null && weaponSocket.GetEquippedWeapon() != null;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            FindNearestEnemy();
        }

        if (currentTarget == null)
        {
            pathFinding.isManagedByCombat = false;
            LoseTarget();
            return;
        }

        pathFinding.isManagedByCombat = true;

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        float currentRange = GetCurrentAttackRange();

        if (dist <= currentRange)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero;
            FaceTarget(currentTarget.position);

            if (isArmed && combat.CurrentMode != NPCCombatMode.Attacking)
                combat.SetMode(NPCCombatMode.Attacking);
        }
        else
        {
            if (Vector3.Distance(agent.destination, currentTarget.position) > 0.5f)
                agent.SetDestination(currentTarget.position);

            if (isArmed && combat.CurrentMode != NPCCombatMode.ArmedIdle)
                combat.SetMode(NPCCombatMode.ArmedIdle);
        }

        // Animacja biegu — liczymy z faktycznej zmiany pozycji, bo agent.velocity może być 0
        if (animator != null && Time.deltaTime > 0f)
        {
            float spd = Vector3.Distance(transform.position, prevPosition) / Time.deltaTime;
            if (spd < 0.15f) spd = 0f;
            animator.SetFloat("Speed", spd);
        }
        prevPosition = transform.position;
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

        if (currentTarget != null)
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

    float GetCurrentAttackRange()
    {
        WeaponData weapon = weaponSocket?.ownerData?.equippedWeapon;
        if (weapon != null && weapon.type != WeaponType.None)
            return weapon.GetRange();
        return attackRange;
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
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? GetCurrentAttackRange() : attackRange);
    }
}
