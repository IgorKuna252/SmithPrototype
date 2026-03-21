using UnityEngine;

/// <summary>
/// Spawnuje wrogów w scenie walki na podstawie trudności kafelka.
/// Trudność = suma HP wrogów. Im wyższa, tym więcej/silniejszych wrogów.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab wroga")]
    [SerializeField] GameObject enemyPrefab;

    [Header("Ustawienia spawnu")]
    [SerializeField] float spacing = 2f;
    [SerializeField] float baseEnemyHP = 100f;
    [SerializeField] int maxEnemies = 8;
    [SerializeField] float spawnHeightOffset = 1f; // Offset Y, żeby wrogowie nie byli w podłodze

    void Start()
    {
        int difficulty = gameManager.Instance.currentBattleDifficulty;

        // Oblicz liczbę wrogów: trudność / bazowe HP (minimum 1, max maxEnemies)
        int enemyCount = Mathf.Clamp(Mathf.CeilToInt(difficulty / baseEnemyHP), 1, maxEnemies);
        
        // HP każdego wroga = trudność / liczba wrogów (żeby suma = trudność)
        float hpPerEnemy = (float)difficulty / enemyCount;

        Debug.Log($"[EnemySpawner] Trudność: {difficulty} → {enemyCount} wrogów po {hpPerEnemy:F0} HP");

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 pos = transform.position + transform.right * (i * spacing);
            pos.y += spawnHeightOffset; // Podnosimy nad podłogę
            GameObject obj = Instantiate(enemyPrefab, pos, transform.rotation);
            obj.name = $"Wróg_{i + 1}";

            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.maxHp = hpPerEnemy;
                enemy.Initialize();
            }
        }
    }
}
