using UnityEngine;
using UnityEngine.UI;

public class WheelController : MonoBehaviour
{
    [Header("Koła Statystyk")]
    public Image damageImage;
    public Image speedImage;
    public Image aoeImage;

    [Header("Ikony Statystyk")]
    public Image damageIcon;
    public Image speedIcon;
    public Image aoeIcon;

    public GameObject wheelContainer;
    
    [Header("Ustawienia Ikon")]
    public float iconRadius = 120f; // Odległość ikon od środka koła (zmień w Inspektorze)

    // Tę metodę możesz wywołać z dowolnego miejsca, podając wartości w procentach (np. 30, 50, 20)

    void Awake()
    {
        wheelContainer.SetActive(false);
    }

    public void SetWheel(bool isOn)
    {
        wheelContainer.SetActive(isOn);
    }
    
    public void UpdateWheel(float damagePct, float speedPct, float aoePct)
    {
        // Każdy stat zajmuje stały wycinek 1/3 koła (120°).
        // Wypełniony kolor jest wycentrowany w tej 1/3 — czarne tło
        // widoczne symetrycznie po obu stronach kolorowego segmentu.
        const float segment = 1f / 3f;

        damageImage.fillAmount = (damagePct / 100f) * segment;
        speedImage.fillAmount  = (speedPct  / 100f) * segment;
        aoeImage.fillAmount    = (aoePct    / 100f) * segment;

        // Środki poszczególnych 1/3 (zgodnie z zegarem od góry): 60°, 180°, 300°
        // Segment zaczyna się w: środek - połowa wypełnionego kąta
        float damageHalf = (damagePct / 100f) * 60f;
        float speedHalf  = (speedPct  / 100f) * 60f;
        float aoeHalf    = (aoePct    / 100f) * 60f;

        damageImage.rectTransform.localEulerAngles = new Vector3(0, 0, -(60f  - damageHalf));
        speedImage.rectTransform.localEulerAngles  = new Vector3(0, 0, -(180f - speedHalf));
        aoeImage.rectTransform.localEulerAngles    = new Vector3(0, 0, -(300f - aoeHalf));

        // Ikony zawsze w centrum swojego segmentu (60°, 180°, 300°)
        PositionIcon(damageIcon, 60f);
        PositionIcon(speedIcon,  180f);
        PositionIcon(aoeIcon,    300f);
    }

    // Funkcja pomocnicza przesuwająca ikonę
    private void PositionIcon(Image icon, float angleInDegrees)
    {
        if (icon == null) return;

        // Unity w funkcjach matematycznych wymaga radianów, nie stopni
        float angleRad = angleInDegrees * Mathf.Deg2Rad;

        // Używamy Sinusa (dla osi X) i Cosinusa (dla osi Y).
        // W Unity godzina 12:00 to kierunek (0, 1), a obrót jest zgodny z zegarem.
        float x = Mathf.Sin(angleRad) * iconRadius;
        float y = Mathf.Cos(angleRad) * iconRadius;

        // Ustawienie pozycji ikony
        icon.rectTransform.anchoredPosition = new Vector2(x, y);
    }
}