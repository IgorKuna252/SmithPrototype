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
    public Vector3 handleOffset = new Vector3(0, 0, -0.4f); // To zastąpi Twoje twarde liczby
    public Vector3 bladeOffset = Vector3.zero;             // Na wypadek, gdybyś chciał ruszyć ostrze   

    private GameObject mainPlayerCamera; 
    private bool isAssemblyMode = false;
    private float assemblyStartTime = 0f;

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
            //if (!placedMetal.isFinished) return;

            GameObject craftedWeapon = new GameObject("CraftedWeapon_" + placedMetal.metalTier.ToString());
            
            if (craftSpawnPoint != null)
            {
                craftedWeapon.transform.position = craftSpawnPoint.position; 
                craftedWeapon.transform.rotation = craftSpawnPoint.rotation;
            }

            placedWood.transform.SetParent(craftedWeapon.transform);
            placedMetal.transform.SetParent(craftedWeapon.transform);

            placedWood.transform.localRotation = Quaternion.identity;
            placedMetal.transform.localRotation = Quaternion.identity;

            // --- AUTOMATYCZNE OBLICZANIE POZYCJI (Zczytywanie na żywo) ---
            
            // Ostrze idzie na pozycję Zero
            placedMetal.transform.localPosition = Vector3.zero;

            MeshFilter woodFilter = placedWood.GetComponentInChildren<MeshFilter>();

            if (woodFilter != null)
            {
                // POBIERAMY ZMODYFIKOWANY TYŁ MIECZA BEZPOŚREDNIO Z WIERZCHOŁKÓW
                float backOfBlade = placedMetal.GetActualBackOfBlade(); 
                
                // Rączka się nie deformuje, więc jej bounds są zawsze poprawne
                float frontOfHandle = woodFilter.mesh.bounds.max.z * woodFilter.transform.localScale.z;

                // Obliczamy idealne miejsce na styk
                float targetZ = backOfBlade - frontOfHandle + connectionOffset;
                
                placedWood.transform.localPosition = new Vector3(0, 0, targetZ);
                
                Debug.Log($"[Automatyczny Pivot] Tył wykutego ostrza to: {backOfBlade}. Przesuwam rączkę na: {targetZ}");
            }
            else
            {
                placedWood.transform.localPosition = new Vector3(0, 0, -0.4f);
            }
            // ----------------------------------------------

            placedMetal.ForceCoolDown();

            Destroy(placedMetal.GetComponent<Rigidbody>());
            Destroy(placedWood.GetComponent<Rigidbody>());
            Destroy(placedMetal); 
            Destroy(placedWood);  

            Rigidbody weaponRb = craftedWeapon.AddComponent<Rigidbody>();
            weaponRb.mass = 2.5f; 
            
            craftedWeapon.AddComponent<FinishedObject>();

            placedMetal = null;
            placedWood = null;
        }
    }
}