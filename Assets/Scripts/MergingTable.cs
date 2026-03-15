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

    [Tooltip("Przesunięcie ostrza względem rączki. Użyj tego, by idealnie złączyć miecz!")]
    public Vector3 bladeOffset = new Vector3(0, 0, 1f);
    
    // Zmienne do przełączania kamery
    private GameObject mainPlayerCamera; 
    private bool isAssemblyMode = false;
    private float assemblyStartTime = 0f; // Zabezpieczenie przed natychmiastowym wyłączeniem stołu

    // Pamięć stołu
    private MetalPiece placedMetal; 
    private WoodPiece placedWood; 

    void Start()
    {
        if (craftingUI != null) craftingUI.SetActive(false);
    }

    void Update()
    {
        // TRYB STOŁU - Wyjście pod 'E' (zabezpieczone 0.2 sekundy opóźnienia)
        if (isAssemblyMode && Input.GetKeyDown(KeyCode.E) && Time.time > assemblyStartTime + 0.2f)
        {
            ExitAssemblyMode();
        }

        // TRYB STOŁU - Łączenie pod 'Spacją'
        if (isAssemblyMode && Input.GetKeyDown(KeyCode.Space))
        {
            CombineItems();
        }
    }

    public void ToggleAssemblyCamera(GameObject playerCam)
    {
        if (isAssemblyMode) return;

        mainPlayerCamera = playerCam; 
        
        mainPlayerCamera.SetActive(false);
        assemblyCamera.SetActive(true);
        isAssemblyMode = true;
        assemblyStartTime = Time.time; // Zapisujemy czas wejścia

        if (craftingUI != null) craftingUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        Debug.Log("Przełączono na widok stołu! Wciśnij Spację, aby połączyć, lub E, aby wyjść.");
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

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        Debug.Log("Wrócono do widoku z oczu gracza.");
    }

    public bool HasMetal() 
    { 
        if (placedMetal != null && placedMetal.transform.parent != ingotPreview.parent)
            placedMetal = null; 
        return placedMetal != null; 
    }

    public bool HasWood() 
    { 
        if (placedWood != null && placedWood.transform.parent != handlePreview.parent)
            placedWood = null; 
        return placedWood != null; 
    }

    public void PlaceMetal(MetalPiece metal)
    {
        metal.transform.position = ingotPreview.position;
        metal.transform.rotation = ingotPreview.rotation;
        metal.transform.SetParent(ingotPreview.parent, true);

        Rigidbody rb = metal.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        placedMetal = metal;
    }

    public void PlaceWood(WoodPiece wood)
    {
        wood.transform.position = handlePreview.position;
        wood.transform.rotation = handlePreview.rotation;
        wood.transform.SetParent(handlePreview.parent, true);

        Rigidbody rb = wood.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        placedWood = wood;
    }

    public void CombineItems()
    {
        if (placedMetal != null && placedWood != null)
        {
            if (!placedMetal.isFinished)
            {
                Debug.LogWarning("Ten metal nie jest jeszcze gotowy!");
                return;
            }

            // 1. Tworzymy główny obiekt nowej broni
            GameObject craftedWeapon = new GameObject("CraftedWeapon_" + placedMetal.metalTier.ToString());
            
            if (craftSpawnPoint != null)
            {
                craftedWeapon.transform.position = craftSpawnPoint.position; 
                craftedWeapon.transform.rotation = craftSpawnPoint.rotation;
            }

            // 2. Podpinamy przedmioty
            placedWood.transform.SetParent(craftedWeapon.transform);
            placedMetal.transform.SetParent(craftedWeapon.transform);

            // 3. Wyrównujemy rotację, żeby patrzyły w tę samą stronę
            placedWood.transform.localRotation = Quaternion.identity;
            placedMetal.transform.localRotation = Quaternion.identity; // Możesz zmienić na np. Quaternion.Euler(0, 180, 0) jeśli ostrze jest tyłem do przodu

            // 4. MAGIA: Ustawiamy rączkę na środku (0,0,0), a ostrze przesuwamy używając naszej zmiennej z Inspektora!
            placedWood.transform.localPosition = new Vector3(0, 0, 0.55f);
            placedMetal.transform.localPosition = new Vector3(0, 0, 0);

            // 5. Czyszczenie starych właściwości fizycznych
            Destroy(placedMetal.GetComponent<Rigidbody>());
            Destroy(placedWood.GetComponent<Rigidbody>());
            Destroy(placedMetal); 
            Destroy(placedWood);  

            // 6. Nowa fizyka dla połączonej całości
            Rigidbody weaponRb = craftedWeapon.AddComponent<Rigidbody>();
            weaponRb.mass = 2.5f; 
            
            craftedWeapon.AddComponent<FinishedObject>();

            // 7. Zwalnianie miejsc na stole
            placedMetal = null;
            placedWood = null;

            Debug.Log("Broń połączona!");
        }
    }
}