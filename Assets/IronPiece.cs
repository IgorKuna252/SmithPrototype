using UnityEngine;

public class IronPiece : MonoBehaviour
{
    [Header("Ustawienia Temperatury")]
    public float currentTemperature = 20f; // Temperatura pokojowa
    public float maxTemperature = 1000f;
    public float coolingRate = 10f; // Jak szybko stygnie
    public float forgingTemperature = 500f; // Minimalna temp. do kucia

    [Header("Ustawienia Kucia")]
    public int hitsRequired = 10; // Ile uderzeñ potrzeba do ukoñczenia
    private int currentHits = 0;
    public bool isFinished = false;

    private MeshRenderer meshRenderer;
    private bool isInForge = false;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        // Ch³odzenie metalu, jeœli nie jest w piecu
        if (!isInForge && currentTemperature > 20f)
        {
            currentTemperature -= coolingRate * Time.deltaTime;
        }

        UpdateVisuals();
    }

    // Funkcja wywo³ywana, gdy klikniemy na obiekt metalu (symulacja uderzenia m³otem)
    public void HitMetal()
    {
        if (isFinished) return;

        if (currentTemperature >= forgingTemperature)
        {
            currentHits++;
            Debug.Log($"Uderzenie! Postêp: {currentHits}/{hitsRequired}");

            // Skalowanie obiektu, aby symulowaæ sp³aszczanie/wyd³u¿anie
            transform.localScale = new Vector3(
                transform.localScale.x + 0.01f,
                transform.localScale.y - 0.01f,
                transform.localScale.z + 0.05f
            );

            if (currentHits >= hitsRequired)
            {
                isFinished = true;
                Debug.Log("Przedmiot zosta³ pomyœlnie wykuty!");
                // Tutaj mo¿esz podmieniæ model na gotowy miecz
            }
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go kuæ! W³ó¿ go do pieca.");
        }
    }

    // Funkcje do wykrywania pieca
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Forge"))
        {
            isInForge = true;
            // Ogrzewanie metalu
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

    // Zmiana koloru w zale¿noœci od temperatury (od szarego do czerwono-¿ó³tego)
    void UpdateVisuals()
    {
        float temperatureNormalized = (currentTemperature - 20f) / (maxTemperature - 20f);
        Color coldColor = Color.gray;
        Color hotColor = new Color(1f, 0.4f, 0f); // ¯arz¹cy siê pomarañczowy

        meshRenderer.material.color = Color.Lerp(coldColor, hotColor, temperatureNormalized);
    }
}