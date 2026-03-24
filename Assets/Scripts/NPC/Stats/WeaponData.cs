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
    public float areaOfEffect;
    public float bladeLength;

    // Domyslna dlugosc ostrza dla kazdego typu (bazowa przed kuciem)
    static float GetDefaultBladeLength(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return 0.7f;
            case WeaponType.Axe:   return 0.35f;
            default:               return 0.3f;
        }
    }

    public WeaponData(string name, WeaponType type, MetalType metalTier, float bladeLength = -1f)
    {
        this.weaponName = name;
        this.type = type;
        this.metalTier = metalTier;
        this.bladeLength = bladeLength;

        float defaultLength = GetDefaultBladeLength(type);
        float lengthRatio = (bladeLength >= 0f) ? bladeLength / defaultLength : 1f;

        this.baseDamage = CalculateBaseDamage(type, metalTier);
        this.attackSpeed = GetBaseAttackSpeed(type) / Mathf.Max(lengthRatio, 0.5f);
        this.range = GetBaseRange(type);
        this.areaOfEffect = CalculateAoE(type, lengthRatio);
    }

    public static WeaponData Empty()
    {
        return new WeaponData("Brak", WeaponType.None, MetalType.Copper)
        {
            baseDamage = 0f,
            attackSpeed = 0.8f,
            range = 1.5f,
            areaOfEffect = 0f
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

        return $"{weaponName} ({metalTier} {type})\nDMG: {baseDamage:F0} | SPD: {attackSpeed:F1} | AOE: {areaOfEffect:F1}";
    }

    // Znormalizowane wartosci (0-100) do wyswietlania na kole
    // Kazdy stat skalowany do swojego zakresu, zeby byly porownywalne
    const float MAX_DAMAGE = 12f;        // Axe base = 12 (metal nie wplywa na razie)
    const float MAX_ATTACK_SPEED = 2.0f; // Sword z minimalnym lengthRatio (0.5) = 1.0/0.5
    const float MAX_AOE = 12f;

    public float GetNormalizedDamage()  { return Mathf.Clamp((baseDamage / MAX_DAMAGE) * 100f, 0f, 100f); }
    public float GetNormalizedSpeed()   { return Mathf.Clamp((attackSpeed / MAX_ATTACK_SPEED) * 100f, 0f, 100f); }
    public float GetNormalizedAoE()     { return Mathf.Clamp((areaOfEffect / MAX_AOE) * 100f, 0f, 100f); }

    // --- TABELE STATYSTYK ---

    static float CalculateBaseDamage(WeaponType type, MetalType metal)
    {
        // Na razie damage zalezy tylko od typu broni (metal do dodania pozniej)
        switch (type)
        {
            case WeaponType.Sword: return 8f;
            case WeaponType.Axe:   return 12f;
            default:               return 0f;
        }
    }

    static float GetBaseAttackSpeed(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return 1.0f;
            case WeaponType.Axe:   return 0.7f;
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

    static float CalculateAoE(WeaponType type, float lengthRatio)
    {
        switch (type)
        {
            // Miecz: dluzsze ostrze = wiekszy atak obszarowy
            case WeaponType.Sword: return 5f * lengthRatio;
            // Topor: rąbie w punkt, dlugosc niewiele zmienia
            case WeaponType.Axe:   return 2f + (lengthRatio - 1f) * 0.5f;
            default:               return 1f;
        }
    }
}
