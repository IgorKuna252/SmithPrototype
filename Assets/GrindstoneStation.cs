using UnityEngine;

public class GrindstoneStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    private IronPiece currentIron;
    private bool isGrindingMode = false;
    private float bladeSlidePosition = 0f;
    private float currentDip = 0f;
    private bool isFlipped = false;

    private Transform mainCamera;
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    // ZABEZPIECZENIE: Czas wejścia do stacji, by uniknąć podwójnego kliknięcia
    private float grindStartTime = 0f;

    void Start()
    {
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void Update()
    {
        if (isGrindingMode && currentIron != null)
        {
            HandleGrindingMinigame();

            // NOWOŚĆ: Wyjść można dopiero po upływie pół sekundy od wejścia!
            if (Input.GetKeyDown(KeyCode.E) && Time.time > grindStartTime + 0.5f)
            {
                ExitGrindingMode();
            }
        }
    }

    public void EnterGrindingMode(IronPiece iron)
    {
        currentIron = iron;
        isGrindingMode = true;
        grindStartTime = Time.time; // Zapisujemy, o której weszliśmy

        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        bladeSlidePosition = 0f;
        currentDip = 0f;
        isFlipped = false;

        Rigidbody rb = currentIron.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        currentIron.transform.SetParent(snapPoint);
        currentIron.transform.localPosition = Vector3.zero;
        currentIron.transform.localRotation = Quaternion.identity;

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.parent;
            originalCameraLocalPos = mainCamera.localPosition;
            originalCameraLocalRot = mainCamera.localRotation;
            mainCamera.SetParent(null);
        }

        // TWARDE WYŁĄCZENIE GRACZA
        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }
    }

    private void HandleGrindingMinigame()
    {
        // WYMUSZENIE: Kamera jest zamrożona w sockecie co klatkę (uniemożliwia to rozglądanie się, nawet jeśli gracz by działał)
        if (mainCamera != null && cameraSocket != null)
        {
            mainCamera.position = cameraSocket.position;
            mainCamera.rotation = cameraSocket.rotation;
        }

        if (Input.GetMouseButtonDown(1)) isFlipped = !isFlipped;
        float targetRotationZ = isFlipped ? 180f : 0f;
        currentIron.transform.localRotation = Quaternion.Lerp(currentIron.transform.localRotation, Quaternion.Euler(0, 0, targetRotationZ), Time.deltaTime * 8f);

        float scroll = Input.mouseScrollDelta.y;
        bladeSlidePosition += scroll * 0.05f;

        Mesh mesh = currentIron.GetComponent<MeshFilter>().mesh;
        float maxSlide = mesh.bounds.extents.z + 0.05f;
        bladeSlidePosition = Mathf.Clamp(bladeSlidePosition, -maxSlide, maxSlide);

        if (Input.GetMouseButton(0))
        {
            currentIron.GrindPerfectEdge(-bladeSlidePosition, isFlipped);
            currentDip = Mathf.Lerp(currentDip, 0.04f, Time.deltaTime * 10f);
        }
        else
        {
            currentDip = Mathf.Lerp(currentDip, 0f, Time.deltaTime * 10f);
        }

        currentIron.transform.localPosition = new Vector3(currentDip, 0, bladeSlidePosition);
    }

    private void ExitGrindingMode()
    {
        isGrindingMode = false;

        currentIron.transform.SetParent(null);
        Rigidbody rb = currentIron.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        currentIron = null;

        if (playerObject != null)
        {
            playerObject.SetActive(true);
        }

        if (mainCamera != null)
        {
            mainCamera.SetParent(originalCameraParent);
            mainCamera.localPosition = originalCameraLocalPos;
            mainCamera.localRotation = originalCameraLocalRot;
        }
    }
}