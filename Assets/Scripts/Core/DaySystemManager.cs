using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DaySystemManager : MonoBehaviour
{
    [Header("UI Tablicy Planowania")]
    public TextMeshProUGUI dayText;
    // Opcjonalne okienko podsumowania, przypisz w Inspektorze
    public GameObject summaryWindow; 

    [Header("Obiekty do odblokowania (Dzień 2+)")]
    public GameObject crucibleObject; // Przeciągnij CrucibleParent
    public GameObject moldManagerObject; // Przeciągnij MoldManager

    void Start()
    {
        // 1. Ustawienie tekstu Dnia
        if (gameManager.Instance != null && dayText != null)
        {
            dayText.text = $"Dzień: {gameManager.Instance.currentDay}";
        }

        // 2. Blokowanie obiektów na 1 dniu
        UpdateWorkshopProgression();

        if (summaryWindow != null)
            summaryWindow.SetActive(false);

        // Zapamiętywanie zawartości stojaka! Odtwarzamy bronie jeśli jakieś przeżyły noc:
        if (gameManager.Instance != null && gameManager.Instance.savedRackWeapons.Count > 0)
        {
            WeaponRack[] racks = FindObjectsByType<WeaponRack>(FindObjectsSortMode.None);
            System.Array.Sort(racks, (a, b) => a.name.CompareTo(b.name));

            int i = 0;
            foreach (GameObject wObj in gameManager.Instance.savedRackWeapons)
            {
                if (wObj != null && i < racks.Length)
                {
                    FinishedObject w = wObj.GetComponent<FinishedObject>();
                    if (w != null) racks[i].PlaceWeapon(w);
                    i++;
                }
            }
            
            // Wyczyść po załadowaniu
            gameManager.Instance.savedRackWeapons.Clear();
        }
    }

    [Header("Nagrody (podłącz teksty z 2 przycisków wyboru)")]
    public TextMeshProUGUI reward1Text;
    public TextMeshProUGUI reward2Text;

    private string[] rewardPool = { "AxeHandle", "SwordHandle", "Steel", "Gold", "Platinum", "BlueSteel", "Vibranium" };
    private string currentChoice1;
    private string currentChoice2;

    private void UpdateWorkshopProgression()
    {
        // Awaryjne szukanie obiektów po nazwach ze sceny
        if (crucibleObject == null) crucibleObject = GameObject.Find("CrucibleParent");
        if (moldManagerObject == null) moldManagerObject = GameObject.Find("MoldManager");

        // Tymczasowe całkowite wyłączenie pieców odlewniczych (wiaderko itp) na 1 dzień 
        // (ZAKOMENTOWANE NA CZAS TESTÓW - żeby stół nie znikał przy włączaniu gry!)
        // if (crucibleObject != null) crucibleObject.SetActive(false);
        // if (moldManagerObject != null) moldManagerObject.SetActive(false);
    }

    public void EndDayButton()
    {
        Debug.Log("[DaySystemManager] Kończymy dzień...");
        
        // --- LOSOWANIE NAGRÓD ---
        currentChoice1 = rewardPool[Random.Range(0, rewardPool.Length)];
        do {
            currentChoice2 = rewardPool[Random.Range(0, rewardPool.Length)];
        } while (currentChoice1 == currentChoice2);

        if (reward1Text != null) reward1Text.text = FormatRewardObj(currentChoice1);
        if (reward2Text != null) reward2Text.text = FormatRewardObj(currentChoice2);
        // ------------------------

        if (summaryWindow != null) summaryWindow.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (BlacksmithInteraction.Instance != null) BlacksmithInteraction.Instance.enabled = false;
    }

    private string FormatRewardObj(string rewardName)
    {
        if (rewardName == "AxeHandle") return "Długi trzonek x 3";
        if (rewardName == "SwordHandle") return "Krótki trzonek x 3";
        return $"Sztabka {rewardName} x 3";
    }

    public void SelectReward1AndNextDay() 
    { 
        GiveReward(currentChoice1); 
        GoToNextDay(); 
    }
    
    public void SelectReward2AndNextDay() 
    { 
        GiveReward(currentChoice2); 
        GoToNextDay(); 
    }

    private void GiveReward(string reward)
    {
        if (gameManager.Instance != null)
        {
            gameManager.Instance.AddResource(reward, 3);
        }
    }

    public void GoToNextDay()
    {
        // PAMIĘĆ O STOJAKACH Z BRONIAMI:
        // Przed resetem sceny zdejmujemy bronie, uodparniamy na destrukcję sceny i pakujemy do GameManagera
        if (gameManager.Instance != null)
        {
            WeaponRack[] racks = FindObjectsByType<WeaponRack>(FindObjectsSortMode.None);
            System.Array.Sort(racks, (a, b) => a.name.CompareTo(b.name));

            gameManager.Instance.savedRackWeapons.Clear();

            foreach (var rack in racks)
            {
                if (!rack.IsEmpty())
                {
                    FinishedObject w = rack.TakeWeapon();
                    w.transform.SetParent(null);
                    DontDestroyOnLoad(w.gameObject);
                    gameManager.Instance.savedRackWeapons.Add(w.gameObject);
                }
            }

            gameManager.Instance.currentDay++;
            Debug.Log($"[DaySystemManager] Ładowanie Dnia: {gameManager.Instance.currentDay}");
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
