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

    [Header("Grip - Pozycja w dłoni (jeśli przywrócisz ludków)")]
    public Vector3 gripPositionOffset = Vector3.zero;
    public Vector3 gripRotation = new Vector3(0f, -90f, -30f);

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

    public void ClearMetal() { placedMetal = null; }
    public void ClearWood() { placedWood = null; }

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
            string weaponName = "Wykuta Broń";

            // 1. Tworzymy kontener
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

            // 3. Pozycjonowanie
            placedMetal.transform.localPosition = Vector3.zero;

            MeshFilter woodFilter = placedWood.GetComponentInChildren<MeshFilter>();
            if (woodFilter != null)
            {
                float backOfBlade = placedMetal.GetActualBackOfBlade(); 
                float frontOfHandle = woodFilter.mesh.bounds.max.z * woodFilter.transform.localScale.z;

                float currentOffsetZ = -0.04f; 
                float currentOffsetX = 0f;     

                float targetZ = backOfBlade - frontOfHandle + currentOffsetZ;
                
                placedWood.transform.localPosition = new Vector3(currentOffsetX, 0, targetZ);
                
                Debug.Log($"[Dynamiczny Pivot Z] Tył ostrza: {backOfBlade}. Przesuwam rączkę na X: {currentOffsetX}, Z: {targetZ}");
            }

            // --- FINALIZACJA ---
            placedMetal.ForceCoolDown();

            Vector3 gripLocalPos = placedWood.transform.localPosition;
            string metalName = placedMetal.metalTier.ToString();

            // Usuwamy stare komponenty wejściowe
            Destroy(placedMetal.GetComponent<Rigidbody>());
            Destroy(placedWood.GetComponent<Rigidbody>());
            Destroy(placedMetal);
            Destroy(placedWood);

            Rigidbody weaponRb = craftedWeapon.AddComponent<Rigidbody>();
            weaponRb.mass = 2.5f;

            FinishedObject finishedObj = craftedWeapon.AddComponent<FinishedObject>();

            BoxCollider col = craftedWeapon.AddComponent<BoxCollider>();
            col.size = new Vector3(0.1f, 0.1f, 1f);
            col.center = new Vector3(0, 0, 0.2f);

            GameObject grip = new GameObject("GripPoint");
            grip.transform.SetParent(craftedWeapon.transform);
            
            grip.transform.localPosition = gripLocalPos + gripPositionOffset;
            grip.transform.localRotation = Quaternion.Euler(gripRotation);

            // Odejmujemy surowiec z ekwipunku jeśli istnieje
            if (gameManager.Instance != null && gameManager.Instance.inventory.ContainsKey(metalName)) {
                gameManager.Instance.RemoveResource(metalName, 1);
            }

            placedMetal = null;
            placedWood = null;
        }
    }
}