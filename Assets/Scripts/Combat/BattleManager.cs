using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Zarządza przebiegiem bitwy: monitoruje stan walki,
/// wyświetla wynik (wygrana/przegrana) i wraca do mapy.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("UI Wyniku")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultTitleText;
    [SerializeField] TextMeshProUGUI resultDescriptionText;
    [SerializeField] GameObject returnButton;

    [Header("Ustawienia")]
    [SerializeField] string mapSceneName = "MapScene";
    [SerializeField] float checkInterval = 0.5f; // Co ile sprawdza stan bitwy

    private float checkTimer;
    private bool battleEnded = false;

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Odblokuj kursor dla UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (battleEnded) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            CheckBattleState();
        }
    }

    void CheckBattleState()
    {
        // Szukamy żywych wrogów i żywych sojuszników
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        ExiledCitizen[] allies = Object.FindObjectsByType<ExiledCitizen>(FindObjectsSortMode.None);

        // Filtrujemy — liczymy tylko sojuszników, którzy są w drużynie i żyją
        int aliveAllies = 0;
        foreach (var ally in allies)
        {
            if (ally.health > 0f)
                aliveAllies++;
        }

        if (enemies.Length == 0)
        {
            // WYGRANA — wszyscy wrogowie pokonani!
            EndBattle(true);
        }
        else if (aliveAllies == 0 && allies.Length == 0)
        {
            // PRZEGRANA — wszyscy sojusznicy polegli (zostali zniszczeni)
            EndBattle(false);
        }
    }

    void EndBattle(bool victory)
    {
        battleEnded = true;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (victory)
        {
            // Losowa nagroda za wygraną
            int randomAmount = Random.Range(1, 6);
            string[] resources = { "Copper", "Bronze", "Iron", "Steel", "Gold", "Platinum", "BlueSteel", "Vibranium" };
            string randomResource = resources[Random.Range(0, resources.Length)];

            gameManager.Instance.AddResource(randomResource, randomAmount);

            // Oznacz kafelek jako zdobyty (po nazwie — przetrwa zmianę scen)
            string tileName = gameManager.Instance.currentBattleTileName;
            if (!string.IsNullOrEmpty(tileName))
                gameManager.Instance.ownedTiles.Add(tileName);

            if (resultTitleText != null)
                resultTitleText.text = "Zwycięstwo!";
            if (resultDescriptionText != null)
                resultDescriptionText.text = $"Zdobyłeś teren!\nNagroda: {randomAmount}x {randomResource}";

            Debug.Log($"[BattleManager] WYGRANA! Nagroda: {randomAmount}x {randomResource}");
        }
        else
        {
            if (resultTitleText != null)
                resultTitleText.text = "Porażka!";
            if (resultDescriptionText != null)
                resultDescriptionText.text = "Twoi wojownicy polegli w walce.";

            Debug.Log("[BattleManager] PRZEGRANA!");
        }

        // Synchronizuj statystyki ocalałych z powrotem do gameManager
        SyncSurvivorsBack();
    }

    void SyncSurvivorsBack()
    {
        // Brak drużyny do synchronizacji
    }

    /// <summary>
    /// Podepnij pod przycisk "Powrót do mapy" w UI.
    /// </summary>
    public void ReturnToMap()
    {
        // Wyczyść dane bitwy
        gameManager.Instance.selectedFighters.Clear();
        gameManager.Instance.currentBattleTileName = null;

        SceneManager.LoadScene(mapSceneName);
    }
}
