using System.Collections;
using UnityEngine;

public class prefabSpawning : MonoBehaviour
{
    [Header("Klienci (Noc)")]
    [SerializeField] GameObject customerPrefab;
    [SerializeField] int customerCount = 5;

    [Header("Sloty (3 widoczni klienci na raz)")]
    [SerializeField] int slotCount = 3;
    [SerializeField] float slotSpacing = 1.5f;
    [SerializeField] float respawnDelay = 2f;
    [Tooltip("Odstęp między pojawianiem się kolejnych NPC na początku nocy")]
    [SerializeField] float initialSpawnDelay = 1.5f;

    [Header("Punkty Poruszania")]
    [SerializeField] Transform spawnObject;          // środek slotów (gdzie NPC stoją obsługiwani)
    [SerializeField] Transform spawnEntryPoint;      // miejsce wejścia — stąd NPC przychodzą do slotu
    [SerializeField] Transform targetNPCReject;
    [SerializeField] Transform targetNPCAccept;

    private GameObject[] slots;
    private Vector3[] slotPositions;
    private int spawnedCount = 0;

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnShopOpened += SpawnNightCustomers;
        }
        else
        {
            Debug.LogWarning("Brak DayNight Managera! Klienci nie będą się pojawiać.");
        }
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnShopOpened -= SpawnNightCustomers;
        }
    }

    void CalculateSlotPositions()
    {
        slotPositions = new Vector3[slotCount];
        Vector3 origin = spawnObject.position;
        for (int i = 0; i < slotCount; i++)
        {
            slotPositions[i] = origin + spawnObject.right * (i * slotSpacing);
        }
    }

    public void ClearCurrentQueue()
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = null;
        }

        ExiledCitizen[] allCitizens = Object.FindObjectsByType<ExiledCitizen>(FindObjectsSortMode.None);
        foreach (var c in allCitizens)
        {
            if (c != null) Destroy(c.gameObject);
        }

        Merchant[] allMerchants = Object.FindObjectsByType<Merchant>(FindObjectsSortMode.None);
        foreach (var m in allMerchants)
        {
            if (m != null) Destroy(m.gameObject);
        }
    }

    private void SpawnNightCustomers()
    {
        ClearCurrentQueue();
        CalculateSlotPositions();

        slots = new GameObject[slotCount];
        spawnedCount = 0;

        StartCoroutine(StaggeredInitialSpawn());
    }

    private IEnumerator StaggeredInitialSpawn()
    {
        int initial = Mathf.Min(slotCount, customerCount);
        for (int i = 0; i < initial; i++)
        {
            SpawnIntoSlot(i);
            if (i < initial - 1)
                yield return new WaitForSeconds(initialSpawnDelay);
        }
    }

    private void SpawnIntoSlot(int slotIndex)
    {
        if (customerPrefab == null) return;
        if (spawnedCount >= customerCount) return;

        Vector3 entryPos = spawnEntryPoint != null ? spawnEntryPoint.position : slotPositions[slotIndex];
        GameObject obj = Instantiate(customerPrefab, entryPos, Quaternion.Euler(0, -90, 0));
        obj.name = $"Klient_Nocny_{spawnedCount + 1}";

        SetupCitizenData(obj);
        slots[slotIndex] = obj;
        spawnedCount++;

        // NPC idzie z punktu wejścia do swojego slotu
        npcPathFinding npc = obj.GetComponent<npcPathFinding>();
        if (npc != null) npc.MoveToQueuePosition(slotPositions[slotIndex]);
    }

    private void SetupCitizenData(GameObject obj)
    {
        ExiledCitizen citizen = obj.GetComponent<ExiledCitizen>();
        npcPathFinding npc = obj.GetComponent<npcPathFinding>();
        WeaponSocket socket = obj.GetComponentInChildren<WeaponSocket>();

        if (citizen != null)
        {
            citizen.GenerateRandomStats();
            if (TaskManager.Instance != null)
                citizen.task = TaskManager.Instance.GetRandomTask();

            if (gameManager.Instance != null)
                citizen.rewardResource = gameManager.Instance.GetRandomUnlockedMaterial();

            CitizenData tempData = new CitizenData(obj.name, citizen);

            if (socket != null)
            {
                socket.ownerData = tempData;
                socket.ownerName = obj.name;
            }
        }

        if (npc != null)
        {
            npc.rejectObject = targetNPCReject;
            npc.acceptObject = targetNPCAccept;
        }
    }

    public void OnNPCProcessed(GameObject npc)
    {
        if (slots == null || npc == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == npc)
            {
                FreeSlot(i);
                return;
            }
        }
    }

    private void FreeSlot(int slotIndex)
    {
        slots[slotIndex] = null;
        if (spawnedCount < customerCount)
        {
            StartCoroutine(RespawnAfterDelay(slotIndex));
        }
    }

    private IEnumerator RespawnAfterDelay(int slotIndex)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (slots != null && slotIndex < slots.Length && slots[slotIndex] == null)
        {
            SpawnIntoSlot(slotIndex);
        }
    }
}
