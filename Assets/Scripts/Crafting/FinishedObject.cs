using UnityEngine;

public class FinishedObject : MonoBehaviour, IPickable
{
    [Header("Ustawienia na stojaku")]
    public Transform hangPoint;
    public void OnPickUp()
    {
        // Tutaj możesz dodać np. wyłączenie grawitacji (obsługuje to BlacksmithInteraction)
        Debug.Log("Podniesiono: " + gameObject.name);
    }

    public void OnDrop()
    {
        // Tutaj możesz dodać dźwięk upadku miecza o ziemię
    }
}