using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")] 
    public float reachDistance = 3f;
    public Transform holdPosition;

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
        if (isInteractingWithNPC)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseNPCInteraction();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
            TryInteractWithNPC();

        if (Input.GetMouseButtonDown(0))
        {
            if (!TryInteract(KeyCode.Mouse0))
                TryPickUp();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem != null)
                DropItem();
            else
                TryPickUp();
        }
    }

    void TryInteractWithNPC()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            npcPathFinding npc = hit.collider.GetComponent<npcPathFinding>();
            if (npc == null) return;

            isInteractingWithNPC = true;
            playerMovement.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            NPCInteractionUI.Instance.Show(npc);
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

    bool TryInteract(KeyCode key)
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null) return false;

            if (!interactable.Interact(key)) return false;

            // Jeśli to IronPiece, przekaż punkt uderzenia do deformacji mesh
            IronPiece iron = hit.collider.GetComponent<IronPiece>();
            if (iron != null)
            {
                iron.HitMetal(hit.point, hit.normal);
            }

            return true;
        }
        return false;
    }

    bool TryPickUp()
    {
        if (heldItem != null) return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            IPickable pickable = hit.collider.GetComponent<IPickable>();
            if (pickable == null) return false;

            heldItem = hit.collider.gameObject;
            heldItemRb = heldItem.GetComponent<Rigidbody>();

            if (heldItemRb != null)
            {
                heldItemRb.useGravity = false;
                heldItemRb.isKinematic = true;
            }

            heldItem.transform.SetParent(holdPosition);
            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;

            pickable.OnPickUp();
            return true;
        }
        return false;
    }

    void DropItem()
    {
        heldItem.GetComponent<IPickable>()?.OnDrop();
        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.useGravity = true;
            heldItemRb.isKinematic = false;
        }

        heldItem = null;
        heldItemRb = null;
    }
}