using UnityEngine;

public class ExiledCitizen : MonoBehaviour
{
    public float health;
    public float maxHealth;
    public float strength;
    public float intelligence;
    public float speed;
    public string equippedWeaponName;


    public void Initialize(float maxHealth, float strength, float intelligence, float speed)
    {
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.strength = strength;
        this.intelligence = intelligence;
        this.speed = speed;
        this.equippedWeaponName = "Brak";
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

        // Usuń z drużyny w gameManager
        var manager = gameManager.Instance;
        if (manager != null)
        {
            for (int i = 0; i < manager.team.Count; i++)
            {
                if (manager.team[i].name == name)
                {
                    manager.removeTeamMember(i);
                    break;
                }
            }
        }

        Destroy(gameObject);
    }

    public string GetStats()
    {
        return $"{name} | Broń: {equippedWeaponName}\nHP: {health:F0}/{maxHealth:F0}\nSTR: {strength:F0}\nINT: {intelligence:F0}\nSPD: {speed:F0}";
    }
}