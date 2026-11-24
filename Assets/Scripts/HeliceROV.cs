using UnityEngine;

// Rotation visible d'une helice. La commande (-1 a 1) sera donnee par le pilotage du lot 4.
public class HeliceROV : MonoBehaviour
{
    [Range(-1f, 1f)] public float commande;
    public float toursParMinuteMax = 900f;

    private void Update()
    {
        transform.Rotate(0f, 0f, commande * toursParMinuteMax * 6f * Time.deltaTime, Space.Self);
    }
}
