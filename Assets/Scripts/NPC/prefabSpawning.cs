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
        // Sprawdzamy czy mamy cykl Dnia i Nocy, by nasłuchiwać zmian
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNightStarted += SpawnNightCustomers;
            DayNightManager.Instance.OnDayStarted += SpawnDayMerchant;
            
            // Reakcja natychmiastowa na stan przy załadowaniu gry (żeby ktoś stał po kliknięciu Play)
            if (DayNightManager.Instance.isDay)
                SpawnDayMerchant();
            else
                SpawnNightCustomers();
        }
        else
        {
            CalculateQueuePositions(customerCount);
            Debug.LogWarning("Brak DayNight Managera (TimeManager)! Pobieram stary system spawnu.");
            SpawnNightCustomers();
        }
    }

    void OnDestroy()
    {
        // Usunięcie powiązań z pamięci na wypadek zamknięcia sceny
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNightStarted -= SpawnNightCustomers;
            DayNightManager.Instance.OnDayStarted -= SpawnDayMerchant;
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

            GameObject obj = Instantiate(customerPrefab, queuePositions[i], Quaternion.identity);
            obj.name = $"Klient_Nocny_{i + 1}";

            SetupCitizenData(obj);
            npcQueue.Add(obj);
        }
    }

    private void SpawnDayMerchant()
    {
        ClearCurrentQueue();
        // Dla kupca wystarczy jedna pozycja
        CalculateQueuePositions(1); 

        if (merchantPrefab != null)
        {
            GameObject obj = Instantiate(merchantPrefab, queuePositions[0], Quaternion.identity);
            obj.name = "Kupiec_Poranny_1";

            // Tutaj później wyłapiesz go, i podepniesz jego GUI/Skrypt
            // Obecnie zachowuje się nawigacyjnie jak zwykły NPC (wchodzi i stoi przed Tobą)
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
            // Założenie bazujące na Twoim wcześniejszym kodzie - jeśli ma isinTeam to go nie bierzemy
            if (npc != null && npc.isInTeam) continue;

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
