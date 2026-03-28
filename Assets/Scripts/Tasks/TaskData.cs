using UnityEngine;

[System.Serializable]
public class TaskEntry
{
    public string Task;
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

    public AssignedTask(TaskEntry entry)
    {
        description = entry.Task;
    }

    /// Zwraca procentowe ukończenie zadania (0.0 - 1.0)
    public float CalculateTaskCompletion()
    {
        return 1f;
    }

    /// Opcjonalne kompatybilne sprawdzenie na sztywno, kiedy potrzeba
    public bool CheckWeapon()
    {
        return true;
    }

    /// Zwraca czytelny tekst wymagan do wyswietlenia w UI
    public string GetRequirementsText()
    {
        return "Przynieś: Jakakolwiek Sztabka + Trzonek";
    }
}
