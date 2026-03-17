using UnityEngine;

// Zauważ, że dodaliśmy tu interfejs IPickable
public class Crucible : MonoBehaviour, IPickable
{
    [Header("Ustawienia Odlewania")]
    public float pourRange = 3f;

    private Camera mainCamera;
    private MoldManager currentMold;
    private bool isPouring = false;
    
    // Zmienna, która mówi nam, czy gracz trzyma ten obiekt
    private bool isHeld = false; 

    void Start()
    {
        mainCamera = Camera.main; 
    }

    void Update()
    {
        // MAGIA: Jeśli tygiel nie jest w ręku gracza, ucinamy logikę w tym miejscu!
        if (!isHeld) return;

        if (Input.GetMouseButton(0))
        {
            TryPourMetal();
        }
        else if (isPouring)
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
}