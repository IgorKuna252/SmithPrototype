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
        invText.text = $"Zasoby:\n" +
                       $"Copper: {inv["Copper"]}\n" +
                       $"Bronze: {inv["Bronze"]}\n" +
                       $"Iron: {inv["Iron"]}\n" +
                       $"Steel: {inv["Steel"]}\n" +
                       $"Gold: {inv["Gold"]}\n" +
                       $"Platinum: {inv["Platinum"]}\n" +
                       $"BlueSteel: {inv["BlueSteel"]}\n" +
                       $"Vibranium: {inv["Vibranium"]}";
    }
}