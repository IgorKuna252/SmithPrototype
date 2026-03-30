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

        inventory[name] -= amount; 
        Debug.Log($"Zużyto {amount} {name}. Pozostało w EQ: {inventory[name]}");
        return true;
    }
    
    [Header("Odblokowania Ekranów Dnia")]
    public bool unlockedFoundry = false;
    public bool unlockedAxeMold = false;
    public int type1Progress = 0;
    public int type2Progress = 0;

    [Header("Pula odblokowanych materiałów (do nagród od klientów)")]
    public List<string> unlockedMaterials = new List<string> { "Copper", "Bronze", "Iron", "SwordHandle" };

    /// <summary>
    /// Losuje materiał z puli odblokowanych zasobów
    /// </summary>
    public string GetRandomUnlockedMaterial()
    {
        if (unlockedMaterials.Count == 0) return "Iron"; // fallback
        return unlockedMaterials[Random.Range(0, unlockedMaterials.Count)];
    }

    /// <summary>
    /// Dodaje nowy materiał do puli (jeśli jeszcze go nie ma)
    /// </summary>
    public void UnlockMaterial(string materialName)
    {
        if (!unlockedMaterials.Contains(materialName))
        {
            unlockedMaterials.Add(materialName);
            Debug.Log($"[gameManager] Odblokowano nowy materiał w puli: {materialName}");
        }
    }
}
