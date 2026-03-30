using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Podłącz w Inspectorze:
//   Panel      — GameObject z panelem (np. ciemne tło z trzema RawImage)
//   ImgScheme  — RawImage na schemat
//   ImgWeapon  — RawImage na sylwetkę broni
//   ImgOverlay — RawImage na nakładkę
//   Label      — TextMeshProUGUI na nagłówek z wynikiem %
//   CloseButton — Button który zamyka panel
public class SilhouetteDebugUI : MonoBehaviour
{
    [Header("Referencje UI (przypisz w Inspectorze)")]
    public GameObject        Panel;
    public RawImage          ImgScheme;
    public RawImage          ImgWeapon;
    public RawImage          ImgOverlay;
    public TextMeshProUGUI   Label;
    public Button            CloseButton;

    private BlacksmithInteraction _blacksmith;

    private void Start()
    {
        _blacksmith = Object.FindFirstObjectByType<BlacksmithInteraction>();

        ForgeShapeEvaluator evaluator = Object.FindFirstObjectByType<ForgeShapeEvaluator>();
        if (evaluator != null)
            evaluator.OnDebugReady += Show;

        if (CloseButton != null)
            CloseButton.onClick.AddListener(Close);

        if (Panel != null)
            Panel.SetActive(false);
    }

    private void OnDestroy()
    {
        ForgeShapeEvaluator evaluator = Object.FindFirstObjectByType<ForgeShapeEvaluator>();
        if (evaluator != null)
            evaluator.OnDebugReady -= Show;
    }

    private void Show(Texture2D normScheme, Texture2D normWeapon, Texture2D overlay)
    {
        if (Panel == null) return;

        if (ImgScheme  != null) ImgScheme.texture  = normScheme;
        if (ImgWeapon  != null) ImgWeapon.texture  = normWeapon;
        if (ImgOverlay != null) ImgOverlay.texture = overlay;

        if (Label != null)
        {
            Color[] s = normScheme.GetPixels();
            Color[] w = normWeapon.GetPixels();
            int ideal = 0, matched = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].r > 0.5f) ideal++;
                if (s[i].r > 0.5f && w[i].r > 0.1f) matched++;
            }
            float pct = ideal > 0 ? (float)matched / ideal * 100f : 0f;
            Label.text = $"Dopasowanie: <color=#00DA33>{pct:F0}%</color>";
        }

        if (_blacksmith != null)
            _blacksmith.SetTransactionUIOpen(true);

        Panel.SetActive(true);
    }

    private void Close()
    {
        if (Panel != null)
            Panel.SetActive(false);

        if (_blacksmith != null)
            _blacksmith.SetTransactionUIOpen(false);
    }
}
