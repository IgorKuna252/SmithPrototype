using UnityEngine;
using TMPro;

public class updaterBoardUI : MonoBehaviour
{
    public TextMeshProUGUI[] teamSlots;
    private gameManager manager;

    void Start()
    {
        manager = gameManager.Instance;
        manager.OnTeamChanged += updateBoard;
        updateBoard();
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.OnTeamChanged -= updateBoard;
    }

    void updateBoard()
    {
        for (int i = 0; i < teamSlots.Length; i++)
        {
            if (teamSlots[i] == null) continue;

            if (i < manager.team.Count)
            {
                teamSlots[i].gameObject.SetActive(true);
                teamSlots[i].text = manager.team[i].GetStats();
            }
            else
            {
                teamSlots[i].gameObject.SetActive(false);
            }
        }
    }
}
