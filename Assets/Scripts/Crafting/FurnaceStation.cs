using UnityEngine;

public class FurnaceStation : MonoBehaviour
{
    [Header("Referencje")]
    public Transform metalSocket;
    public Transform cameraPosition;
    public MeshRenderer fireRenderer; 
    
    [Header("Parametry Pieca")]
    public float currentFurnaceTemp = 20f;
    public float maxFurnaceTemp = 1000f;
    public float bellowsPower = 40f; // Zwolnione z 150f na bardziej stopniowe 40f
    public float coolingRate = 50f;

    [Header("UI Pieca (Opcjonalne)")]
    public GameObject furnaceUI; // Przeciągnij tutaj Canvas z Przyciskiem Pompowania

    private Camera mainCamera;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private Transform originalCameraParent;

    private bool isMinigameActive = false;
    private MetalPiece currentMetal;

    private Color coldFireColor = new Color(0.2f, 0.05f, 0f); // Ciemny popiół
    private Color hotFireColor = new Color(0.8f, 0.25f, 0f);  // Lekko stonowany czerwony-pomarańczowy, bez oczojebności

    void Start()
    {
        mainCamera = Camera.main;
        if (furnaceUI != null) furnaceUI.SetActive(false); // Chowamy interfejs na start
    }

    void Update()
    {
        // Chłodzenie pieca
        if (currentFurnaceTemp > 20f)
        {
            currentFurnaceTemp -= coolingRate * Time.deltaTime;
        }

        // Aktualizacja wizualna paleniska
        UpdateFireVisuals();

        if (!isMinigameActive) return;

        // Przekazywanie ciepła do metalu
        if (currentMetal != null)
        {
            if (currentMetal.currentTemperature < currentFurnaceTemp)
            {
                // Mocne podbicie transferu temperatury z pieca na metal (żeby szybko doganiał kolor pieca)
                currentMetal.currentTemperature += (currentFurnaceTemp * 0.8f) * Time.deltaTime;
                if (currentMetal.currentTemperature > currentMetal.maxTemperature) 
                    currentMetal.currentTemperature = currentMetal.maxTemperature;
            }
        }

        // Wyjście z minigry
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            ExitFurnaceMode();
        }
    }

    /// <summary>
    /// Metoda do podpięcia (OnClick) we wbudowanym Przycisku (Button) na Canvasie!
    /// </summary>
    public void PumpBellows()
    {
        if (!isMinigameActive) return;

        currentFurnaceTemp += bellowsPower;
        if (currentFurnaceTemp > maxFurnaceTemp) currentFurnaceTemp = maxFurnaceTemp;
        Debug.Log($"[Furnace] Pompowanie miechem! Temp: {Mathf.RoundToInt(currentFurnaceTemp)}");
    }

    public void EnterFurnaceMode(MetalPiece metal)
    {
        currentMetal = metal;
        
        // --- AWARYJNE SZUKANIE: Jeśli gracz wszedł z pustymi rękami, ale jakiś metal już leży w piecu ze starych kuć! ---
        if (currentMetal == null && metalSocket != null)
        {
            currentMetal = metalSocket.GetComponentInChildren<MetalPiece>();
        }

        // Wyłączenie grawitacji w piecu (tylko jeśli mamy obiekt i to NOWY obiekt, nie leżący w piecu wcześniej)
        if (metal != null) // warunek na "metal" przyniesiony z zewnątrz
        {
            Rigidbody rb = metal.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            currentMetal.transform.SetParent(metalSocket);
            currentMetal.transform.localPosition = Vector3.zero;
            currentMetal.transform.localRotation = Quaternion.identity;
        }

        // Przestawienie kamery na widok pieca
        originalCameraParent = mainCamera.transform.parent;
        originalCameraPos = mainCamera.transform.localPosition;
        originalCameraRot = mainCamera.transform.localRotation;

        mainCamera.transform.SetParent(cameraPosition);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;

        // Schowanie ogólnego UI gracza (crosshair)
        if (BlacksmithInteraction.Instance != null && BlacksmithInteraction.Instance.playerUI != null)
            BlacksmithInteraction.Instance.playerUI.gameObject.SetActive(false);

        // Pojawienie się specjalnego UI pieca (guzik Dmuchania)
        if (furnaceUI != null) furnaceUI.SetActive(true);

        // Uwolnienie myszki, żeby móc nacisnąć w nowy przycisk na ekranie!!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Zatrzymanie chodzenia ręcznego
        var playerMovement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        BlacksmithInteraction.Instance.enabled = false;
        isMinigameActive = true;
    }

    public void ExitFurnaceMode()
    {
        isMinigameActive = false;

        // Powrót kamery do głowy gracza
        mainCamera.transform.SetParent(originalCameraParent);
        mainCamera.transform.localPosition = originalCameraPos;
        mainCamera.transform.localRotation = originalCameraRot;

        // Reset UI i zaciągnięcie celownika 
        if (furnaceUI != null) furnaceUI.SetActive(false);
        if (BlacksmithInteraction.Instance != null && BlacksmithInteraction.Instance.playerUI != null)
            BlacksmithInteraction.Instance.playerUI.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var playerMovement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = true;

        BlacksmithInteraction.Instance.enabled = true;
        
        // Zostawiamy metal w piecu, by gracz go wziął starym sposobem z celownika!
        currentMetal = null; 
    }

    private void UpdateFireVisuals()
    {
        if (fireRenderer != null)
        {
            float t = Mathf.Clamp01((currentFurnaceTemp - 20f) / (maxFurnaceTemp - 20f));
            Color c = Color.Lerp(coldFireColor, hotFireColor, t);
            
            // Żeby zmienić kolor we wbudowanym materiale bazowym (np. URP Lit lub Standard), odpytujemy _BaseColor lub nakładamy wprost
            Material mat = fireRenderer.material; // Zmiana twarda zamiast blocka dla pewności! (zrobi sobie instancję by nie popsuć innych cube-ów)
            
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);             // Standard shader
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);     // URP shader
            
            if (mat.HasProperty("_EmissionColor")) 
            {
                mat.EnableKeyword("_EMISSION"); // Ważne, żeby włączyć tryb świecenia, jeśli był wyłączony
                
                // Mnożnik odczuwalnie obniżony. Zamiast mnożyć do 400% (c * 4), mnoży max o delikatne 80% w gorączce (c * 1.8f)
                mat.SetColor("_EmissionColor", c * (1f + t * 0.8f)); 
            }
        }
    }
}
