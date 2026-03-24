using UnityEngine;

// Ten skrypt podepniesz pod swój prefab Kupca
public class Merchant : MonoBehaviour, IInteractable
{
    [Header("Sklep Kupca")]
    public string merchantName = "Bob Handlarz";
    
    [Header("Wygląd")]
    [Tooltip("Możesz zmienić kolor w Inspektorze (zostaje nadany na wszystkie dzieci modelu)")]
    public Color merchantColor = new Color(0.8f, 0.6f, 0.2f); // Domyślnie chciwy, kupiecki Złoty!

    void Start()
    {
        // Ta funkcja nadaje nowy, osobny wariant koloru Twojemu kupcowi. 
        // Generuje duplikat materiału dla tego obiektu (material), by nie pomazać oryginalnych NPC 
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer rend in renderers)
        {
            if (rend.material != null)
            {
                // URP często korzysta z _BaseColor, a standardowy shader z _Color - "color" załatwia najprostszy tint.
                rend.material.color = merchantColor;
            }
        }
    }
    
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
