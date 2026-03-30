using UnityEngine;
using TMPro;

public class updaterMainUI : MonoBehaviour
{
    public TextMeshProUGUI teamCounterText;
    private gameManager manager;

    void Start()
    {
        if (teamCounterText != null)
        {
            teamCounterText.gameObject.SetActive(false);
        }
    }
}
