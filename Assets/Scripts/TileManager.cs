using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject fightButton;
    
    [Header("Team Cards")]
    [SerializeField] private Transform cardContainer; // Kontener z Horizontal Layout Group
    [SerializeField] private GameObject cardPrefab;    // Prefab Twojej karty

    private List<GameObject> activeCards = new List<GameObject>();
    private Tile selectedTile;

    void Awake() => Instance = this;

    public void OpenTileUI(Tile tile)
    {
        selectedTile = tile;
        uiPanel.SetActive(true);

        foreach (var card in activeCards) Destroy(card);
        activeCards.Clear();

        if (tile.isOwned)
        {
            statusText.text = "Zająłeś już ten teren";
            fightButton.SetActive(false);
        }
        else
        {
            statusText.text = "Twoja drużyna:";
            fightButton.SetActive(true);

            foreach (var member in gameManager.Instance.team)
            {
                Debug.Log($"Tworzę kartę dla: {member.name}"); // TEST 1
                GameObject newCard = Instantiate(cardPrefab, cardContainer);
            
                TextMeshProUGUI cardText = newCard.GetComponentInChildren<TextMeshProUGUI>();
            
                if (cardText != null)
                {
                    cardText.text = member.GetStats();
                    Debug.Log($"Znaleziono tekst na karcie, ustawiam: {member.GetStats()}"); // TEST 2
                }
                else
                {
                    Debug.LogError("BŁĄD: Nie znaleziono TextMeshProUGUI w prefabie karty!"); // TEST 3
                }
            
                activeCards.Add(newCard);
            }
        }
    }

    public void Fight()
    {
        Debug.Log("Walka rozpoczęta!");
        selectedTile.isOwned = true;
        selectedTile.UpdateVisuals();
        CloseUI();
    }

    public void CloseUI() => uiPanel.SetActive(false);
}