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

    // Zapami�tujemy, JAKI to typ przedmiotu
    private IronPiece heldIron;
    private WoodPiece heldWood;
    private LeatherPiece heldLeather;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // LEWY PRZYCISK MYSZY - Kucie m�otem
        if (Input.GetMouseButtonDown(0))
        {
            HitWithHammer();
        }

        // PRAWY PRZYCISK MYSZY - Podnoszenie / K�adzenie na st� / Upuszczanie
        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem == null) TryPickUp();
            else TryPlaceOrDrop();
        }

        // KLAWISZ E - Prze��czanie kamery na st�
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithTable();
        }
    }

    // NOWA FUNKCJA
    void TryInteractWithTable()
    {
        // Je�li trzymamy przedmiot w r�ku, mo�emy zablokowa� prze��czanie kamery (opcjonalnie)
        // if (heldItem != null) return; 

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                // Przekazujemy kamer� gracza do sto�u, �eby st� wiedzia�, co wy��czy� i co potem w��czy�
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
            // Sprawdzamy czy trafili�my w metal lub drewno
            IronPiece iron = hit.collider.GetComponent<IronPiece>();
            WoodPiece wood = hit.collider.GetComponent<WoodPiece>();
            LeatherPiece leather = hit.collider.GetComponent<LeatherPiece>();

            if (iron != null || wood != null || leather != null)
            {
                heldItem = hit.collider.gameObject;
                heldItemRb = heldItem.GetComponent<Rigidbody>();

                // Zapisujemy komponenty (jedno z nich b�dzie nullem, drugie nie)
                heldIron = iron;
                heldWood = wood;
                heldLeather = leather;

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

    // NOWA METODA: Sprawdza, czy patrzymy na st�. Je�li tak -> k�adzie. Je�li nie -> upuszcza.
    void TryPlaceOrDrop()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // Sprawdzamy czy patrzymy na st�
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                // Mamy �elazo i st� nie ma jeszcze �elaza
                if (heldIron != null && !table.HasMetal())
                {
                    table.PlaceMetal(heldIron);
                    ClearHand();
                    return; // Zako�cz, �eby nie upu�ci� przedmiotu na ziemi�
                }
                // Mamy drewno i st� nie ma jeszcze drewna
                else if (heldWood != null && !table.HasWood())
                {
                    table.PlaceWood(heldWood);
                    ClearHand();
                    return; // Zako�cz
                }

                else if (heldLeather != null && !table.HasLeather())
                {
                    table.PlaceLeather(heldLeather);
                    ClearHand();
                    return;
                }
            }
        }

        // Je�li nie trafili�my w st�, albo miejsce jest zaj�te - rzucamy na ziemi�
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

    // Ma�a funkcja czyszcz�ca nasze "r�ce"
    void ClearHand()
    {
        heldItem = null;
        heldItemRb = null;
        heldIron = null;
        heldWood = null;
        heldLeather = null;
    }
}
