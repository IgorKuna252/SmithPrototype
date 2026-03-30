using UnityEngine;

public class FinishedObject : MonoBehaviour, IPickable
{
    [Header("Właściwości Broni")]
    public MetalType metalTier;
    public float bladeLength = 0f;

    [Header("Ustawienia na stojaku")]
    public Transform hangPoint;
    public void OnPickUp()
    {
        // Tutaj możesz dodać np. wyłączenie grawitacji (obsługuje to BlacksmithInteraction)
        Debug.Log("Podniesiono: " + gameObject.name);
    }

    public void OnDrop()
    {
        
    }
}