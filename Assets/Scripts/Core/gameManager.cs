using System.Collections.Generic;
using UnityEngine;

// Singleton, żeby mieć łatwy dostęp do zarządzania zasobami i złotem z innych skryptów!
public class gameManager : MonoBehaviour
{
    public static gameManager Instance { get; private set; }

    // Słownik z aktualnymi surowcami w ekwipunku

    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public List<GameObject> savedRackWeapons = new List<GameObject>();

    [Header("System Dni")]
    public int currentDay = 1;

    private void Awake()
    {
        // Jeśli instancja już istnieje i to nie my, zniszcz się
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Zapisz tę instancję jako globalną i nie usuwaj przy zmianie sceny
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Wartości początkowe
        inventory["Copper"] = 3;
        inventory["Bronze"] = 3;
        inventory["Iron"] = 3;
        inventory["SwordHandle"] = 9;
    }


    public void AddResource(string name, int amount)
    {
        if (inventory.ContainsKey(name))
            inventory[name] += amount;
        else
            inventory[name] = amount;
        
        Debug.Log($"Dodano {amount} {name}. Stan: {inventory[name]}");
    }

    public bool RemoveResource(string name, int amount)
    {
        if (!inventory.ContainsKey(name) || inventory[name] < amount)
        {
            Debug.LogWarning($"Brak wystarczającej ilości {name}! (masz: {(inventory.ContainsKey(name) ? inventory[name] : 0)}, potrzeba: {amount})");
            return false;
        }

        // PULA NIELIMITOWANA (Faza 1)
        // inventory[name] -= amount; 
        Debug.Log($"Crafting bez zużycia ({name}). Zasoby testowe.");
        return true;
    }
    
}
