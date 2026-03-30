using UnityEngine;

public class WeaponArea : MonoBehaviour
{
    [Header("Stół montażowy")]
    public MergingTable table;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Stare automatyczne zatrzaskiwanie broni zostało wyłączone z racji Drag&Drop. Area zostaje pusta.
    }
}
