using UnityEngine;

public class GrindstoneStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    [Header("Ustawienia Kamienia (Tuning)")]
    [Tooltip("Gdzie fizycznie znajduje się powierzchnia kamienia? (Zwiększaj/Zmniejszaj aż miecz idealnie dotknie tarczy)")]
    public float distanceToStone = 0.1f;
    [Tooltip("Jak głęboko wcinamy się w kamień podczas kliknięcia (szlifowania)")]
    public float grindBiteDepth = 0.015f;

    private MetalPiece currentMetal;
    private bool isGrindingMode = false;
    private float bladeSlidePosition = 0f;
    private float currentDip = 0f;
    private bool isFlipped = false;

    private Transform mainCamera;
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    [Header("Efekty")]
    public ParticleSystem sparksEffect;

    private float grindStartTime = 0f;

    void Start()
    {
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void Update()
    {
        if (isGrindingMode && currentMetal != null)
        {
            HandleGrindingMinigame();

            if (Input.GetKeyDown(KeyCode.E) && Time.time > grindStartTime + 0.5f)
            {
                ExitGrindingMode();
            }
        }
    }

    public void EnterGrindingMode(MetalPiece metal)
    {
        currentMetal = metal;
        isGrindingMode = true;
        grindStartTime = Time.time;

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player");

        bladeSlidePosition = 0f;
        isFlipped = false;

        Rigidbody rb = currentMetal.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        currentMetal.transform.SetParent(snapPoint);
        currentMetal.transform.localPosition = Vector3.zero;
        currentMetal.transform.localRotation = Quaternion.identity;

        // Resetujemy startową pozycję X, by uniknąć skoku animacji
        currentDip = distanceToStone - currentMetal.GetEdgeWidthAt(0, false);

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.parent;
            originalCameraLocalPos = mainCamera.localPosition;
            originalCameraLocalRot = mainCamera.localRotation;
            mainCamera.SetParent(null);
        }

        if (playerObject != null) playerObject.SetActive(false);

        RefreshStatsWheel();
    }

    private void HandleGrindingMinigame()
    {
        if (mainCamera != null && cameraSocket != null)
        {
            mainCamera.position = cameraSocket.position;
            mainCamera.rotation = cameraSocket.rotation;
        }

        if (Input.GetMouseButtonDown(1)) isFlipped = !isFlipped;
        float targetRotationZ = isFlipped ? 180f : 0f;
        currentMetal.transform.localRotation = Quaternion.Lerp(currentMetal.transform.localRotation, Quaternion.Euler(0, 0, targetRotationZ), Time.deltaTime * 8f);

        float scroll = Input.mouseScrollDelta.y;
        bladeSlidePosition += scroll * 0.05f;

        Mesh mesh = currentMetal.GetComponent<MeshFilter>().mesh;
        float minLimit = -mesh.bounds.max.z - 0.05f;
        float maxLimit = -mesh.bounds.min.z + 0.05f;
        bladeSlidePosition = Mathf.Clamp(bladeSlidePosition, minLimit, maxLimit);

        // --- PROCEDURALNE DOPASOWANIE (MAGIA!) ---
        // Skanujemy, jak szeroki jest metal w miejscu, które aktualnie jest pod kamieniem
        float currentEdgeWidth = currentMetal.GetEdgeWidthAt(-bladeSlidePosition, isFlipped);

        // Obliczamy idealną pozycję: Powierzchnia kamienia minus szerokość naszego metalu
        // Dzięki temu krawędź metalu ZAWSZE ląduje idealnie na wartości "distanceToStone"
        float hoverX = distanceToStone - currentEdgeWidth;

        if (Input.GetMouseButton(0))
        {
            currentMetal.GrindPerfectEdge(-bladeSlidePosition, isFlipped);
            RefreshStatsWheel();

            currentDip = Mathf.Lerp(currentDip, hoverX + grindBiteDepth, Time.deltaTime * 15f);

            if (sparksEffect != null && !sparksEffect.isPlaying) sparksEffect.Play();
        }
        else
        {
            // Kiedy puszczamy, metal "cofa się" do idealnego muskania powierzchni
            currentDip = Mathf.Lerp(currentDip, hoverX, Time.deltaTime * 15f);

            if (sparksEffect != null && sparksEffect.isPlaying) sparksEffect.Stop();
        }

        currentMetal.transform.localPosition = new Vector3(currentDip, 0, bladeSlidePosition);
    }

    private void RefreshStatsWheel()
    {
        if (currentMetal == null) return;
        var wheel = BlacksmithInteraction.Instance?.wheel;
        if (wheel == null) return;
        WeaponData preview = currentMetal.GetPreviewStats();
        wheel.SetWheel(true);
        wheel.UpdateWheel(preview.GetNormalizedDamage(), preview.GetNormalizedSpeed(), preview.GetNormalizedAoE());
    }

    private void ExitGrindingMode()
    {
        var wheel = BlacksmithInteraction.Instance?.wheel;
        if (wheel != null) wheel.SetWheel(false);
        isGrindingMode = false;

        currentMetal.transform.SetParent(null);
        Rigidbody rb = currentMetal.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
        }

        currentMetal = null;

        if (playerObject != null) playerObject.SetActive(true);

        if (mainCamera != null)
        {
            mainCamera.SetParent(originalCameraParent);
            mainCamera.localPosition = originalCameraLocalPos;
            mainCamera.localRotation = originalCameraLocalRot;
        }

        if (sparksEffect != null && sparksEffect.isPlaying) sparksEffect.Stop();
    }
}