using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawnuje sztabki metali i uchwyty na podstawie gameManager.inventory.
/// Umieść na scenie MainScene i przypisz prefaby + pozycje spawnu w inspektorze.
/// </summary>
public class ForgeInventorySpawner : MonoBehaviour
{
    public static ForgeInventorySpawner Instance { get; private set; }

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

    // Przechowujemy indeksy, by nowe materiały nie zepsuły siatki upadając na stare
    private int currentIngotIndex = 0;
    private int currentHandleIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

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

        foreach (var kvp in gm.inventory)
        {
            MetalType metalType;
            if (!System.Enum.TryParse(kvp.Key, out metalType)) continue;

            for (int i = 0; i < kvp.Value; i++)
            {
                // Wywołujemy naszą nową, dynamiczną metodę, ale omijamy logikę doliczania rękojeści (zrobi to Start)
                SpawnIngotInternal(metalType, false);
            }
        }
    }

    void SpawnHandles()
    {
        var gm = gameManager.Instance;
        if (gm == null || handleSpawnArea == null) return;

        int swordHandles = gm.inventory.ContainsKey("SwordHandle") ? gm.inventory["SwordHandle"] : 0;
        int axeHandles = gm.inventory.ContainsKey("AxeHandle") ? gm.inventory["AxeHandle"] : 0;

        for (int i = 0; i < swordHandles; i++) SpawnHandleInternal(true);
        for (int i = 0; i < axeHandles; i++) SpawnHandleInternal(false);
    }

    public void SpawnNewBoughtMaterial(MetalType metalType)
    {
        // Spawnuje sztabkę i automatycznie dorzuca na stół po jednym rodzaju drewna
        SpawnIngotInternal(metalType, true);
        SpawnHandleInternal(true);
    }

    private void SpawnIngotInternal(MetalType metalType, bool playDropEffect)
    {
        GameObject prefab = GetIngotPrefab(metalType);
        if (prefab == null) return;

        Vector3 pos = GetGridPosition(ingotSpawnArea, currentIngotIndex);
        
        // Jeśli kupujemy ze sklepu w trakcie gry, podnieśmy go wyżej, żeby ładnie i realistycznie "spadł" na biurko
        if (playDropEffect) pos += Vector3.up * 0.5f;

        GameObject obj = Instantiate(prefab, pos, ingotSpawnArea.rotation);
        obj.name = $"Sztabka_{metalType}_{currentIngotIndex + 1}";

        MetalPiece metal = obj.GetComponent<MetalPiece>();
        if (metal != null) metal.metalTier = metalType;

        currentIngotIndex++;
    }

    private void SpawnHandleInternal(bool isSword)
    {
        if (isSword && swordHandlePrefab != null)
        {
            Vector3 posSword = GetGridPosition(handleSpawnArea, currentHandleIndex);
            GameObject objS = Instantiate(swordHandlePrefab, posSword, handleSpawnArea.rotation);
            objS.name = $"Rękojeść_Miecza_{currentHandleIndex + 1}";
            currentHandleIndex++;
        }
        else if (!isSword && axeHandlePrefab != null)
        {
            Vector3 posAxe = GetGridPosition(handleSpawnArea, currentHandleIndex);
            GameObject objA = Instantiate(axeHandlePrefab, posAxe, handleSpawnArea.rotation);
            objA.name = $"Trzonek_Siekiery_{currentHandleIndex + 1}";
            currentHandleIndex++;
        }
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
