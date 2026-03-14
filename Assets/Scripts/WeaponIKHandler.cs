using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WeaponIKHandler : MonoBehaviour
{
    [Header("IK Targets (puste obiekty pod Weapon_Rig)")]
    public Transform rightHandTarget;
    public Transform leftHandTarget;

    [Header("Obiekt do trzymania")]
    public GameObject heldObject;

    [Header("Kość prawej dłoni (przeciągnij z hierarchii kości)")]
    public Transform rightHandBone;

    [Header("Offset pozycji i rotacji w dłoni")]
    public Vector3 holdOffset = Vector3.zero;
    public Vector3 holdRotationOffset = Vector3.zero;

    [Header("Rig")]
    public Rig weaponRig;

    [Header("Ustawienia IK")]
    [Tooltip("Czy kopiować rotację z gripów? Wyłącz jeśli nadgarstki się wykręcają.")]
    public bool useGripRotation = false;

    private Transform gripRight;
    private Transform gripLeft;

    void Start()
    {
        if (heldObject != null && rightHandBone != null)
        {
            heldObject.transform.SetParent(rightHandBone);
            heldObject.transform.localPosition = holdOffset;
            heldObject.transform.localRotation = Quaternion.Euler(holdRotationOffset);

            Rigidbody rb = heldObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            gripRight = heldObject.transform.Find("Grip_Right");
            gripLeft = heldObject.transform.Find("Grip_Left");
        }

        if (weaponRig != null && gripRight != null)
            weaponRig.weight = 1f;
    }

    void LateUpdate()
    {
        // Prawa ręka — tylko pozycja (rotacja z kości)
        if (gripRight != null && rightHandTarget != null)
        {
            rightHandTarget.position = gripRight.position;

            if (useGripRotation)
                rightHandTarget.rotation = gripRight.rotation;
        }

        // Lewa ręka — pozycja + patrzenie w kierunku sztabki
        if (gripLeft != null && leftHandTarget != null)
        {
            leftHandTarget.position = gripLeft.position;

            if (useGripRotation)
            {
                leftHandTarget.rotation = gripLeft.rotation;
            }
            else if (heldObject != null)
            {
                // Lewa dłoń patrzy w kierunku środka sztabki — naturalny chwyt
                Vector3 dirToObject = heldObject.transform.position - gripLeft.position;
                if (dirToObject.sqrMagnitude > 0.001f)
                    leftHandTarget.rotation = Quaternion.LookRotation(dirToObject, Vector3.up);
            }
        }
    }
}
