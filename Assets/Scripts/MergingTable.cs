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

    void Update()
    {
        if (isAssemblyMode && Input.GetKeyDown(KeyCode.E) && Time.time > assemblyStartTime + 0.2f)
        {
            ExitAssemblyMode();
        }

        if (isAssemblyMode && Input.GetKeyDown(KeyCode.Space))
        {
            CombineItems();
        }

        // Automatyczne czyszczenie, jeśli gracz zabrał przedmiot ręcznie
        if (placedMetal != null && placedMetal.transform.parent != ingotPreview.parent)
            placedMetal = null;
        if (placedWood != null && placedWood.transform.parent != handlePreview.parent)
            placedWood = null;
    }

    public void ToggleAssemblyCamera(GameObject playerCam)
    {
        if (isAssemblyMode) return;

        mainPlayerCamera = playerCam; 
        mainPlayerCamera.SetActive(false);
        assemblyCamera.SetActive(true);
        isAssemblyMode = true;
        assemblyStartTime = Time.time;

        if (craftingUI != null) craftingUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
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

            placedWood.transform.localPosition = new Vector3(0, 0, -0.4f);
            placedMetal.transform.localPosition = new Vector3(0, 0, 0);

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