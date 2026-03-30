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

    private string type2CurrentRandom;
    private string[] type2Pool = { "Steel", "Gold", "Platinum", "BlueSteel", "Vibranium" };

    private void UpdateWorkshopProgression()
    {
        // Awaryjne szukanie obiektów po nazwach ze sceny
        if (crucibleObject == null) crucibleObject = GameObject.Find("CrucibleParent");
        if (moldManagerObject == null) moldManagerObject = GameObject.Find("MoldManager");

        bool hasFoundry = gameManager.Instance != null && gameManager.Instance.unlockedFoundry;
        
        if (crucibleObject != null) crucibleObject.SetActive(hasFoundry);
        if (moldManagerObject != null) moldManagerObject.SetActive(hasFoundry);
    }

    public void EndDayButton()
    {
        Debug.Log("[DaySystemManager] Kończymy dzień...");
        
        if (gameManager.Instance != null)
        {
            if (reward1Text != null) reward1Text.text = GenerateType1RewardText();
            if (reward2Text != null) reward2Text.text = GenerateType2RewardText();
        }

        if (summaryWindow != null) summaryWindow.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (BlacksmithInteraction.Instance != null) BlacksmithInteraction.Instance.enabled = false;
    }

    private string GenerateType1RewardText()
    {
        int p = gameManager.Instance.type1Progress;
        if (p == 0) return "Odblokuj: Odlewnia, Wiadro, Odlew Miecza";
        if (p == 1) return "Długi trzonek x 3";
        if (p == 2) return "Odblokuj: Odlew Siekiery";
        if (p == 3) return "Krótki trzonek x 3";
        
        if (p % 2 == 0) return "Długi trzonek x 3";
        return "Krótki trzonek x 3";
    }

    private string GenerateType2RewardText()
    {
        int p = gameManager.Instance.type2Progress;
        if (p == 0) return "Sztabki Steel x 3";
        if (p == 1) return "Sztabki Gold x 3";
        if (p == 2) return "Sztabki Platinum x 3";
        if (p == 3) return "Sztabki BlueSteel x 3";
        if (p == 4) return "Sztabki Vibranium x 3";
        
        type2CurrentRandom = type2Pool[Random.Range(0, type2Pool.Length)];
        return $"Sztabki {type2CurrentRandom} x 3";
    }

    public void SelectReward1AndNextDay() 
    { 
        GiveType1Reward(); 
        GoToNextDay(); 
    }
    
    public void SelectReward2AndNextDay() 
    { 
        GiveType2Reward(); 
        GoToNextDay(); 
    }

    private void GiveType1Reward()
    {
        if (gameManager.Instance == null) return;
        
        int p = gameManager.Instance.type1Progress;
        if (p == 0) gameManager.Instance.unlockedFoundry = true;
        else if (p == 1) gameManager.Instance.AddResource("AxeHandle", 3);
        else if (p == 2) gameManager.Instance.unlockedAxeMold = true;
        else if (p == 3) gameManager.Instance.AddResource("SwordHandle", 3);
        else
        {
            if (p % 2 == 0) gameManager.Instance.AddResource("AxeHandle", 3);
            else gameManager.Instance.AddResource("SwordHandle", 3);
        }
        
        gameManager.Instance.type1Progress++;
    }

    private void GiveType2Reward()
    {
        if (gameManager.Instance == null) return;
        
        int p = gameManager.Instance.type2Progress;
        string res = "";
        if (p == 0) res = "Steel";
        else if (p == 1) res = "Gold";
        else if (p == 2) res = "Platinum";
        else if (p == 3) res = "BlueSteel";
        else if (p == 4) res = "Vibranium";
        else res = type2CurrentRandom;

        if (!string.IsNullOrEmpty(res))
        {
            gameManager.Instance.AddResource(res, 3);
        }
        
        gameManager.Instance.type2Progress++;
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
