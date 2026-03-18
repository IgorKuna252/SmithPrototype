using UnityEngine;

public class WeaponSocket : MonoBehaviour
{
    [Tooltip("Nazwa kości w hierarchii modelu (np. jointItemR)")]
    public string socketBoneName = "jointItemR";

    Transform socketBone;
    GameObject equippedWeapon;
    Transform gripPoint;
    public string ownerName; // Ustaw to w Inspektorze lub przy Equip
    public CitizenData ownerData; // Przypisz to przy spawnowaniu NPC!

    void Awake()
    {
        socketBone = FindBoneRecursive(transform, socketBoneName);
        if (socketBone == null)
            Debug.LogWarning($"[WeaponSocket] Nie znaleziono kości '{socketBoneName}' w {gameObject.name}");
    }

    public void EquipWeapon(GameObject weapon)
    {
        if (socketBone == null) return;

        UnequipWeapon();

        equippedWeapon = weapon;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Szukamy GripPoint na broni — child o nazwie "GripPoint"
        gripPoint = weapon.transform.Find("GripPoint");

        weapon.transform.SetParent(socketBone);
        weapon.transform.localScale = Vector3.one;

        if (gripPoint != null)
        {
            // Najpierw zastosuj odwrotność rotacji GripPointa
            Quaternion rotCorrection = Quaternion.Inverse(gripPoint.localRotation);
            weapon.transform.localRotation = rotCorrection;
            // Potem przesuń tak żeby GripPoint trafił w origin socketa
            weapon.transform.localPosition = -(rotCorrection * gripPoint.localPosition);
        }
        else
        {
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }
        
        if (ownerData != null)
        {
            ownerData.equippedWeaponName = weapon.name;
            Debug.Log($"Przypisano {weapon.name} do {ownerData.name}");
        }
    }

    public void UnequipWeapon()
    {
        if (equippedWeapon == null) return;

        equippedWeapon.transform.SetParent(null);

        Rigidbody rb = equippedWeapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        equippedWeapon = null;
        gripPoint = null;
    }

    public GameObject GetEquippedWeapon() => equippedWeapon;
    public Transform GetSocketBone() => socketBone;

    static Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }
}
