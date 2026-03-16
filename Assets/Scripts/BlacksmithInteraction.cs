using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public Transform holdPosition;
    public Vector3 holdRotation = new Vector3(90f, 0f, 0f);
    public Vector3 swordHoldRotation = new Vector3(0f, 0f, 0f);

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;
    private PlayerMovement playerMovement;
    private bool isInteractingWithNPC = false;
    private bool isInteractingWithTable = false;
    private MergingTable activeTable = null;

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
            TryPickUp(); // Najpierw próbujemy podnieść, jeśli trzymamy coś - to nic się nie stanie
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem != null) TryPlaceOnTableOrDrop();
            else TryPickUp();
        }
        
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

            //5. Sprawdzenie, czy to Kowadło
            AnvilStation anvil = hit.collider.GetComponentInParent<AnvilStation>();
            if (anvil != null && heldItem != null)
            {
                MetalPiece metal = heldItem.GetComponent<MetalPiece>();
                if (metal != null)
                {
                    anvil.EnterForgingMode(metal);
                    ClearHand();
                }
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

    //bool TryHitOrInteract()
    //{
    //    Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    //    if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
    //    {
    //        // Ten log powie nam DOKŁADNIE w co celujesz
    //        Debug.Log($"Raycast trafił w: {hit.collider.gameObject.name} na warstwie: {hit.collider.gameObject.layer}");

    //        MetalPiece metal = hit.collider.GetComponentInParent<MetalPiece>();
    //        if (metal != null)
    //        {
    //            Debug.Log("Znalazłem skrypt MetalPiece! Wysyłam uderzenie...");
    //            metal.HitMetal(hit.point, hit.normal);
    //            return true;
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Raycast w nic nie trafił. Może reachDistance jest za mały?");
    //    }
    //    return false;
    //}

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

            if (metal != null) targetObj = metal.gameObject;
            else if (wood != null) targetObj = wood.gameObject;
            else if (finished != null) targetObj = finished.gameObject;

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
                heldItem.transform.localPosition = Vector3.zero;

                // --- MAGIA: Różna rotacja dla miecza i dla surowców ---
                if (heldItem.GetComponentInParent<FinishedObject>() != null)
                {
                    // To jest gotowy miecz! Używamy nowej rotacji
                    heldItem.transform.localRotation = Quaternion.Euler(swordHoldRotation);
                }
                else
                {
                    // To jest sztabka lub drewno! Używamy standardowej rotacji
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
        heldItem.GetComponent<IPickable>()?.OnDrop();
        heldItem.transform.SetParent(null);
        if (heldItemRb != null) { heldItemRb.useGravity = true; heldItemRb.isKinematic = false; heldItemRb.detectCollisions = true; }
        ClearHand();
    }

    void ClearHand() { heldItem = null; heldItemRb = null; }
}