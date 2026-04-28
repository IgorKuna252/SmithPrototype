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
    public static SilhouetteDebugUI Instance { get; private set; }

    [Header("Referencje UI (przypisz w Inspectorze)")]
    public GameObject        Panel;
    public RawImage          ImgScheme;
    public RawImage          ImgWeapon;
    public RawImage          ImgOverlay;
    public TextMeshProUGUI   Label;
    public Button            CloseButton;

    [Header("Napisy pod obrazkami (opcjonalne)")]
    public TextMeshProUGUI   LabelScheme;
    public TextMeshProUGUI   LabelWeapon;
    public TextMeshProUGUI   LabelOverlay;

    private BlacksmithInteraction _blacksmith;
    private System.Action _onCloseCallback;

    private void Awake()
    {
        Instance = this;
    }

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
                bool inScheme = Mathf.Max(s[i].r, s[i].g, s[i].b) > 0.05f;
                bool inWeapon = Mathf.Max(w[i].r, w[i].g, w[i].b) > 0.05f;
                if (inScheme) ideal++;
                if (inScheme && inWeapon) matched++;
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
        PlayerUIScript playerUI = FindFirstObjectByType<PlayerUIScript>();
        playerUI?.ClearScheme();
        
        if (Panel != null)
            Panel.SetActive(false);

        if (_blacksmith != null)
            _blacksmith.SetTransactionUIOpen(false);

        _onCloseCallback?.Invoke();
        _onCloseCallback = null;
    }

    public void ShowTransaction(float matchPercentage, string rewardMaterial, int rewardAmount, bool noScheme, System.Action onCloseCallback)
    {
        _onCloseCallback = onCloseCallback;

        bool hideImages = noScheme;
        if (ImgScheme   != null) ImgScheme.gameObject.SetActive(!hideImages);
        if (ImgWeapon   != null) ImgWeapon.gameObject.SetActive(!hideImages);
        if (ImgOverlay  != null) ImgOverlay.gameObject.SetActive(!hideImages);
        if (LabelScheme != null) LabelScheme.gameObject.SetActive(!hideImages);
        if (LabelWeapon != null) LabelWeapon.gameObject.SetActive(!hideImages);
        if (LabelOverlay!= null) LabelOverlay.gameObject.SetActive(!hideImages);

        if (Label != null)
        {
            string matchLine = noScheme
                ? "<color=#00DA33>Każda broń jest dobra!</color>"
                : $"Dopasowanie: <color=#00DA33>{matchPercentage:F0}%</color>";

            Label.text = $"{matchLine}\n" +
                         $"Otrzymujesz: <color=#FFD700>{rewardMaterial} x{rewardAmount}</color>";
        }

        if (_blacksmith != null)
            _blacksmith.SetTransactionUIOpen(true);

        if (Panel != null)
            Panel.SetActive(true);
    }
}
