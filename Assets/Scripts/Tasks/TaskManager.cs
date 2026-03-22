using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private TaskList taskList;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadTasks();
    }

    void LoadTasks()
    {
        TextAsset file = Resources.Load<TextAsset>("tasks");
        if (file == null)
        {
            Debug.LogWarning("[TaskManager] Nie znaleziono Resources/tasks.json!");
            return;
        }

        taskList = JsonUtility.FromJson<TaskList>(file.text);
        Debug.Log($"[TaskManager] Zaladowano {taskList.Tasks.Length} taskow");
    }

    /// Losuje task z puli i tworzy instancje z wylosowanymi wartosciami docelowymi
    public AssignedTask GetRandomTask()
    {
        if (taskList == null || taskList.Tasks.Length == 0) return null;

        TaskEntry entry = taskList.Tasks[Random.Range(0, taskList.Tasks.Length)];
        return new AssignedTask(entry);
    }
}
