using UnityEngine;
using System.Collections; // Wymagane dla IEnumerator (Korutyny animacji)

public class AnvilStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    [Header("M³otek (Nowoœæ!)")]
    public Transform hammerObject; // Twój model m³otka
    public Vector3 hammerHoverOffset = new Vector3(0, 0.4f, 0); // Jak wysoko nad kowad³em wisi m³ot
    public Vector3 hammerStrikeRotation = new Vector3(60f, 0, 0); // O ile stopni obraca siê przy uderzeniu
    private bool isSwinging = false; // Czy m³otek aktualnie uderza?

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

        // Ukrywamy m³otek na starcie gry
        if (hammerObject != null) hammerObject.gameObject.SetActive(false);
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

        // POKAZUJEMY M£OTEK
        if (hammerObject != null)
        {
            hammerObject.gameObject.SetActive(true);
            isSwinging = false;
        }
    }

    private void HandleForgingMinigame()
    {
        if (mainCamera != null && cameraSocket != null)
        {
            mainCamera.position = cameraSocket.position;
            mainCamera.rotation = cameraSocket.rotation;
        }

        float targetRotationZ = rotationStep * 90f;
        currentMetal.transform.localRotation =
            Quaternion.Lerp(
                currentMetal.transform.localRotation,
                Quaternion.Euler(0, 0, targetRotationZ),
                Time.deltaTime * 15f
            );

        float scroll = Input.mouseScrollDelta.y;
        slidePosition += scroll * 0.05f;

        Mesh mesh = currentMetal.GetComponent<MeshFilter>().mesh;
        float maxSlide = mesh.bounds.extents.z + 0.1f;

        slidePosition = Mathf.Clamp(slidePosition, -maxSlide, maxSlide);
        currentMetal.transform.localPosition = new Vector3(0, 0, slidePosition);

        // --- SYSTEM LASERA (Przeniesiony wy¿ej, by m³otek œledzi³ kursor co klatkê!) ---
        Ray ray = camComponent.ScreenPointToRay(Input.mousePosition);
        Plane metalPlane = new Plane(currentMetal.transform.up, currentMetal.transform.position);

        if (metalPlane.Raycast(ray, out float enter))
        {
            Vector3 cursorPoint = ray.GetPoint(enter);

            // 1. Œledzenie kursora przez m³otek (Hover)
            if (hammerObject != null && !isSwinging)
            {
                // M³otek wisi nad punktem kursora
                Vector3 targetHoverPosition = cursorPoint + hammerHoverOffset;
                hammerObject.position = Vector3.Lerp(hammerObject.position, targetHoverPosition, Time.deltaTime * 15f);

                // M³otek wraca do prostej rotacji po uderzeniu
                hammerObject.rotation = Quaternion.Lerp(hammerObject.rotation, Quaternion.identity, Time.deltaTime * 15f);
            }

            // 2. Klikniêcie = Animacja uderzenia
            if (Input.GetMouseButtonDown(0) && Time.time > lastHitTime + hammerCooldown && !isSwinging)
            {
                lastHitTime = Time.time;

                // Odpalamy asynchroniczn¹ animacjê (Korutynê)
                if (hammerObject != null)
                {
                    StartCoroutine(SwingHammerAnim(cursorPoint));
                }
                else
                {
                    // Fallback, jeœli nie przypisa³eœ modelu m³otka w Inspektorze
                    PerformHitEffects(cursorPoint);
                }
            }
        }
    }

    // --- PROCEDURALNA ANIMACJA M£OTKA ---
    private IEnumerator SwingHammerAnim(Vector3 hitPoint)
    {
        isSwinging = true;

        Vector3 startPos = hammerObject.position;
        Quaternion startRot = hammerObject.rotation;

        // Obliczamy rotacjê uderzeniow¹ (pochylenie)
        Quaternion strikeRot = startRot * Quaternion.Euler(hammerStrikeRotation);

        // FAZA 1: B³yskawiczny zamach w dó³
        float swingDownTime = 0.05f; // Uderzenie trwa u³amek sekundy
        float elapsed = 0f;

        while (elapsed < swingDownTime)
        {
            hammerObject.position = Vector3.Lerp(startPos, hitPoint, elapsed / swingDownTime);
            hammerObject.rotation = Quaternion.Lerp(startRot, strikeRot, elapsed / swingDownTime);
            elapsed += Time.deltaTime;
            yield return null; // Czekamy do nastêpnej klatki
        }

        // FAZA 2: IMPACT (Kontakt z metalem)
        hammerObject.position = hitPoint;
        hammerObject.rotation = strikeRot;

        PerformHitEffects(hitPoint); // Wgniatamy siatkê i puszczamy iskry!

        // FAZA 3: Odskoczenie do góry (Odrzut)
        float swingUpTime = 0.1f;
        elapsed = 0f;

        while (elapsed < swingUpTime)
        {
            // M³otek naturalnie wraca do punktu startowego
            hammerObject.position = Vector3.Lerp(hitPoint, startPos, elapsed / swingUpTime);
            hammerObject.rotation = Quaternion.Lerp(strikeRot, startRot, elapsed / swingUpTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isSwinging = false;
    }

    private void PerformHitEffects(Vector3 hitPoint)
    {
        // Sprawdzamy, czy uderzenie faktycznie trafi³o i odkszta³ci³o stal
        bool validHit = currentMetal.HitMetal(hitPoint, currentMetal.transform.up);

        // Odpalamy iskry TYLKO, jeœli trafiliœmy w metal (a nie w puste kowad³o)
        if (validHit && hitSparks != null)
        {
            hitSparks.transform.position = hitPoint;
            hitSparks.Play();
        }
    }

    private void ExitForgingMode()
    {
        isForgingMode = false;

        // CHOWAMY M£OTEK
        if (hammerObject != null) hammerObject.gameObject.SetActive(false);

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