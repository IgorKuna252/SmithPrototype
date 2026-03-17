using UnityEngine;

public class ExiledCitizen : MonoBehaviour
{
    public float health;
    public float maxHealth;
    public float strength;
    public float intelligence;
    public float speed;

    public void Initialize(float maxHealth, float strength, float intelligence, float speed)
    {
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.strength = strength;
        this.intelligence = intelligence;
        this.speed = speed;
    }

    public void GenerateRandomStats()
    {
        maxHealth = Random.Range(60f, 120f);
        health = maxHealth;

        strength = Random.Range(5f, 20f);
        intelligence = Random.Range(5f, 20f);
        speed = Random.Range(5f, 20f);
    }

    public string GetStats()
    {
        return $"Health: {health:F2}/{maxHealth:F2}\nStrength: {strength:F2}\nIntelligence: {intelligence:F2}\nSpeed: {speed:F2}";
    }
}