using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject fightButton;
    [SerializeField] private TextMeshProUGUI teamStrengthText;
    
    [Header("Team Selection")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;

    private List<GameObject> activeCards = new List<GameObject>();
    private List<Toggle> currentToggles = new List<Toggle>(); 
    private Tile selectedTile;

    void Awake() => Instance = this;

    public void OpenTileUI(Tile tile)
    {
        selectedTile = tile;
        uiPanel.SetActive(true);

        // Czyścimy poprzednie karty i listy
        foreach (var card in activeCards) Destroy(card);
        activeCards.Clear();
        currentToggles.Clear();

        // Przygotowujemy wspólny tekst trudności
        string difficultyString = $"Poziom trudności: {tile.difficulty}\n";

        if (tile.isOwned)
        {
            statusText.text = difficultyString + "Zająłeś już ten teren";
            fightButton.SetActive(false);
            teamStrengthText.text = ""; // Ukrywamy siłę, bo nie walczymy
        }
        else
        {
            statusText.text = difficultyString + "Walka! Wybierz drużynę:";
            fightButton.SetActive(true);

            foreach (var member in gameManager.Instance.team)
            {
                GameObject newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponentInChildren<TextMeshProUGUI>().text = member.GetStats();
                
                Toggle t = newCard.GetComponentInChildren<Toggle>();
                // Dodajemy nasłuchiwanie kliknięcia checkboxa
                t.onValueChanged.AddListener(delegate { UpdateTeamStrength(); });
                
                currentToggles.Add(t);
                activeCards.Add(newCard);
            }
            
            UpdateTeamStrength();
        }
    }

    public void UpdateTeamStrength()
    {
        float totalStrength = 0;
        int minCount = Mathf.Min(currentToggles.Count, gameManager.Instance.team.Count);

        for (int i = 0; i < minCount; i++)
        {
            if (currentToggles[i].isOn)
            {
                // Teraz poprawnie bierzemy dane z listy gameManagera używając indeksu 'i'
                var member = gameManager.Instance.team[i];
                totalStrength += (member.health + member.strength + member.intelligence + member.speed);
            }
        }
        teamStrengthText.text = $"Siła Drużyny: {totalStrength:F0}";
    }
    
    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel; // Nowy panel z wynikiem
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultDescriptionText;

    public void Fight()
    {
        float totalStrength = 0;
    
        // Obliczamy sumę statystyk
        for (int i = 0; i < currentToggles.Count; i++)
        {
            if (currentToggles[i].isOn)
            {
                var member = gameManager.Instance.team[i];
                // Tu zsumuj dokładnie to, co chcesz:
                totalStrength += (member.health + member.strength + member.intelligence + member.speed);
            }
        }

        // DEBUG: Wypiszmy to na ekranie/w konsoli przed porównaniem
        Debug.Log($"WALKA: Siła drużyny: {totalStrength} | Trudność kafelka: {selectedTile.difficulty}");

        CloseUI();
        resultPanel.SetActive(true);

        if (totalStrength >= selectedTile.difficulty)
        {
            // WYGRANA
            int randomAmount = Random.Range(1, 6); // 1 do 5
            string[] resources = { "żelazo", "srebro", "złoto" };
            string randomResource = resources[Random.Range(0, resources.Length)];

            resultTitleText.text = "Wygrałeś!";
            resultDescriptionText.text = $"Otrzymujesz {randomAmount} jednostek surowca: {randomResource}";
        
            // Oznaczamy kafelek jako wygrany
            selectedTile.isOwned = true;
            selectedTile.UpdateVisuals();
        }
        else
        {
            // PRZEGRANA
            resultTitleText.text = "Przegrałeś!";
            resultDescriptionText.text = "Nie wystarczyło sił, aby zdobyć ten teren.";
        }
    }

    public void CloseUI() => uiPanel.SetActive(false);
}