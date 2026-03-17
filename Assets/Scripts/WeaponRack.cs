using UnityEngine;

public class WeaponRack : MonoBehaviour
{
    [Header("Punkt zawieszenia (Twój Anchor Point)")]
    public Transform weaponSlot;

    private FinishedObject storedWeapon;

    // Sprawdza, czy slot na stojaku jest pusty
    public bool IsEmpty()
    {
        return storedWeapon == null;
    }

    // Odkładanie broni na stojak
    public void PlaceWeapon(FinishedObject weapon)
    {
        storedWeapon = weapon;
        weapon.transform.SetParent(weaponSlot);

        // 1. Wyłączamy grawitację
        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 2. Kopiujemy idealny obrót z Twojego Anchora na stojaku (to Twoje X: 90)
        weapon.transform.rotation = weaponSlot.rotation;

        // 3. SZUKAMY RĄCZKI (Twój pomysł!)
        // Skrypt sam przeszukuje całą wykutą broń w poszukiwaniu punktu o nazwie "HangPoint"
        Transform targetHangPoint = null;
        Transform[] allChildren = weapon.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == "HangPoint") // Pamiętaj: wielkość liter ma znaczenie!
            {
                targetHangPoint = child;
                break;
            }
        }

        // 4. Ostateczne pozycjonowanie
        if (targetHangPoint != null)
        {
            // Jeśli znaleźliśmy HangPoint w rączce, obliczamy różnicę i dociągamy go do stojaka
            Vector3 offset = weaponSlot.position - targetHangPoint.position;
            weapon.transform.position += offset;
        }
        else
        {
            // Zapasowe wyjście, jeśli jakaś broń nie ma dodanego HangPointa
            weapon.transform.localPosition = Vector3.zero;
        }
    }

    // Opcjonalnie: zabieranie broni ze stojaka (żebyś miał kompletny system)
    public FinishedObject TakeWeapon()
    {
        FinishedObject weaponToReturn = storedWeapon;
        storedWeapon = null;
        return weaponToReturn;
    }
}