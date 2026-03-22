using UnityEngine;

public class WeaponArea : MonoBehaviour
{
    [Header("Stół montażowy")]
    public MergingTable table;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (table == null) return;

        MetalPiece metal = other.GetComponentInParent<MetalPiece>();
        if (metal != null && !table.HasMetal())
        {
            table.PlaceMetal(metal);
            Debug.Log($"[WeaponArea] Metal przekazany na stół: {metal.metalTier}");
            return;
        }

        WoodPiece wood = other.GetComponentInParent<WoodPiece>();
        if (wood != null && !table.HasWood())
        {
            table.PlaceWood(wood);
            Debug.Log($"[WeaponArea] Rączka przekazana na stół: {wood.partType}");
        }
    }
}
