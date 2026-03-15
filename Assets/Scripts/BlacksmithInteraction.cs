using UnityEngine;

public class BlacksmithInteraction : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    public float reachDistance = 3f;
    public Transform holdPosition; // Miejsce, gdzie trzymamy przedmiot

    private Camera playerCamera;

    // Zmienne do trzymanego przedmiotu
    private GameObject heldItem;
    private Rigidbody heldItemRb;

    // Zapamiętujemy, JAKI to typ przedmiotu
    private MetalPiece heldMetal;
    private WoodPiece heldWood;
    private FinishedObject heldFinishedWeapon;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // LEWY PRZYCISK MYSZY - Kucie młotem
        if (Input.GetMouseButtonDown(0))
        {
            HitWithHammer();
        }

        // PRAWY PRZYCISK MYSZY - Podnoszenie / Kładzenie na stół / Upuszczanie
        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem == null) TryPickUp();
            else TryPlaceOrDrop();
        }

        // KLAWISZ E - Przełączanie kamery na stół
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithTable();
        }
    }

    void TryInteractWithTable()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                table.ToggleAssemblyCamera(playerCamera.gameObject);
            }
        }
    }

    void HitWithHammer()
    {
        if (heldItem != null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // Tutaj też dodajemy InParent, na wypadek gdybyś kiedyś zrobił ostrze z kilku części!
            MetalPiece metal = hit.collider.GetComponentInParent<MetalPiece>();
            if (metal != null)
            {
                metal.HitMetal();
            }
        }
    }

    void TryPickUp()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // ZMIANA 1: Szukamy skryptów również na rodzicach!
            MetalPiece metal = hit.collider.GetComponentInParent<MetalPiece>();
            WoodPiece wood = hit.collider.GetComponentInParent<WoodPiece>();
            FinishedObject finishedWeapon = hit.collider.GetComponentInParent<FinishedObject>();

            if (metal != null || wood != null || finishedWeapon != null)
            {
                // ZMIANA 2: Podnosimy cały GŁÓWNY obiekt ze skryptem, a nie samą trafioną belkę!
                if (metal != null) heldItem = metal.gameObject;
                else if (wood != null) heldItem = wood.gameObject;
                else if (finishedWeapon != null) heldItem = finishedWeapon.gameObject;

                heldItemRb = heldItem.GetComponent<Rigidbody>();

                heldMetal = metal;
                heldWood = wood;
                heldFinishedWeapon = finishedWeapon;

                if (heldItemRb != null)
                {
                    heldItemRb.useGravity = false;
                    heldItemRb.isKinematic = true;
                }

                heldItem.transform.SetParent(holdPosition);
                heldItem.transform.localPosition = Vector3.zero;
                heldItem.transform.localRotation = Quaternion.identity;
            }
        }
    }

    void TryPlaceOrDrop()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // Sprawdzamy czy patrzymy na stół
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                // Mamy metal i stół nie ma jeszcze metalu
                if (heldMetal != null && !table.HasMetal())
                {
                    table.PlaceMetal(heldMetal);
                    ClearHand();
                    return; 
                }
                // Mamy drewno i stół nie ma jeszcze drewna
                else if (heldWood != null && !table.HasWood())
                {
                    table.PlaceWood(heldWood);
                    ClearHand();
                    return; 
                }
            }
        }

        // Jeśli nie trafiliśmy w stół, albo miejsce jest zajęte - rzucamy na ziemię
        DropItem();
    }

    void DropItem()
    {
        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.useGravity = true;
            heldItemRb.isKinematic = false;
        }

        ClearHand();
    }

    void ClearHand()
    {
        heldItem = null;
        heldItemRb = null;
        heldMetal = null;
        heldWood = null;
        heldFinishedWeapon = null;
    }
}