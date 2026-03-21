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

    public void SetWheel(bool isOn)
    {
        wheelContainer.SetActive(isOn);
    }
    
    public void UpdateWheel(float damagePct, float speedPct, float aoePct)
    {
        // 1. Zabezpieczenie i normalizacja
        float total = damagePct + speedPct + aoePct;

        if (total <= 0)
        {
            Debug.LogWarning("Suma procentów musi być większa od zera!");
            return;
        }

        float damageFill = damagePct / total;
        float speedFill = speedPct / total;
        float aoeFill = aoePct / total;

        // 2. Ustawienie wielkości kawałków koła
        damageImage.fillAmount = damageFill;
        speedImage.fillAmount = speedFill;
        aoeImage.fillAmount = aoeFill;

        // 3. Obroty kół (Czerwony na 12:00)
        damageImage.rectTransform.localEulerAngles = Vector3.zero;
        speedImage.rectTransform.localEulerAngles = new Vector3(0, 0, -damageFill * 360f);
        aoeImage.rectTransform.localEulerAngles = new Vector3(0, 0, -(damageFill + speedFill) * 360f);

        // 4. Obliczenie kątów dla środków każdego "kawałka"
        // Mnożymy przez 360, aby uzyskać stopnie. Dzielimy przez 2, aby trafić w sam środek kawałka.
        float damageIconAngle = (damageFill * 360f) / 2f;
        float speedIconAngle = (damageFill * 360f) + ((speedFill * 360f) / 2f);
        float aoeIconAngle = ((damageFill + speedFill) * 360f) + ((aoeFill * 360f) / 2f);

        // 5. Ustawienie pozycji ikon
        PositionIcon(damageIcon, damageIconAngle);
        PositionIcon(speedIcon, speedIconAngle);
        PositionIcon(aoeIcon, aoeIconAngle);
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