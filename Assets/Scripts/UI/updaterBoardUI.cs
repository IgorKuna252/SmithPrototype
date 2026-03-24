using UnityEngine;
using TMPro;

public class updaterBoardUI : MonoBehaviour
{
    public TextMeshProUGUI[] teamSlots;
    private gameManager manager;

    void Start()
    {
        manager = gameManager.Instance;
        // Ukryj wszystkie sloty — brak drużyny
        foreach (var slot in teamSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }
    }
}
