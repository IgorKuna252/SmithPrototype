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
        public Vector3 axeHoldPosition = new Vector3(0, 0, 0.2f); 
        public Vector3 axeHoldRotation = new Vector3(90, 0, 0);

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;
    private PlayerMovement playerMovement;
    private bool isInteractingWithNPC = false;
    private bool isInteractingWithTable = false;
    private MergingTable activeTable = null;
    public static BlacksmithInteraction Instance;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (isInteractingWithNPC)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseNPCInteraction();
            return;
        }

        if (isInteractingWithTable)
        {
            // Wychodzenie ze stołu (E lub ESC)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                CloseTableInteraction();
            }
            
            // Łączenie przedmiotów spacją
            if (Input.GetKeyDown(KeyCode.Space) && activeTable != null)
            {
                activeTable.CombineItems();
            }
            return; // Blokujemy resztę Update, żeby gracz nie rzucał promieniami w tle!
        }

        if (Input.GetKeyDown(KeyCode.E)) TryInteractWithEnvironmentE();

        if (Input.GetMouseButtonDown(0))
        {
            if (!TryHitOrInteract()) TryPickUp();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem != null) TryPlaceOnTableOrDrop();
            else TryPickUp();
        }
        
    }

    void Awake() { Instance = this; }

    public bool IsHoldingItem()
    {
        // Zwraca true, jeśli w ręku gracza znajduje się jakikolwiek obiekt
        return heldItem != null; 
    }

    public void CloseTableInteraction()
    {
        if (activeTable != null)
        {
            activeTable.ExitAssemblyMode();
        }
        isInteractingWithTable = false;
        activeTable = null;
        playerMovement.enabled = true; // Oddajemy chodzenie
    }

    void TryInteractWithEnvironmentE()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            npcPathFinding npc = hit.collider.GetComponent<npcPathFinding>();
            if (npc != null)
            {
                isInteractingWithNPC = true;
                playerMovement.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                NPCInteractionUI.Instance.Show(npc);
                return;
            }

            SceneTransition sceneTransition = hit.collider.GetComponent<SceneTransition>();
            if (sceneTransition != null) { sceneTransition.ChangeScene(); return; }

            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null) 
            { 
                activeTable = table;
                isInteractingWithTable = true;
                playerMovement.enabled = false; // Zabieramy chodzenie
                table.ToggleAssemblyCamera(playerCamera.gameObject); 
                return; 
            }

            GrindstoneStation station = hit.collider.GetComponent<GrindstoneStation>();
            if (station != null && heldItem != null)
            {
                MetalPiece metal = heldItem.GetComponent<MetalPiece>();
                if (metal != null) { station.EnterGrindingMode(metal); ClearHand(); }
                return;
            }
        }
    }

    public void CloseNPCInteraction()
    {
        isInteractingWithNPC = false;
        playerMovement.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        NPCInteractionUI.Instance.Hide();
    }

    bool TryHitOrInteract()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // Sprawdzamy MetalPiece bezpośrednio lub u rodzica (ważne gdy leży na stole)
            MetalPiece metal = hit.collider.GetComponent<MetalPiece>();
            if (metal == null) metal = hit.collider.GetComponentInParent<MetalPiece>();

            if (metal != null)
            {
                metal.HitMetal(hit.point, hit.normal);
                return true;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.Interact()) return true;
        }
        return false;
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

        if (mold != null && mold.IsReadyToExtract()) 
        {
            targetObj = mold.ExtractItem();
        }

        // stojaczki kodzik
        if (targetObj != null)
        {
            // Przypadek 1: Gracz celownikiem trafił idealnie w sam miecz.
            WeaponRack rack = targetObj.GetComponentInParent<WeaponRack>();
            if (rack != null) rack.TakeWeapon();
        }
        else
        {
            // Przypadek 2: Gracz nie trafił w miecz, ale trafił w duży hitbox stojaka.
            WeaponRack rack = hit.collider.GetComponent<WeaponRack>();
            if (rack != null && !rack.IsEmpty())
            {
                FinishedObject weaponFromRack = rack.TakeWeapon();
                if (weaponFromRack != null)
                {
                    targetObj = weaponFromRack.gameObject;
                }
            }
        }

        if (targetObj != null)
        {
            // Odpinamy od stołu/kowadła przed podniesieniem
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
            // --- MAGIA: Różna pozycja i rotacja TYLKO dla gotowych broni ---
            
            FinishedObject heldFinished = heldItem.GetComponentInParent<FinishedObject>();

            if (heldFinished != null)
            {
                // To jest gotowa broń zmontowana na stole, sprawdzamy jej TYP:
                if (heldFinished.weaponType == FinishedObject.WeaponType.Axe)
                {
                    heldItem.transform.localPosition = axeHoldPosition;
                    heldItem.transform.localRotation = Quaternion.Euler(axeHoldRotation);
                }
                else if (heldFinished.weaponType == FinishedObject.WeaponType.Sword)
                {
                    heldItem.transform.localPosition = swordHoldPosition;
                    heldItem.transform.localRotation = Quaternion.Euler(swordHoldRotation);
                }
                else
                {
                    // Failsafe, gdyby coś poszło nie tak
                    heldItem.transform.localPosition = Vector3.zero;
                    heldItem.transform.localRotation = Quaternion.Euler(holdRotation);
                }
            }
            else if (heldItem.GetComponent<Crucible>() != null)
            {
                // TYGIEL: Przewracamy go o 90 stopni na X
                heldItem.transform.localPosition = Vector3.zero;
                heldItem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                // To są wszystkie inne przedmioty: surowe części, drewno, sztabki
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
            // Stojaki
            WeaponRack rack = hit.collider.GetComponent<WeaponRack>();
            if (rack != null && rack.IsEmpty())
            {
                // Upewniamy się, że trzymamy w ręku gotowy miecz (FinishedObject), a nie deskę czy sztabkę
                FinishedObject heldFinishedWeapon = heldItem.GetComponent<FinishedObject>();
                if (heldFinishedWeapon != null)
                {
                    rack.PlaceWeapon(heldFinishedWeapon);
                    ClearHand();
                    return; // Przerywamy funkcję, żeby kod nie zrzucił miecza na ziemię!
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
        
        // WYMUSZONE WYBUDZENIE:
        heldItemRb.WakeUp(); 
        
        // Nadaj mu minimalny pęd, żeby "poczuł", że się rusza
        heldItemRb.AddForce(Vector3.down * 0.1f, ForceMode.Impulse);
    }
    
    heldItem.GetComponent<IPickable>()?.OnDrop();
    ClearHand();
}

    void ClearHand() { heldItem = null; heldItemRb = null; }
}