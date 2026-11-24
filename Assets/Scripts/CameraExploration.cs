using UnityEngine;
using UnityEngine.InputSystem;

// Camera libre. Les touches sont lues par position physique : ZQSD sur un clavier AZERTY.
public class CameraExploration : MonoBehaviour
{
    public float vitesse = 6f;
    public float acceleration = 4f;
    public float sensibilite = 0.12f;
    public float altitudeMax = 60f;
    public float margeFond = 1f;

    private float lacet, tangage;
    private AmbianceSousMarine ambiance;
    private Collider fond;

    private void Start()
    {
        ambiance = GetComponent<AmbianceSousMarine>();
        GameObject f = GameObject.Find("FondMarin");
        if (f != null) fond = f.GetComponent<Collider>();
        Recaler();
    }

    // Reprend l'orientation actuelle de la camera.
    public void Recaler()
    {
        Vector3 e = transform.eulerAngles;
        lacet = e.y;
        tangage = Mathf.DeltaAngle(0f, e.x);
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;

        if (m != null && m.rightButton.isPressed)
        {
            Vector2 d = m.delta.ReadValue() * sensibilite;
            lacet += d.x;
            tangage = Mathf.Clamp(tangage - d.y, -89f, 89f);
        }
        transform.rotation = Quaternion.Euler(tangage, lacet, 0f);
        if (k == null) return;

        Vector3 dir = Vector3.zero;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) dir += transform.forward;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) dir -= transform.forward;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) dir += transform.right;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) dir -= transform.right;
        if (k.eKey.isPressed) dir += Vector3.up;
        if (k.qKey.isPressed) dir -= Vector3.up;

        float v = vitesse * (k.leftShiftKey.isPressed ? acceleration : 1f);
        Vector3 p = transform.position + Vector3.ClampMagnitude(dir, 1f) * (v * Time.deltaTime);
        p.y = Mathf.Min(p.y, altitudeMax);
        // Ne pas traverser le fond.
        if (fond != null && fond.Raycast(new Ray(new Vector3(p.x, 100f, p.z), Vector3.down), out RaycastHit sol, 400f))
            p.y = Mathf.Max(p.y, sol.point.y + margeFond);
        transform.position = p;
    }

    private void OnGUI()
    {
        bool eau = ambiance != null && ambiance.SousEau;
        string etat = ambiance == null ? "" :
            eau ? $"Profondeur : {ambiance.Profondeur:F1} m" : $"Au-dessus de l'eau : {-ambiance.Profondeur:F1} m";
        GUI.Box(new Rect(10, 10, 330, 92), GUIContent.none);
        GUI.Label(new Rect(20, 15, 310, 84),
            "<b>BLUE ROV - exploration</b>\n" + etat +
            "\nZQSD ou fleches : avancer    E / A : monter / descendre" +
            "\nClic droit + souris : regarder    Maj : vite");
    }
}
