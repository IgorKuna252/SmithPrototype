using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI invText;

    void Update() 
    {
        // Używamy @ przed stringiem (tzw. verbatim string), 
        // jeśli chcesz pisać tekst w wielu liniach bez używania 
        // ALBO używamy  do nowej linii.
        
        var inv = gameManager.Instance.inventory;
        
        // Opcja 1: Zwykłe użycie 
        invText.text = $"Zasoby:" +
                       $"Copper: {inv["Copper"]}" +
                       $"Bronze: {inv["Bronze"]}" +
                       $"Iron: {inv["Iron"]}" +
                       $"Steel: {inv["Steel"]}" +
                       $"Gold: {inv["Gold"]}" +
                       $"Platinum: {inv["Platinum"]}" +
                       $"BlueSteel: {inv["BlueSteel"]}" +
                       $"Vibranium: {inv["Vibranium"]}";
    }
}