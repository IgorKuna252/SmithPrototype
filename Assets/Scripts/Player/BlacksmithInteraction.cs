using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public float throwForce = 12f; // Siła wyrzucania trzymanego przedmiotu 
    public Transform holdPosition;
    public Vector3 holdRotation = new Vector3(90f, 0f, 0f);

    [Header("Pozycje trzymania w ręku")]

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;
    private PlayerMovement playerMovement;

    private bool isInteractingWithTable = false;
    private MergingTable activeTable = null;


    public Canvas playerUI;
    
    public static BlacksmithInteraction Instance;

    void Awake() { Instance = this; }

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // 2. BLOKADA KAMERY STOŁU 
            if (isInteractingWithTable)
            {
                // WYJŚCIE: Jeśli wciśniesz ESC lub E -> Wracasz do swobodnego chodzenia
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
                {
                    CloseTableInteraction();
                }
                return; // Blokujemy resztę, żeby gracz nie machał rękami pod stołem
            }

        // ==========================================
        // GŁÓWNY KLAWISZ 'E' - ROBI WSZYSTKO W ŚWIECIE
        // ==========================================
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithEnvironmentE();
        }

        // Miotanie trzymanym przedmiotem - KLAWISZ 'Q'
        if (Input.GetKeyDown(KeyCode.Q) && heldItem != null)
        {
            ThrowItem();
        }

        // Zapasowe sterowanie MYSZĄ (dla wygody i starych nawyków)
        if (Input.GetMouseButtonDown(0)) TryPickUp();
        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem != null) TryPlaceOnTableOrDrop();
            else TryPickUp();
        }
    }

    // --- CENTRUM ZARZĄDZANIA KLAWISZEM 'E' ---
    void TryInteractWithEnvironmentE()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            Debug.Log($"[Raycast Test] Gracz patrzy na obiekt: {hit.collider.gameObject.name}");

            // 3. ZMIANA SCENY
            SceneTransition sceneTransition = hit.collider.GetComponent<SceneTransition>();
            if (sceneTransition != null) { sceneTransition.ChangeScene(); return; }

            // 4. STOJAK NA BROŃ (Kładzenie gotowej broni)
            WeaponRack rack = hit.collider.GetComponent<WeaponRack>();
            if (rack != null && rack.IsEmpty() && heldItem != null && heldItem.GetComponent<FinishedObject>())
            {
                rack.PlaceWeapon(heldItem.GetComponent<FinishedObject>());
                ClearHand();
                return;
            }

            // 5. STÓŁ MONTAŻOWY (Kładzenie lub Wejście w Kamerę)
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                if (heldItem != null)
                {
                    // Jeśli trzymasz rzecz -> Połóż na stół pod klawiszem E, bez ograniczeń ilości
                    MetalPiece metal = heldItem.GetComponent<MetalPiece>();
                    WoodPiece wood = heldItem.GetComponent<WoodPiece>();
                    
                    if (metal != null || wood != null) 
                    { 
                        table.DropItemOntoTable(heldItem.transform, hit.point); 
                        ClearHand(); 
                        return; 
                    }
                }
                else
                {
                    // Ręce puste -> ZAWSZE wchodzimy w kamerę stołu
                    activeTable = table;
                    isInteractingWithTable = true;
                    playerMovement.enabled = false;
                    table.ToggleAssemblyCamera(playerCamera.gameObject);
                    return;
                }
            }

            // 6. SZLIFIERKA
            GrindstoneStation station = hit.collider.GetComponent<GrindstoneStation>();
            if (station != null && heldItem != null && heldItem.GetComponent<MetalPiece>())
            {
                station.EnterGrindingMode(heldItem.GetComponent<MetalPiece>());
                ClearHand();
                return;
            }

            // 7. KOWADŁO
            AnvilStation anvil = hit.collider.GetComponentInParent<AnvilStation>();
            if (anvil != null && heldItem != null && heldItem.GetComponent<MetalPiece>())
            {
                anvil.EnterForgingMode(heldItem.GetComponent<MetalPiece>());
                ClearHand();
                return;
            }

            // 7.5 PIEC
            FurnaceStation furnace = hit.collider.GetComponentInParent<FurnaceStation>();
            if (furnace != null)
            {
                // Jeśli niczego nie trzymamy lub trzymamy metal - wchodzimy. (Z zabezpieczeniem czy trzymamy coś dziwnego - jeśli dziwnego, nie wchodzimy)
                if (heldItem == null) 
                {
                    furnace.EnterFurnaceMode(null);
                    return;
                }
                else if (heldItem.GetComponent<MetalPiece>() != null)
                {
                    furnace.EnterFurnaceMode(heldItem.GetComponent<MetalPiece>());
                    ClearHand();
                    return;
                }
            }

            // 8. PODNOSZENIE Z ZIEMI (Zawsze najwyższy priorytet, gdy mamy puste ręce!)
            if (heldItem == null)
            {
                // TryPickUp automatycznie radzi sobie z wyciąganiem z form, stojaków i podnoszeniem z ziemi
                if (TryPickUp()) return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }

            // ==========================================
            // 10. UPUSZCZANIE NA ZIEMIĘ (Zwykła podłoga/ściana)
            // ==========================================
            // Jeśli dotarliśmy aż tutaj, trzymamy przedmiot, ale nie trafiliśmy w żaden stół roboczy
            if (heldItem != null)
            {
                DropItem();
                return;
            }
            }
            else
            {
            // ==========================================
            // 11. UPUSZCZANIE W POWIETRZE (Patrzymy w niebo)
            // ==========================================
            // Jeśli laser w nic nie trafił (brak kolizji), ale mamy coś w rękach
            if (heldItem != null)
            {
                DropItem();
            }
        }
    }

    public bool IsHoldingItem() { return heldItem != null; }

    public void CloseTableInteraction()
    {
        if (activeTable != null) activeTable.ExitAssemblyMode();
        isInteractingWithTable = false;
        activeTable = null;
        playerMovement.enabled = true;
    }

    bool TryPickUp()
    {
        if (heldItem != null) return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            GameObject targetObj = null;
            MetalPiece metal = hit.collider.GetComponentInParent<MetalPiece>();
            WoodPiece wood = hit.collider.GetComponentInParent<WoodPiece>();
            FinishedObject finished = hit.collider.GetComponentInParent<FinishedObject>();
            Crucible crucible = hit.collider.GetComponentInParent<Crucible>();
            MoldManager mold = hit.collider.GetComponentInParent<MoldManager>();

            if (metal != null) targetObj = metal.gameObject;
            else if (wood != null) targetObj = wood.gameObject;
            else if (finished != null) targetObj = finished.gameObject;
            else if (crucible != null) targetObj = crucible.gameObject;
            
            // NOWOŚĆ: Wyciąganie rudy uwięzionej W PIECU za jego colliderem
            FurnaceStation furnace = hit.collider.GetComponentInParent<FurnaceStation>();
            if (furnace != null && furnace.metalSocket != null && targetObj == null)
            {
                MetalPiece metalInside = furnace.metalSocket.GetComponentInChildren<MetalPiece>();
                if (metalInside != null) targetObj = metalInside.gameObject;
            }

            if (mold != null && mold.IsReadyToExtract()) targetObj = mold.ExtractItem();

            // Zdejmowanie ze stojaka / stołu
            if (targetObj != null)
            {
                WeaponRack rack = targetObj.GetComponentInParent<WeaponRack>();
                if (rack != null) rack.TakeWeapon();

                // Skasowano sztuczne odpinanie ze stołu - obiekty po prostu leżą
            }
            else
            {
                WeaponRack rack = hit.collider.GetComponent<WeaponRack>();
                if (rack != null && !rack.IsEmpty())
                {
                    FinishedObject weaponFromRack = rack.TakeWeapon();
                    if (weaponFromRack != null) targetObj = weaponFromRack.gameObject;
                }
            }

            if (targetObj != null)
            {
                targetObj.transform.SetParent(null);
                heldItem = targetObj.gameObject; // lub targetObj jesli to transform
                
                // Morderstwo fizyki: Zamiast usypiać Rigidbody, zabijamy go na czas trzymania broni! 
                // Zagnieżdżone w graczu (który też ma Rigidbody) powodują przeciążenia i bugi przy bieganiu!
                Rigidbody[] rbs = heldItem.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in rbs) Destroy(rb);
                heldItemRb = null;

                // GWARANCJA LEKKOŚCI: Sztywno wyłączamy MeshCollidery wszystkich dzieci!
                foreach (Collider col in heldItem.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }

                heldItem.transform.SetParent(holdPosition);

                if (heldItem.GetComponent<Crucible>() != null)
                {
                    heldItem.transform.localPosition = Vector3.zero;
                    heldItem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    // Ustawiamy docelową rotację dłoni na wejściu
                    heldItem.transform.localRotation = Quaternion.Euler(holdRotation);

                    // Szukamy specyficznego klejonego punktu dłoni (np. na drewnianej rączce broni)
                    Transform gripPoint = heldItem.transform.Find("GripPoint");
                    
                    if (gripPoint != null)
                    {
                        // Przesuwamy całą bryłę złączoną tak, żeby punkt rączki wpadł dokładnie w środek ekranu (holdPosition)
                        Vector3 offset = heldItem.transform.position - gripPoint.position;
                        heldItem.transform.position = holdPosition.position + offset;
                    }
                    else
                    {
                        // Wyśrodkowanie geometryczne - szuka siatek wszystkich dzieci by utworzyć wyśrodkowany masyw
                        Renderer[] rends = heldItem.GetComponentsInChildren<Renderer>();
                        if (rends.Length > 0)
                        {
                            Bounds totalBounds = rends[0].bounds;
                            for (int i = 1; i < rends.Length; i++) 
                            {
                                totalBounds.Encapsulate(rends[i].bounds);
                            }
                            Vector3 centerOffset = heldItem.transform.position - totalBounds.center;
                            heldItem.transform.position = holdPosition.position + centerOffset;
                        }
                        else
                        {
                            heldItem.transform.localPosition = Vector3.zero;
                        }
                    }
                }

                targetObj.GetComponent<IPickable>()?.OnPickUp();
                return true;
            }
        }
        return false;
    }

    void TryPlaceOnTableOrDrop()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            WeaponRack rack = hit.collider.GetComponent<WeaponRack>();
            if (rack != null && rack.IsEmpty())
            {
                FinishedObject heldFinishedWeapon = heldItem.GetComponent<FinishedObject>();
                if (heldFinishedWeapon != null)
                {
                    rack.PlaceWeapon(heldFinishedWeapon);
                    ClearHand();
                    return;
                }
            }

            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                MetalPiece heldMetal = heldItem.GetComponent<MetalPiece>();
                WoodPiece heldWood = heldItem.GetComponent<WoodPiece>();

                if (heldMetal != null || heldWood != null) 
                { 
                    table.DropItemOntoTable(heldItem.transform, hit.point); 
                    ClearHand(); 
                    return; 
                }
            }
        }
        DropItem();
    }

    void DropItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);

        if (heldItemRb == null) heldItemRb = heldItem.AddComponent<Rigidbody>();

        if (heldItemRb != null)
        {
            heldItemRb.interpolation = RigidbodyInterpolation.Interpolate;
            heldItemRb.isKinematic = false;
            heldItemRb.useGravity = true;
            heldItemRb.detectCollisions = true;

            heldItemRb.WakeUp();
            heldItemRb.AddForce(Vector3.down * 0.1f, ForceMode.Impulse);
        }

        heldItem.GetComponent<IPickable>()?.OnDrop();
        ClearHand();
    }

    void ThrowItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);

        if (heldItemRb == null) heldItemRb = heldItem.AddComponent<Rigidbody>();

        if (heldItemRb != null)
        {
            heldItemRb.interpolation = RigidbodyInterpolation.Interpolate;
            heldItemRb.isKinematic = false;
            heldItemRb.useGravity = true;
            heldItemRb.detectCollisions = true;

            heldItemRb.WakeUp();
            
            // Odbijamy lekko do góry wektor patrzenia kamery, by nakreślić naturalną łagodną parabolę lotu
            Vector3 throwDirection = playerCamera.transform.forward + (Vector3.up * 0.15f);
            heldItemRb.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);
            
            // Dokładamy losowy moment obrotowy żeby przedmiot realistycznie rotował w locie po rzuceniu
            heldItemRb.AddTorque(new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f)), ForceMode.Impulse);
        }

        heldItem.GetComponent<IPickable>()?.OnDrop();
        ClearHand();
    }

    void ClearHand() 
    { 
        if (heldItem != null)
        {
            // Przywracamy fizykę kolizji dla wszystkich sklejonych części po wypuszczeniu z dłoni
            foreach (Collider col in heldItem.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
            }
        }
        heldItem = null; 
        heldItemRb = null; 
    }
}