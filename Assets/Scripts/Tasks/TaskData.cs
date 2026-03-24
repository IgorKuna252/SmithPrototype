using UnityEngine;

[System.Serializable]
public class StatRequirement
{
    public float Min;
    public float Max;

    /// Wymaganie aktywne gdy Max > 0
    public bool IsActive => Max > 0f;

    /// Losuje wartosc docelowa z zakresu Min-Max (znormalizowana 0-100)
    public float Roll()
    {
        return Mathf.Round(Random.Range(Min, Max));
    }
}

[System.Serializable]
public class TaskEntry
{
    public string Task;
    public StatRequirement Damage;
    public StatRequirement Speed;
    public StatRequirement Aoe;
}

[System.Serializable]
public class TaskList
{
    public TaskEntry[] Tasks;
}

[System.Serializable]
public class AssignedTask
{
    public string description;
    public float requiredDamage;  // -1 = brak wymagania
    public float requiredSpeed;
    public float requiredAoe;

    public AssignedTask(TaskEntry entry)
    {
        description    = entry.Task;
        requiredDamage = entry.Damage != null && entry.Damage.IsActive ? entry.Damage.Roll() : -1f;
        requiredSpeed  = entry.Speed  != null && entry.Speed.IsActive  ? entry.Speed.Roll()  : -1f;
        requiredAoe    = entry.Aoe    != null && entry.Aoe.IsActive    ? entry.Aoe.Roll()    : -1f;
    }

    /// Zwraca procentowe ukończenie zadania (0.0 - 1.0)
    public float CalculateTaskCompletion(WeaponData weapon)
    {
        float totalScore = 0f;
        int statsChecked = 0;

        if (requiredDamage >= 0f)
        {
            // Ograniczamy do max 1.0 (100%), więc zrobienie dużo lepszej broni po prostu gwarantuje dowiezienie tej statystyki na maxa.
            totalScore += requiredDamage <= 0f ? 1f : Mathf.Clamp01(weapon.GetNormalizedDamage() / requiredDamage);
            statsChecked++;
        }
        if (requiredSpeed >= 0f)
        {
            totalScore += requiredSpeed <= 0f ? 1f : Mathf.Clamp01(weapon.GetNormalizedSpeed() / requiredSpeed);
            statsChecked++;
        }
        if (requiredAoe >= 0f)
        {
            totalScore += requiredAoe <= 0f ? 1f : Mathf.Clamp01(weapon.GetNormalizedAoE() / requiredAoe);
            statsChecked++;
        }

        // Zabezpieczenie przed zadaniami bez statystyk - standardowo dajemy równe 1.0
        if (statsChecked == 0) return 1f;

        return totalScore / statsChecked;
    }

    /// Opcjonalne kompatybilne sprawdzenie na sztywno, kiedy potrzeba
    public bool CheckWeapon(WeaponData weapon)
    {
        return CalculateTaskCompletion(weapon) >= 1f;
    }

    /// Zwraca czytelny tekst wymagan do wyswietlenia w UI
    public string GetRequirementsText()
    {
        var sb = new System.Text.StringBuilder();
        if (requiredDamage >= 0f) sb.AppendLine($"DMG >= {requiredDamage:F0}%");
        if (requiredSpeed  >= 0f) sb.AppendLine($"SPD >= {requiredSpeed:F0}%");
        if (requiredAoe    >= 0f) sb.AppendLine($"AOE >= {requiredAoe:F0}%");
        return sb.ToString().TrimEnd();
    }
}
