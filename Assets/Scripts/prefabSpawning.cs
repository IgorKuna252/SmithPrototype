using UnityEngine;

public class prefabSpawning : MonoBehaviour
{
    [SerializeField] GameObject prefabObject;
    private static int spawnedCount = 5; 
    [SerializeField] GameObject[] spawnedObjects;
    [SerializeField] Transform spawnObject;
    [SerializeField] Transform targetNPCReject;
    [SerializeField] Transform targetNPCAccept;
    void Start()
    {
        spawnedObjects = new GameObject[spawnedCount];
        for (int i = 0; i < spawnedCount; i++)
        {
            Vector3 spawnPosition = spawnObject.transform.position;
            spawnedObjects[i] = Instantiate(prefabObject, spawnPosition, Quaternion.identity);
            npcPathFinding npc = spawnedObjects[i].GetComponent<npcPathFinding>();
            npc.rejectObject = targetNPCReject.transform;
            npc.acceptObject = targetNPCAccept.transform;
            spawnObject.transform.position += new Vector3(1.5f, 0, 0);
        }
    }
}
