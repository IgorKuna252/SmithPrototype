using UnityEngine;
using UnityEngine.EventSystems; // KLUCZOWE: Dodaj to!

public class InputManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 1. SPRAWDŹ: Czy myszka jest nad obiektem UI?
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Tak, kliknąłeś w UI (np. checkbox lub panel). 
                // Przerywamy, nie strzelamy Raycastem w świat 3D.
                return;
            }

            // 2. Jeśli nie kliknąłeś w UI, dopiero wtedy szukaj kafelka
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.TryGetComponent(out Tile tile))
                {
                    tile.OnClick();
                }
            }
        }
    }
}