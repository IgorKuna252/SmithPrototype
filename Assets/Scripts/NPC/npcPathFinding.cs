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

        bool added = manager.addTeamMember(this.gameObject);
        
        if (added)
        {
            isInTeam = true;
            
            var socket = GetComponentInChildren<WeaponSocket>();
            if (socket != null)
            {
                CitizenData officialData = manager.team[manager.team.Count - 1];
                socket.ownerData = officialData;
                socket.ownerName = officialData.name;

                // Jeśli NPC miał broń przed akceptacją — zapisz ją
                GameObject currentWeapon = socket.GetEquippedWeapon();
                if (currentWeapon != null)
                {
                    FinishedObject finished = currentWeapon.GetComponent<FinishedObject>();
                    if (finished != null)
                        officialData.equippedWeapon = new WeaponData(currentWeapon.name, finished.weaponType, finished.metalTier, finished.bladeLength, finished.flatness);

                    officialData.weaponMeshes = SavedMeshData.SaveFrom(currentWeapon);

                    GameObject clone = Object.Instantiate(currentWeapon);
                    clone.name = currentWeapon.name + "_template";
                    clone.SetActive(false);
                    Object.DontDestroyOnLoad(clone);
                    officialData.savedWeaponTemplate = clone;
                }
                
                Debug.Log($"[Socket] Zaktualizowano na oficjalne dane: {officialData.name}");
            }
            
            // Rozstaw członków drużyny w linii obok acceptObject
            int teamIndex = manager.team.Count - 1;
            Vector3 targetPos = acceptObject.position + acceptObject.right * (teamIndex * 1.5f);
            agentNPC.SetDestination(targetPos);
        }
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
