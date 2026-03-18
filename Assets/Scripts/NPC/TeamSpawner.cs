using UnityEngine;

/// <summary>
/// Spawnuje członków drużyny z gameManager.team w scenie walki.
/// Przypisuje im zapisane statsy, broń (z klonu) i oznacza jako isInTeam.
/// </summary>
public class TeamSpawner : MonoBehaviour
{
    [SerializeField] GameObject npcPrefab;
    [SerializeField] float spacing = 1.5f;

    void Start()
    {
        var gm = gameManager.Instance;
        var team = gm.team;
        var selected = gm.selectedFighters;

        // Jeśli nie wybrano nikogo (np. debug), spawnuj całą drużynę
        if (selected.Count == 0)
        {
            for (int i = 0; i < team.Count; i++)
                selected.Add(i);
        }

        for (int s = 0; s < selected.Count; s++)
        {
            int teamIndex = selected[s];
            if (teamIndex < 0 || teamIndex >= team.Count) continue;

            CitizenData data = team[teamIndex];
            Vector3 pos = transform.position + transform.right * (s * spacing);

            GameObject obj = Instantiate(npcPrefab, pos, transform.rotation);
            obj.name = data.name;

            // 1. Odtwórz statystyki
            ExiledCitizen citizen = obj.GetComponent<ExiledCitizen>();
            citizen.health       = data.health;
            citizen.maxHealth    = data.maxHealth;
            citizen.strength     = data.strength;
            citizen.intelligence = data.intelligence;
            citizen.speed        = data.speed;
            citizen.equippedWeaponName = data.equippedWeaponName;

            // 2. Oznacz jako członka drużyny
            npcPathFinding npc = obj.GetComponent<npcPathFinding>();
            npc.isInTeam = true;

            // 3. Przypisz ownerData do WeaponSocket
            WeaponSocket socket = obj.GetComponentInChildren<WeaponSocket>();
            if (socket != null)
            {
                socket.ownerData = data;
                socket.ownerName = data.name;

                // 4. Odtwórz broń z zapisanego klonu (customowa broń gracza)
                if (data.savedWeaponTemplate != null)
                {
                    GameObject weapon = Instantiate(data.savedWeaponTemplate);
                    weapon.SetActive(true);
                    weapon.name = data.equippedWeaponName;
                    SavedMeshData.RestoreTo(weapon, data.weaponMeshes);
                    socket.EquipWeapon(weapon);

                    // 5. Ustaw tryb walki
                    NPCCombat combat = obj.GetComponent<NPCCombat>();
                    if (combat != null)
                        combat.SetMode(NPCCombatMode.ArmedIdle);
                }
            }
        }
    }
}
