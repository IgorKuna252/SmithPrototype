using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string sceneToLoad;

    public void ChangeScene()
    {
        // 1. Zresetuj kursor przed przejściem do nowej sceny
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Załaduj scenę
        SceneManager.LoadScene(sceneToLoad);
    }
}