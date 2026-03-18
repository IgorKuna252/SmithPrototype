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

    private List<GameObject> npcQueue = new List<GameObject>();
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
            npcQueue.Add(obj);
        }

        // 6. Odtwórz członków drużyny jeśli wracamy z innej sceny
        RespawnTeamMembers();
    }

    /// <summary>
    /// Odtwarza NPC drużynowe z gameManager.team przy powrocie do MainScene.
    /// </summary>
    void RespawnTeamMembers()
    {
        var gm = gameManager.Instance;
        if (gm == null || gm.team.Count == 0) return;

        Vector3 basePos = targetNPCAccept != null ? targetNPCAccept.position : Vector3.zero;

        for (int i = 0; i < gm.team.Count; i++)
        {
            CitizenData data = gm.team[i];

            // Spawnuj NPC obok acceptObject
            Vector3 pos = basePos + Vector3.right * (i * queueSpacing);
            GameObject obj = Instantiate(prefabObject, pos, Quaternion.identity);
            obj.name = data.name;

            // Odtwórz statystyki
            ExiledCitizen citizen = obj.GetComponent<ExiledCitizen>();
            citizen.health       = data.health;
            citizen.maxHealth    = data.maxHealth;
            citizen.strength     = data.strength;
            citizen.intelligence = data.intelligence;
            citizen.speed        = data.speed;
            citizen.equippedWeaponName = data.equippedWeaponName;

            // Oznacz jako członka drużyny
            npcPathFinding npc = obj.GetComponent<npcPathFinding>();
            npc.isInTeam = true;
            npc.rejectObject = targetNPCReject;
            npc.acceptObject = targetNPCAccept;

            // Podłącz WeaponSocket do danych drużyny
            WeaponSocket socket = obj.GetComponentInChildren<WeaponSocket>();
            if (socket != null)
            {
                socket.ownerData = data;
                socket.ownerName = data.name;

                // Odtwórz broń z zapisanego klonu
                if (data.savedWeaponTemplate != null)
                {
                    GameObject weapon = Instantiate(data.savedWeaponTemplate);
                    weapon.SetActive(true);
                    weapon.name = data.equippedWeaponName;
                    socket.EquipWeapon(weapon);
                }
            }
        }
    }

    public void OnNPCProcessed()
    {
        if (npcQueue.Count == 0) return;

        // Usuń pierwszego (przetworzonego) NPC z listy
        npcQueue.RemoveAt(0);

        // Przesuń pozostałych nie-drużynowych na nowe pozycje
        RepositionQueue();
    }

    /// <summary>
    /// Jawne usunięcie konkretnego NPC z kolejki (np. po zniszczeniu).
    /// </summary>
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
            if (npc.isInTeam) continue;

            if (index < queuePositions.Count)
            {
                npc.MoveToQueuePosition(queuePositions[index]);
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