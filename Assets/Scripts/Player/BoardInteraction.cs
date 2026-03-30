using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class BoardInteraction : MonoBehaviour, IInteractable
{
    [Header("Co ma się stać po wciśnięciu E?")]
    [Tooltip("Możesz tu podpiąć funkcję DaySystemManager.EndDayButton!")]
    public UnityEvent onInteract;

    public bool Interact()
    {
        Debug.Log("[BoardInteraction] Gracz wcisnął E na Tablicy Zadań!");
        
        if (onInteract != null)
        {
            onInteract.Invoke();
        }
        return true;
    }
}
