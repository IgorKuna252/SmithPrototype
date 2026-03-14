using UnityEngine;
using TMPro;

public class updaterMainUI : MonoBehaviour
{
    public TextMeshProUGUI teamCounterText;
    private gameManager manager;

    void Start()
    {
        manager = FindObjectOfType<gameManager>();
        updateCounter();
    }

    void Update()
    {
        if (manager.updated)
        {
            updateCounter();
        }
    }

    void updateCounter()
    {
        teamCounterText.text = manager.team.Count + "/" + gameManager.teamSize;
    }
}
