using UnityEngine;
using System;

public class DayNightManager : MonoBehaviour
{
    // Singleton żeby by łatwo dostępny z innych skryptów powołujących NPC!
    public static DayNightManager Instance { get; private set; }

    [Header("Ustawienia Czasu")]
    [Tooltip("Obecny czas w grze (w formacie 0 - 24 godzin)")]
    [Range(0, 24)] public float currentTime = 8f; // Zaczynamy o 8:00 rano
    
    [Tooltip("Ile minut w grze mija podczas 1 sekundy prawdziwego czasu? (10f = bardzo szybko)")]
    public float timeMultipler = 10f; 

    [Header("Oświetlenie (Słońce / Księżyc)")]
    public Light directionalLight;
    public AnimationCurve lightIntensityCurve; 
    public Gradient lightColorGradient;

    [Header("Stany i Wydarzenia")]
    public bool isDay = true;
    
    // Użyjemy tych Eventów w przyszłości, by skrypty od NPC same wiedziały, kiedy spawać
    public event Action OnDayStarted;    
    public event Action OnNightStarted;  

    // Ramy godzinowe dla dnia i nocy
    private const float startOfDayHour = 6f;
    private const float startOfNightHour = 18f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        UpdateTime();
        UpdateLighting();
        CheckTimeEvents();
    }

    private void UpdateTime()
    {
        // deltaTime to ułamek sekundy. Mnożymy przez mnożnik czasu i konwertujemy ze stopni (minut) na format 24h
        currentTime += (Time.deltaTime * timeMultipler) / 60f; 

        if (currentTime >= 24f)
        {
            currentTime %= 24f; // Wyzerowanie przy północy
        }
    }

    private void UpdateLighting()
    {
        if (directionalLight == null) return;

        // "t" to procentowa wartość doby (od 0 do 1)
        float t = currentTime / 24f; 

        // Fizyczny obrót słońca po osi. 
        // Mnożymy t * 360, bo tyle ma pełny obrót Ziemi, a minusujemy by o 00:00 był równo pod mapą
        float sunXRotation = (t * 360f) - 90f; 
        directionalLight.transform.localRotation = Quaternion.Euler(sunXRotation, 170f, 0f);

        // Nakładanie koloru i intensywności ze zdefiniowanych kluczy z Inspektora!
        directionalLight.intensity = lightIntensityCurve.Evaluate(t);
        directionalLight.color = lightColorGradient.Evaluate(t);
    }

    private void CheckTimeEvents()
    {
        // Prawda jeśli czas jest między 6:00 rano a 18:00
        bool isCurrentlyDay = (currentTime >= startOfDayHour && currentTime < startOfNightHour);

        if (isCurrentlyDay && !isDay)
        {
            isDay = true;
            Debug.Log("🌞 Nastał Nowy Dzień! Tablica zadań odświeżona.");
            OnDayStarted?.Invoke();
        }
        else if (!isCurrentlyDay && isDay)
        {
            isDay = false;
            Debug.Log("🌙 Nastała Noc! Czas na odpoczynek przy piecu.");
            OnNightStarted?.Invoke();
        }
    }

    // Ta metoda uruchamia się automatycznie w Edytorze, gdy dodasz ten skrypt do jakiegoś obiektu.
    // Dzięki temu ustawi fajne domyślne kolory i krzywe od razu!
    private void Reset()
    {
        lightIntensityCurve = new AnimationCurve();
        lightIntensityCurve.AddKey(0f, 0.1f);    // 00:00 Noc - tylko blask
        lightIntensityCurve.AddKey(0.20f, 0.1f); // 4:00 Rano
        lightIntensityCurve.AddKey(0.25f, 0.5f); // 6:00 Wschód
        lightIntensityCurve.AddKey(0.5f, 1.2f);  // 12:00 Północ (Najjaśniej)
        lightIntensityCurve.AddKey(0.75f, 0.5f); // 18:00 Zachód
        lightIntensityCurve.AddKey(0.80f, 0.1f); // 19:30 Wieczór
        lightIntensityCurve.AddKey(1f, 0.1f);    // 00:00 Noc
        
        // Brak wymogu wygładzania AnimationUtility na rzecz kompatybilności buildów
        lightColorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0f);    // Niebieskawa Noc
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.4f, 0.2f), 0.25f);   // Pomarańczowy Wschód
        colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f);      // Ciepłe słoneczne południe
        colorKeys[3] = new GradientColorKey(new Color(1f, 0.4f, 0.2f), 0.75f);   // Miedziany Zachód
        colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1f);    // Niebieskawa Noc
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        
        lightColorGradient.SetKeys(colorKeys, alphaKeys);
    }
}
