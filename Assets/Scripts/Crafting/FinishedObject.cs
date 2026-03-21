using UnityEngine;

public class FinishedObject : MonoBehaviour, IPickable
{
    public WeaponType weaponType = WeaponType.None;
    public MetalType metalTier = MetalType.Iron;
    public float bladeLength;
    public string itemName = "Wykuty Miecz";

    [Header("Ustawienia na stojaku")]
    public Transform hangPoint;
    public void OnPickUp()
    {
        // Tutaj możesz dodać np. wyłączenie grawitacji (obsługuje to BlacksmithInteraction)
        Debug.Log("Podniesiono: " + itemName);
    }

    public void OnDrop()
    {
        // Tutaj możesz dodać dźwięk upadku miecza o ziemię
    }
}