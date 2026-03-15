using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isOwned = false;

    // Metoda zmieniająca kolor w zależności od stanu
    public void UpdateVisuals()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
    
        // Używamy tej samej logiki wykrywania co w generatorze
        int propertyId = renderer.sharedMaterial.HasProperty("_BaseColor") ? Shader.PropertyToID("_BaseColor") : Shader.PropertyToID("_Color");
    
        block.SetColor(propertyId, isOwned ? Color.green : Color.red);
        renderer.SetPropertyBlock(block);
    }
    
    // Metoda wywoływana przez system kliknięć
    public void OnClick()
    {
        TileManager.Instance.OpenTileUI(this);
    }
}