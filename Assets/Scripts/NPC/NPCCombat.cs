using UnityEngine;

public enum NPCCombatMode
{
    Unarmed,    // npc nie trzyma broni
    ArmedIdle,  // trzyma broń, nie atakuje
    Attacking   // automatycznie atakuje cały czas
}

public class NPCCombat : MonoBehaviour
{
    public float attackCooldown = 1.2f; // Co ile uderza npc
    public float hitboxActiveDelay = 0.1f; // Od kiedy jest włączony
    public float hitboxActiveDuration = 0.3f; // Na ile wlaczany jest htibox (wylaczamy zeby nie dzialal jak pila łańcuchowa

    WeaponSocket weaponSocket;
    ExiledCitizen citizenStats;
    Animator animator;
    WeaponHitbox activeHitbox;

    float cooldownTimer;
    float hitboxTimer;
    bool hitboxWaiting;
    bool hitboxOn;

    NPCCombatMode currentMode = NPCCombatMode.Unarmed;
    public NPCCombatMode CurrentMode => currentMode;

    void Awake()
    {
        weaponSocket = GetComponentInChildren<WeaponSocket>();
        citizenStats = GetComponent<ExiledCitizen>();
        animator = GetComponentInChildren<Animator>();
    }

    public void SetMode(NPCCombatMode mode)
    {
        currentMode = mode;
        animator.SetBool("InCombat", mode != NPCCombatMode.Unarmed);

        if (mode != NPCCombatMode.Attacking)
            DeactivateHitbox();
        else
            cooldownTimer = attackCooldown;
    }

    public void TriggerAttack()
    {
        if (weaponSocket.GetEquippedWeapon() == null) return;
        animator.SetTrigger("Attack");
        hitboxWaiting = true;
        hitboxTimer = hitboxActiveDelay;
    }

    void Update()
    {
        if (hitboxWaiting)
        {
            hitboxTimer -= Time.deltaTime;
            if (hitboxTimer <= 0f)
            {
                ActivateHitbox();
                hitboxWaiting = false;
                hitboxOn = true;
                hitboxTimer = hitboxActiveDuration;
            }
        }
        else if (hitboxOn)
        {
            hitboxTimer -= Time.deltaTime;
            if (hitboxTimer <= 0f)
            {
                DeactivateHitbox();
                hitboxOn = false;
            }
        }

        if (currentMode != NPCCombatMode.Attacking) return;
        if (weaponSocket.GetEquippedWeapon() == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            TriggerAttack();
            cooldownTimer = attackCooldown;
        }
    }

    void ActivateHitbox()
    {
        activeHitbox = weaponSocket.GetEquippedWeapon()?.GetComponent<WeaponHitbox>();
        if (activeHitbox != null)
            activeHitbox.Activate(citizenStats.strength, gameObject);
    }

    void DeactivateHitbox()
    {
        if (activeHitbox != null)
        {
            activeHitbox.Deactivate();
            activeHitbox = null;
        }
    }
}
