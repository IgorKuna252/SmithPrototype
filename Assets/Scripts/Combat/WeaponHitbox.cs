using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    Collider hitboxCollider;
    float currentDamage;
    GameObject owner;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    public void Activate(float damage, GameObject attackOwner)
    {
        currentDamage = damage;
        owner = attackOwner;
        if (hitboxCollider != null) hitboxCollider.enabled = true;
        // WeaponSocket ustawia detectCollisions=false żeby broń nie odpychała obiektów —
        // włączamy tylko na czas zamachu żeby trigger działał
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null) rb.detectCollisions = true;
    }

    public void Deactivate()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null) rb.detectCollisions = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        if (other.gameObject == owner) return;
        if (other.transform.IsChildOf(owner.transform)) return;

        bool ownerIsTeam = owner.GetComponent<npcPathFinding>() != null;
        bool targetIsTeam = other.GetComponentInParent<npcPathFinding>() != null;

        // Nie bij swoich
        if (ownerIsTeam && targetIsTeam) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null) enemy.TakeDamage(currentDamage);

        ExiledCitizen citizen = other.GetComponentInParent<ExiledCitizen>();
        if (citizen != null) citizen.TakeDamage(currentDamage);
    }
}
