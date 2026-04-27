using UnityEngine;

[System.Serializable]
public class StatRequirement
{
    public float Min;
    public float Max;

    public bool IsActive => Max > 0f;

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
    public PerimeterTriangle[] Triangles;
    public MetalType RequiredMetal;
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
    public PerimeterTriangle[] triangles;
    public MetalType requiredMetal;

    public AssignedTask(TaskEntry entry)
    {
        description    = entry.Task;
        requiredDamage = entry.Damage != null && entry.Damage.IsActive ? entry.Damage.Roll() : -1f;
        requiredSpeed  = entry.Speed  != null && entry.Speed.IsActive  ? entry.Speed.Roll()  : -1f;
        requiredAoe    = entry.Aoe    != null && entry.Aoe.IsActive    ? entry.Aoe.Roll()    : -1f;
        triangles      = entry.Triangles ?? new PerimeterTriangle[0];
        requiredMetal  = entry.RequiredMetal;
    }

    public float CalculateTaskCompletion(WeaponData weapon)
    {
        float totalScore = 0f;
        int statsChecked = 0;

        if (requiredDamage >= 0f)
        {
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

        if (statsChecked == 0) return 1f;

        return totalScore / statsChecked;
    }

    public bool CheckWeapon(WeaponData weapon)
    {
        return CalculateTaskCompletion(weapon) >= 1f;
    }
}
