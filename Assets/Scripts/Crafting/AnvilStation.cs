using UnityEngine;
using System.Collections;

public class AnvilStation : MonoBehaviour
{
    [Header("Przypisz te obiekty:")]
    public Transform snapPoint;
    public Transform cameraSocket;
    public GameObject playerObject;

    [Header("Młotek")]
    public GameObject hammerPrefab;
    private GameObject hammerObject;
    public Vector3 hammerHoverOffset = new Vector3(0, 0.4f, 0);
    public Vector3 hammerStrikeRotation = new Vector3(60f, 0, 0);

    [Header("Obrót i Korekta Pozycji (Pivot)")]
    public float sidewaysTwistAngle = 90f;
    public Vector3 normalStrikeOffset = Vector3.zero;           // Przesunięcie wizualne gdy prosto
    public Vector3 sidewaysStrikeOffset = new Vector3(0.2f, 0, 0); // Przesunięcie wizualne gdy bokiem (Zmień to w Inspektorze!)

    private bool isHammerSideways = false;
    private bool isSwinging = false;

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

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player");

        slidePosition = 0f;
        rotationStep = 0;
        isHammerSideways = false;

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

        if (playerObject != null) playerObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        currentMetal.transform.localRotation = Quaternion.Lerp(
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

        Ray ray = camComponent.ScreenPointToRay(Input.mousePosition);
        Plane metalPlane = new Plane(currentMetal.transform.up, currentMetal.transform.position);

        if (metalPlane.Raycast(ray, out float enter))
        {
            Vector3 cursorPoint = ray.GetPoint(enter);

            // Wybieramy odpowiedni offset w zależności od tego, czy młot jest obrócony
            Vector3 currentVisualOffset = isHammerSideways ? sidewaysStrikeOffset : normalStrikeOffset;

            // 1. ŚLEDZENIE I OBRÓT (HOVER)
            if (hammerObject != null && !isSwinging)
            {
                // Dodajemy nasz offset korygujący!
                Vector3 targetHoverPosition = cursorPoint + currentVisualOffset + hammerHoverOffset;
                hammerObject.transform.position = Vector3.Lerp(hammerObject.transform.position, targetHoverPosition, Time.deltaTime * 15f);

                float currentTwist = isHammerSideways ? sidewaysTwistAngle : 0f;
                Quaternion targetHoverRotation = Quaternion.Euler(0, currentTwist, 0);
                hammerObject.transform.rotation = Quaternion.Lerp(hammerObject.transform.rotation, targetHoverRotation, Time.deltaTime * 15f);
            }

            // 2. PRZEŁĄCZANIE TRYBU MŁOTKA (PPM)
            if (Input.GetMouseButtonDown(1))
            {
                isHammerSideways = !isHammerSideways;
            }

            // 3. UDERZENIE MŁOTEM (LPM)
            if (Input.GetMouseButtonDown(0) && Time.time > lastHitTime + hammerCooldown && !isSwinging)
            {
                lastHitTime = Time.time;

                HitType hitType = isHammerSideways ? HitType.Widen : HitType.Lengthen;

                if (hammerObject != null)
                {
                    // Przekazujemy wizualny offset do animacji
                    StartCoroutine(SwingHammerAnim(cursorPoint, hitType, currentVisualOffset));
                }
                else
                {
                    PerformHitEffects(cursorPoint, hitType);
                }
            }
        }
    }

    private IEnumerator SwingHammerAnim(Vector3 hitPoint, HitType hitType, Vector3 visualOffset)
    {
        isSwinging = true;

        Vector3 startPos = hammerObject.transform.position;
        Quaternion startRot = hammerObject.transform.rotation;

        Quaternion strikeRot = startRot * Quaternion.Euler(hammerStrikeRotation);

        // Punkt, w który uderzy WIZUALNIE model młotka
        Vector3 visualStrikePos = hitPoint + visualOffset;

        float swingDownTime = 0.05f;
        float elapsed = 0f;

        while (elapsed < swingDownTime)
        {
            hammerObject.transform.position = Vector3.Lerp(startPos, visualStrikePos, elapsed / swingDownTime);
            hammerObject.transform.rotation = Quaternion.Lerp(startRot, strikeRot, elapsed / swingDownTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        hammerObject.transform.position = visualStrikePos;
        hammerObject.transform.rotation = strikeRot;

        // Ale gra WIE, że uderzyłeś dokładnie tam, gdzie myszką!
        PerformHitEffects(hitPoint, hitType);

        float swingUpTime = 0.1f;
        elapsed = 0f;

        while (elapsed < swingUpTime)
        {
            hammerObject.transform.position = Vector3.Lerp(visualStrikePos, startPos, elapsed / swingUpTime);
            hammerObject.transform.rotation = Quaternion.Lerp(strikeRot, startRot, elapsed / swingUpTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isSwinging = false;
    }

    private void PerformHitEffects(Vector3 hitPoint, HitType hitType)
    {
        bool validHit = currentMetal.HitMetal(hitPoint, currentMetal.transform.up, hitType);

        if (validHit && hitSparks != null)
        {
            hitSparks.transform.position = hitPoint;
            hitSparks.Play();
        }
    }

    private void ExitForgingMode()
    {
        isForgingMode = false;

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

        if (playerObject != null) playerObject.SetActive(true);

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