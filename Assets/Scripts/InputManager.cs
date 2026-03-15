using UnityEngine;

public class InputManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Lewy przycisk myszy
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Jeśli kliknęliśmy w coś, co ma skrypt Tile
                if (hit.collider.TryGetComponent(out Tile tile))
                {
                    tile.OnClick();
                }
            }
        }
    }
}