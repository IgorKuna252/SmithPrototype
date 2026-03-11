using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public Transform holdPosition; // Miejsce, gdzie trzymamy przedmiot

    private Camera playerCamera;
    private GameObject heldItem;
    private Rigidbody heldItemRb;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // LEWY PRZYCISK MYSZY - Kucie m³otem
        if (Input.GetMouseButtonDown(0))
        {
            HitWithHammer();
        }

        // PRAWY PRZYCISK MYSZY - Podnoszenie / Upuszczanie
        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem == null)
            {
                TryPickUp();
            }
            else
            {
                DropItem();
            }
        }
    }

    void HitWithHammer()
    {
        // Zabezpieczenie: nie kujemy metalu, kiedy trzymamy go w powietrzu!
        if (heldItem != null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            IronPiece iron = hit.collider.GetComponent<IronPiece>();
            if (iron != null)
            {
                iron.HitMetal();
            }
        }
    }

    void TryPickUp()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            IronPiece iron = hit.collider.GetComponent<IronPiece>();
            if (iron != null)
            {
                heldItem = hit.collider.gameObject;
                heldItemRb = heldItem.GetComponent<Rigidbody>();

                if (heldItemRb != null)
                {
                    // Wy³¹czamy grawitacjê i fizykê na czas trzymania, ¿eby obiekt nie wariowa³
                    heldItemRb.useGravity = false;
                    heldItemRb.isKinematic = true;
                }

                // Podpinamy sztabkê pod nasz punkt trzymania
                heldItem.transform.SetParent(holdPosition);
                heldItem.transform.localPosition = Vector3.zero; // Œrodkujemy w punkcie
                heldItem.transform.localRotation = Quaternion.identity; // Resetujemy obrót
            }
        }
    }

    void DropItem()
    {
        // Odepinamy sztabkê od gracza
        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            // W³¹czamy grawitacjê i fizykê z powrotem, ¿eby sztabka spad³a
            heldItemRb.useGravity = true;
            heldItemRb.isKinematic = false;
        }

        heldItem = null;
        heldItemRb = null;
    }
}