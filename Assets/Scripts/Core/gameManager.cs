using System.Collections.Generic;
using UnityEngine;


public class gameManager : MonoBehaviour
{
    // Singleton
    public static gameManager Instance { get; private set; }

    public int gold = 500;

    // Event wywoływany gdy złoto się zmieni
    public event System.Action OnGoldChanged;

    // Dane aktualnej bitwy (ustawiane przez TileManager przed przejściem do BattleScene)
    [HideInInspector] public List<int> selectedFighters = new List<int>();
    [HideInInspector] public int currentBattleDifficulty = 0;
    [HideInInspector] public string currentBattleTileName; // nazwa kafelka, np. "Tile (2, -1)"

    // Trwała pamięć mapy — przetrwa zmiany scen
    public HashSet<string> ownedTiles = new HashSet<string>();
    public Dictionary<string, int> tileDifficulties = new Dictionary<string, int>();

    public Dictionary<string, int> inventory = new Dictionary<string, int>();

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
        
        inventory["Copper"] = 1;
        inventory["Bronze"] = 1;
        inventory["Iron"] = 1;
        inventory["Steel"] = 1;
        inventory["Gold"] = 1;
        inventory["Platinum"] = 1;
        inventory["BlueSteel"] = 1;
        inventory["Vibranium"] = 1;
    }

    public void NotifyGoldChanged()
    {
        OnGoldChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        NotifyGoldChanged();
    }

    public bool RemoveGold(int amount)
    {
        if (gold < amount)
        {
            Debug.LogWarning($"Za mało złota! (masz: {gold}, potrzeba: {amount})");
            return false;
        }
        gold -= amount;
        NotifyGoldChanged();
        return true;
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
        Debug.Log($"Zużyto {amount} {name}. Pozostało: {inventory[name]}");
        return true;
    }
    
}
