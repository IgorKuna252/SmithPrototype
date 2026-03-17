using UnityEngine;

public class MergingTable : MonoBehaviour
{
    [Header("Miejsca na stole")]
    public Transform ingotPreview; 
    public Transform handlePreview; 

    [Header("Kamery i Sterowanie")]
    public GameObject assemblyCamera; 
    public MonoBehaviour[] scriptsToDisable;

    [Header("Crafting - Łączenie")]
    public Transform craftSpawnPoint; 
    public GameObject craftingUI; 

    [Header("Mikro-korekta łączenia")]
    public float connectionOffset = -0.03f;

    [Header("Ustawienia Pozycji Części")]
    public Vector3 handleOffset = new Vector3(0, 0, -0.4f);
    public Vector3 bladeOffset = Vector3.zero;

    [Header("Grip - Rotacja broni w dłoni NPC")]
    [Tooltip("Rotacja GripPointa — dostosuj żeby ostrze celowało do przodu NPC")]
    public Vector3 gripRotation = new Vector3(0f, 0f, -90f);

    private GameObject mainPlayerCamera; 
    private bool isAssemblyMode = false;

    private MetalPiece placedMetal; 
    private WoodPiece placedWood; 

    void Start()
    {
        if (craftingUI != null) craftingUI.SetActive(false);
    }

public void ToggleAssemblyCamera(GameObject playerCam)
    {
        if (isAssemblyMode) return;

        mainPlayerCamera = playerCam; 
        mainPlayerCamera.SetActive(false);
        assemblyCamera.SetActive(true);
        isAssemblyMode = true;
        // assemblyStartTime = Time.time; <-- To też możesz usunąć, jeśli nie masz już Update() w stole

        if (craftingUI != null) craftingUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // CAŁKOWICIE USUNIĘTO pętlę wyłączającą skrypty!
    }

    public void ExitAssemblyMode()
    {
        if (!isAssemblyMode) return;

        mainPlayerCamera.SetActive(true);
        assemblyCamera.SetActive(false);
        isAssemblyMode = false;

        if (craftingUI != null) craftingUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // CAŁKOWICIE USUNIĘTO pętlę włączającą skrypty!
    }

    public bool HasMetal() => placedMetal != null;
    public bool HasWood() => placedWood != null;

    public void PlaceMetal(MetalPiece metal)
    {
        metal.transform.SetParent(ingotPreview.parent, true);
        metal.transform.position = ingotPreview.position;
        metal.transform.rotation = ingotPreview.rotation;

        Rigidbody rb = metal.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.detectCollisions = true; // Ważne: musi mieć kolizje do kucia i podnoszenia!
        }

        placedMetal = metal;
    }

    public void PlaceWood(WoodPiece wood)
    {
        wood.transform.SetParent(handlePreview.parent, true);
        wood.transform.position = handlePreview.position;
        wood.transform.rotation = handlePreview.rotation;

        Rigidbody rb = wood.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }

        placedWood = wood;
    }

    public void CombineItems()
{
    if (placedMetal != null && placedWood != null)
    {
        if (!placedMetal.isFinished) return;

        // --- LOGIKA ROZPOZNAWANIA PRZEPISU ---
        string weaponName = "Zniszczona Broń";
        bool validRecipe = false;

        // Sprawdzamy czy to Topór
        if (placedMetal.partType == MetalPiece.MetalPartType.AxeHead && 
            placedWood.partType == WoodPiece.HandleType.AxeHandle)
        {
            weaponName = "Wykuty Topór";
            validRecipe = true;
        }
        // Sprawdzamy czy to Miecz
        else if (placedMetal.partType == MetalPiece.MetalPartType.SwordBlade && 
                 placedWood.partType == WoodPiece.HandleType.SwordHandle)
        {
            weaponName = "Wykuty Miecz";
            validRecipe = true;
        }

        if (!validRecipe)
        {
            Debug.LogWarning("Te części do siebie nie pasują!");
            return;
        }

        // 1. Tworzymy kontener z nazwą konkretnej broni
        GameObject craftedWeapon = new GameObject(weaponName + "_" + placedMetal.metalTier.ToString());
        
        if (craftSpawnPoint != null)
        {
            craftedWeapon.transform.position = craftSpawnPoint.position;
            craftedWeapon.transform.rotation = craftSpawnPoint.rotation;
        }

        // 2. Podpinanie do rodzica
        placedWood.transform.SetParent(craftedWeapon.transform);
        placedMetal.transform.SetParent(craftedWeapon.transform);

        placedWood.transform.localRotation = Quaternion.identity;
        placedMetal.transform.localRotation = Quaternion.identity;

        // 3. Pozycjonowanie (Używamy Twojej dynamicznej logiki)
        placedMetal.transform.localPosition = Vector3.zero;

        MeshFilter woodFilter = placedWood.GetComponentInChildren<MeshFilter>();
        if (woodFilter != null)
        {
            float backOfBlade = placedMetal.GetActualBackOfBlade(); 
            float frontOfHandle = woodFilter.mesh.bounds.max.z * woodFilter.transform.localScale.z;

            // Możesz dodać osobne offsety dla miecza i topora jeśli trzeba:
            float currentOffset = (placedMetal.partType == MetalPiece.MetalPartType.AxeHead) ? connectionOffset : connectionOffset; 

            float targetZ = backOfBlade - frontOfHandle + currentOffset;
            placedWood.transform.localPosition = new Vector3(0, 0, targetZ);
        }

        // --- FINALIZACJA ---
        placedMetal.ForceCoolDown();

        // Zapamiętaj pozycję rączki PRZED zniszczeniem komponentów
        Vector3 gripLocalPos = placedWood.transform.localPosition;

        // Usuwamy fizykę części, by nie gryzła się z fizyką całej broni
        Destroy(placedMetal.GetComponent<Rigidbody>());
        Destroy(placedWood.GetComponent<Rigidbody>());
        Destroy(placedMetal);
        Destroy(placedWood);

        Rigidbody weaponRb = craftedWeapon.AddComponent<Rigidbody>();
        weaponRb.mass = 2.5f;

        craftedWeapon.AddComponent<FinishedObject>();

        BoxCollider col = craftedWeapon.AddComponent<BoxCollider>();
        col.size = new Vector3(0.1f, 0.1f, 1f);
        col.center = new Vector3(0, 0, 0.2f);

        craftedWeapon.AddComponent<WeaponHitbox>();

        GameObject grip = new GameObject("GripPoint");
        grip.transform.SetParent(craftedWeapon.transform);
        grip.transform.localPosition = gripLocalPos;
        grip.transform.localRotation = Quaternion.Euler(gripRotation);

        placedMetal = null;
        placedWood = null;
    }
}

// Wyciągnąłem to do osobnej funkcji, żeby kod był czystszy
private void FinalizeCrafting(GameObject weapon)
{
    placedMetal.ForceCoolDown();

    Destroy(placedMetal.GetComponent<Rigidbody>());
    Destroy(placedWood.GetComponent<Rigidbody>());
    Destroy(placedMetal); 
    Destroy(placedWood);  

    Rigidbody weaponRb = weapon.AddComponent<Rigidbody>();
    weaponRb.mass = (weapon.name.Contains("Topór")) ? 4.0f : 2.5f; // Topór cięższy!
    
    weapon.AddComponent<FinishedObject>();

    placedMetal = null;
    placedWood = null;
    
    Debug.Log("Stworzono: " + weapon.name);
}
}