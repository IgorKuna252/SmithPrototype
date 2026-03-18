using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawnuje sztabki metali i uchwyty na podstawie gameManager.inventory.
/// Umieść na scenie MainScene i przypisz prefaby + pozycje spawnu w inspektorze.
/// </summary>
public class ForgeInventorySpawner : MonoBehaviour
{
    [Header("Prefaby sztabek (8 materiałów)")]
    [SerializeField] GameObject copperIngotPrefab;
    [SerializeField] GameObject bronzeIngotPrefab;
    [SerializeField] GameObject ironIngotPrefab;
    [SerializeField] GameObject steelIngotPrefab;
    [SerializeField] GameObject goldIngotPrefab;
    [SerializeField] GameObject platinumIngotPrefab;
    [SerializeField] GameObject blueSteelIngotPrefab;
    [SerializeField] GameObject vibraniumIngotPrefab;

    [Header("Prefaby uchwytów")]
    [SerializeField] GameObject swordHandlePrefab;
    [SerializeField] GameObject axeHandlePrefab;

    [Header("Pozycje spawnu")]
    [SerializeField] Transform ingotSpawnArea;
    [SerializeField] Transform handleSpawnArea;
    [SerializeField] float spacing = 0.3f;
    [SerializeField] int maxPerRow = 4;

    void Start()
    {
        SpawnIngots();
        SpawnHandles();
    }

    GameObject GetIngotPrefab(MetalType type)
    {
        switch (type)
        {
            case MetalType.Copper:    return copperIngotPrefab;
            case MetalType.Bronze:    return bronzeIngotPrefab;
            case MetalType.Iron:      return ironIngotPrefab;
            case MetalType.Steel:     return steelIngotPrefab;
            case MetalType.Gold:      return goldIngotPrefab;
            case MetalType.Platinum:  return platinumIngotPrefab;
            case MetalType.BlueSteel: return blueSteelIngotPrefab;
            case MetalType.Vibranium: return vibraniumIngotPrefab;
            default: return ironIngotPrefab;
        }
    }

    void SpawnIngots()
    {
        var gm = gameManager.Instance;
        if (gm == null || ingotSpawnArea == null) return;

        int spawnIndex = 0;

        foreach (var kvp in gm.inventory)
        {
            MetalType metalType;
            if (!System.Enum.TryParse(kvp.Key, out metalType)) continue;

            GameObject prefab = GetIngotPrefab(metalType);
            if (prefab == null) continue;

            for (int i = 0; i < kvp.Value; i++)
            {
                Vector3 pos = GetGridPosition(ingotSpawnArea, spawnIndex);
                GameObject obj = Instantiate(prefab, pos, ingotSpawnArea.rotation);
                obj.name = $"Sztabka_{kvp.Key}_{i + 1}";

                MetalPiece metal = obj.GetComponent<MetalPiece>();
                if (metal != null)
                    metal.metalTier = metalType;

                spawnIndex++;
            }
        }

        Debug.Log($"[ForgeSpawner] Zaspawnowano {spawnIndex} sztabek");
    }

    void SpawnHandles()
    {
        var gm = gameManager.Instance;
        if (gm == null || handleSpawnArea == null) return;

        // Policz łączną liczbę sztabek
        int totalIngots = 0;
        foreach (var kvp in gm.inventory)
        {
            // Tylko policz materiały które są typami metalu
            MetalType metalType;
            if (System.Enum.TryParse(kvp.Key, out metalType))
                totalIngots += kvp.Value;
        }

        int spawnIndex = 0;

        // Spawnuj uchwyty mieczy
        if (swordHandlePrefab != null)
        {
            for (int i = 0; i < totalIngots; i++)
            {
                Vector3 pos = GetGridPosition(handleSpawnArea, spawnIndex);
                GameObject obj = Instantiate(swordHandlePrefab, pos, handleSpawnArea.rotation);
                obj.name = $"Rękojeść_Miecza_{i + 1}";
                spawnIndex++;
            }
        }

        // Spawnuj trzonki siekier
        if (axeHandlePrefab != null)
        {
            for (int i = 0; i < totalIngots; i++)
            {
                Vector3 pos = GetGridPosition(handleSpawnArea, spawnIndex);
                GameObject obj = Instantiate(axeHandlePrefab, pos, handleSpawnArea.rotation);
                obj.name = $"Trzonek_Siekiery_{i + 1}";
                spawnIndex++;
            }
        }

        Debug.Log($"[ForgeSpawner] Zaspawnowano {spawnIndex} uchwytów (po {totalIngots} na typ)");
    }

    /// <summary>
    /// Oblicza pozycję w siatce na podstawie indeksu.
    /// Rozmieszcza obiekty w rzędach (wzdłuż right) i kolumnach (wzdłuż forward).
    /// </summary>
    Vector3 GetGridPosition(Transform origin, int index)
    {
        int col = index % maxPerRow;
        int row = index / maxPerRow;

        return origin.position
            + origin.right * (col * spacing)
            + origin.forward * (row * spacing);
    }
}
