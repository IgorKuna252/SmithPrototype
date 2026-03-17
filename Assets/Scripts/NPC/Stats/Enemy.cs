using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp = 100f;
    float currentHp;

    void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"[Enemy] {name} otrzymał {damage:F1} obrażeń | HP: {currentHp:F1}/{maxHp:F1}");

        if (currentHp <= 0f)
        {
            Debug.Log($"[Enemy] {name} zniszczony!");
            Destroy(gameObject);
        }
    }
}
