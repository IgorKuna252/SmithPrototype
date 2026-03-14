using UnityEngine;
using TMPro;

public class updaterBoardUI : MonoBehaviour
{
    public TextMeshProUGUI[] teamSlots;
    private gameManager manager;

    void Start()
    {
        manager = FindObjectOfType<gameManager>();
        updateBoard();
    }

    void Update()
    {
        if (manager.updated)
        {
            updateBoard();
            manager.updated = false;
        }
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
