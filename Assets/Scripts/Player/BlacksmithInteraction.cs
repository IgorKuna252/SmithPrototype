using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public Transform holdPosition;
    public Vector3 holdRotation = new Vector3(90f, 0f, 0f);

    [Header("Pozycje trzymania w ręku - Typy")]
    public Vector3 swordHoldPosition = Vector3.zero;
    public Vector3 swordHoldRotation = Vector3.zero;
    public Vector3 axeHoldPosition = new Vector3(1f, 1f, 1f);
    public Vector3 axeHoldRotation = new Vector3(270, 270, 180);

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;
    private PlayerMovement playerMovement;

    private bool isInteractingWithNPC = false;
    private bool isInteractingWithTable = false;
    private MergingTable activeTable = null;
    [HideInInspector] public WheelController wheel;


    public Canvas playerUI;
    
    public static BlacksmithInteraction Instance;

    void Awake() { Instance = this; }

    void Start()
    {
        wheel = playerUI.GetComponent<WheelController>();
        playerCamera = GetComponentInChildren<Camera>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // 1. BLOKADA NPC
        if (isInteractingWithNPC)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseNPCInteraction();
            return;
        }

        // 2. BLOKADA KAMERY STOŁU 
        if (isInteractingWithTable)
        {
            // ŁĄCZENIE: Jeśli wciśniesz E (lub Spację) i stół ma obie części -> Łączymy!
            if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) && activeTable != null && activeTable.HasMetal() && activeTable.HasWood())
            {
                activeTable.CombineItems();
                CloseTableInteraction();
            }
            // WYJŚCIE: Jeśli wciśniesz ESC (albo E, gdy brakuje części) -> Po prostu wychodzisz
            else if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetKeyDown(KeyCode.E) && (!activeTable.HasMetal() || !activeTable.HasWood())))
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
                    wheel.SetWheel(false);
                    ClearHand();

                    NPCCombat combat = socket.GetComponent<NPCCombat>();
                    if (combat != null) combat.SetMode(NPCCombatMode.ArmedIdle);
                    return;
                }
            }

            // 2. ROZMOWA Z NPC
            npcPathFinding npc = hit.collider.GetComponent<npcPathFinding>() ?? hit.collider.GetComponentInParent<npcPathFinding>();
            if (npc != null)
            {
                isInteractingWithNPC = true;
                playerMovement.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
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
                    // Jeśli trzymasz rzecz -> Połóż na stół pod klawiszem E
                    MetalPiece metal = heldItem.GetComponent<MetalPiece>();
                    WoodPiece wood = heldItem.GetComponent<WoodPiece>();
                    if (metal != null && !table.HasMetal()) { table.PlaceMetal(metal); ClearHand(); return; }
                    if (wood != null && !table.HasWood()) { table.PlaceWood(wood); ClearHand(); return; }
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

    public void CloseNPCInteraction()
    {
        isInteractingWithNPC = false;
        playerMovement.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        NPCInteractionUI.Instance.Hide();
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

            if (mold != null && mold.IsReadyToExtract()) targetObj = mold.ExtractItem();

            // Zdejmowanie ze stojaka
            if (targetObj != null)
            {
                WeaponRack rack = targetObj.GetComponentInParent<WeaponRack>();
                if (rack != null) rack.TakeWeapon();
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
                heldItem = targetObj;
                heldItemRb = heldItem.GetComponent<Rigidbody>();

                if (heldItemRb != null)
                {
                    heldItemRb.useGravity = false;
                    heldItemRb.isKinematic = true;
                    heldItemRb.detectCollisions = false;
                }

                heldItem.transform.SetParent(holdPosition);

                FinishedObject heldFinished = heldItem.GetComponentInParent<FinishedObject>();
                if (heldFinished != null)
                {
                    if (heldFinished.weaponType == WeaponType.Axe)
                    {
                        heldItem.transform.localPosition = axeHoldPosition;
                        heldItem.transform.localRotation = Quaternion.Euler(axeHoldRotation);
                    }
                    else if (heldFinished.weaponType == WeaponType.Sword)
                    {
                        heldItem.transform.localPosition = swordHoldPosition;
                        heldItem.transform.localRotation = Quaternion.Euler(swordHoldRotation);
                    }
                    else
                    {
                        heldItem.transform.localPosition = Vector3.zero;
                        heldItem.transform.localRotation = Quaternion.Euler(holdRotation);
                    }

                    WeaponData tempWeapon = new WeaponData(heldFinished.itemName, heldFinished.weaponType, heldFinished.metalTier, heldFinished.bladeLength);
                    wheel.SetWheel(true);
                    wheel.UpdateWheel(tempWeapon.GetNormalizedDamage(), tempWeapon.GetNormalizedSpeed(), tempWeapon.GetNormalizedAoE());
                }
                else if (heldItem.GetComponent<Crucible>() != null)
                {
                    heldItem.transform.localPosition = Vector3.zero;
                    heldItem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    heldItem.transform.localPosition = Vector3.zero;
                    heldItem.transform.localRotation = Quaternion.Euler(holdRotation);
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

                if (heldMetal != null && !table.HasMetal()) { table.PlaceMetal(heldMetal); ClearHand(); return; }
                else if (heldWood != null && !table.HasWood()) { table.PlaceWood(heldWood); ClearHand(); return; }
            }
        }
        DropItem();
    }

    void DropItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = false;
            heldItemRb.useGravity = true;
            heldItemRb.detectCollisions = true;

            heldItemRb.WakeUp();
            heldItemRb.AddForce(Vector3.down * 0.1f, ForceMode.Impulse);
        }

        heldItem.GetComponent<IPickable>()?.OnDrop();
        wheel.SetWheel(false);
        ClearHand();
    }

    void ClearHand() { heldItem = null; heldItemRb = null; }
}