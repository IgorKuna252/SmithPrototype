using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MoldSetup
{
    public string moldName;
    public GameObject moldGroupParent;
    public Transform liquidVisual;
    public GameObject prefabToSpawn;
    [HideInInspector] public Vector3 originalScale; // Każda forma pamięta swoją skalę!
    [HideInInspector] public Material liquidMat;    // Każda forma pamięta swój materiał!
}

public class MoldManager : MonoBehaviour
{
    [Header("Kamera Formy")]
    public GameObject assemblyCamera;
    private GameObject mainPlayerCamera;
    public bool isAssemblyMode = false;

    [Header("Dokowanie Tygla")]
    public Transform crucibleDockPoint;
    [HideInInspector] public Crucible dockedCrucible;

    [Header("Konfiguracja Form")]
    public List<MoldSetup> molds;
    private int currentMoldIndex = 0;

    [Header("Ustawienia Odlewania")]
    public float fillSpeed = 0.5f;
    public float coolingTime = 3f;

    private float currentFill = 0f;
    private bool isFilling = false;
    private bool isFull = false;
    private bool isCooled = false;
    private float coolTimer = 0f;
    
    // Zapamiętany typ metalu z Tygla
    private MetalType moldedMetal;

    void Start()
    {
        for (int i = 0; i < molds.Count; i++)
        {
            // ZAPISUJEMY SKALĘ ZANIM COKOLWIEK WYŁĄCZYMY
            if (molds[i].liquidVisual != null)
            {
                molds[i].originalScale = molds[i].liquidVisual.localScale;
                
                // Pobieramy materiał
                Renderer rend = molds[i].liquidVisual.GetComponentInChildren<Renderer>();
                if (rend != null) molds[i].liquidMat = rend.material;
                
                // Resetujemy na start
                molds[i].liquidVisual.localScale = new Vector3(molds[i].originalScale.x, 0f, molds[i].originalScale.z);
                molds[i].liquidVisual.gameObject.SetActive(false);
            }

            // Włączamy tylko startową grupę (np. Sword)
            molds[i].moldGroupParent.SetActive(i == currentMoldIndex);
        }
    }

    void Update()
    {
        if (isFilling && !isFull)
        {
            MoldSetup activeMold = molds[currentMoldIndex];

            // WYMUSZAMY WŁĄCZENIE (Upewnij się, że w Inspektorze podpiąłeś tu AxeFix/SwordFix)
            if (!activeMold.liquidVisual.gameObject.activeSelf)
                activeMold.liquidVisual.gameObject.SetActive(true);

            currentFill += fillSpeed * Time.deltaTime;
            currentFill = Mathf.Clamp01(currentFill);

            // Matematyka skalowania:
            // $$S_y = S_{original} \cdot currentFill$$
            float currentYScale = Mathf.Lerp(0f, activeMold.originalScale.y, currentFill);
            activeMold.liquidVisual.localScale = new Vector3(activeMold.originalScale.x, currentYScale, activeMold.originalScale.z);

            if (currentFill >= 1f)
            {
                isFull = true;
                isFilling = false;
                Debug.Log("Forma pełna!");
            }
        }

        if (isFull && !isCooled)
        {
            coolTimer += Time.deltaTime;
            MoldSetup activeMold = molds[currentMoldIndex];
            
            if (activeMold.liquidMat != null)
            {
                float lerp = coolTimer / coolingTime;
                activeMold.liquidMat.color = Color.Lerp(new Color(1f, 0.4f, 0f), Color.gray, lerp);
            }

            if (coolTimer >= coolingTime)
            {
                isCooled = true;
                Debug.Log("Metal ostygł! Możesz wyciągnąć odlew (LPM).");
            }
        }
    }

    public bool IsFull() => isFull;

    public void ReceiveMetal(MetalType incomingMetal)
    {
        if (!isFull && !isCooled) 
        {
            isFilling = true;
            moldedMetal = incomingMetal;
        }
    }

    public void StopReceivingMetal()
    {
        isFilling = false;
    }

    public bool IsReadyToExtract() => isCooled;

    public GameObject ExtractItem()
    {
        if (isCooled)
        {
            MoldSetup activeMold = molds[currentMoldIndex];
            GameObject spawnedItem = Instantiate(activeMold.prefabToSpawn, activeMold.liquidVisual.position, activeMold.liquidVisual.rotation);
            
            // Nadpisujemy zmienną w stworzonym przedmiocie danymi z tygla
            MetalPiece piece = spawnedItem.GetComponent<MetalPiece>();
            if (piece != null)
            {
                piece.metalTier = moldedMetal;
            }
            
            ResetMold(activeMold);
            return spawnedItem;
        }
        return null;
    }

    private void ResetMold(MoldSetup mold)
    {
        currentFill = 0f;
        isFull = false;
        isCooled = false;
        coolTimer = 0f;
        isFilling = false;
        mold.liquidVisual.gameObject.SetActive(false);
        mold.liquidVisual.localScale = new Vector3(mold.originalScale.x, 0f, mold.originalScale.z);
        if (mold.liquidMat != null) mold.liquidMat.color = new Color(1f, 0.4f, 0f);
    }

    // Logika wejścia i wyjścia z kamery
    public void ToggleAssemblyCamera(GameObject playerCam)
    {
        if (isAssemblyMode) return;

        mainPlayerCamera = playerCam; 
        mainPlayerCamera.SetActive(false);
        if(assemblyCamera != null) assemblyCamera.SetActive(true);
        isAssemblyMode = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitAssemblyMode()
    {
        if (!isAssemblyMode) return;

        if (mainPlayerCamera != null) mainPlayerCamera.SetActive(true);
        if(assemblyCamera != null) assemblyCamera.SetActive(false);
        isAssemblyMode = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool ChangeMold()
    {
        // 1. BLOKADA: Jeśli wlewasz metal, forma jest pełna lub stygnie - nie przełączaj
        if (isFilling || currentFill > 0 || isCooled) 
        {
            Debug.Log("Nie można zmienić formy: proces odlewania w toku.");
            return false;
        }

        // Jeśli wszystko OK - przełączamy formę
        molds[currentMoldIndex].moldGroupParent.SetActive(false);
        currentMoldIndex = (currentMoldIndex + 1) % molds.Count;
        molds[currentMoldIndex].moldGroupParent.SetActive(true);
        
        Debug.Log("Zmieniono formę na: " + molds[currentMoldIndex].moldName);
        return true;
    }
    public void DockCrucible(Crucible crucible)
    {
        dockedCrucible = crucible;
        crucible.Dock(this, crucibleDockPoint);
    }

    public void UndockCrucible()
    {
        if (dockedCrucible != null)
        {
            dockedCrucible.Undock();
            dockedCrucible = null;
        }
    }
}