using UnityEngine;
using TMPro;

public class updaterMainUI : MonoBehaviour
{
    public TextMeshProUGUI teamCounterText;
    private gameManager manager;

    void Start()
    {
        manager = gameManager.Instance;
        manager.OnGoldChanged += updateCounter;
        updateCounter();
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.OnGoldChanged -= updateCounter;
    }

    void updateCounter()
    {
        if (teamCounterText != null)
        {
            teamCounterText.text = "Złoto: " + manager.gold;
        }
    }
}
