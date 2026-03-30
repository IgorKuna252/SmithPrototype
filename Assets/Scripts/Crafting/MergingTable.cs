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

    [Header("Mikro-korekta łączenia Z (Przód/Tył)")]
    public float swordConnectionOffset = -0.03f; // Offset dla miecza
    public float axeConnectionOffset = -0.05f;   // Offset dla topora

    [Header("Mikro-korekta łączenia X (Lewo/Prawo)")]
    public float swordConnectionOffsetX = 0f;    // Boczny offset dla miecza
    public float axeConnectionOffsetX = 0f;      // Boczny offset dla topora

    [Header("Ustawienia Pozycji Części")]
    public Vector3 handleOffset = new Vector3(0, 0, -0.4f);
    public Vector3 bladeOffset = Vector3.zero;

    [Header("Grip - Pozycja i Rotacja w dłoni NPC")]
    [Tooltip("Dodatkowe przesunięcie pozycji GripPointa miecza (X, Y, Z)")]
    public Vector3 swordGripPositionOffset = Vector3.zero;
    [Tooltip("Dodatkowe przesunięcie pozycji GripPointa topora (X, Y, Z)")]
    public Vector3 axeGripPositionOffset = Vector3.zero;

    [Tooltip("Rotacja GripPointa miecza — dostosuj żeby ostrze celowało do przodu NPC")]
    public Vector3 swordGripRotation = new Vector3(0f, -90f, -30f);
    [Tooltip("Rotacja GripPointa topora — dostosuj żeby ostrze celowało do przodu NPC")]
    public Vector3 axeGripRotation = new Vector3(0f, 0f, -90f);

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

        if (craftingUI != null) craftingUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
            // --- LOGIKA ROZPOZNAWANIA PRZEPISU ---
            string weaponName = "Zniszczona Broń";
            bool validRecipe = false;
            bool isAxe = false;

            // Sprawdzamy czy to Topór
            if (placedMetal.partType == MetalPiece.MetalPartType.AxeHead &&
                placedWood.partType == WoodPiece.HandleType.AxeHandle)
            {
                weaponName = "Wykuty Topór";
                validRecipe = true;
                isAxe = true;
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
                placedMetal = null;
                placedWood = null;
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

            // 3. Pozycjonowanie
            MeshFilter woodFilter = placedWood.GetComponentInChildren<MeshFilter>();
            if (woodFilter != null)
            {
                float backOfBlade = placedMetal.GetActualBackOfBlade(); // ujemne (min.z * scale)
                float frontOfHandle = woodFilter.mesh.bounds.max.z * woodFilter.transform.localScale.z;

                float currentOffsetZ = (placedMetal.partType == MetalPiece.MetalPartType.AxeHead)
                                  ? axeConnectionOffset
                                  : swordConnectionOffset;
                float currentOffsetX = (placedMetal.partType == MetalPiece.MetalPartType.AxeHead)
                                  ? axeConnectionOffsetX
                                  : swordConnectionOffsetX;

                // Rączka zawsze w tym samym miejscu
                float targetZ = backOfBlade - frontOfHandle + currentOffsetZ;
                placedWood.transform.localRotation = Quaternion.identity;
                placedWood.transform.localPosition = new Vector3(currentOffsetX, 0, targetZ);

                // Ostrze zawsze na pozycji zero i z domyślną rotacją
                placedMetal.transform.localPosition = Vector3.zero;
                placedMetal.transform.localRotation = Quaternion.identity;

                Debug.Log($"[MergingTable] backOfBlade: {backOfBlade:F3}, Z rączki: {targetZ:F3}");
            }
            placedMetal.ForceCoolDown();
            
            placedWood.gameObject.name = "HandlePart";
            placedMetal.gameObject.name = "MetalPart";

            // Zapamiętaj dane PRZED zniszczeniem komponentów
            Vector3 gripLocalPos = placedWood.transform.localPosition;
            MetalType savedMetalTier = placedMetal.metalTier;
            float savedBladeLength = placedMetal.GetBladeLength();
            Debug.Log($"[MergingTable] Blade length: {savedBladeLength:F3}");
            string metalName = savedMetalTier.ToString();

            // Usuwamy fizykę części, by nie gryzła się z fizyką całej broni
            Destroy(placedMetal.GetComponent<Rigidbody>());
            Destroy(placedWood.GetComponent<Rigidbody>());
            Destroy(placedMetal);
            Destroy(placedWood);

            Rigidbody weaponRb = craftedWeapon.AddComponent<Rigidbody>();
            weaponRb.mass = 2.5f;

            FinishedObject finishedObj = craftedWeapon.AddComponent<FinishedObject>();
            finishedObj.weaponType = isAxe ? WeaponType.Axe : WeaponType.Sword;
            finishedObj.metalTier = savedMetalTier;
            finishedObj.bladeLength = savedBladeLength;
            finishedObj.itemName = weaponName + " " + metalName;

            BoxCollider col = craftedWeapon.AddComponent<BoxCollider>();
            col.size = new Vector3(0.1f, 0.1f, 1f);
            col.center = new Vector3(0, 0, 0.2f);

            craftedWeapon.AddComponent<WeaponHitbox>();

            GameObject grip = new GameObject("GripPoint");
            grip.transform.SetParent(craftedWeapon.transform);
            
            // Magia: Wybieramy odpowiedni offset z Inspektora w zależności od typu broni
            Vector3 currentGripOffset = isAxe ? axeGripPositionOffset : swordGripPositionOffset;
            
            // Dodajemy ten offset do bazowej pozycji rączki
            grip.transform.localPosition = gripLocalPos + currentGripOffset;
            grip.transform.localRotation = Quaternion.Euler(isAxe ? axeGripRotation : swordGripRotation);

            // Odejmij surowiec z ekwipunku
            gameManager.Instance.RemoveResource(metalName, 1);

            placedMetal = null;
            placedWood = null;
        }
    }
}