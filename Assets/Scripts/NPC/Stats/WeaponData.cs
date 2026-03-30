using UnityEngine;

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public MetalType metalTier;
    public bool isValid;

    public float baseDamage;
    public float attackSpeed;   // ataki na sekunde
    public float range;
    public float areaOfEffect;
    public float bladeLength;

    // Domyslna dlugosc ostrza (bazowa przed kuciem)
    static float GetDefaultBladeLength()
    {
        return 0.5f;
    }

    public WeaponData(string name, MetalType metalTier, float bladeLength = -1f)
    {
        this.weaponName = name;
        this.metalTier = metalTier;
        this.bladeLength = bladeLength;
        this.isValid = true;

        float defaultLength = GetDefaultBladeLength();
        float lengthRatio = (bladeLength >= 0f) ? bladeLength / defaultLength : 1f;

        this.baseDamage = CalculateBaseDamage(metalTier);
        this.attackSpeed = GetBaseAttackSpeed() / Mathf.Max(lengthRatio, 0.5f);
        this.range = GetBaseRange();
        this.areaOfEffect = CalculateAoE(lengthRatio);
    }

    public static WeaponData Empty()
    {
        return new WeaponData("Brak", MetalType.Copper)
        {
            isValid = false,
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
        if (!isValid)
            return "Brak broni";

        return $"{weaponName} ({metalTier})\nDMG: {baseDamage:F0} | SPD: {attackSpeed:F1} | AOE: {areaOfEffect:F1}";
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

    static float CalculateBaseDamage(MetalType metal)
    {
        return 10f; // Stała wartość, metal do uwzględnienia później
    }

    static float GetBaseAttackSpeed()
    {
        return 0.8f;
    }

    static float GetBaseRange()
    {
        return 1.8f;
    }

    static float CalculateAoE(float lengthRatio)
    {
        return 3f * lengthRatio;
    }
}
