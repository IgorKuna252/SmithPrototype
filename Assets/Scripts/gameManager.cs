using System;
using UnityEngine;
using TMPro;
public class gameManager : MonoBehaviour
{
    public GameObject[] team;
    public const int teamSize = 4;
    public TextMeshProUGUI teamCounterText;
    
    
    
    void Start()
    {
        team = Array.Empty<GameObject>();
        syncCounter();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool addTeamMember(GameObject npc)
    {
        if(team.Length < teamSize)
        {
            team[team.Length] = npc;
            syncCounter();
            return true;
        }
        else
        {
            Debug.Log("Team is full!");
            return false;
        }
    }

    public bool removeTeamMember(GameObject npc)
    {
        if(team.Length > 0)
        {
            for(int i = 0; i < team.Length; i++)
            {
                if(team[i] == npc)
                {
                    team[i] = null;
                    return true;
                }
            }
            Debug.Log("NPC not found in team!");
            return false;
        }
        else
        {
            Debug.Log("Team is empty!");
            return false;
        }
    }

    void syncCounter()
    {
        teamCounterText.text = team.Length.ToString()+"/"+teamSize.ToString();
    }
}
