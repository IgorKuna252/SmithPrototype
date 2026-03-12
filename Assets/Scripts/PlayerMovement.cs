using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Szukamy kamery, która jest podpiêta pod gracza
        cameraTransform = GetComponentInChildren<Camera>().transform;

        // Ukrywamy i blokujemy kursor na œrodku ekranu!
        // UWAGA: Aby odzyskaæ kursor w edytorze, wciœnij klawisz ESC.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. ROZGL¥DANIE SIÊ
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Blokada, ¿eby nie z³amaæ karku

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // Góra/dó³ (kamera)
        transform.Rotate(Vector3.up * mouseX); // Lewo/prawo (ca³a postaæ)

        // 2. CHODZENIE (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move.y = -9.81f; // Prosta, sta³a grawitacja dociskaj¹ca do ziemi

        controller.Move(move * speed * Time.deltaTime);
    }
}