using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")] public float reachDistance = 3f;
    public Transform holdPosition;

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!TryPickUp())
                TryInteract(KeyCode.Mouse0);
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem != null)
                DropItem();
            else
                TryInteract(KeyCode.Mouse1);
        }
    }

    bool TryInteract(KeyCode key)
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null) return false;

            interactable.Interact(key);
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