using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    Collider hitboxCollider;
    float currentDamage;
    GameObject owner;
    readonly HashSet<int> hitRoots = new HashSet<int>();

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider == null)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                if (c != null && c.isTrigger)
                {
                    hitboxCollider = c;
                    break;
                }
            }
        }
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
        hitRoots.Clear();
        if (hitboxCollider != null) hitboxCollider.enabled = true;
    }

    public void Deactivate()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        if (other.gameObject == owner) return;
        if (other.transform.IsChildOf(owner.transform)) return;

        Transform targetRoot = other.transform.root;
        int targetId = targetRoot.GetInstanceID();
        if (hitRoots.Contains(targetId)) return;
        hitRoots.Add(targetId);

        bool ownerIsTeam = owner.GetComponent<npcPathFinding>() != null;
        bool targetIsTeam = other.GetComponentInParent<npcPathFinding>() != null;

        // Nie bij swoich
        if (ownerIsTeam && targetIsTeam) return;

        ExiledCitizen citizen = other.GetComponentInParent<ExiledCitizen>();
        if (citizen != null) citizen.TakeDamage(currentDamage);

        // Kukła / hooki: SendMessage tylko na jednym GO często omija skrypt na dziecku — BroadcastMessage schodzi w dół hierarchii.
        targetRoot.BroadcastMessage("OnWeaponSwingHit", SendMessageOptions.DontRequireReceiver);
        HitmarkerOverlay.NotifyHit();
    }
}
