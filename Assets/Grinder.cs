using UnityEngine;

public class Grinder : MonoBehaviour
{
    [Header("Ustawienia Szlifierki")]
    public float grindRate = 0.05f; // Zmniejszy³em trochê czas dla p³ynniejszego szlifowania
    private float nextGrindTime = 0f;
    private Collider grindCollider;

    void Start()
    {
        // Pobieramy collider szlifierki, ¿eby móc na nim robiæ obliczenia
        grindCollider = GetComponent<Collider>();
    }

    // OnTriggerStay odpala siê nawet wtedy, gdy trzymamy obiekt (isKinematic = true)
    void OnTriggerStay(Collider other)
    {
        if (Time.time >= nextGrindTime)
        {
            IronPiece iron = other.GetComponent<IronPiece>();

            if (iron != null)
            {
                // MAGICZNA ZMIANA: Teraz szukamy punktu na SZTABCE (other), 
                // który jest najbli¿ej œrodka KAMIENIA (transform.position). 
                // To daje nam 100% precyzyjny punkt styku ostrza!
                Vector3 contactPoint = other.ClosestPoint(transform.position);

                iron.SharpenEdge(contactPoint);
                nextGrindTime = Time.time + grindRate;
            }
        }
    }
}