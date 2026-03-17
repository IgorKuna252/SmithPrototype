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
        // Sprawdzamy, czy metal został wykuty
        if (!placedMetal.isFinished) return;

        // 1. Tworzymy kontener dla broni
        GameObject craftedWeapon = new GameObject("CraftedWeapon_" + placedMetal.metalTier.ToString());
        
        if (craftSpawnPoint != null)
        {
            craftedWeapon.transform.position = craftSpawnPoint.position;
            craftedWeapon.transform.rotation = craftSpawnPoint.rotation;
        }

        // 2. Podpinamy Rodziców (Axe_Root, Handle_Root) do nowej broni
        placedWood.transform.SetParent(craftedWeapon.transform);
        placedMetal.transform.SetParent(craftedWeapon.transform);

        // 3. Resetujemy rotacje - osie Rodziców stają się osiami broni
        placedWood.transform.localRotation = Quaternion.identity;
        placedMetal.transform.localRotation = Quaternion.identity;

        // --- DYNAMICZNE OBLICZANIE POZYCJI (Na bazie Rodziców) ---
        
        // Ostrze (Rodzic) idzie na Zero
        placedMetal.transform.localPosition = Vector3.zero;

        MeshFilter woodFilter = placedWood.GetComponentInChildren<MeshFilter>();

        if (woodFilter != null)
        {
            // POBIERAMY TYŁ OSTRZA: 
            // Zakładamy, że GetActualBackOfBlade zwraca pozycję wierzchołka 
            // w lokalnym układzie Rodzica (skala 1:1)
            float backOfBlade = placedMetal.GetActualBackOfBlade(); 
            
            // POBIERAMY PRZÓD RĄCZKI:
            // Musimy uwzględnić skalę dziecka, żeby wiedzieć, gdzie fizycznie kończy się rączka 
            // względem swojego Rodzica (Handle_Root)
            float frontOfHandle = woodFilter.mesh.bounds.max.z * woodFilter.transform.localScale.z;

            // Obliczamy idealne miejsce styku dla Rodzica rączki
            float targetY = backOfBlade - frontOfHandle + connectionOffset;
            
            // Ustawiamy Rodzica rączki na obliczonej pozycji
            placedWood.transform.localPosition = new Vector3(0, 0, targetY);
            
            Debug.Log($"[Dynamiczny Pivot Root] Tył ostrza: {backOfBlade}. Przesuwam Rodzica rączki na: {targetY}");
        }
        else
        {
            // Failsafe, jeśli nie znajdzie mesha rączki
            placedWood.transform.localPosition = handleOffset;
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
}