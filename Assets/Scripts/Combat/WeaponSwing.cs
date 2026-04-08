using System.Collections;
using UnityEngine;

/// <summary>
/// Zamach FPP: tilt na bok, slash symetryczny wokół kierunku patrzenia, szybkość z WeaponData/attackSpeed,
/// zasięg z bladeLength/range. Tylko w dłoni (holdPosition). Faza cięcia włącza fizyczny WeaponHitbox.
/// </summary>
public class WeaponSwing : MonoBehaviour
{
    [Header("Kierunek slasha")]
    public bool invertDirections;
    public float centerYawBiasDegrees;

    [Tooltip("Kierunek ostrza w lokalnej przestrzeni broni (auto-detekcja z rendererów).")]
    public bool autoDetectBladeDirection = true;
    public Vector3 bladeDirectionLocalFallback = Vector3.up;

    [Header("Skala od zasięgu (WeaponData.range + bladeLength)")]
    public float minSlashDegrees = 30f;
    public float maxSlashDegrees = 180f;
    public float minSideTiltDegrees = 20f;
    public float maxSideTiltDegrees = 90f;
    public float minRangeForMapping = 1.2f;
    public float maxRangeForMapping = 2.6f;
    public float bladeLengthToRangeMultiplier = 1f;

    [Header("Czasy bazowe × skala z attackSpeed")]
    public float windupDuration = 0.12f;
    public float slashDuration = 0.22f;
    public float returnDuration = 0.2f;
    public float returnSettleDuration = 0.04f;
    public float returnEasePower = 2.2f;
    [Range(0f, 0.9f)] public float returnLocalBlendStart = 0.78f;

    [Header("Skala szybkości")]
    public float referenceAttackSpeed = 1f;
    public float minDurationScale = 0.35f;
    public float maxDurationScale = 3f;

    [Header("Pivot")]
    public string gripChildName = "GripPoint";
    public Vector3 gripPivotLocalFallback = Vector3.zero;

    public Camera swingCamera;
    public bool invertTilt;

    bool isSwinging;
    Transform gripTransform;
    FinishedObject finishedObject;
    WeaponHitbox weaponHitbox;
    Rigidbody weaponRb;
    Vector3 bladeDirectionLocalCached = Vector3.up;

    void Awake()
    {
        ResolveGrip();
        finishedObject = GetComponent<FinishedObject>();
        weaponHitbox = GetComponent<WeaponHitbox>();
        if (weaponHitbox == null) weaponHitbox = GetComponentInChildren<WeaponHitbox>();
        weaponRb = GetComponent<Rigidbody>();
        if (bladeDirectionLocalFallback.sqrMagnitude > 1e-6f)
            bladeDirectionLocalCached = bladeDirectionLocalFallback.normalized;
        else
            bladeDirectionLocalCached = Vector3.up;
    }

    void Start()
    {
        if (weaponHitbox == null) weaponHitbox = GetComponentInChildren<WeaponHitbox>();
    }

    float GetSpeedDurationScale()
    {
        if (finishedObject == null) return 1f;
        var wd = new WeaponData(gameObject.name, finishedObject.metalTier, finishedObject.bladeLength);
        if (wd.attackSpeed <= 1e-4f) return maxDurationScale;
        return Mathf.Clamp(referenceAttackSpeed / wd.attackSpeed, minDurationScale, maxDurationScale);
    }

    WeaponData GetWd()
    {
        if (finishedObject == null) return null;
        return new WeaponData(gameObject.name, finishedObject.metalTier, finishedObject.bladeLength);
    }

    float GetReachNormalized01()
    {
        var wd = GetWd();
        if (wd == null) return 0f;
        float bladePart = Mathf.Max(wd.bladeLength, 0f) * bladeLengthToRangeMultiplier;
        float rangeStat = Mathf.Max(wd.GetRange(), 0f);
        float effectiveReach = Mathf.Max(rangeStat, bladePart);
        return Mathf.InverseLerp(minRangeForMapping, maxRangeForMapping, effectiveReach);
    }

    void ResolveGrip()
    {
        if (gripTransform == null && !string.IsNullOrEmpty(gripChildName))
            gripTransform = transform.Find(gripChildName);
    }

    Vector3 GripLocalOnWeaponRoot()
    {
        ResolveGrip();
        return gripTransform != null ? gripTransform.localPosition : gripPivotLocalFallback;
    }

    Camera GetCam()
    {
        if (swingCamera != null) return swingCamera;
        if (Camera.main != null) return Camera.main;
        return transform.root.GetComponentInChildren<Camera>();
    }

    bool IsHeldInPlayerHand()
    {
        BlacksmithInteraction bi = BlacksmithInteraction.Instance;
        if (bi == null || bi.holdPosition == null) return false;
        return transform.parent == bi.holdPosition;
    }

    void EnablePhysicalHitbox()
    {
        if (weaponHitbox == null) return;
        GameObject owner = BlacksmithInteraction.Instance != null
            ? BlacksmithInteraction.Instance.gameObject
            : transform.root.gameObject;
        if (weaponRb != null) weaponRb.detectCollisions = true;
        weaponHitbox.Activate(0f, owner);
    }

    void DisablePhysicalHitbox()
    {
        if (weaponHitbox != null) weaponHitbox.Deactivate();
        if (weaponRb != null) weaponRb.detectCollisions = !IsHeldInPlayerHand();
    }

    void Update()
    {
        if (!IsHeldInPlayerHand())
        {
            isSwinging = false;
            DisablePhysicalHitbox();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !isSwinging)
            StartCoroutine(SwingRoutine());
    }

    static void GetRestWorldPose(
        Transform weapon,
        Transform parent,
        Vector3 restLocalPos,
        Quaternion restLocalRot,
        out Vector3 restWorldPos,
        out Quaternion restWorldRot)
    {
        if (parent != null)
        {
            restWorldPos = parent.TransformPoint(restLocalPos);
            restWorldRot = parent.rotation * restLocalRot;
        }
        else
        {
            restWorldPos = weapon.position;
            restWorldRot = weapon.rotation;
        }
    }

    static Vector3 TopDownSwingAxis() => Vector3.up;

    static void ApplyAroundPivot(
        Vector3 restWorldPos,
        Quaternion restWorldRot,
        Vector3 pivotWorld,
        Vector3 axisWorld,
        float angleDeg,
        Transform t)
    {
        Quaternion r = Quaternion.AngleAxis(angleDeg, axisWorld);
        t.SetPositionAndRotation(
            r * (restWorldPos - pivotWorld) + pivotWorld,
            r * restWorldRot);
    }

    static Vector3[] GetBoundsCorners(Bounds b)
    {
        Vector3 min = b.min, max = b.max;
        return new[]
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z),
        };
    }

    void RefreshBladeDirectionLocal(Vector3 gripLocal)
    {
        if (!autoDetectBladeDirection) return;
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return;
        Vector3 bestLocal = Vector3.zero;
        float bestSqr = 0f;
        foreach (Renderer r in rs)
        {
            if (r == null) continue;
            foreach (var c in GetBoundsCorners(r.bounds))
            {
                Vector3 local = transform.InverseTransformPoint(c);
                Vector3 v = local - gripLocal;
                float sqr = v.sqrMagnitude;
                if (sqr > bestSqr) { bestSqr = sqr; bestLocal = v; }
            }
        }
        if (bestSqr > 1e-6f) bladeDirectionLocalCached = bestLocal.normalized;
    }

    float ComputeCenterOffsetAtSwingStart(
        Transform parent,
        Vector3 restLocalPos,
        Quaternion restLocalRot,
        Camera cam,
        float sideTiltDeg,
        Vector3 gripLocal)
    {
        GetRestWorldPose(transform, parent, restLocalPos, restLocalRot, out _, out Quaternion restWRot);
        Vector3 tiltAxis = cam != null
            ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (tiltAxis.sqrMagnitude < 1e-6f) tiltAxis = Vector3.forward;
        tiltAxis.Normalize();
        Quaternion tiltR = Quaternion.AngleAxis(-sideTiltDeg, tiltAxis);
        Quaternion tiltedRot = tiltR * restWRot;
        Vector3 bladeWorldFlat = Vector3.ProjectOnPlane(tiltedRot * bladeDirectionLocalCached, Vector3.up);
        Vector3 viewForwardFlat = cam != null
            ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (bladeWorldFlat.sqrMagnitude < 1e-6f) bladeWorldFlat = Vector3.forward;
        if (viewForwardFlat.sqrMagnitude < 1e-6f) viewForwardFlat = Vector3.forward;
        bladeWorldFlat.Normalize();
        viewForwardFlat.Normalize();
        return Vector3.SignedAngle(bladeWorldFlat, viewForwardFlat, Vector3.up);
    }

    IEnumerator SwingRoutine()
    {
        isSwinging = true;
        DisablePhysicalHitbox();

        Transform parent = transform.parent;
        Vector3 restLocalPos = transform.localPosition;
        Quaternion restLocalRot = transform.localRotation;
        Vector3 gripLocal = GripLocalOnWeaponRoot();

        float reach01 = GetReachNormalized01();
        float slashMag = Mathf.Lerp(minSlashDegrees, maxSlashDegrees, reach01);
        float halfSlash = slashMag * 0.5f;
        float rightAngle = halfSlash;
        float leftAngle = -halfSlash;
        if (invertDirections) { rightAngle = -rightAngle; leftAngle = -leftAngle; }

        float tiltDir = invertTilt ? -1f : 1f;
        float targetSideTilt = Mathf.Lerp(minSideTiltDegrees, maxSideTiltDegrees, reach01) * tiltDir;

        Camera cam = GetCam();
        RefreshBladeDirectionLocal(gripLocal);
        float centerOffsetDeg = ComputeCenterOffsetAtSwingStart(
            parent, restLocalPos, restLocalRot, cam, targetSideTilt, gripLocal) + centerYawBiasDegrees;

        float durScale = GetSpeedDurationScale();
        float wDur = windupDuration * durScale;
        float sDur = slashDuration * durScale;
        float rDur = returnDuration * durScale;

        float t = 0f;
        while (t < wDur)
        {
            if (!IsHeldInPlayerHand()) { isSwinging = false; DisablePhysicalHitbox(); yield break; }
            t += Time.deltaTime;
            float u = wDur > 1e-5f ? Mathf.Clamp01(t / wDur) : 1f;
            float ang = Mathf.SmoothStep(0f, rightAngle, u);
            float tilt = Mathf.SmoothStep(0f, targetSideTilt, u);
            StepPose(ang, tilt, centerOffsetDeg, parent, restLocalPos, restLocalRot, gripLocal, cam);
            yield return null;
        }

        EnablePhysicalHitbox();
        t = 0f;
        while (t < sDur)
        {
            if (!IsHeldInPlayerHand()) { isSwinging = false; DisablePhysicalHitbox(); yield break; }
            t += Time.deltaTime;
            float u = sDur > 1e-5f ? Mathf.Clamp01(t / sDur) : 1f;
            float ang = Mathf.SmoothStep(rightAngle, leftAngle, u);
            StepPose(ang, targetSideTilt, centerOffsetDeg, parent, restLocalPos, restLocalRot, gripLocal, cam);
            yield return null;
        }
        DisablePhysicalHitbox();

        t = 0f;
        while (t < rDur)
        {
            if (!IsHeldInPlayerHand()) { isSwinging = false; DisablePhysicalHitbox(); yield break; }
            t += Time.deltaTime;
            float u = rDur > 1e-5f ? Mathf.Clamp01(t / rDur) : 1f;
            float shapedU = returnEasePower > 1f ? 1f - Mathf.Pow(1f - u, returnEasePower) : u;
            float ang = Mathf.Lerp(leftAngle, 0f, shapedU);
            float tilt = Mathf.Lerp(targetSideTilt, 0f, shapedU);
            StepPose(ang, tilt, centerOffsetDeg, parent, restLocalPos, restLocalRot, gripLocal, cam);
            if (u >= returnLocalBlendStart)
            {
                float lu = Mathf.InverseLerp(returnLocalBlendStart, 1f, u);
                float le = Mathf.SmoothStep(0f, 1f, lu);
                transform.localPosition = Vector3.Lerp(transform.localPosition, restLocalPos, le);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, restLocalRot, le);
            }
            yield return null;
        }

        if (returnSettleDuration > 1e-5f)
        {
            float settleT = 0f;
            Vector3 fromPos = transform.localPosition;
            Quaternion fromRot = transform.localRotation;
            while (settleT < returnSettleDuration)
            {
                if (!IsHeldInPlayerHand()) { isSwinging = false; DisablePhysicalHitbox(); yield break; }
                settleT += Time.deltaTime;
                float su = Mathf.Clamp01(settleT / returnSettleDuration);
                float eased = Mathf.SmoothStep(0f, 1f, su);
                transform.localPosition = Vector3.Lerp(fromPos, restLocalPos, eased);
                transform.localRotation = Quaternion.Slerp(fromRot, restLocalRot, eased);
                yield return null;
            }
        }

        transform.localPosition = restLocalPos;
        transform.localRotation = restLocalRot;
        DisablePhysicalHitbox();
        isSwinging = false;
    }

    void StepPose(
        float cumulativeScreenAngleDeg,
        float sideTiltDeg,
        float centerOffsetDeg,
        Transform parent,
        Vector3 restLocalPos,
        Quaternion restLocalRot,
        Vector3 gripLocal,
        Camera cam)
    {
        GetRestWorldPose(transform, parent, restLocalPos, restLocalRot,
            out Vector3 restWPos, out Quaternion restWRot);
        Vector3 pivotWorld = restWPos + restWRot * gripLocal;
        Vector3 tiltAxis = cam != null
            ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (tiltAxis.sqrMagnitude < 1e-6f) tiltAxis = Vector3.forward;
        tiltAxis.Normalize();
        Quaternion tiltR = Quaternion.AngleAxis(-sideTiltDeg, tiltAxis);
        Vector3 tiltedPos = tiltR * (restWPos - pivotWorld) + pivotWorld;
        Quaternion tiltedRot = tiltR * restWRot;
        Vector3 axis = TopDownSwingAxis();
        ApplyAroundPivot(tiltedPos, tiltedRot, pivotWorld, axis, centerOffsetDeg + cumulativeScreenAngleDeg, transform);
    }
}
