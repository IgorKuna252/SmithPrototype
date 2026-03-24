using UnityEngine;

// Ten skrypt podepniesz pod swój prefab Kupca
public class Merchant : MonoBehaviour, IInteractable
{
    [Header("Sklep Kupca")]
    public string merchantName = "Bob Handlarz";

    public bool Interact()
    {
        Debug.Log($"[{merchantName}] Otwieram sklep! Witaj w dzień.");
        
        // Pokaż dedykowane UI dla Kupca, tak jak to robiłeś dla klientów w NPCInteractionUI
        if (MerchantUI.Instance != null)
        {
            MerchantUI.Instance.Show(this);
        }
        else
        {
            Debug.LogError("Brak MerchantUI na scenie! Stwórz panel i podepnij skrypt MerchantUI.");
        }

        return true; 
    }
}
