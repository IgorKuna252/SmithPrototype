using System.Collections;
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

        {
            // Gdy dotarł do celu — zatrzymaj
            if (!agentNPC.pathPending && agentNPC.hasPath && agentNPC.remainingDistance <= agentNPC.stoppingDistance)
            {
                agentNPC.ResetPath();
                agentNPC.velocity = Vector3.zero;

                // Jeśli celem, do którego właśnie doszliśmy, były drzwi wyjściowe (rejectObject) - wracamy do domu i znikamy z gry!
                if (rejectObject != null && Vector3.Distance(transform.position, rejectObject.position) <= 2.5f)
                {
                    Destroy(gameObject);
                }
            }

            // Brak ścieżki = stój
            if (!agentNPC.hasPath)
            {
                agentNPC.velocity = Vector3.zero;

                // Członek drużyny patrzy w kierunku acceptObject
                if (acceptObject != null)
                    transform.rotation = Quaternion.Slerp(transform.rotation, acceptObject.rotation, Time.deltaTime * 5f);
            }
        }

        float speed = agentNPC.velocity.magnitude;
        if (speed < 0.15f) speed = 0f;
        if (animator != null)
            animator.SetFloat("Speed", speed);
    }

    void SetDestination(Transform target)
    {
        if (target != null)
            agentNPC.SetDestination(target.position);
    }

    public float GetSpeed()             { return citizenStats.GetSpeed(); }
    public float GetIntelligence()      { return citizenStats.GetIntelligence(); }
    public float GetStrengh()           { return citizenStats.GetStrength(); }

    public float GetNormalizedStrength()     { return citizenStats.GetNormalizedStrength(); }
    public float GetNormalizedSpeed()        { return citizenStats.GetNormalizedSpeed(); }
    public float GetNormalizedIntelligence() { return citizenStats.GetNormalizedIntelligence(); }

    public bool IsTaskFulfilled()
    {
        AssignedTask task = citizenStats.GetAssignedTask();
        WeaponData wpn = GetWeaponData();
        if (task == null || wpn == null || !wpn.isValid) return false;
        return task.CheckWeapon(wpn);
    }

    public WeaponData GetWeaponData()
    {
        WeaponSocket socket = GetComponentInChildren<WeaponSocket>();
        return socket?.ownerData?.equippedWeapon;
    }

    public void ProcessTransaction()
    {
        AssignedTask task = citizenStats.GetAssignedTask();
        WeaponData wpn = GetWeaponData();
        GameObject weaponObj = GetWeaponGameObject();

        if (task == null || wpn == null || !wpn.isValid) return;

        // Aktualizujemy schemat UI o trójkąty bieżącego zadania tego NPC
        ForgeShapeEvaluator evaluator = Object.FindFirstObjectByType<ForgeShapeEvaluator>();
        if (evaluator != null && evaluator.uiShapeObject != null)
        {
            WeaponSchemeBuilder scheme = evaluator.uiShapeObject.GetComponent<WeaponSchemeBuilder>();
            if (scheme != null) scheme.SetTriangles(task.triangles);
        }

        float schemeMatch;
        if (task.triangles == null || task.triangles.Length == 0)
        {
            schemeMatch = 1f;
        }
        else if (evaluator != null && weaponObj != null)
        {
            Transform handle = weaponObj.transform.Find("HandlePart");
            if (handle != null) handle.gameObject.SetActive(false);

            schemeMatch = evaluator.EvaluateForgingAccuracy(weaponObj) / 100f;

            if (handle != null) handle.gameObject.SetActive(true);
        }
        else
        {
            schemeMatch = 0f;
        }

        // Oblicz ilość materiału na podstawie dopasowania
        float matchPercent = schemeMatch * 100f;
        int rewardAmount;
        if (matchPercent >= 80f) rewardAmount = 3;
        else if (matchPercent >= 40f) rewardAmount = 2;
        else rewardAmount = 1;

        // Pobierz wylosowany wcześniej materiał od tego NPC
        string rewardMaterial = citizenStats.rewardResource;
        if (string.IsNullOrEmpty(rewardMaterial)) rewardMaterial = "Iron";

        Debug.Log($"[Transakcja] Schemat: {matchPercent:F0}% | Nagroda: {rewardMaterial} x{rewardAmount}");

        if (SilhouetteDebugUI.Instance != null)
        {
            agentNPC.velocity = Vector3.zero;

            SilhouetteDebugUI.Instance.ShowTransaction(matchPercent, rewardMaterial, rewardAmount, () =>
            {
                // Dodaj materiały do EQ gracza
                if (gameManager.Instance != null)
                    gameManager.Instance.AddResource(rewardMaterial, rewardAmount);

                // Spawnuj fizyczne obiekty na warsztacie
                if (ForgeInventorySpawner.Instance != null)
                {
                    MetalType metalType;
                    if (System.Enum.TryParse(rewardMaterial, out metalType))
                    {
                        for (int i = 0; i < rewardAmount; i++)
                            ForgeInventorySpawner.Instance.SpawnNewBoughtMaterial(metalType);
                    }
                }

                var spawner = Object.FindFirstObjectByType<prefabSpawning>();
                if (spawner != null) spawner.OnNPCProcessed();
                
                WeaponAccepted();
            });
        }
        else
        {
            // Fallback bez UI
            if (gameManager.Instance != null)
                gameManager.Instance.AddResource(rewardMaterial, rewardAmount);

            var spawner = Object.FindFirstObjectByType<prefabSpawning>();
            if (spawner != null) spawner.OnNPCProcessed();
            WeaponAccepted();
        }
    }

    public void MoveToQueuePosition(Vector3 position)
    {
        agentNPC.updateRotation = true;
        agentNPC.SetDestination(position);
    }

    public void WeaponAccepted(float waitTime = 2f)
    {
        StartCoroutine(WeaponAcceptedRoutine(waitTime));
    }

    IEnumerator WeaponAcceptedRoutine(float waitTime)
    {
        agentNPC.ResetPath();
        agentNPC.velocity = Vector3.zero;
        yield return new WaitForSeconds(waitTime);
        SetDestination(rejectObject);
    }
    
    public GameObject GetWeaponGameObject()
    {
        WeaponSocket socket = GetComponentInChildren<WeaponSocket>();
        return socket?.GetEquippedWeapon();
    }
}
