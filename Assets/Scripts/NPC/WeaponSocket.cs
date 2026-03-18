using UnityEngine;

public class WeaponSocket : MonoBehaviour
{
    [Tooltip("Nazwa kości w hierarchii modelu (np. jointItemR)")]
    public string socketBoneName = "jointItemR";

    Transform socketBone;
    [SerializeField] GameObject equippedWeapon;
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

        gripPoint = weapon.transform.Find("GripPoint");

        weapon.transform.SetParent(socketBone);
        weapon.transform.localScale = Vector3.one;

        if (gripPoint != null)
        {
            Quaternion rotCorrection = Quaternion.Inverse(gripPoint.localRotation);
            weapon.transform.localRotation = rotCorrection;
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

            // Zapisz typ broni (potrzebne do odtworzenia w scenie walki)
            FinishedObject finished = weapon.GetComponent<FinishedObject>();
            if (finished != null)
                ownerData.equippedWeaponType = finished.weaponType.ToString();

            Debug.Log($"[Socket] Zapisano {weapon.name} ({ownerData.equippedWeaponType}) w: {ownerData.name}");

            // Bug 6 fix: Synchronizuj też ExiledCitizen, żeby panel interakcji pokazywał broń
            ExiledCitizen citizen = GetComponent<ExiledCitizen>();
            if (citizen != null)
                citizen.equippedWeaponName = weapon.name;

            // Odśwież tablicę statystyk
            if (gameManager.Instance != null)
                gameManager.Instance.NotifyTeamChanged();
        }
        else
        {
            Debug.LogError("WeaponSocket nie ma przypisanego ownerData!");
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
