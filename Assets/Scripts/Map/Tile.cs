using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isOwned = false;
    public int difficulty; // Zostawiamy tę zmienną

    public void UpdateVisuals()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        
        int propertyId = renderer.sharedMaterial.HasProperty("_BaseColor") 
            ? Shader.PropertyToID("_BaseColor") 
            : Shader.PropertyToID("_Color");
        
        block.SetColor(propertyId, isOwned ? Color.green : Color.red);
        renderer.SetPropertyBlock(block);
    }

    public void OnClick()
    {
        TileManager.Instance.OpenTileUI(this); // Przekazujemy "this", czyli cały klocek
    }
}