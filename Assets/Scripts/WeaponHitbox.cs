using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    Collider hitboxCollider;
    float currentDamage;
    GameObject owner;
    bool isActive;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    /// <summary>
    /// Włączany przez NPCCombat na czas zamachu.
    /// </summary>
    public void Activate(float damage, GameObject attackOwner)
    {
        currentDamage = damage;
        owner = attackOwner;
        isActive = true;
        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
    }

    public void Deactivate()
    {
        isActive = false;
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Nie bij samego siebie
        if (other.gameObject == owner) return;
        if (other.transform.IsChildOf(owner.transform)) return;

        ExiledCitizen target = other.GetComponentInParent<ExiledCitizen>();
        if (target != null)
        {
            target.health -= currentDamage;
            Debug.Log($"[WeaponHitbox] {owner.name} zadał {currentDamage:F1} obrażeń dla {target.gameObject.name} (HP: {target.health:F1}/{target.maxHealth:F1})");
        }
    }
}
