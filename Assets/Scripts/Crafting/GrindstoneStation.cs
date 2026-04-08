using UnityEngine;

public class GrindstoneStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    [Header("Ustawienia Kamienia (Tuning)")]
    public float distanceToStone = 0.15f;
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
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        currentMetal.transform.SetParent(snapPoint);
        currentMetal.transform.localPosition = Vector3.zero;
        currentMetal.transform.localRotation = Quaternion.identity;

        currentDip = distanceToStone - currentMetal.GetEdgeWidthAt(0, false);

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.parent;
            originalCameraLocalPos = mainCamera.localPosition;
            originalCameraLocalRot = mainCamera.localRotation;
            mainCamera.SetParent(null);
        }

        if (playerObject != null) playerObject.SetActive(false);

        // Wyświetlamy instrukcje specyficzne dla Szlifierki
        StationUIManager.Instance.ShowInstructions(
            "<b>SZLIFIERKA</b>\n" +
            "LPM - Szlifowanie / Wżeranie\n" +
            "LPM + SHIFT - Równanie (Pilnik)\n" +
            "SCROLL - Przesuwanie miecza\n" +
            "PPM - Odwrócenie ostrza\n" +
            "E - Wyjście"
        );
    }

    private void HandleGrindingMinigame()
    {
        if (mainCamera != null && cameraSocket != null)
        {
            mainCamera.position = cameraSocket.position;
            mainCamera.rotation = cameraSocket.rotation;
        }

        // Obracanie miecza
        if (Input.GetMouseButtonDown(1)) isFlipped = !isFlipped;
        float targetRotationZ = isFlipped ? 180f : 0f;
        currentMetal.transform.localRotation = Quaternion.Lerp(currentMetal.transform.localRotation, Quaternion.Euler(0, 0, targetRotationZ), Time.deltaTime * 8f);

        // Płynne przesuwanie i chłodzenie
        float scroll = Input.mouseScrollDelta.y;
        bladeSlidePosition += scroll * 0.015f;

        // Jeśli ruszamy rolką, resetujemy barierę zniszczenia!
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentMetal.ResetEdgeIntegrity();
        }

        // Limity
        if (currentMetal.metalSpine.Count > 0)
        {
            float minLimit = -currentMetal.metalSpine[currentMetal.metalSpine.Count - 1].z - 0.05f;
            float maxLimit = -currentMetal.metalSpine[0].z + 0.05f;
            bladeSlidePosition = Mathf.Clamp(bladeSlidePosition, minLimit, maxLimit);
        }

        float currentEdgeWidth = currentMetal.GetEdgeWidthAt(-bladeSlidePosition, isFlipped);
        float hoverX = distanceToStone - currentEdgeWidth;

        // --- OBSŁUGA SZLIFOWANIA (Z SHIFTEM) ---
        if (Input.GetMouseButton(0))
        {
            // Zmiana działania w zależności od klawisza SHIFT
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // TRYB PILNIKA: Prasuje nierówności do płaskiej linii (Świetne do ostrzy)
                currentMetal.SmoothPerfectEdge(-bladeSlidePosition, isFlipped);

                // Kamień wciska się trochę słabiej w modelu 3D dla odróżnienia wizualnego
                currentDip = Mathf.Lerp(currentDip, hoverX + (grindBiteDepth * 0.5f), Time.deltaTime * 15f);
            }
            else
            {
                // TRYB NORMALNY: Standardowe, punktowe zjadanie krawędzi i wżeranie się w głąb
                currentMetal.GrindPerfectEdge(-bladeSlidePosition, isFlipped);

                // Głębokie wcięcie w model 3D
                currentDip = Mathf.Lerp(currentDip, hoverX + grindBiteDepth, Time.deltaTime * 15f);
            }

            if (sparksEffect != null && !sparksEffect.isPlaying) sparksEffect.Play();
        }
        else
        {
            currentDip = Mathf.Lerp(currentDip, hoverX, Time.deltaTime * 15f);
            if (sparksEffect != null && sparksEffect.isPlaying) sparksEffect.Stop();
        }

        currentMetal.transform.localPosition = new Vector3(currentDip, 0, bladeSlidePosition);
    }

    private void ExitGrindingMode()
    {
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

        // Ukrywamy instrukcje przy wyjściu
        StationUIManager.Instance.HideInstructions();
    }
}