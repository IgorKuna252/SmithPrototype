using UnityEngine;

public class IK_Implementation : MonoBehaviour
{
    [Header("IK Targets (children of Rig)")]
    public Transform rightHandTarget;
    public Transform leftHandTarget;

    [Header("Grip Points (children of Sword)")]
    public Transform rightGrip;
    public Transform leftGrip;

    [Header("Weapon Hold Point")]
    public Transform weaponHoldPoint;
    public Transform weapon;

    [Header("Hand Rotation Offset (tweak in inspector)")]
    public Vector3 rightHandRotationOffset;
    public Vector3 leftHandRotationOffset;

    [Header("Swing Settings")]
    public float swingRange = 0.6f;
    public float swingSpeed = 5f;

    void LateUpdate()
    {
        if (weapon != null && weaponHoldPoint != null)
        {
            float t = Mathf.Sin(Time.time * swingSpeed);
            float horizontal = t * swingRange;
            float depth = -Mathf.Abs(t) * swingRange * 0.5f;
            weapon.position = weaponHoldPoint.position
                + transform.right * horizontal
                + transform.forward * depth;
            weapon.rotation = weaponHoldPoint.rotation;
        }

        if (rightHandTarget != null && rightGrip != null)
        {
            rightHandTarget.position = rightGrip.position;
            rightHandTarget.rotation = rightGrip.rotation * Quaternion.Euler(rightHandRotationOffset);
        }

        if (leftHandTarget != null && leftGrip != null)
        {
            leftHandTarget.position = leftGrip.position;
            leftHandTarget.rotation = leftGrip.rotation * Quaternion.Euler(leftHandRotationOffset);
        }
    }
}
