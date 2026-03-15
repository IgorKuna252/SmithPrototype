using UnityEngine;
using TMPro; // Upewnij się, że masz to na górze

public class TileManager : MonoBehaviour
{
    public static TileManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject fightButton; // PRZECIĄGNIJ TU GUZIK WALCZ W INSPEKTORZE

    private Tile selectedTile;

    void Awake() => Instance = this;

    public void OpenTileUI(Tile tile)
    {
        selectedTile = tile;
        uiPanel.SetActive(true);

        Debug.Log($"Otwieram panel. Czy pole jest zajęte (isOwned)? {tile.isOwned}");

        if (tile.isOwned)
        {
            statusText.text = "Zająłeś już ten teren";
            fightButton.SetActive(false); 
            Debug.Log("Ukrywam guzik Walka!");
        }
        else
        {
            statusText.text = "Walka!";
            fightButton.SetActive(true); 
            Debug.Log("Pokazuję guzik Walka!");
        }
    }

    public void Fight()
    {
        if (selectedTile != null)
        {
            selectedTile.isOwned = true;
            selectedTile.UpdateVisuals();
            CloseUI(); // Używamy dedykowanej metody
        }
    }

    // Dodaj to, żebyś mógł użyć tej samej metody w guziku Anuluj
    public void CloseUI()
    {
        uiPanel.SetActive(false);
    }
}