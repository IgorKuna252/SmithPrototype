using UnityEngine;

public class PlayerUIScript : MonoBehaviour
{
    [Header("Referencje")]
    public Transform docelowyRodzicUI;
    public GameObject tloSchematu;

    private GameObject ostatnioSklonowanySchemat;
    

    public void SkopiujSchemat()
    {

        // 1. Szukamy oryginalnego schematu
        ForgeShapeEvaluator evaluator = Object.FindFirstObjectByType<ForgeShapeEvaluator>();

        if (evaluator == null)
        {
            return;
        }

        if (evaluator.uiShapeObject == null)
        {
            return;
        }

        if (docelowyRodzicUI == null)
        {
            return;
        }


        // 2. USUWANIE STAREGO SCHEMATU
        if (ostatnioSklonowanySchemat != null)
        {
            Destroy(ostatnioSklonowanySchemat);
        }

        // 3. KLONOWANIE NOWEGO SCHEMATU
        ostatnioSklonowanySchemat = Instantiate(evaluator.uiShapeObject, docelowyRodzicUI, false);
        ostatnioSklonowanySchemat.name = "OstatniSchematGracza";
        
        ostatnioSklonowanySchemat.SetActive(true);
        if (tloSchematu != null) tloSchematu.SetActive(true);
        
        // 4. CENTROWANIE w UI gracza
        RectTransform rectTransform = ostatnioSklonowanySchemat.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}