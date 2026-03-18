using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Ustawienia Generatora")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int layers = 3;
    [SerializeField] private float spacing = 1.1f;

    void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        GameObject mapHolder = new GameObject("GeneratedMap");
        var gm = gameManager.Instance;

        for (int x = -layers; x <= layers; x++)
        {
            for (int y = -layers; y <= layers; y++)
            {
                Vector3 spawnPosition = new Vector3(x * spacing, y * spacing, 0f);
                GameObject newTile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity);
                newTile.transform.SetParent(mapHolder.transform);
                string tileName = $"Tile ({x}, {y})";
                newTile.name = tileName;

                Tile tileComponent = newTile.GetComponent<Tile>();
                if (tileComponent != null)
                {
                    // Środkowy kafelek zawsze owned
                    if (x == 0 && y == 0)
                    {
                        tileComponent.isOwned = true;
                    }
                    // Sprawdź czy ten kafelek był wygrany w przeszłości
                    else if (gm.ownedTiles.Contains(tileName))
                    {
                        tileComponent.isOwned = true;
                    }

                    // Przywróć trudność lub wygeneruj nową (tylko raz)
                    if (gm.tileDifficulties.ContainsKey(tileName))
                    {
                        tileComponent.difficulty = gm.tileDifficulties[tileName];
                    }
                    else
                    {
                        tileComponent.difficulty = Random.Range(100, 801);
                        gm.tileDifficulties[tileName] = tileComponent.difficulty;
                    }

                    tileComponent.UpdateVisuals();
                }
            }
        }
    }
}