[System.Serializable]
public class TaskEntry
{
    public string Task;
    public PerimeterTriangle[] Triangles;
    // Pusty string lub brak pola = bez wymagania konkretnego metalu
    public string RequiredMetal;
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
    public PerimeterTriangle[] triangles;
    public MetalType requiredMetal;
    public bool checkMetal;

    public AssignedTask(TaskEntry entry)
    {
        description = entry.Task;
        triangles   = entry.Triangles ?? new PerimeterTriangle[0];

        checkMetal = !string.IsNullOrEmpty(entry.RequiredMetal)
                     && System.Enum.TryParse<MetalType>(entry.RequiredMetal, out requiredMetal);
        if (!checkMetal) requiredMetal = MetalType.Iron;
    }

    public bool CheckWeapon(WeaponData weapon)
    {
        return weapon != null && weapon.isValid;
    }
}
