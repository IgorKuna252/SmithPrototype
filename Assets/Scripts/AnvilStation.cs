using UnityEngine;

public class AnvilStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    private MetalPiece currentMetal;
    private bool isForgingMode = false;
    private float slidePosition = 0f;
    private int rotationStep = 0;

    private Transform mainCamera;
    private Camera camComponent;
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    private float forgeStartTime = 0f;

    private float lastHitTime = 0f;
    public float hammerCooldown = 0.2f;

    [Header("Efekty")]
    public ParticleSystem hitSparks;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
            camComponent = Camera.main;
        }
    }

    void Update()
    {
        if (isForgingMode && currentMetal != null)
        {
            HandleForgingMinigame();

            if (Input.GetKeyDown(KeyCode.E) && Time.time > forgeStartTime + 0.5f)
            {
                ExitForgingMode();
            }
        }
    }

    public void EnterForgingMode(MetalPiece metal)
    {
        currentMetal = metal;
        isForgingMode = true;
        forgeStartTime = Time.time;

        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        slidePosition = 0f;
        rotationStep = 0;

        Rigidbody rb = currentMetal.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        currentMetal.transform.SetParent(snapPoint, true);
        currentMetal.transform.localPosition = Vector3.zero;

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.parent;
            originalCameraLocalPos = mainCamera.localPosition;
            originalCameraLocalRot = mainCamera.localRotation;
            mainCamera.SetParent(null);
        }

        if (playerObject != null)
            playerObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleForgingMinigame()
    {
        if (mainCamera != null && cameraSocket != null)
        {
            mainCamera.position = cameraSocket.position;
            mainCamera.rotation = cameraSocket.rotation;
        }

        //// Obrót metalu (PPM)
        //if (Input.GetMouseButtonDown(1))
        //{
        //    rotationStep = (rotationStep + 1) % 4;
        //}

        float targetRotationZ = rotationStep * 90f;
        currentMetal.transform.localRotation =
            Quaternion.Lerp(
                currentMetal.transform.localRotation,
                Quaternion.Euler(0, 0, targetRotationZ),
                Time.deltaTime * 15f
            );

        // Przesuwanie metalu scroll
        float scroll = Input.mouseScrollDelta.y;
        slidePosition += scroll * 0.05f;

        Mesh mesh = currentMetal.GetComponent<MeshFilter>().mesh;
        float maxSlide = mesh.bounds.extents.z + 0.1f;

        slidePosition = Mathf.Clamp(slidePosition, -maxSlide, maxSlide);

        currentMetal.transform.localPosition = new Vector3(0, 0, slidePosition);

        // --- SYSTEM KUCIA ---
        if (Input.GetMouseButtonDown(0) && Time.time > lastHitTime + hammerCooldown)
        {
            Ray ray = camComponent.ScreenPointToRay(Input.mousePosition);

            Plane metalPlane =
                new Plane(
                    currentMetal.transform.up,
                    currentMetal.transform.position
                );

            if (metalPlane.Raycast(ray, out float enter))
            {
                lastHitTime = Time.time;

                Vector3 hitPoint = ray.GetPoint(enter);

                currentMetal.HitMetal(hitPoint, currentMetal.transform.up);

                if (hitSparks != null)
                {
                    hitSparks.transform.position = hitPoint;
                    hitSparks.Play();
                }

                Debug.DrawRay(ray.origin, ray.direction * 3f, Color.red, 1f);
            }
        }
    }

    private void ExitForgingMode()
    {
        isForgingMode = false;

        currentMetal.transform.SetParent(null);

        Rigidbody rb = currentMetal.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
        }

        currentMetal = null;

        if (playerObject != null)
            playerObject.SetActive(true);

        if (mainCamera != null)
        {
            mainCamera.SetParent(originalCameraParent);
            mainCamera.localPosition = originalCameraLocalPos;
            mainCamera.localRotation = originalCameraLocalRot;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
