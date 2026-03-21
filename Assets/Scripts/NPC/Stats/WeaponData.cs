using UnityEngine;

public enum WeaponType { None, Sword, Axe }

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public WeaponType type;
    public MetalType metalTier;

    public float baseDamage;
    public float attackSpeed;   // ataki na sekunde
    public float range;

    public WeaponData(string name, WeaponType type, MetalType metalTier)
    {
        this.weaponName = name;
        this.type = type;
        this.metalTier = metalTier;
        this.baseDamage = CalculateBaseDamage(type, metalTier);
        this.attackSpeed = GetBaseAttackSpeed(type);
        this.range = GetBaseRange(type);
    }

    public static WeaponData Empty()
    {
        return new WeaponData("Brak", WeaponType.None, MetalType.Copper)
        {
            baseDamage = 0f,
            attackSpeed = 0.8f,
            range = 1.5f
        };
    }

    // --- METODY PUBLICZNE ---

    /// Oblicza calkowite obrazenia: bron + sila postaci
    public float GetDamage(float strength)
    {
        return baseDamage + strength;
    }

    /// Cooldown miedzy atakami (odwrotnosc attackSpeed)
    public float GetAttackCooldown()
    {
        if (attackSpeed <= 0f) return 2f;
        return 1f / attackSpeed;
    }

    public float GetRange()
    {
        return range;
    }

    public string GetStats()
    {
        if (type == WeaponType.None)
            return "Brak broni";

        return $"{weaponName} ({metalTier} {type})\nDMG: {baseDamage:F0} | SPD: {attackSpeed:F1} | RNG: {range:F1}";
    }

    // --- TABELE STATYSTYK ---

    static float CalculateBaseDamage(WeaponType type, MetalType metal)
    {
        float typeDamage;
        switch (type)
        {
            case WeaponType.Sword: typeDamage = 8f;  break;
            case WeaponType.Axe:   typeDamage = 12f; break;
            default:               typeDamage = 0f;  break;
        }

        float metalMultiplier;
        switch (metal)
        {
            case MetalType.Copper:    metalMultiplier = 0.6f; break;
            case MetalType.Bronze:    metalMultiplier = 0.8f; break;
            case MetalType.Iron:      metalMultiplier = 1.0f; break;
            case MetalType.Steel:     metalMultiplier = 1.3f; break;
            case MetalType.Gold:      metalMultiplier = 0.9f; break;
            case MetalType.Platinum:  metalMultiplier = 1.5f; break;
            case MetalType.BlueSteel: metalMultiplier = 1.8f; break;
            case MetalType.Vibranium: metalMultiplier = 2.5f; break;
            default:                  metalMultiplier = 1.0f; break;
        }

        return typeDamage * metalMultiplier;
    }

    static float GetBaseAttackSpeed(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return 1.0f;  // szybki
            case WeaponType.Axe:   return 0.7f;  // wolniejszy
            default:               return 0.8f;
        }
    }

    static float GetBaseRange(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return 2.0f;
            case WeaponType.Axe:   return 1.8f;
            default:               return 1.5f;
        }
    }
}
