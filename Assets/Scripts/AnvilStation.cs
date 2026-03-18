using UnityEngine;
using System.Collections; // Wymagane dla IEnumerator (Korutyny animacji)

public class AnvilStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    [Header("M�otek (Nowo��!)")]
    public GameObject hammerPrefab; // Mój model młotka
    private GameObject hammerObject;
    public Vector3 hammerHoverOffset = new Vector3(0, 0.4f, 0); // Jak wysoko nad kowad�em wisi m�ot
    public Vector3 hammerStrikeRotation = new Vector3(60f, 0, 0); // O ile stopni obraca si� przy uderzeniu
    private bool isSwinging = false; // Czy m�otek aktualnie uderza?

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
        if (hammerPrefab != null)
            hammerObject = Instantiate(hammerPrefab);
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
            camComponent = Camera.main;
        }

        // Ukrywamy m�otek na starcie gry
        if (hammerObject != null) hammerObject.SetActive(false);
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
            rb.isKinematic = false;
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

        // POKAZUJEMY M�OTEK
        if (hammerObject != null)
        {
            hammerObject.SetActive(true);
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

        // --- SYSTEM LASERA (Przeniesiony wy�ej, by m�otek �ledzi� kursor co klatk�!) ---
        Ray ray = camComponent.ScreenPointToRay(Input.mousePosition);
        Plane metalPlane = new Plane(currentMetal.transform.up, currentMetal.transform.position);

        if (metalPlane.Raycast(ray, out float enter))
        {
            Vector3 cursorPoint = ray.GetPoint(enter);

            // 1. �ledzenie kursora przez m�otek (Hover)
            if (hammerObject != null && !isSwinging)
            {
                // M�otek wisi nad punktem kursora
                Vector3 targetHoverPosition = cursorPoint + hammerHoverOffset;
                hammerObject.transform.position = Vector3.Lerp(hammerObject.transform.position, targetHoverPosition, Time.deltaTime * 15f);

                // M�otek wraca do prostej rotacji po uderzeniu
                hammerObject.transform.rotation = Quaternion.Lerp(hammerObject.transform.rotation, Quaternion.identity, Time.deltaTime * 15f);
            }

            // 2. Klikni�cie = Animacja uderzenia
            if (Input.GetMouseButtonDown(0) && Time.time > lastHitTime + hammerCooldown && !isSwinging)
            {
                lastHitTime = Time.time;

                // Odpalamy asynchroniczn� animacj� (Korutyn�)
                if (hammerObject != null)
                {
                    StartCoroutine(SwingHammerAnim(cursorPoint));
                }
                else
                {
                    // Fallback, je�li nie przypisa�e� modelu m�otka w Inspektorze
                    PerformHitEffects(cursorPoint);
                }
            }
        }
    }

    // --- PROCEDURALNA ANIMACJA M�OTKA ---
    private IEnumerator SwingHammerAnim(Vector3 hitPoint)
    {
        isSwinging = true;

        Vector3 startPos = hammerObject.transform.position;
        Quaternion startRot = hammerObject.transform.rotation;

        // Obliczamy rotacj� uderzeniow� (pochylenie)
        Quaternion strikeRot = startRot * Quaternion.Euler(hammerStrikeRotation);

        // FAZA 1: B�yskawiczny zamach w d�
        float swingDownTime = 0.05f; // Uderzenie trwa u�amek sekundy
        float elapsed = 0f;

        while (elapsed < swingDownTime)
        {
            hammerObject.transform.position = Vector3.Lerp(startPos, hitPoint, elapsed / swingDownTime);
            hammerObject.transform.rotation = Quaternion.Lerp(startRot, strikeRot, elapsed / swingDownTime);
            elapsed += Time.deltaTime;
            yield return null; // Czekamy do nast�pnej klatki
        }

        // FAZA 2: IMPACT (Kontakt z metalem)
        hammerObject.transform.position = hitPoint;
        hammerObject.transform.rotation = strikeRot;

        PerformHitEffects(hitPoint); // Wgniatamy siatk� i puszczamy iskry!

        // FAZA 3: Odskoczenie do g�ry (Odrzut)
        float swingUpTime = 0.1f;
        elapsed = 0f;

        while (elapsed < swingUpTime)
        {
            // M�otek naturalnie wraca do punktu startowego
            hammerObject.transform.position = Vector3.Lerp(hitPoint, startPos, elapsed / swingUpTime);
            hammerObject.transform.rotation = Quaternion.Lerp(strikeRot, startRot, elapsed / swingUpTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isSwinging = false;
    }

    private void PerformHitEffects(Vector3 hitPoint)
    {
        // Sprawdzamy, czy uderzenie faktycznie trafi�o i odkszta�ci�o stal
        bool validHit = currentMetal.HitMetal(hitPoint, currentMetal.transform.up);

        // Odpalamy iskry TYLKO, je�li trafili�my w metal (a nie w puste kowad�o)
        if (validHit && hitSparks != null)
        {
            hitSparks.transform.position = hitPoint;
            hitSparks.Play();
        }
    }

    private void ExitForgingMode()
    {
        isForgingMode = false;

        // CHOWAMY M�OTEK
        if (hammerObject != null) hammerObject.SetActive(false);

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