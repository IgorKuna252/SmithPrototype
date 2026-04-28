using UnityEngine;

public class FinishedObject : MonoBehaviour, IPickable
{
    [Header("Właściwości Broni")]
    public MetalType metalTier;
    public float bladeLength = 0f;

    // Transform ostrza (zachowany po zniszczeniu MetalPiece w MergingTable),
    // używany przez ForgeShapeEvaluator do wyrównania broni do kamery.
    public Transform bladeRoot;

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