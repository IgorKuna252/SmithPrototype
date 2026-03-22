using UnityEngine;

public class WeaponArea : MonoBehaviour
{
    [Header("Sloty przypięcia")]
    public Transform metalSlot;
    public Transform handleSlot;

    [Header("Stół montażowy")]
    public MergingTable table;

    private MetalPiece placedMetal;
    private WoodPiece placedWood;

    public bool HasMetal() => placedMetal != null;
    public bool HasWood() => placedWood != null;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnTriggerEnter(Collider other)
    {
        MetalPiece metal = other.GetComponentInParent<MetalPiece>();
        if (metal != null && !HasMetal())
        {
            PlaceMetal(metal);
            return;
        }

        WoodPiece wood = other.GetComponentInParent<WoodPiece>();
        if (wood != null && !HasWood())
        {
            PlaceWood(wood);
        }
    }

    public void PlaceMetal(MetalPiece metal)
    {
        placedMetal = metal;
        SnapToSlot(metal.gameObject, metalSlot);
        Debug.Log($"[WeaponArea] Metal przypięty: {metal.metalTier}");
        TryPassToTable();
    }

    public void PlaceWood(WoodPiece wood)
    {
        placedWood = wood;
        SnapToSlot(wood.gameObject, handleSlot);
        Debug.Log($"[WeaponArea] Rączka przypięta: {wood.partType}");
        TryPassToTable();
    }

    void SnapToSlot(GameObject item, Transform slot)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        item.transform.SetParent(slot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    void TryPassToTable()
    {
        if (table == null) return;

        if (placedMetal != null && !table.HasMetal())
        {
            table.PlaceMetal(placedMetal);
            placedMetal = null;
        }

        if (placedWood != null && !table.HasWood())
        {
            table.PlaceWood(placedWood);
            placedWood = null;
        }
    }

    public MetalPiece TakeMetal()
    {
        MetalPiece metal = placedMetal;
        placedMetal = null;
        return metal;
    }

    public WoodPiece TakeWood()
    {
        WoodPiece wood = placedWood;
        placedWood = null;
        return wood;
    }
}
