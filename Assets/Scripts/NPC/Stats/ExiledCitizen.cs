using UnityEngine;

public class ExiledCitizen : MonoBehaviour
{
    public float health;
    public float maxHealth;
    public float strength;
    public float intelligence;
    public float speed;

    public AssignedTask task;

    public void Initialize(float maxHealth, float strength, float intelligence, float speed)
    {
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.strength = strength;
        this.intelligence = intelligence;
        this.speed = speed;
    }

    const float MAX_STAT = 20f;

    public float GetStrength()      { return strength; }
    public float GetIntelligence()  { return intelligence; }
    public float GetSpeed()         { return speed; }

    public float GetNormalizedStrength()     { return (strength / MAX_STAT) * 100f; }
    public float GetNormalizedIntelligence() { return (intelligence / MAX_STAT) * 100f; }
    public float GetNormalizedSpeed()        { return (speed / MAX_STAT) * 100f; }

    public AssignedTask GetAssignedTask()
    {
        return task; 
    }

    public void GenerateRandomStats()
    {
        maxHealth = Random.Range(60f, 120f);
        health = maxHealth;

        strength = Random.Range(5f, 20f);
        intelligence = Random.Range(5f, 20f);
        speed = Random.Range(5f, 20f);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"[Citizen] {name} otrzymał {damage:F1} obrażeń | HP: {health:F1}/{maxHealth:F1}");

        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[Citizen] {name} zginął!");
        Destroy(gameObject);
    }

    public string GetStats()
    {
        WeaponSocket socket = GetComponentInChildren<WeaponSocket>();
        string weaponInfo = socket?.ownerData?.equippedWeapon != null
            ? socket.ownerData.equippedWeapon.GetStats()
            : "Brak broni";
        return $"{name} | {weaponInfo}\nHP: {health:F0}/{maxHealth:F0}\nSTR: {strength:F0}\nINT: {intelligence:F0}\nSPD: {speed:F0}";
    }
}