using UnityEngine;

// Definiujemy nasze tiery metali
public enum MetalType
{
    Copper,      // Miedź
    Bronze,      // Brąz
    Iron,        // Żelazo
    Steel,       // Stal
    Gold,        // Złoto
    Platinum,    // Platyna
    BlueSteel,   // Niebieska Stal
    Vibranium    // Wibranium
}

public class MetalPiece : MonoBehaviour
{
    [Header("Typ Metalu")]
    public MetalType metalTier; // Rozwijana lista w Inspektorze!

    [Header("Ustawienia Temperatury")]
    public float currentTemperature = 20f; 
    public float maxTemperature = 1000f;
    public float coolingRate = 10f; 
    public float forgingTemperature = 500f; 

    [Header("Ustawienia Kucia")]
    public int hitsRequired = 5; 
    private int currentHits = 0;
    public bool isFinished = false;

    private MeshRenderer meshRenderer;
    private bool isInForge = false;
    private Color baseColdColor; // Przechowuje domyślny kolor dla wybranego metalu

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        SetBaseColor(); // Ustawiamy kolor na starcie
    }

    // Ta magiczna funkcja Unity odpala się za każdym razem, gdy zmienisz coś w Inspektorze!
    // Dzięki temu zobaczysz zmianę koloru metalu od razu w edytorze, bez odpalania gry.
    void OnValidate()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            SetBaseColor();
            UpdateVisuals();
        }
    }

    void Update()
    {
        if (!isInForge && currentTemperature > 20f)
        {
            currentTemperature -= coolingRate * Time.deltaTime;
        }

        UpdateVisuals();
    }

    public void HitMetal()
    {
        if (isFinished) return;

        if (currentTemperature >= forgingTemperature)
        {
            currentHits++;
            Debug.Log($"Uderzenie! Postęp: {currentHits}/{hitsRequired}");

            // Zapisujemy starą skalę Y, by wyliczyć różnicę
            float oldYScale = transform.localScale.y;
            float minThickness = 0.05f; 
            float newYScale = Mathf.Max(oldYScale - 0.01f, minThickness);
            
            // Różnica w skali (będzie wartością ujemną, bo spłaszczamy)
            float differenceY = newYScale - oldYScale;

            // Zmieniamy skalę tak jak wcześniej
            transform.localScale = new Vector3(
                transform.localScale.x + 0.01f,
                newYScale, 
                transform.localScale.z + 0.05f
            );

            // MAGIA: Przesuwamy obiekt o połowę różnicy w skali. 
            // Dzięki temu dół obiektu ZAWSZE zostaje w tym samym miejscu!
            transform.position += new Vector3(0, differenceY / 2f, 0);

            if (currentHits >= hitsRequired)
            {
                isFinished = true;
                Debug.Log($"Przedmiot z {metalTier} został pomyślnie wykuty!");
            }
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go kuć! Włóż go do pieca.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Forge"))
        {
            isInForge = true;
            if (currentTemperature < maxTemperature)
            {
                currentTemperature += 50f * Time.deltaTime;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Forge"))
        {
            isInForge = false;
        }
    }

    // --- NOWE: Przypisywanie kolorów na podstawie wybranego metalu ---
    void SetBaseColor()
    {
        switch (metalTier)
        {
            case MetalType.Copper:      baseColdColor = new Color(0.8f, 0.4f, 0.2f); break; // Miedziany/Pomarańczowy
            case MetalType.Bronze:      baseColdColor = new Color(0.6f, 0.5f, 0.2f); break; // Ciemny, brudny żółty
            case MetalType.Iron:        baseColdColor = new Color(0.5f, 0.5f, 0.5f); break; // Zwykły szary
            case MetalType.Steel:       baseColdColor = new Color(0.7f, 0.75f, 0.8f); break; // Jasnoszary/Srebrzysty
            case MetalType.Gold:        baseColdColor = new Color(1f, 0.84f, 0f);    break; // Czysty złoty
            case MetalType.Platinum:    baseColdColor = new Color(0.9f, 0.9f, 0.95f); break; // Bardzo jasny, prawie biały
            case MetalType.BlueSteel:   baseColdColor = new Color(0.2f, 0.4f, 0.6f); break; // Stalowy niebieski
            case MetalType.Vibranium:   baseColdColor = new Color(0.6f, 0.2f, 0.8f); break; // Fantastyczny fiolet
            default:                    baseColdColor = Color.gray; break;
        }
    }

    void UpdateVisuals()
    {
        float temperatureNormalized = Mathf.Clamp01((currentTemperature - 20f) / (maxTemperature - 20f));
        
        // baseColdColor bierze kolor z naszego switcha wyżej!
        Color hotColor = new Color(1f, 0f, 0f); // Ten sam żarzący się pomarańczowy dla wszystkich

        meshRenderer.sharedMaterial.color = Color.Lerp(baseColdColor, hotColor, temperatureNormalized);
    }
}