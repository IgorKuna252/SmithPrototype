using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 5f;
    public float gravity = -19.62f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float xRotation = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Szukamy kamery, ktora jest podpieta pod gracza
        cameraTransform = GetComponentInChildren<Camera>().transform;

        // Ukrywamy i blokujemy kursor na srodku ekranu!
        // UWAGA: Aby odzyskac kursor w edytorze, wcisnij klawisz ESC.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. ROZGLADANIE SIE
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Blokada, zeby nie zlamac karku

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // Gora/dol (kamera)
        transform.Rotate(Vector3.up * mouseX); // Lewo/prawo (cala postac)

        // 2. CHODZENIE (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // 3. GRAWITACJA I SKAKANIE (Spacja)
        if (controller.isGrounded)
        {
            // Mala sila dociskajaca, zeby isGrounded dzialalo stabilnie
            verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        controller.Move(move * speed * Time.deltaTime);
    }
}