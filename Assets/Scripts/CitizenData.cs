[System.Serializable]
public class CitizenData
{
    public string name;
    public float health;
    public float maxHealth;
    public float strength;
    public float intelligence;
    public float speed;

    public CitizenData(string name, ExiledCitizen citizen)
    {
        this.name = name;
        health = citizen.health;
        maxHealth = citizen.maxHealth;
        strength = citizen.strength;
        intelligence = citizen.intelligence;
        speed = citizen.speed;
    }
}
