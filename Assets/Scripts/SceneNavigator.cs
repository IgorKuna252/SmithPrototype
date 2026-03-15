using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    // Nazwa sceny 3D (wpisz dokładnie taką, jaka jest w Build Settings)
    [SerializeField] private string mainGameSceneName = "MainScene"; 

    public void BackToGame()
    {
        // 1. Zresetuj kursor przed wyjściem
        Cursor.lockState = CursorLockMode.Locked; // Blokujemy kursor, bo gra 3D tego wymaga
        Cursor.visible = false;                   // Ukrywamy kursor

        // 2. Wróć do sceny
        SceneManager.LoadScene(mainGameSceneName);
    }
}