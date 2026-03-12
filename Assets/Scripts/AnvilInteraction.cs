using UnityEngine;

public class AnvilInteraction : MonoBehaviour
{
    public bool HasMetal()
    {
        // Jeœli stó³ myœli, ¿e ma metal, ale rodzic metalu siê zmieni³ (czyli gracz go zabra³ do rêki)
        if (placedMetal != null && placedMetal.transform.parent != ingotPreview.parent)
        {
            placedMetal = null; // Zresetuj pamiêæ sto³u - miejsce jest znowu wolne!
        }
        return placedMetal != null;
    }
}
