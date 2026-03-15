using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Ustawienia Generatora")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int layers = 3;
    [SerializeField] private float spacing = 1.1f;

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        GameObject mapHolder = new GameObject("GeneratedMap");

        for (int x = -layers; x <= layers; x++)
        {
            for (int y = -layers; y <= layers; y++)
            {
                Vector3 spawnPosition = new Vector3(x * spacing, y * spacing, 0f);
                GameObject newTile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity);
                newTile.transform.SetParent(mapHolder.transform);
                newTile.name = $"Tile ({x}, {y})";

                Tile tileComponent = newTile.GetComponent<Tile>();
                if (tileComponent != null)
                {
                    // 1. Ustawiamy stan
                    tileComponent.isOwned = (x == 0 && y == 0);
                
                    // 2. Delegujemy odpowiedzialność za kolor do samej klasy Tile
                    tileComponent.UpdateVisuals(); 
                }
            }
        }
    }
}