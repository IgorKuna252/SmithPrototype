[System.Serializable]
public class CitizenData
{
    public string name;
    public float health;
    public float maxHealth;
    public float strength;
    public float intelligence;
    public float speed;
    public string equippedWeaponName; // Dodajemy to!


    public CitizenData(string name, ExiledCitizen citizen)
    {
        this.name = name;
        health = citizen.health;
        maxHealth = citizen.maxHealth;
        strength = citizen.strength;
        intelligence = citizen.intelligence;
        speed = citizen.speed;
        this.equippedWeaponName = "Brak"; // Domyślnie brak broni
    }

    public string GetStats()
    {
        return $"{name} | Broń: {equippedWeaponName}\nHP: {health:F0}/{maxHealth:F0}\nSTR: {strength:F0}\nINT: {intelligence:F0}\nSPD: {speed:F0}";
    }
}
