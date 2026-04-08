using System.Collections;
using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    public int hitsTaken;

    [Header("Hit reaction")]
    public float shakeDuration = 0.28f;
    public float shakeAngle = 22f;
    public float shakeFrequency = 14f;
    public float hitKickBack = 0.06f;

    Quaternion baseLocalRotation;
    Vector3 baseLocalPosition;
    Coroutine shakeRoutine;

    void Awake()
    {
        baseLocalRotation = transform.localRotation;
        baseLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeRoutine == null)
        {
            baseLocalRotation = transform.localRotation;
            baseLocalPosition = transform.localPosition;
        }
    }

    public void OnWeaponSwingHit()
    {
        hitsTaken++;
        Debug.Log($"[Dummy] Hit! Count={hitsTaken} | {name}");
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / shakeDuration);
            float envelope = 1f - u;
            float wave = Mathf.Sin(t * shakeFrequency * Mathf.PI * 2f);
            float z = wave * shakeAngle * envelope;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, z);
            transform.localPosition = baseLocalPosition + new Vector3(0f, 0f, -hitKickBack * envelope);
            yield return null;
        }
        transform.localRotation = baseLocalRotation;
        transform.localPosition = baseLocalPosition;
        shakeRoutine = null;
    }
}
