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

    [Header("Wskaźnik aktywnego zadania")]
    [Tooltip("Obiekt (np. ikona nad głową) zapalany gdy gracz przyjmie zadanie od tego NPC.")]
    public GameObject taskMarker;

    public void SetTaskMarker(bool active)
    {
        if (taskMarker) taskMarker.SetActive(active);
    }

    void Start()
    {
        agentNPC = GetComponent<NavMeshAgent>();
        citizenStats = GetComponent<ExiledCitizen>();
        animator = GetComponentInChildren<Animator>();
        manager = gameManager.Instance;

        if (animator != null)
            animator.applyRootMotion = false;

        if (taskMarker) taskMarker.SetActive(false);

        if (agentNPC != null)
            agentNPC.stoppingDistance = 0.5f;

        if (citizenStats == null || agentNPC == null || manager == null)
        {
            Debug.LogWarning("There is no NavMeshAgent or ExiledCitizen attached to " + gameObject.name);
        }
    }

    void Update()
    {
        if (!agentNPC) return;
        {
            if (!agentNPC.pathPending && agentNPC.hasPath && agentNPC.remainingDistance <= agentNPC.stoppingDistance)
            {
                agentNPC.ResetPath();
                agentNPC.velocity = Vector3.zero;
                agentNPC.updateRotation = false; // Skrypt przejmuje obrót → patrzenie w okienko

                // Jeśli celem, do którego właśnie doszliśmy, były drzwi wyjściowe (rejectObject)
                if (rejectObject && Vector3.Distance(transform.position, rejectObject.position) <= 2.5f)
                {
                    NPCInteractionUI.ClearActiveTaskNPC(this);
                    Destroy(gameObject);
                }
            }

            // Jak brak ścieżki to stój
            if (!agentNPC.hasPath)
            {
                agentNPC.velocity = Vector3.zero;

                // NPC patrzy W STRONĘ acceptObject (okienka), nie kopiuje jego rotacji
                if (acceptObject)
                {
                    Vector3 dir = acceptObject.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                }
            }
        }

        float speed = agentNPC.velocity.magnitude;
        if (speed < 0.15f) speed = 0f;
        if (animator)
            animator.SetFloat("Speed", speed);
    }

    void SetDestination(Transform target)
    {
        if (target)
            agentNPC.SetDestination(target.position);
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
        ForgeShapeEvaluator evaluator = FindFirstObjectByType<ForgeShapeEvaluator>();
        if (evaluator && evaluator.uiShapeObject)
        {
            WeaponSchemeBuilder scheme = evaluator.uiShapeObject.GetComponent<WeaponSchemeBuilder>();
            if (scheme)
            {
                scheme.SetTriangles(task.triangles);
                scheme.color = task.checkMetal ? MetalPiece.GetMetalColor(task.requiredMetal) : Color.white;
            }
            evaluator.expectedMetal = task.requiredMetal;
            evaluator.checkMetalColor = task.checkMetal;
        }

        bool noScheme = task.triangles == null || task.triangles.Length == 0;
        float schemeMatch;
        if (noScheme)
        {
            schemeMatch = 1f;
        }
        else if (evaluator != null && weaponObj != null)
        {
            Transform handle = weaponObj.transform.Find("HandlePart");
            if (handle) handle.gameObject.SetActive(false);

            schemeMatch = evaluator.EvaluateForgingAccuracy(weaponObj) / 100f;

            if (handle) handle.gameObject.SetActive(true);
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

        if (SilhouetteDebugUI.Instance)
        {
            StopAllCoroutines();
            agentNPC.ResetPath();
            agentNPC.velocity = Vector3.zero;
            agentNPC.isStopped = true;

            SilhouetteDebugUI.Instance.ShowTransaction(matchPercent, rewardMaterial, rewardAmount, noScheme, () =>
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
                if (spawner != null) spawner.OnNPCProcessed(gameObject);

                if (this != null)
                {
                    agentNPC.isStopped = false;
                    WeaponAccepted(0f);
                }
            });
        }
        else
        {
            if (gameManager.Instance)
                gameManager.Instance.AddResource(rewardMaterial, rewardAmount);

            var spawner = FindFirstObjectByType<prefabSpawning>();
            if (spawner) spawner.OnNPCProcessed(gameObject);
            WeaponAccepted();
        }
    }

    public void MoveToQueuePosition(Vector3 position)
    {
        if (agentNPC == null) agentNPC = GetComponent<NavMeshAgent>();
        if (agentNPC == null) return;
        agentNPC.updateRotation = true;
        agentNPC.SetDestination(position);
    }

    public void WeaponAccepted(float waitTime = 2f)
    {
        NPCInteractionUI.ClearActiveTaskNPC(this);
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
