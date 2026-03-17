using UnityEngine;

public class FinishedObject : MonoBehaviour, IPickable
{
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