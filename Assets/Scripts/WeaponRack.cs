using UnityEngine;

public class WeaponRack : MonoBehaviour
{
    [Header("Punkt zawieszenia (Twój Anchor Point)")]
    public Transform weaponSlot;

    private FinishedObject storedWeapon;

    public bool IsEmpty()
    {
        return storedWeapon == null;
    }

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

        // 2. Kopiujemy idealny obrót z Twojego Anchora na stojaku
        weapon.transform.rotation = weaponSlot.rotation;

        // 3. SZUKAMY TWOJEGO NOWEGO SKRYPTU
        HangPoint targetHangPoint = weapon.GetComponentInChildren<HangPoint>();

        if (targetHangPoint != null)
        {
            // 4. Jeśli w skrypcie zaznaczyłeś odwrócenie, obracamy broń o 180 stopni
            if (targetHangPoint.reverseOnRack)
            {
                weapon.transform.Rotate(0, 180, 0, Space.Self);
            }

            // 5. Ostateczne pozycjonowanie - dociągamy pozycję tego skryptu do stojaka
            Vector3 offset = weaponSlot.position - targetHangPoint.transform.position;
            weapon.transform.position += offset;
        }
        else
        {
            // Zapasowe wyjście, jeśli jakaś broń nie ma dodanego skryptu HangPoint
            weapon.transform.localPosition = Vector3.zero;
        }
    }

    public FinishedObject TakeWeapon()
    {
        FinishedObject weaponToReturn = storedWeapon;
        storedWeapon = null;
        return weaponToReturn;
    }
}