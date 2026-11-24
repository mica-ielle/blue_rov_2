using UnityEngine;

// Fait tourner le rotor autour de son axe Z local.
public class RotorEolienne : MonoBehaviour
{
    public float toursParMinute = 10f;

    private void Update()
    {
        transform.Rotate(0f, 0f, toursParMinute * 6f * Time.deltaTime, Space.Self);
    }
}
