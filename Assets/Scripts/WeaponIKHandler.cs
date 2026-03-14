using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WeaponIKHandler : MonoBehaviour
{
    [Header("IK Target lewej ręki (pusty obiekt pod Weapon_Rig)")]
    public Transform leftHandTarget;

    [Header("Kość prawej dłoni (przeciągnij z hierarchii kości)")]
    public Transform rightHandBone;

    [Header("Offset pozycji i rotacji miecza w prawej dłoni")]
    public Vector3 holdOffset = Vector3.zero;
    public Vector3 holdRotationOffset = new Vector3(0f, 0f, 90f);

    [Header("Rig")]
    public Rig weaponRig;

    [Header("Obiekt do trzymania (opcjonalny - można ustawić dynamicznie)")]
    public GameObject heldObject;

    private Transform gripLeft;

    void Start()
    {
        // Na start wyłącz IK
        if (weaponRig != null)
            weaponRig.weight = 0f;

        if (heldObject != null)
            SetHeldObject(heldObject);
    }

    /// <summary>
    /// Dynamicznie przypisuje obiekt do trzymania.
    /// Obiekt musi mieć child "Grip_Left" do pozycjonowania lewej ręki.
    /// Prawa ręka trzyma naturalnie przez parenting do kości.
    /// </summary>
    public void SetHeldObject(GameObject obj)
    {
        if (obj == null || rightHandBone == null)
        {
            Debug.LogWarning($"SetHeldObject: obj={obj}, rightHandBone={rightHandBone}");
            return;
        }

        if (heldObject != null && heldObject != obj)
            ClearHeldObject();

        heldObject = obj;

        // Parentuj do kości prawej ręki (worldPositionStays=false żeby odziedziczyć pozycję)
        heldObject.transform.SetParent(rightHandBone, false);
        heldObject.transform.localPosition = holdOffset;
        heldObject.transform.localRotation = Quaternion.Euler(holdRotationOffset);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Szukaj tylko lewego gripa - prawa ręka nie potrzebuje IK
        gripLeft = heldObject.transform.Find("Grip_Left");

        if (gripLeft == null)
            Debug.LogWarning($"Brak 'Grip_Left' na {obj.name} — lewa ręka nie dociągnie do miecza");

        // Włącz IK tylko jeśli mamy grip dla lewej ręki
        if (weaponRig != null && gripLeft != null)
            weaponRig.weight = 1f;
    }

    public void ClearHeldObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        heldObject = null;
        gripLeft = null;

        if (weaponRig != null)
            weaponRig.weight = 0f;
    }

    public bool IsHolding => heldObject != null;

    void LateUpdate()
    {
        if (heldObject == null || gripLeft == null || leftHandTarget == null) return;

        // Tylko lewa ręka dociąga przez IK do gripa na mieczu
        // Prawa ręka trzyma miecz naturalnie (parenting do kości)
        leftHandTarget.position = gripLeft.position;

        Vector3 dirToObject = heldObject.transform.position - gripLeft.position;
        if (dirToObject.sqrMagnitude > 0.001f)
            leftHandTarget.rotation = Quaternion.LookRotation(dirToObject, Vector3.up);
    }
}
