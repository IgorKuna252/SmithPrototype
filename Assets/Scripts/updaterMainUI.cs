using UnityEngine;
using TMPro;

public class updaterMainUI : MonoBehaviour
{
    public TextMeshProUGUI teamCounterText;
    private gameManager manager;

    void Start()
    {
        manager = gameManager.Instance;
        manager.OnTeamChanged += updateCounter;
        updateCounter();
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.OnTeamChanged -= updateCounter;
    }

    void updateCounter()
    {
        teamCounterText.text = manager.team.Count + "/" + gameManager.teamSize;
    }
}
