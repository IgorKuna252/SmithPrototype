using System.Collections.Generic;
using UnityEngine;

public class prefabSpawning : MonoBehaviour
{
    [SerializeField] GameObject prefabObject;
    [SerializeField] int spawnedCount = 5;
    [SerializeField] Transform spawnObject;
    [SerializeField] Transform targetNPCReject;
    [SerializeField] Transform targetNPCAccept;
    [SerializeField] float queueSpacing = 1.5f;

    private Queue<GameObject> npcQueue = new Queue<GameObject>();
    private List<Vector3> queuePositions = new List<Vector3>();

    void Start()
    {
        Vector3 origin = spawnObject.position;

        for (int i = 0; i < spawnedCount; i++)
        {
            queuePositions.Add(origin + spawnObject.right * (i * queueSpacing));

            GameObject obj = Instantiate(prefabObject, queuePositions[i], Quaternion.identity);

            // 1. Pobieramy komponenty RAZ
            ExiledCitizen citizen = obj.GetComponent<ExiledCitizen>();
            npcPathFinding npc = obj.GetComponent<npcPathFinding>();
            WeaponSocket socket = obj.GetComponentInChildren<WeaponSocket>();

            // 2. Logika danych
            citizen.GenerateRandomStats();
            CitizenData tempData = new CitizenData(obj.name, citizen);

            // 3. Przypisanie danych do socketa
            if (socket != null)
            {
                socket.ownerData = tempData;
                socket.ownerName = obj.name;
            }
            
            // 4. Konfiguracja NPC
            npc.rejectObject = targetNPCReject;
            npc.acceptObject = targetNPCAccept;

            // 5. Dodanie do kolejki
            npcQueue.Enqueue(obj);
        }
    }

    public void OnNPCProcessed()
    {
        if (npcQueue.Count == 0) return;

        npcQueue.Dequeue();

        int index = 0;
        foreach (GameObject npcObj in npcQueue)
        {
            npcPathFinding npc = npcObj.GetComponent<npcPathFinding>();
            // Uwaga: upewnij się, że isInTeam jest poprawnie aktualizowane
            if (npc.isInTeam) continue; 
            npc.MoveToQueuePosition(queuePositions[index]);
            index++;
        }
    }

    public GameObject GetFirstInQueue()
    {
        if (npcQueue.Count == 0) return null;
        return npcQueue.Peek();
    }
}