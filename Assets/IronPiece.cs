using UnityEngine;

public class IronPiece : MonoBehaviour, IInteractable, IPickable
{
    [Header("Ustawienia Temperatury")]
    public float currentTemperature = 20f;
    public float maxTemperature = 1000f;
    public float coolingRate = 10f;
    public float forgingTemperature = 500f;

    [Header("Ustawienia Kucia")]
    public int hitsRequired = 10;
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
        if (!isInForge && currentTemperature > 20f)
        {
            currentTemperature -= coolingRate * Time.deltaTime;
        }

        UpdateVisuals();
    }

    public void Interact(KeyCode key)
    {
        if (isFinished) return;

        if (currentTemperature >= forgingTemperature)
        {
            currentHits++;
            Debug.Log($"Uderzenie! Postęp: {currentHits}/{hitsRequired}");

            transform.localScale = new Vector3(
                transform.localScale.x + 0.01f,
                transform.localScale.y - 0.01f,
                transform.localScale.z + 0.05f
            );

            if (currentHits >= hitsRequired)
            {
                isFinished = true;
                Debug.Log("Przedmiot został pomyślnie wykuty!");
            }
        }
        else
        {
            Debug.Log("Metal jest zbyt zimny, by go kuć! Włóż go do pieca.");
        }
    }

    public void OnPickUp()
    {
        isInForge = false;
    }

    public void OnDrop()
    {
        // możesz tu dodać logikę np. efekt iskier przy upuszczeniu
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

    void UpdateVisuals()
    {
        float temperatureNormalized = (currentTemperature - 20f) / (maxTemperature - 20f);
        Color coldColor = Color.gray;
        Color hotColor = new Color(1f, 0.4f, 0f);

        meshRenderer.material.color = Color.Lerp(coldColor, hotColor, temperatureNormalized);
    }
}