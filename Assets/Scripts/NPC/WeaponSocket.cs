using UnityEngine;

public class WeaponSocket : MonoBehaviour
{
    [Tooltip("Nazwa kości w hierarchii modelu (np. jointItemR)")]
    public string socketBoneName = "jointItemR";

    [Header("Ustawienia pozycji i rotacji w dłoni NPC")]
    public Vector3 swordHoldPosition = Vector3.zero;
    public Vector3 swordHoldRotation = new Vector3(-90f, 0f, 0f);

    public Vector3 axeHoldPosition = new Vector3(0.15f, 0.9f, -0.14f);
    public Vector3 axeHoldRotation = new Vector3(-90f, 45f, 0f);

    Transform socketBone;
    [SerializeField] GameObject equippedWeapon;
    
    public string ownerName; 
    public CitizenData ownerData; 

    void Awake()
    {
        socketBone = FindBoneRecursive(transform, socketBoneName);
        if (socketBone == null)
            Debug.LogWarning($"[WeaponSocket] Nie znaleziono kości '{socketBoneName}' w {gameObject.name}");
    }

    // --- TYMCZASOWY KOD DO USTAWIENIA BRONI NA ŻYWO ---
    void Update()
    {
        if (equippedWeapon != null)
        {
            FinishedObject finishedObj = equippedWeapon.GetComponent<FinishedObject>();
            if (finishedObj != null)
                ApplyHoldTransform(equippedWeapon, finishedObj);
        }
    }
    // --------------------------------------------------

    void ApplyHoldTransform(GameObject weapon, FinishedObject finishedObj)
    {
        Vector3 holdPos;
        Quaternion holdRot;

        if (finishedObj.weaponType == WeaponType.Axe)
        {
            holdPos = axeHoldPosition;
            holdRot = Quaternion.Euler(axeHoldRotation);
        }
        else if (finishedObj.weaponType == WeaponType.Sword)
        {
            holdPos = swordHoldPosition;
            holdRot = Quaternion.Euler(swordHoldRotation);
        }
        else
        {
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            return;
        }

        weapon.transform.localRotation = holdRot;

        // Kompensacja dłuższego ostrza — rączka przesuwa się w tył,
        // więc przesuwamy broń wzdłuż jej osi Z o nadmiar długości
        float defaultLength = (finishedObj.weaponType == WeaponType.Sword) ? 0.7f : 0.35f;
        float extra = finishedObj.bladeLength - defaultLength;
        Vector3 handleShift = holdRot * new Vector3(0f, 0f, extra * 0.15f);

        weapon.transform.localPosition = holdPos + handleShift;
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

        weapon.transform.SetParent(socketBone);
        weapon.transform.localScale = Vector3.one;

        FinishedObject finishedObj = weapon.GetComponent<FinishedObject>();

        if (finishedObj != null)
            ApplyHoldTransform(weapon, finishedObj);
        else
        {
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }
        
        if (ownerData != null)
        {
            if (finishedObj != null)
                ownerData.equippedWeapon = new WeaponData(weapon.name, finishedObj.weaponType, finishedObj.metalTier, finishedObj.bladeLength);

            if (ownerData.savedWeaponTemplate != null)
                Object.Destroy(ownerData.savedWeaponTemplate);

            ownerData.weaponMeshes = SavedMeshData.SaveFrom(weapon);

            GameObject weaponClone = Object.Instantiate(weapon);
            weaponClone.name = weapon.name + "_template";
            weaponClone.SetActive(false);
            Object.DontDestroyOnLoad(weaponClone);
            ownerData.savedWeaponTemplate = weaponClone;

            if (gameManager.Instance != null)
                gameManager.Instance.NotifyGoldChanged();
                
            // Podmieniłem puste Notify na wywołanie nowo napisanej funkcji, 
            // która fizycznie ocenia wściekłość/zadowolenie zadania i dorzuca złota!
            npcPathFinding pathFinding = GetComponentInParent<npcPathFinding>();
            if (pathFinding != null)
                pathFinding.ProcessTransaction();
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