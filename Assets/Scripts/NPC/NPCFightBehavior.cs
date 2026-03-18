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

    // Debug — logujemy co sekundę żeby nie spamować
    float debugTimer = 0f;
    const float DEBUG_INTERVAL = 1f;

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

        string status = $"[FightBehavior] {gameObject.name}: ";
        status += agent == null ? "❌ BRAK NavMeshAgent | " : "✅ NavMeshAgent | ";
        status += (agent != null && agent.isOnNavMesh) ? "✅ Na NavMesh | " : "❌ NIE na NavMesh | ";
        status += pathFinding.isInTeam ? "✅ isInTeam | " : "❌ NIE w drużynie | ";
        
        bool armed = weaponSocket != null && weaponSocket.GetEquippedWeapon() != null;
        status += armed ? "✅ Uzbrojony | " : "⚠️ Bez broni | ";
        
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        status += $"Wrogów na scenie: {enemies.Length}";
        
        Debug.Log(status);
    }

    bool ShouldLog()
    {
        debugTimer -= Time.deltaTime;
        if (debugTimer <= 0f)
        {
            debugTimer = DEBUG_INTERVAL;
            return true;
        }
        return false;
    }

    void Update()
    {
        bool log = ShouldLog();

        if (!pathFinding.isInTeam)
        {
            if (log) Debug.Log($"[FB] {gameObject.name}: STOP — nie w drużynie");
            return;
        }
        if (agent == null)
        {
            if (log) Debug.Log($"[FB] {gameObject.name}: STOP — agent == null");
            return;
        }
        if (!agent.isOnNavMesh)
        {
            if (log) Debug.Log($"[FB] {gameObject.name}: STOP — agent NIE na NavMesh");
            return;
        }

        pathFinding.isManagedByCombat = true;

        bool isArmed = weaponSocket != null && weaponSocket.GetEquippedWeapon() != null;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            FindNearestEnemy(log);
        }

        if (currentTarget == null)
        {
            if (log) Debug.Log($"[FB] {gameObject.name}: currentTarget == null → LoseTarget");
            LoseTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (log)
        {
            Debug.Log($"[FB] {gameObject.name}: target={currentTarget.name}, dist={dist:F1}, attackRange={attackRange}, armed={isArmed}, agentSpeed={agent.speed}, agentVelocity={agent.velocity.magnitude:F2}, hasPath={agent.hasPath}, pathPending={agent.pathPending}");
        }

        if (dist <= attackRange)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero;
            FaceTarget(currentTarget.position);

            if (isArmed && combat.CurrentMode != NPCCombatMode.Attacking)
            {
                if (log) Debug.Log($"[FB] {gameObject.name}: W zasięgu! Ustawiam ATTACK");
                combat.SetMode(NPCCombatMode.Attacking);
            }
        }
        else
        {
            bool setDest = agent.SetDestination(currentTarget.position);
            if (log) Debug.Log($"[FB] {gameObject.name}: Za daleko, SetDestination → {setDest}");

            if (isArmed && combat.CurrentMode != NPCCombatMode.ArmedIdle)
                combat.SetMode(NPCCombatMode.ArmedIdle);
        }
    }

    void FindNearestEnemy(bool log)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        if (log) Debug.Log($"[FB] {gameObject.name}: FindNearestEnemy — znaleziono {enemies.Length} wrogów");

        float nearest = float.MaxValue;
        Transform found = null;

        foreach (Enemy e in enemies)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (log) Debug.Log($"[FB]   → {e.name} dystans: {d:F1} (max: {detectionRadius})");

            if (d < nearest && d <= detectionRadius)
            {
                nearest = d;
                found = e.transform;
            }
        }

        if (log) Debug.Log($"[FB]   Wynik: found={found?.name ?? "NULL"}, currentTarget={currentTarget?.name ?? "NULL"}");

        if (found == currentTarget) return;

        currentTarget = found;

        if (currentTarget == null)
            LoseTarget();
        else
        {
            agent.stoppingDistance = 0f;
            if (log) Debug.Log($"[FB]   Nowy cel: {currentTarget.name}!");
        }
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

