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

        // Jeśli broń ma ustawiony swój uniwersalny punkt zawieszenia:
        if (weapon.hangPoint != null)
        {
            // 1. Wyrównujemy kąty (Rotację)
            Quaternion rotDiff = weaponSlot.rotation * Quaternion.Inverse(weapon.hangPoint.rotation);
            weapon.transform.rotation = rotDiff * weapon.transform.rotation;

            // 2. Wyrównujemy pozycję (przemieszczamy broń tak, by hangPoint trafił w Anchor)
            Vector3 posDiff = weaponSlot.position - weapon.hangPoint.position;
            weapon.transform.position += posDiff;
        }
        else
        {
            // Stary kod zapasowy (gdybyś zapomniał dodać punktu do jakiejś broni)
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        weapon.transform.localScale = Vector3.one;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
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