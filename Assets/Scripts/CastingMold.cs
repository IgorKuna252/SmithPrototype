using UnityEngine;
using System.Collections.Generic;

public class CastingMold : MonoBehaviour, IInteractable
{
    [Header("Dostępne kształty odlewu")]
    // Tutaj wrzucasz prefabrykaty, które forma może wyprodukować (np. MetalPiece_Sword, MetalPiece_Ingot)
    public List<GameObject> possibleCasts; 
    private int currentCastIndex = 0;

    [Header("Wizualizacja Płynu")]
    public Transform liquidVisual; // Płaski obiekt wewnątrz formy (Plane)
    public float maxFillHeight = 0.1f; // Jak wysoko ma się podnieść płyn
    private float minFillHeight;

    [Header("Ustawienia Odlewania")]
    public float fillSpeed = 0.5f; // Szybkość wypełniania
    public float coolingTime = 3f; // Czas stygnięcia w sekundach

    // Stany formy
    private float currentFill = 0f;
    private bool isFilling = false;
    private bool isFull = false;
    private bool isCooled = false;
    private float coolTimer = 0f;

    private Material liquidMaterial;

    void Start()
    {
        if (liquidVisual != null)
        {
            minFillHeight = liquidVisual.localPosition.y;
            liquidVisual.gameObject.SetActive(false); // Na starcie forma jest pusta
            
            Renderer rend = liquidVisual.GetComponent<Renderer>();
            if (rend != null) liquidMaterial = rend.material;
        }
    }

    void Update()
    {
        // 1. Logika wypełniania
        if (isFilling && !isFull)
        {
            liquidVisual.gameObject.SetActive(true);
            currentFill += fillSpeed * Time.deltaTime;

            // Podnoszenie poziomu płynu
            float newY = Mathf.Lerp(minFillHeight, maxFillHeight, currentFill);
            liquidVisual.localPosition = new Vector3(liquidVisual.localPosition.x, newY, liquidVisual.localPosition.z);

            if (currentFill >= 1f)
            {
                isFull = true;
                isFilling = false;
                Debug.Log("Forma pełna! Zaczyna stygnąć.");
            }
        }

        // 2. Logika stygnięcia
        if (isFull && !isCooled)
        {
            coolTimer += Time.deltaTime;
            
            // Opcjonalnie: Zmiana koloru z pomarańczowego (gorący) na szary (zimny)
            if (liquidMaterial != null)
            {
                float lerp = coolTimer / coolingTime;
                liquidMaterial.color = Color.Lerp(new Color(1f, 0.4f, 0f), Color.gray, lerp); // Zmienia z pomarańczowego na szary
            }

            if (coolTimer >= coolingTime)
            {
                isCooled = true;
                Debug.Log("Metal ostygł! Można wyciągnąć odlew.");
            }
        }
    }

    // Wywoływane przez BlacksmithInteraction pod klawiszem 'E' (z interfejsu IInteractable)
    public bool Interact()
    {
        if (currentFill > 0) 
        {
            Debug.Log("Nie możesz zmienić formy, w środku jest już metal!");
            return false;
        }

        // Przełączamy na kolejny kształt
        currentCastIndex++;
        if (currentCastIndex >= possibleCasts.Count) currentCastIndex = 0;

        Debug.Log("Zmieniono formę na: " + possibleCasts[currentCastIndex].name);
        
        // Tutaj opcjonalnie możesz też zmieniać wizualny model samej formy, jeśli masz różne modele
        
        return true;
    }

    // Funkcja do wywoływania, gdy gracz leje metal (np. trzymając Tygiel i klikając LPM)
    public void ReceiveMetal()
    {
        if (!isFull)
        {
            isFilling = true;
        }
    }

    // Służy do zatrzymania lania (gdy gracz puści przycisk)
    public void StopReceivingMetal()
    {
        isFilling = false;
    }

    // Wyciąganie gotowego przedmiotu
    public void ExtractItem()
    {
        if (isCooled)
        {
            // Spawnujemy gotowy prefabrykat (Twój MetalPiece)
            GameObject spawnedItem = Instantiate(possibleCasts[currentCastIndex], transform.position + Vector3.up * 0.5f, transform.rotation);
            
            // Resetujemy formę do stanu początkowego
            currentFill = 0f;
            isFull = false;
            isCooled = false;
            coolTimer = 0f;
            liquidVisual.gameObject.SetActive(false);
            liquidVisual.localPosition = new Vector3(liquidVisual.localPosition.x, minFillHeight, liquidVisual.localPosition.z);
            
            if (liquidMaterial != null) liquidMaterial.color = new Color(1f, 0.4f, 0f); // Wracamy do pomarańczowego

            Debug.Log("Wyciągnięto przedmiot: " + spawnedItem.name);
        }
        else if (isFull)
        {
            Debug.Log("Metal jest jeszcze zbyt gorący!");
        }
    }
}