using System.Collections.Generic;
using UnityEngine;

public class prefabSpawning : MonoBehaviour
{
    [Header("Klienci (Noc)")]
    [SerializeField] GameObject customerPrefab;
    [SerializeField] int customerCount = 5;

    [Header("Kupiec (Dzień)")]
    [Tooltip("Póki co możesz dać tu prefab zwykłego klienta, później zrobimy mu Osobny Skrypt i GUI Kupca")]
    [SerializeField] GameObject merchantPrefab;

    [Header("Punkty Poruszania")]
    [SerializeField] Transform spawnObject;
    [SerializeField] Transform targetNPCReject;
    [SerializeField] Transform targetNPCAccept;
    [SerializeField] float queueSpacing = 1.5f;

    private List<GameObject> npcQueue = new List<GameObject>();
    private List<Vector3> queuePositions = new List<Vector3>();

    void Start()
    {
        // Klienci pojawiają się TYLKO gdy gracz otworzy warsztat (tabliczka)
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

    void CalculateQueuePositions(int maxCount)
    {
        queuePositions.Clear();
        Vector3 origin = spawnObject.position;
        for (int i = 0; i < maxCount; i++)
        {
            queuePositions.Add(origin + spawnObject.right * (i * queueSpacing));
        }
    }

    public void ClearCurrentQueue()
    {
        npcQueue.Clear();

        // Niszczymy absolutnie każdego klienta NPC błąkającego się po mapie
        ExiledCitizen[] allCitizens = Object.FindObjectsByType<ExiledCitizen>(FindObjectsSortMode.None);
        foreach (var c in allCitizens)
        {
            if (c != null) Destroy(c.gameObject);
        }

        // To samo robimy z Kupcami
        Merchant[] allMerchants = Object.FindObjectsByType<Merchant>(FindObjectsSortMode.None);
        foreach (var m in allMerchants)
        {
            if (m != null) Destroy(m.gameObject);
        }
    }

    private void SpawnNightCustomers()
    {
        ClearCurrentQueue();
        CalculateQueuePositions(customerCount);

        for (int i = 0; i < customerCount; i++)
        {
            if (customerPrefab == null) break;

            GameObject obj = Instantiate(customerPrefab, queuePositions[i], Quaternion.Euler(0, -90, 0));
            obj.name = $"Klient_Nocny_{i + 1}";

            SetupCitizenData(obj);
            npcQueue.Add(obj);
        }
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
            
            // Losowanie nagrody z puli odblokowanych materiałów
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

    public void OnNPCProcessed()
    {
        if (npcQueue.Count == 0) return;
        npcQueue.RemoveAt(0);
        RepositionQueue();
    }
    
    public void RemoveNPC(GameObject npc)
    {
        npcQueue.Remove(npc);
        RepositionQueue();
    }

    void RepositionQueue()
    {
        int index = 0;
        foreach (GameObject npcObj in npcQueue)
        {
            if (npcObj == null) continue;

            npcPathFinding npc = npcObj.GetComponent<npcPathFinding>();

            if (index < queuePositions.Count)
            {
                if (npc != null) npc.MoveToQueuePosition(queuePositions[index]);
                index++;
            }
        }
    }

    public GameObject GetFirstInQueue()
    {
        if (npcQueue.Count == 0) return null;
        return npcQueue[0];
    }
}
