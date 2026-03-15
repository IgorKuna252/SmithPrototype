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

    // Zapamiêtujemy, JAKI to typ przedmiotu
    private IronPiece heldIron;
    private WoodPiece heldWood;
    private LeatherPiece heldLeather;

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

        // PRAWY PRZYCISK MYSZY - Podnoszenie / K³adzenie na stó³ / Upuszczanie
        if (Input.GetMouseButtonDown(1))
        {
            if (heldItem == null) TryPickUp();
            else TryPlaceOrDrop();
        }

        // KLAWISZ E - Prze³¹czanie kamery na stó³
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithTable();
        }
    }

    // NOWA FUNKCJA
    void TryInteractWithTable()
    {
        // Jeœli trzymamy przedmiot w rêku, mo¿emy zablokowaæ prze³¹czanie kamery (opcjonalnie)
        // if (heldItem != null) return; 

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                // Przekazujemy kamerê gracza do sto³u, ¿eby stó³ wiedzia³, co wy³¹czyæ i co potem w³¹czyæ
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
            // Sprawdzamy czy trafiliœmy w metal lub drewno
            IronPiece iron = hit.collider.GetComponent<IronPiece>();
            WoodPiece wood = hit.collider.GetComponent<WoodPiece>();
            LeatherPiece leather = hit.collider.GetComponent<LeatherPiece>();

            if (iron != null || wood != null || leather != null)
            {
                heldItem = hit.collider.gameObject;
                heldItemRb = heldItem.GetComponent<Rigidbody>();

                // Zapisujemy komponenty (jedno z nich bêdzie nullem, drugie nie)
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

    // NOWA METODA: Sprawdza, czy patrzymy na stó³. Jeœli tak -> k³adzie. Jeœli nie -> upuszcza.
    void TryPlaceOrDrop()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
        {
            // Sprawdzamy czy patrzymy na stó³
            MergingTable table = hit.collider.GetComponent<MergingTable>();
            if (table != null)
            {
                // Mamy ¿elazo i stó³ nie ma jeszcze ¿elaza
                if (heldIron != null && !table.HasMetal())
                {
                    table.PlaceMetal(heldIron);
                    ClearHand();
                    return; // Zakoñcz, ¿eby nie upuœciæ przedmiotu na ziemiê
                }
                // Mamy drewno i stó³ nie ma jeszcze drewna
                else if (heldWood != null && !table.HasWood())
                {
                    table.PlaceWood(heldWood);
                    ClearHand();
                    return; // Zakoñcz
                }

                else if (heldLeather != null && !table.HasLeather())
                {
                    table.PlaceLeather(heldLeather);
                    ClearHand();
                    return;
                }
            }
        }

        // Jeœli nie trafiliœmy w stó³, albo miejsce jest zajête - rzucamy na ziemiê
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

    // Ma³a funkcja czyszcz¹ca nasze "rêce"
    void ClearHand()
    {
        heldItem = null;
        heldItemRb = null;
        heldIron = null;
        heldWood = null;
        heldLeather = null;
    }
}