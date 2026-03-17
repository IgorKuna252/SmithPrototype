using UnityEngine;

/// <summary>
/// Spawnuje członków drużyny z gameManager.team w scenie walki.
/// Przypisuje im zapisane statsy i oznacza jako isInTeam.
/// </summary>
public class TeamSpawner : MonoBehaviour
{
    [SerializeField] GameObject npcPrefab;
    [SerializeField] float spacing = 1.5f;

    void Start()
    {
        var team = gameManager.Instance.team;

        for (int i = 0; i < team.Count; i++)
        {
            CitizenData data = team[i];
            Vector3 pos = transform.position + transform.right * (i * spacing);

            GameObject obj = Instantiate(npcPrefab, pos, transform.rotation);

            ExiledCitizen citizen = obj.GetComponent<ExiledCitizen>();
            citizen.health      = data.health;
            citizen.maxHealth   = data.maxHealth;
            citizen.strength    = data.strength;
            citizen.intelligence = data.intelligence;
            citizen.speed       = data.speed;

            npcPathFinding npc = obj.GetComponent<npcPathFinding>();
            npc.isInTeam = true;
        }
    }
}
