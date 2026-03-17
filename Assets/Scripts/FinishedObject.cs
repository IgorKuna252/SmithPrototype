using UnityEngine;

public class FinishedObject : MonoBehaviour, IPickable
{
    // Definiujemy, czym może być gotowy obiekt
    public enum WeaponType { None, Sword, Axe }
    public WeaponType weaponType = WeaponType.None;
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