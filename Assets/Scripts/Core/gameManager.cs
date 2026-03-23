using System.Collections.Generic;
using UnityEngine;

public class gameManager : MonoBehaviour
{
    // Singleton
    public static gameManager Instance { get; private set; }

    public List<CitizenData> team = new List<CitizenData>();
    public const int teamSize = 4;

    // Event wywoływany gdy drużyna się zmieni (dodanie/usunięcie/equip broni)
    public event System.Action OnTeamChanged;

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

    /// <summary>
    /// Wywołaj to po każdej zmianie w drużynie (dodanie, usunięcie, equip broni).
    /// Powiadomi wszystkie podpięte systemy UI.
    /// </summary>
    public void NotifyTeamChanged()
    {
        OnTeamChanged?.Invoke();
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
    
    public bool addTeamMember(GameObject npc)
    {
        if (team.Count >= teamSize)
        {
            Debug.Log("Team is full!");
            return false;
        }

        ExiledCitizen citizen = npc.GetComponent<ExiledCitizen>();
        if (citizen == null)
        {
            Debug.LogWarning("No ExiledCitizen on " + npc.name);
            return false;
        }

        team.Add(new CitizenData(npc.name, citizen));
        NotifyTeamChanged();
        return true;
    }

    public bool removeTeamMember(int index)
    {
        if (index >= 0 && index < team.Count)
        {
            // Zniszcz klon broni z DontDestroyOnLoad, żeby nie wyciekł
            if (team[index].savedWeaponTemplate != null)
                Destroy(team[index].savedWeaponTemplate);

            team.RemoveAt(index);
            NotifyTeamChanged();
            return true;
        }

        Debug.Log("Invalid team index!");
        return false;
    }

    public CitizenData getMember(int index)
    {
        if (index >= 0 && index < team.Count)
            return team[index];
        return null;
    }
}
