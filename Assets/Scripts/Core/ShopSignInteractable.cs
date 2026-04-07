using UnityEngine;
using TMPro;

/// <summary>
/// Tabliczka "Otwarte / Zamknięte" przy wejściu do jaskini.
/// Gracz klika [E] aby przełączyć stan warsztatu.
/// 
/// KONFIGURACJA NA SCENIE:
///   1. Utwórz pusty GameObject lub użyj istniejącego obiektu tabliczki przy wejściu do jaskini
///   2. Dodaj do niego BoxCollider (zaznacz isTrigger = false, żeby raycast gracza go łapał)
///   3. Dodaj ten skrypt (ShopSignInteractable)
///   4. (Opcjonalnie) Przeciągnij obiekt TextMeshPro z napisem do pola "Sign Text" - wtedy napis będzie się zmieniał automatycznie
/// </summary>
public class ShopSignInteractable : MonoBehaviour, IInteractable
{
    [Header("Stan Tabliczki")]
    [Tooltip("Czy warsztat jest aktualnie otwarty?")]
    public bool isOpen = false;

    [Header("Opcjonalny Tekst na Tabliczce (3D TextMeshPro)")]
    [Tooltip("Przeciągnij tu TextMeshPro z tabliczki, żeby napis się zmieniał")]
    public TextMeshPro signText3D;

    [Header("Opcjonalny Tekst na Tabliczce (Canvas TextMeshPro)")]
    [Tooltip("Alternatywnie: przeciągnij TextMeshProUGUI z Canvas UI")]
    public TextMeshProUGUI signTextUI;

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            isOpen = !DayNightManager.Instance.isDay;
            DayNightManager.Instance.OnNightStarted += () => { isOpen = true; UpdateSignVisual(); };
            DayNightManager.Instance.OnDayStarted  += () => { isOpen = false; UpdateSignVisual(); };
        }
        UpdateSignVisual();
    }

    /// <summary>
    /// Wywoływane przez BlacksmithInteraction gdy gracz klika [E] patrząc na tabliczkę
    /// </summary>
    public bool Interact()
    {
        if (DayNightManager.Instance == null)
        {
            Debug.LogWarning("[ShopSign] Brak DayNightManagera na scenie!");
            return false;
        }

        var mgr = DayNightManager.Instance;

        if (!isOpen)
        {
            // OTWIERAMY warsztat — jeśli dzień, czas przyspieszy do nocy
            isOpen = true;
            mgr.OpenShop();
        }
        else
        {
            // ZAMYKAMY — przerywa też fast-forward jeśli aktywny
            isOpen = false;
            mgr.CloseShop();
        }

        UpdateSignVisual();
        return true;
    }

    private void UpdateSignVisual()
    {
        string text = isOpen ? "OTWARTE" : "ZAMKNIĘTE";

        if (signText3D != null) signText3D.text = text;
        if (signTextUI != null) signTextUI.text = text;
    }
}
