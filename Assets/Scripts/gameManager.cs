using System.Collections.Generic;
using UnityEngine;

public class gameManager : MonoBehaviour
{
    public List<CitizenData> team = new List<CitizenData>();
    public const int teamSize = 4;
    public bool updated = false;

    public bool addTeamMember(GameObject npc)
    {
        if (team.Count >= teamSize)
        {
            Debug.Log("Team is full!");
            return false;
        }

        ExiledCitizen citizen = npc.GetComponent<ExiledCitizen>();
        if (citizen == null)
        {
            Debug.LogWarning("No ExiledCitizen on " + npc.name);
            return false;
        }

        team.Add(new CitizenData(npc.name, citizen));
        updated = true;
        return true;
    }

    public bool removeTeamMember(int index)
    {
        if (index >= 0 && index < team.Count)
        {
            team.RemoveAt(index);
            updated = true;
            return true;
        }

        Debug.Log("Invalid team index!");
        return false;
    }

    public CitizenData getMember(int index)
    {
        if (index >= 0 && index < team.Count)
            return team[index];
        return null;
    }
}
