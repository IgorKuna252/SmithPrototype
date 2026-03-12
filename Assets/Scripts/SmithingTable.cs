using UnityEngine;

public class MergingTable : MonoBehaviour
{
    [Header("Miejsca na stole")]
    public Transform ingotPreview;
    public Transform handlePreview;
    public Transform leatherPreview;

    [Header("Kamery")]
    public GameObject assemblyCamera; // Przeci¹gnij tu swoj¹ now¹ kamerê AssemblyCamera
    private GameObject mainPlayerCamera; // Stó³ zapamiêta kamerê gracza
    private bool isAssemblyMode = false;

    public MonoBehaviour[] scriptsToDisable;

    private IronPiece placedMetal;
    private WoodPiece placedWood;
    private LeatherPiece placedLeather;

    void Update()
    {
        // Jeœli jesteœmy w trybie sk³adania i gracz wciœnie "E" (lub Escape), wracamy do normalnego widoku
        if (isAssemblyMode && Input.GetKeyDown(KeyCode.E))
        {
            ExitAssemblyMode();
        }
    }

    // NOWA FUNKCJA: W³¹czanie trybu sk³adania
    public void ToggleAssemblyCamera(GameObject playerCam)
    {
        if (isAssemblyMode) return; // Zabezpieczenie, ¿eby nie w³¹czyæ podwójnie

        mainPlayerCamera = playerCam; // Zapamiêtujemy kamerê gracza

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
        // Prze³¹czamy kamery
        mainPlayerCamera.SetActive(false);
        assemblyCamera.SetActive(true);
        isAssemblyMode = true;

        // Tutaj opcjonalnie mo¿esz zablokowaæ poruszanie siê gracza, jeœli masz do tego skrypt

        Debug.Log("Prze³¹czono na widok sto³u! Wciœnij E, aby wyjœæ.");
    }

    // NOWA FUNKCJA: Wy³¹czanie trybu sk³adania
    public void ExitAssemblyMode()
    {
        // Przywracamy kamery
        mainPlayerCamera.SetActive(true);
        assemblyCamera.SetActive(false);
        isAssemblyMode = false;

        // --- 1. SCHOWAJ I ZABLOKUJ KURSOR MYSZY ---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // --- 2. W£¥CZ CHODZENIE I ROZGL¥DANIE ---
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        Debug.Log("Wrócono do widoku z oczu gracza.");
    }

    // INTELIGENTNE SPRAWDZANIE:
    public bool HasMetal()
    {
        // Jeœli stó³ myœli, ¿e ma metal, ale rodzic metalu siê zmieni³ (czyli gracz go zabra³ do rêki)
        if (placedMetal != null && placedMetal.transform.parent != ingotPreview.parent)
        {
            placedMetal = null; // Zresetuj pamiêæ sto³u - miejsce jest znowu wolne!
        }
        return placedMetal != null;
    }

    public bool HasWood()
    {
        // To samo dla drewna
        if (placedWood != null && placedWood.transform.parent != handlePreview.parent)
        {
            placedWood = null; // Gracz zabra³ drewno, zwalniamy miejsce
        }
        return placedWood != null;
    }

    public bool HasLeather()
    {
        // To samo dla drewna
        if (placedLeather != null && placedLeather.transform.parent != handlePreview.parent)
        {
            placedLeather = null; // Gracz zabra³ skóre, zwalniamy miejsce
        }
        return placedLeather != null;
    }

    public void PlaceMetal(IronPiece metal)
    {
        metal.transform.position = ingotPreview.position;
        metal.transform.rotation = ingotPreview.rotation;
        metal.transform.SetParent(ingotPreview.parent, true);

        Rigidbody rb = metal.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        placedMetal = metal;
        Debug.Log("Sztabka po³o¿ona na stole!");
    }

    public void PlaceWood(WoodPiece wood)
    {
        wood.transform.position = handlePreview.position;
        wood.transform.rotation = handlePreview.rotation;
        wood.transform.SetParent(handlePreview.parent, true);

        Rigidbody rb = wood.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        placedWood = wood;
        Debug.Log("Drewno po³o¿one na stole!");
    }

    public void PlaceLeather(LeatherPiece leather)
    {
        leather.transform.position = leatherPreview.position;
        leather.transform.rotation = leatherPreview.rotation;
        leather.transform.SetParent(leatherPreview.parent, true);

        Rigidbody rb = leather.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        placedLeather = leather;
        Debug.Log("Skóra po³o¿ona na stole!");
    }
}