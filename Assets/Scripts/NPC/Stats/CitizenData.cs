using UnityEngine;

[System.Serializable]
public class CitizenData
{
    public string name;
    public float health;
    public float maxHealth;
    public float strength;
    public float intelligence;
    public float speed;
    public WeaponData equippedWeapon;
    public AssignedTask task;

    public GameObject savedWeaponTemplate;

    public SavedMeshData[] weaponMeshes;

    public CitizenData(string name, ExiledCitizen citizen)
    {
        this.name = name;
        health = citizen.health;
        maxHealth = citizen.maxHealth;
        strength = citizen.strength;
        intelligence = citizen.intelligence;
        speed = citizen.speed;
        task = citizen.task;
    }
    public float GetStrength()
    {
        return strength;
    }

    public float GetIntelligence()
    {
        return intelligence;
    }

    public float GetSpeed()
    {
        return speed;
    }
    public string GetStats()
    {
        string weaponInfo = equippedWeapon != null ? equippedWeapon.GetStats() : "Brak broni";
        return $"{name} | {weaponInfo}\nHP: {health:F0}/{maxHealth:F0}\nSTR: {strength:F0}\nINT: {intelligence:F0}\nSPD: {speed:F0}";
    }
}
