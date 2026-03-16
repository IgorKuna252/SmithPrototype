using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public Transform holdPosition;
    public Vector3 holdRotation = new Vector3(90f, 0f, 0f);

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;
    private PlayerMovement playerMovement;
    private bool isInteractingWithNPC = false;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // Jeśli rozmawiamy z NPC, blokujemy resztę akcji
        if (isInteractingWithNPC)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseNPCInteraction();
            return;
        }

        // KLAWISZ E - Interakcje ze środowiskiem (NPC, Sceny, Stół montażowy, Szlifierka)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithEnvironmentE();
        }

        // LEWY PRZYCISK MYSZY - Używanie (Kucie) lub Podnoszenie
        if (Input.GetMouseButtonDown(0))
        {
            // Najpierw próbujemy użyć obiektu (np. uderzyć młotem)
            if (!TryHitOrInteract())
            {
                // Jeśli się nie udało, próbujemy podnieść
                TryPickUp();
            }
        }

        // PRAWY PRZYCISK MYSZY - Kładzenie na stół, Upuszczanie lub Podnoszenie
        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem != null)
                TryPlaceOnTableOrDrop();
            else
                TryPickUp();
        }
    }

    void TryInteractWithEnvironmentE()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // 1. Sprawdzenie, czy to NPC
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

            // 2. Sprawdzenie, czy to obiekt do zmiany sceny
            SceneTransition sceneTransition = hit.collider.GetComponent<SceneTransition>();
            if (sceneTransition != null)
            {
                sceneTransition.ChangeScene();
                return;
            }

            // 3. Sprawdzenie, czy to stół montażowy
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                table.ToggleAssemblyCamera(playerCamera.gameObject);
                return;
            }

            // 4. Sprawdzenie, czy to szlifierka - działa tylko gdy coś trzymamy i jest to MetalPiece
            GrindstoneStation station = hit.collider.GetComponent<GrindstoneStation>();
            if (station != null && heldItem != null)
            {
                MetalPiece metal = heldItem.GetComponent<MetalPiece>();
                if (metal != null)
                {
                    // Upewnij się, że GrindstoneStation w Twoim projekcie zostało zaktualizowane, 
                    // aby przyjmować MetalPiece zamiast IronPiece!
                    station.EnterGrindingMode(metal);
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

    bool TryHitOrInteract()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // Ten log powie nam DOKŁADNIE w co celujesz
            Debug.Log($"Raycast trafił w: {hit.collider.gameObject.name} na warstwie: {hit.collider.gameObject.layer}");

            MetalPiece metal = hit.collider.GetComponentInParent<MetalPiece>();
            if (metal != null)
            {
                Debug.Log("Znalazłem skrypt MetalPiece! Wysyłam uderzenie...");
                metal.HitMetal(hit.point, hit.normal);
                return true;
            }
        }
        else
        {
            Debug.Log("Raycast w nic nie trafił. Może reachDistance jest za mały?");
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
            IPickable pickable = hit.collider.GetComponentInParent<IPickable>();

            // Sprawdzamy nowy system z Main (IPickable)
            if (pickable != null)
            {
                targetObj = ((MonoBehaviour)pickable).gameObject;
            }
            else
            {
                // Fallback na starszy system (szukanie w rodzicach)
                MetalPiece metal = hit.collider.GetComponentInParent<MetalPiece>();
                WoodPiece wood = hit.collider.GetComponentInParent<WoodPiece>();
                FinishedObject finishedWeapon = hit.collider.GetComponentInParent<FinishedObject>();

                if (metal != null) targetObj = metal.gameObject;
                else if (wood != null) targetObj = wood.gameObject;
                else if (finishedWeapon != null) targetObj = finishedWeapon.gameObject;
            }

            // Jeśli znaleźliśmy obiekt do podniesienia
            if (targetObj != null)
            {
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
                heldItem.transform.localRotation = Quaternion.Euler(holdRotation);

                pickable?.OnPickUp();
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
            // Sprawdzamy czy patrzymy na stół montażowy
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                MetalPiece heldMetal = heldItem.GetComponent<MetalPiece>();
                WoodPiece heldWood = heldItem.GetComponent<WoodPiece>();

                // Kładziemy metal
                if (heldMetal != null && !table.HasMetal())
                {
                    table.PlaceMetal(heldMetal);
                    ClearHand();
                    return;
                }
                // Kładziemy drewno
                else if (heldWood != null && !table.HasWood())
                {
                    table.PlaceWood(heldWood);
                    ClearHand();
                    return;
                }
            }
        }

        // Jeśli nie trafiliśmy w stół lub nie mieliśmy odpowiedniego przedmiotu - upuszczamy
        DropItem();
    }

    void DropItem()
    {
        heldItem.GetComponent<IPickable>()?.OnDrop();
        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.useGravity = true;
            heldItemRb.isKinematic = false;
            heldItemRb.detectCollisions = true;
        }

        ClearHand();
    }

    void ClearHand()
    {
        heldItem = null;
        heldItemRb = null;
    }
}