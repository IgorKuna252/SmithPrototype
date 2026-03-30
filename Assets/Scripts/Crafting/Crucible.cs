using UnityEngine;

// Zauważ, że dodaliśmy tu interfejs IPickable
public class Crucible : MonoBehaviour, IPickable
{
    [Header("Ustawienia Odlewania")]
    public float pourRange = 3f;

    [Header("Odlewanie w stacji (Dokowanie)")]
    public float tiltSpeed = 100f;

    [Header("Zawartość")]
    public float currentFill = 0f;
    public float maxCapacity = 100f;
    public MetalType currentMetal;

    [Header("Wizualizacja Płynu")]
    public Transform liquidVisual;
    private Material liquidMat;
    private Vector3 originalLiquidScale;
    
    private bool isDocked = false;
    private MoldManager dockedMold = null;
    private float currentTilt = 0f;

    private Camera mainCamera;
    private MoldManager currentMold;
    private bool isPouring = false;
    
    // Zmienna, która mówi nam, czy gracz trzyma ten obiekt
    private bool isHeld = false; 

    void Start()
    {
        mainCamera = Camera.main; 
        
        if (liquidVisual != null)
        {
            Renderer rend = liquidVisual.GetComponentInChildren<Renderer>();
            if (rend != null) liquidMat = rend.material;
            originalLiquidScale = liquidVisual.localScale;
            UpdateLiquidVisual();
        }
    }

    void Update()
    {
        if (isDocked)
        {
            if (Input.GetMouseButton(0))
            {
                currentTilt = Mathf.MoveTowards(currentTilt, -90f, tiltSpeed * Time.deltaTime);
                if (currentTilt <= -45f && dockedMold != null)
                {
                    if (currentFill > 0 && !dockedMold.IsFull())
                    {
                        dockedMold.ReceiveMetal(currentMetal);
                        isPouring = true;
                        
                        // Zmniejszamy płyn w tyglu z prędkością napełniania formy (zakładamy 100 w Tyglu vs 1 w Moldzie)
                        currentFill = Mathf.Max(0, currentFill - (dockedMold.fillSpeed * Time.deltaTime * 100f));
                        UpdateLiquidVisual();
                    }
                    else
                    {
                        dockedMold.StopReceivingMetal();
                        isPouring = false;
                    }
                }
            }
            else
            {
                currentTilt = Mathf.MoveTowards(currentTilt, 0f, tiltSpeed * Time.deltaTime);
                if (currentTilt > -45f && isPouring)
                {
                    if (dockedMold != null) dockedMold.StopReceivingMetal();
                    isPouring = false;
                }
            }
            
            // Obracamy prosto po osi Y w kierunku do -90 st.
            transform.localRotation = Quaternion.Euler(0f, currentTilt, 0f);
            return;
        }

        // Celowo zablokowano lanie prosto z ręki w zwykłym widoku postaci
        if (isPouring)
        {
            StopPouring();
        }
    }

    private void TryPourMetal()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pourRange))
        {
            MoldManager mold = hit.collider.GetComponent<MoldManager>();
            
            if (mold != null)
            {
                mold.ReceiveMetal();
                currentMold = mold;
                isPouring = true;
                return;
            }
        }

        StopPouring();
    }

    private void StopPouring()
    {
        if (currentMold != null)
        {
            currentMold.StopReceivingMetal();
            currentMold = null;
        }
        isPouring = false;
    }

    // --- FUNKCJE POJEMNOŚCI TYGLA ---

    public void AddMetal(MetalType metal, float amount)
    {
        if (currentFill <= 0) currentMetal = metal;
        currentFill = Mathf.Clamp(currentFill + amount, 0, maxCapacity);
        UpdateLiquidVisual();
    }

    private void UpdateLiquidVisual()
    {
        if (liquidVisual != null)
        {
            if (currentFill > 0)
            {
                liquidVisual.gameObject.SetActive(true);
                float percent = currentFill / maxCapacity;
                liquidVisual.localScale = new Vector3(originalLiquidScale.x, originalLiquidScale.y * percent, originalLiquidScale.z);
                
                if (liquidMat != null) liquidMat.color = new Color(1f, 0.4f, 0f);
            }
            else
            {
                liquidVisual.gameObject.SetActive(false);
            }
        }
    }

    // --- FUNKCJE Z INTERFEJSU IPickable ---
    
    public void OnPickUp()
    {
        isHeld = true; // Gracz podniósł tygiel, pozwalamy na lanie!
    }

    public void OnDrop()
    {
        isHeld = false; // Gracz wyrzucił tygiel
        StopPouring();  // Zabezpieczenie: jeśli gracz wyrzuci tygiel w trakcie lania, automatycznie zakręcamy kurek
    }

    // --- FUNKCJE DOKOWANIA NA STATION ---

    public void Dock(MoldManager mold, Transform dockPoint)
    {
        isDocked = true;
        dockedMold = mold;
        
        transform.SetParent(dockPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        currentTilt = 0f;
        isPouring = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void Undock()
    {
        isDocked = false;
        currentTilt = 0f;
        transform.localRotation = Quaternion.identity;
        
        if (isPouring)
        {
            if (dockedMold != null) dockedMold.StopReceivingMetal();
            isPouring = false;
        }
        dockedMold = null;
    }
}