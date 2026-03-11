using UnityEngine;

public class ExiledCitizen : MonoBehaviour
{
    public float health;
    public float maxHealth;
    public float strength;
    public float stamina;

    public void Initialize(float maxHealth, float strength, float stamina)
    {
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.strength = strength;
        this.stamina = stamina;
    }

    public void GenerateRandomStats()
    {
        maxHealth = Random.Range(60f, 120f);
        health = maxHealth;

        strength = Random.Range(5f, 20f);
        stamina = Random.Range(5f, 20f);
    }

    public string GetStats()
    {
        return $"Health: {health:F2}/{maxHealth:F2}\nStrength: {strength:F2}\nStamina: {stamina:F2}";
    }
}