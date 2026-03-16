using UnityEngine;

public class NPCCombat : MonoBehaviour
{
    [Header("Obrażenia")]
    [Tooltip("Bazowe obrażenia (mnożone przez strength NPC)")]
    public float baseDamage = 5f;

    [Tooltip("Przerwa między atakami")]
    public float attackCooldown = 1.2f;

    [Header("Animacja")]
    [Tooltip("Nazwa triggera ataku w Animatorze")]
    public string attackTrigger = "Attack";

    [Tooltip("Nazwa boola walki w Animatorze (idle vs combat idle)")]
    public string combatBool = "InCombat";

    [Tooltip("Czas po uzyciu triggera w którym hitbox jest aktywny (sekundy)")]
    public float hitboxActiveDelay = 0.1f;

    [Tooltip("Jak długo hitbox jest aktywny podczas zamachu")]
    public float hitboxActiveDuration = 0.3f;

    WeaponSocket weaponSocket;
    ExiledCitizen citizenStats;
    Animator animator;
    WeaponHitbox activeHitbox;

    float cooldownTimer;
    float hitboxTimer;
    bool hitboxWaiting;
    bool hitboxOn;
    bool combatActive;

    void Awake()
    {
        weaponSocket = GetComponent<WeaponSocket>();
        citizenStats = GetComponent<ExiledCitizen>();
        animator = GetComponentInChildren<Animator>();
    }

    public void SetCombatActive(bool active)
    {
        combatActive = active;
        if (animator != null)
            animator.SetBool(combatBool, active);

        if (!active) DeactivateHitbox();
    }

    void Update()
    {
        if (!combatActive || weaponSocket == null) return;
        if (weaponSocket.GetEquippedWeapon() == null) return;

        // Obsługa timera hitboxa
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

        // Cooldown między atakami
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            TriggerAttack();
            cooldownTimer = attackCooldown;
        }
    }

    void TriggerAttack()
    {
        if (animator != null)
            animator.SetTrigger(attackTrigger);

        // Włącz hitbox z opóźnieniem (żeby trafił w odpowiednim momencie animacji)
        hitboxWaiting = true;
        hitboxTimer = hitboxActiveDelay;
    }

    void ActivateHitbox()
    {
        activeHitbox = weaponSocket.GetEquippedWeapon().GetComponent<WeaponHitbox>();
        if (activeHitbox != null)
        {
            float damage = baseDamage;
            if (citizenStats != null)
                damage *= (1f + citizenStats.strength * 0.1f);

            activeHitbox.Activate(damage, gameObject);
        }
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
