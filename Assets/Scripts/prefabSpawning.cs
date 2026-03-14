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

    [Header("Obiekt do trzymania przez NPC (opcjonalny)")]
    [SerializeField] GameObject holdingObject;

    private Queue<GameObject> npcQueue = new Queue<GameObject>();
    private List<Vector3> queuePositions = new List<Vector3>();

    void Start()
    {
        Vector3 origin = spawnObject.position;

        for (int i = 0; i < spawnedCount; i++)
        {
            queuePositions.Add(origin + spawnObject.right * (i * queueSpacing));

            GameObject obj = Instantiate(prefabObject, queuePositions[i], Quaternion.identity);

            npcPathFinding npc = obj.GetComponent<npcPathFinding>();
            npc.rejectObject = targetNPCReject;
            npc.acceptObject = targetNPCAccept;

            ExiledCitizen citizen = obj.GetComponent<ExiledCitizen>();
            citizen.GenerateRandomStats();

            if (holdingObject != null)
            {
                GameObject item = Instantiate(holdingObject);
                npc.GiveItem(item);
            }

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
            npcObj.GetComponent<npcPathFinding>().MoveToQueuePosition(queuePositions[index]);
            index++;
        }
    }

    public GameObject GetFirstInQueue()
    {
        if (npcQueue.Count == 0) return null;
        return npcQueue.Peek();
    }
}