using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp = 100f;
    float currentHp;
    bool initialized = false;

    void Start()
    {
        // EnemySpawner może ustawić maxHp PO Instantiate ale PRZED pierwszym Update
        Initialize();
    }

    public void Initialize()
    {
        currentHp = maxHp;
        initialized = true;
    }

    void Update()
    {
        // Zabezpieczenie gdyby maxHp zostało ustawione po Start()
        if (!initialized)
            Initialize();
    }

    public void TakeDamage(float damage)
    {
        if (!initialized) Initialize();

        currentHp -= damage;
        Debug.Log($"[Enemy] {name} otrzymał {damage:F1} obrażeń | HP: {currentHp:F1}/{maxHp:F1}");

        if (currentHp <= 0f)
        {
            Debug.Log($"[Enemy] {name} zniszczony!");
            Destroy(gameObject);
        }
    }
}
