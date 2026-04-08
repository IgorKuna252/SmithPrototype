using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public float throwForce = 12f; // Siła wyrzucania trzymanego przedmiotu 
    public Transform holdPosition;
    public Vector3 holdRotation = new Vector3(90f, 0f, 0f);
    public GameObject playerVisuals;

    [Header("Pozycje trzymania w ręku")]
    [Tooltip("Skończona broń (FinishedObject): przesunięcie punktu Grip względem holdPosition, w osach kamery (X=prawo, Y=góra, Z=przed obiektyw). (0,0,0) = dokładnie na środku celownika jak u kolegów.")]
    public Vector3 craftedWeaponGripOffsetCameraSpace = new Vector3(0.25f, -0.08f, 0.18f);
    [Tooltip("Rotacja skończonej broni względem holdPosition (stopnie Euler). Domyślnie jak dotąd: miecz w dłoni.")]
    public Vector3 craftedWeaponHoldRotationEuler = new Vector3(-90f, 0f, 90f);

    public Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;
    private PlayerMovement playerMovement;

    private bool isInteractingWithTable = false;
    private bool isTransactionUIOpen = false;
    private MergingTable activeTable = null;
    
    private bool isInteractingWithNPC = false;

    private bool isInteractingWithMold = false;
    private MoldManager activeMold = null;

    public WheelController wheel;
    public Canvas playerUI;
    
    public static BlacksmithInteraction Instance;

    /// <summary>Czy gracz jest w jakiejkolwiek interakcji (stół, NPC, forma, transakcja)?</summary>
    public bool IsBusy => isInteractingWithTable || isInteractingWithNPC || isInteractingWithMold || isTransactionUIOpen;

    void Awake() { Instance = this; }

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerMovement = GetComponent<PlayerMovement>();

        if (wheel == null && playerUI != null)
            wheel = playerUI.GetComponent<WheelController>();

        if (playerVisuals == null)
        {
            // Próba automatycznego znalezienia, żeby oszczędzić zapominania!
            Transform vis = transform.Find("Visuals") ?? transform.Find("PlayerVisuals") ?? transform.Find("Model") ?? transform.Find("PlayerModel");
            if (vis != null) 
            {
                playerVisuals = vis.gameObject;
            }
            else
            {
                Debug.LogWarning("[BlacksmithInteraction] UWAGA: Nie podpięto 'Player Visuals' w inspektorze! Aby ręce gracza znikały przy stole, przeciągnij model gracza do tego pola w skrypcie BlacksmithInteraction.");
            }
        }
    }

    public void SetTransactionUIOpen(bool open)
    {
        isTransactionUIOpen = open;
        playerMovement.enabled = !open;
        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // BLOKADA OKNA WYNIKI TRANSAKCJI
        if (isTransactionUIOpen) return;

        // 1. BLOKADA NPC
        if (isInteractingWithNPC)
        {
            if (Input.GetKeyDown(KeyCode.E)) CloseNPCInteraction();
            return;
        }

        // 2. BLOKADA KAMERY STOŁU
        if (isInteractingWithTable)
        {
            // WYJŚCIE: Tylko klawisz E -> Wracasz do swobodnego chodzenia
            if (Input.GetKeyDown(KeyCode.E))
            {
                CloseTableInteraction();
            }
            return; // Blokujemy resztę, żeby gracz nie machał rękami pod stołem
        }

        // 3. BLOKADA KAMERY FORMY
        if (isInteractingWithMold)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                CloseMoldInteraction();
            }
            else if (Input.GetMouseButtonDown(1)) // PPM do zmiany formy
            {
                if (activeMold != null) activeMold.ChangeMold();
            }
            return;
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
            // 1. ZDAWANIE BRONI DLA NPC
            if (heldItem != null && heldItem.GetComponent<FinishedObject>() != null)
            {
                WeaponSocket socket = hit.collider.GetComponent<WeaponSocket>();
                if (socket == null) socket = hit.collider.GetComponentInParent<WeaponSocket>();
                if (socket != null)
                {
                    heldItem.GetComponent<IPickable>()?.OnDrop();
                    heldItem.transform.SetParent(null);
                    socket.EquipWeapon(heldItem);
                    if (wheel != null) wheel.SetWheel(false);
                    ClearHand();

                    npcPathFinding npcPath = socket.GetComponent<npcPathFinding>() ?? socket.GetComponentInParent<npcPathFinding>();
                    if (npcPath != null)
                        npcPath.ProcessTransaction();

                    return;
                }
            }

            // ROZMOWA Z NPC LUB KUPCEM
            npcPathFinding npc = hit.collider.GetComponent<npcPathFinding>() ?? hit.collider.GetComponentInParent<npcPathFinding>();
            if (npc != null)
            {
                // Skoro kod tutaj dotarł, to nie kupiec, lecimy ze standardowym panelem NPC:
                isInteractingWithNPC = true;
                playerMovement.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                if (playerVisuals != null) playerVisuals.SetActive(false);
                
                NPCInteractionUI.Instance.Show(npc);
                return;
            }

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
                    
                    if (playerVisuals != null) playerVisuals.SetActive(false);
                    
                    table.ToggleAssemblyCamera(playerCamera.gameObject);
                    return;
                }
            }

            // 6. SZLIFIERKA
            GrindstoneStation station = hit.collider.GetComponent<GrindstoneStation>();
            if (station != null && heldItem != null && heldItem.GetComponent<MetalPiece>())
            {
                MetalPiece grindMetal = heldItem.GetComponent<MetalPiece>();
                station.EnterGrindingMode(grindMetal);
                ClearHand();
                ShowMetalPreview(grindMetal);
                return;
            }

            // 7. KOWADŁO
            AnvilStation anvil = hit.collider.GetComponentInParent<AnvilStation>();
            if (anvil != null && heldItem != null && heldItem.GetComponent<MetalPiece>())
            {
                MetalPiece forgeMetal = heldItem.GetComponent<MetalPiece>();
                anvil.EnterForgingMode(forgeMetal);
                ClearHand();
                ShowMetalPreview(forgeMetal);
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

            // FORMY (MOLD MANAGER)
            MoldManager mold = hit.collider.GetComponentInParent<MoldManager>();
            if (mold != null)
            {
                Crucible heldCrucible = heldItem != null ? heldItem.GetComponent<Crucible>() : null;
                
                // Jeśli jesteśmy z pustymi rękami LUB trzymamy tygiel, i forma nie czeka na wyjęcie obiektu
                if ((heldItem == null || heldCrucible != null) && !mold.IsReadyToExtract())
                {
                    activeMold = mold;
                    isInteractingWithMold = true;
                    playerMovement.enabled = false;
                    mold.ToggleAssemblyCamera(playerCamera.gameObject);
                    
                    if (playerVisuals != null) playerVisuals.SetActive(false);
                    
                    if (heldCrucible != null)
                    {
                        mold.DockCrucible(heldCrucible);
                        ClearHand(); // zapominamy że trzymamy, bo stacja go przechwyciła
                    }
                    return;
                }
            }

            // NAPEŁNIANIE WIADERKA CZYMŚ Z RĘKI
            Crucible targetCrucible = hit.collider.GetComponentInParent<Crucible>();
            if (targetCrucible != null)
            {
                MetalPiece heldMetal = heldItem != null ? heldItem.GetComponent<MetalPiece>() : null;
                
                if (heldMetal != null)
                {
                    if (heldMetal.currentTemperature >= heldMetal.forgingTemperature)
                    {
                        // Wrzucasz rozgrzany metal z ręki prosto do tygla
                        targetCrucible.FillWithMetal(heldMetal.metalTier);
                        Destroy(heldItem);
                        ClearHand();
                    }
                    else
                    {
                        Debug.Log("Metal jest za zimny by włożyć go do tygla! Najpierw go rozgrzej w piecu.");
                    }
                    return;
                }
            }

            // PODNOSZENIE Z ZIEMI (Zawsze najwyższy priorytet, gdy mamy puste ręce!)
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

            // UPUSZCZANIE NA ZIEMIĘ (Zwykła podłoga/ściana)
            // Jeśli dotarliśmy aż tutaj, trzymamy przedmiot, ale nie trafiliśmy w żaden stół roboczy
            if (heldItem != null)
            {
                DropItem();
                return;
            }
        }
        else
        {
            // UPUSZCZANIE W POWIETRZE (Patrzymy w niebo)
            // Jeśli laser w nic nie trafił (brak kolizji), ale mamy coś w rękach
            if (heldItem != null)
            {
                DropItem();
            }
        }
    }

    public bool IsHoldingItem() { return heldItem != null; }

    public void ShowMetalPreview(MetalPiece metal)
    {
        if (wheel == null || metal == null) return;
        MeshFilter mf = metal.GetComponent<MeshFilter>();
        float bladeLength = (mf != null) ? mf.mesh.bounds.size.z : metal.startLength;
        WeaponData preview = new WeaponData("Podgląd", metal.metalTier, bladeLength);
        wheel.SetWheel(true);
        wheel.UpdateWheel(preview.GetNormalizedDamage(), preview.GetNormalizedSpeed(), preview.GetNormalizedAoE());
    }

    public void CloseTableInteraction()
    {
        if (activeTable != null) activeTable.ExitAssemblyMode();
        isInteractingWithTable = false;
        activeTable = null;
        playerMovement.enabled = true;
        
        if (playerVisuals != null) playerVisuals.SetActive(true);
    }

    public void CloseMoldInteraction()
    {
        if (activeMold != null)
        {
            activeMold.ExitAssemblyMode();
            
            Crucible pickedCrucible = activeMold.dockedCrucible;
            if (pickedCrucible != null)
            {
                activeMold.UndockCrucible();
                
                pickedCrucible.transform.SetParent(null);
                heldItem = pickedCrucible.gameObject;
                heldItemRb = heldItem.GetComponent<Rigidbody>();
                if (heldItemRb != null)
                {
                    heldItemRb.useGravity = false;
                    heldItemRb.isKinematic = true;
                    heldItemRb.detectCollisions = false;
                }
                heldItem.transform.SetParent(holdPosition);
                heldItem.transform.localPosition = Vector3.zero;
                heldItem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                
                pickedCrucible.OnPickUp();
            }
            
            if (playerVisuals != null) playerVisuals.SetActive(true);
        }
        
        isInteractingWithMold = false;
        activeMold = null;
        playerMovement.enabled = true;
    }

    public void CloseNPCInteraction()
    {
        isInteractingWithNPC = false;
        playerMovement.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        NPCInteractionUI.Instance.Hide();
        
        if (playerVisuals != null) playerVisuals.SetActive(true);
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
                else if (heldItem.GetComponent<WoodPiece>() != null)
                {
                    // Ustawiamy docelową rotację dłoni specjalnie dla surowego trzonka, żeby stał na sztorc!
                    // Jeśli 0,0,0 kładłoby go krzywo, wystarczy zmienić tu na np. (0, 0, 90f)
                    heldItem.transform.localRotation = Quaternion.Euler(-90, 0, 90f);

                    // Szukamy punktu trzymania, gdyby drewno miało go w prefabie
                    Transform gripPoint = null;
                    foreach (Transform t in heldItem.GetComponentsInChildren<Transform>())
                    {
                        if (t.name == "GripPoint") { gripPoint = t; break; }
                    }

                    if (gripPoint != null)
                    {
                        Vector3 offset = heldItem.transform.position - gripPoint.position;
                        heldItem.transform.position = holdPosition.position + offset;
                    }
                    else
                    {
                        // Wyśrodkowanie z geometrii
                        Renderer[] rends = heldItem.GetComponentsInChildren<Renderer>();
                        if (rends.Length > 0)
                        {
                            Bounds totalBounds = rends[0].bounds;
                            for (int i = 1; i < rends.Length; i++) totalBounds.Encapsulate(rends[i].bounds);
                            Vector3 centerOffset = heldItem.transform.position - totalBounds.center;
                            heldItem.transform.position = holdPosition.position + centerOffset;
                        }
                        else
                        {
                            heldItem.transform.localPosition = Vector3.zero;
                        }
                    }
                }
                else
                {
                    // Używamy wspólnego ułożenia: jeśli to gotowa broń (skończona), powinna w dłoni 
                    // leżeć z tą samą rotacją, co domyślny uchwyt drewna wyżej.
                    if (heldItem.GetComponent<FinishedObject>() != null)
                    {
                        heldItem.transform.localRotation = Quaternion.Euler(craftedWeaponHoldRotationEuler);
                    }
                    else
                    {
                        // Inne przedmioty (metale, tygielki z fallbacku) korzystają ze standardowego ułożenia
                        heldItem.transform.localRotation = Quaternion.Euler(holdRotation);
                    }

                    // Szukamy specyficznego klejonego punktu dłoni (np. na drewnianej rączce broni)
                    Transform gripPoint = null;
                    foreach (Transform t in heldItem.GetComponentsInChildren<Transform>())
                    {
                        if (t.name == "GripPoint") { gripPoint = t; break; }
                    }
                    
                    if (gripPoint != null)
                    {
                        // Grip na holdPosition (+ opcjonalnie obok celownika dla skończonej broni)
                        Vector3 offset = heldItem.transform.position - gripPoint.position;
                        Vector3 target = holdPosition.position + offset;
                        if (heldItem.GetComponent<FinishedObject>() != null && playerCamera != null)
                        {
                            Vector3 o = craftedWeaponGripOffsetCameraSpace;
                            target += playerCamera.transform.right * o.x
                                + playerCamera.transform.up * o.y
                                + playerCamera.transform.forward * o.z;
                        }
                        heldItem.transform.position = target;
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

                FinishedObject pickedWeapon = heldItem.GetComponent<FinishedObject>();
                if (wheel != null && pickedWeapon != null)
                {
                    WeaponData wpn = new WeaponData(pickedWeapon.name, pickedWeapon.metalTier, pickedWeapon.bladeLength);
                    wheel.SetWheel(true);
                    wheel.UpdateWheel(wpn.GetNormalizedDamage(), wpn.GetNormalizedSpeed(), wpn.GetNormalizedAoE());
                }

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
        if (wheel != null) wheel.SetWheel(false);
    }
}