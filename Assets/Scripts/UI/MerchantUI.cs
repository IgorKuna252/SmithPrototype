using UnityEngine;
using TMPro;

public class MerchantUI : MonoBehaviour
{
    public static MerchantUI Instance;

    [Header("Główny Panel Kupca")]
    public GameObject panel;
    public TextMeshProUGUI merchantNameText;

    private Merchant currentMerchant;
    private PlayerMovement playerMovement;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        // Pozwólmy graczowi wyjść ze sklepu pod przyciskiem ESC
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void Show(Merchant merchant)
    {
        currentMerchant = merchant;

        if (merchantNameText != null)
            merchantNameText.text = merchant.merchantName;

        panel.SetActive(true);

        // Zablokowanie ruchu gracza i odblokowanie myszki
        if (playerMovement != null) playerMovement.enabled = false;
        
        // Magiczny skrypt z BlacksmithInteraction.Instance zjadałby nam Inputa, więc warto ustawić flagę public isInteractingWithNPC, ale dla uproszczenia tylko kursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentMerchant = null;

        // Odblokowanie gracza
        if (playerMovement != null) playerMovement.enabled = true;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Zabezpieczenie przed błędem z `BlacksmithInteraction` gdyby tamtejszy skrypt nadal coś czytał
        if (BlacksmithInteraction.Instance != null)
        {
            // Można ewentualnie odblokować jakąś flagę jeżeli użyliście jej dla Kupca
        }
    }

    // --- Metody do podpięcia pod przyciski (Buttons) w Unity UI --- //
    
    public void BuyIron()
    {
        Debug.Log("Sklep usunięty - nie można nic kupić.");
    }

    public void BuyCopper()
    {
        Debug.Log("Sklep usunięty - nie można nic kupić.");
    }

    // Dodaj więcej funkcji pod kolejne surowce lub zrób jedną uniwersalną metodę
}
