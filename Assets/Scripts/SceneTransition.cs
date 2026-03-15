using UnityEngine;
using UnityEngine.SceneManagement; // Wymagane do ładowania scen!

public class SceneTransition : MonoBehaviour
{
    [Header("Ustawienia przejścia")][Tooltip("Dokładna nazwa sceny, do której przechodzimy (np. Level_02)")]
    public string sceneToLoad;

    public void ChangeScene()
    {
        Debug.Log($"Przechodzenie do sceny: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}