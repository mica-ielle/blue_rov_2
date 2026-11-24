using UnityEngine;
using UnityEngine.InputSystem;

// Physique sous-marine et pilotage du BlueROV2. Nord = +Z, Est = +X.
[RequireComponent(typeof(Rigidbody))]
public class PilotageROV : MonoBehaviour
{
    [Header("Masse et flottabilite")]
    public float masse = 11.5f;
    [Tooltip("Masse d'eau entrainee, moyenne des trois axes")]
    public float masseAjoutee = 10f;
    [Tooltip("Poussee d'Archimede / poids. Plus de 1 : le robot remonte seul")]
    public float flottabilite = 1.01f;
    public Vector3 centreMasse = new Vector3(0f, -0.05f, 0f);
    public Vector3 centrePoussee = new Vector3(0f, 0.02f, 0f);

    [Header("Trainee : lateral, vertical, avant")]
    public Vector3 traineeLineaire = new Vector3(6.2f, 5.2f, 4f);
    public Vector3 traineeQuadratique = new Vector3(21.7f, 37f, 18.2f);
    public float traineeRotation = 0.6f;
    public float traineeRotationQuadratique = 1.5f;

    [Header("Propulseurs")]
    public float pousseeMax = 32f;
    public float tempsReponse = 0.12f;
    [Range(0.1f, 1f)] public float gain = 0.5f;

    [Header("Asservissements")]
    public bool maintienProfondeur = true;
    public bool maintienCap = true;
    public Vector3 pidProfondeur = new Vector3(80f, 8f, 60f);
    public Vector2 pdCap = new Vector2(3f, 1.4f);

    [Header("Courant marin (m/s, Est / Nord)")]
    public bool courantActif;
    public Vector2 courant = new Vector2(0.15f, 0.08f);
    public float limiteZone = 280f;

    // Commande de test : lateral, vertical, avant, lacet. Utilisee si commandeExterne est vrai.
    [HideInInspector] public bool commandeExterne;
    [HideInInspector] public Vector4 commande;

    public float Profondeur => Niveau - transform.position.y;
    public float Cap => Mathf.Repeat(transform.eulerAngles.y, 360f);
    public float Vitesse => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float Altitude { get; private set; } = -1f;
    public float Obstacle { get; private set; } = -1f;
    private float Niveau => Mer.Instance != null ? Mer.Instance.transform.position.y : 0f;

    private const float G = 9.81f, DemiHauteur = 0.127f;
    private Rigidbody rb;
    private EquipementsROV equipements;
    private Transform[] props;
    private HeliceROV[] helices;
    private float[] etat;
    private float consigneProfondeur, consigneCap, integrale;
    private float surfaceAvant;
    private bool houlePrete;
    private Vector3 departPos;
    private Quaternion departRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        equipements = GetComponent<EquipementsROV>();
        Transform parent = transform.Find("Propulseurs");
        props = new Transform[parent.childCount];
        helices = new HeliceROV[props.Length];
        etat = new float[props.Length];
        for (int i = 0; i < props.Length; i++)
        {
            props[i] = parent.GetChild(i);
            helices[i] = props[i].GetComponentInChildren<HeliceROV>();
        }
        rb.mass = masse + masseAjoutee;
        rb.useGravity = false;          // poids et Archimede sont calcules ici
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.centerOfMass = centreMasse;
        rb.inertiaTensor = new Vector3(0.30f, 0.32f, 0.28f);
        rb.inertiaTensorRotation = Quaternion.identity;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        departPos = transform.position;
        departRot = transform.rotation;
        Verrouiller();
    }

    private void Verrouiller()
    {
        consigneProfondeur = Profondeur;
        consigneCap = transform.eulerAngles.y;
        integrale = 0f;
    }

    public void RetourDepart()
    {
        rb.position = departPos;
        rb.rotation = departRot;
        transform.SetPositionAndRotation(departPos, departRot);
        rb.linearVelocity = rb.angularVelocity = Vector3.zero;
        System.Array.Clear(etat, 0, etat.Length);
        Verrouiller();
    }

    public void RegarderProfondeur(bool actif)
    {
        maintienProfondeur = actif;
        Verrouiller();
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.hKey.wasPressedThisFrame) RegarderProfondeur(!maintienProfondeur);
        if (k.gKey.wasPressedThisFrame) { maintienCap = !maintienCap; Verrouiller(); }
        if (k.kKey.wasPressedThisFrame) courantActif = !courantActif;
        if (k.rKey.wasPressedThisFrame) RetourDepart();
        if (k.digit1Key.wasPressedThisFrame) gain = 0.25f;
        if (k.digit2Key.wasPressedThisFrame) gain = 0.5f;
        if (k.digit3Key.wasPressedThisFrame) gain = 0.75f;
        if (k.digit4Key.wasPressedThisFrame) gain = 1f;
    }

    private Vector4 Lire()
    {
        if (commandeExterne) return commande;
        Keyboard k = Keyboard.current;
        // En camera libre, le clavier deplace la camera et le robot tient sa position.
        if (k == null || (equipements != null && equipements.VueLibre)) return Vector4.zero;
        float Axe(bool plus, bool moins) => (plus ? 1f : 0f) - (moins ? 1f : 0f);
        return new Vector4(
            Axe(k.dKey.isPressed, k.aKey.isPressed),
            Axe(k.eKey.isPressed, k.qKey.isPressed),
            Axe(k.wKey.isPressed, k.sKey.isPressed),
            Axe(k.rightArrowKey.isPressed, k.leftArrowKey.isPressed));
    }

    private static float Immersion(float y, float surface, float demi) => Mathf.Clamp01((surface - y) / (2f * demi) + 0.5f);

    private void FixedUpdate()
    {
        float surface = Mer.Instance != null ? Mer.Instance.Hauteur(rb.position) : 0f;
        float immerge = Immersion(rb.position.y, surface, DemiHauteur);

        // Poids et poussee d'Archimede
        float poids = masse * G;
        rb.AddForceAtPosition(Vector3.down * poids, transform.TransformPoint(centreMasse));
        rb.AddForceAtPosition(Vector3.up * (poids * flottabilite * immerge), transform.TransformPoint(centrePoussee));

        // Trainee, relative au courant
        Vector3 eau = courantActif ? new Vector3(courant.x, 0f, courant.y) : Vector3.zero;
        // La houle entraine l'eau, de moins en moins avec la profondeur.
        if (houlePrete)
            eau.y += (surface - surfaceAvant) / Time.fixedDeltaTime * Mathf.Exp(-0.12f * Mathf.Max(0f, surface - rb.position.y));
        surfaceAvant = surface;
        houlePrete = true;
        Vector3 v = transform.InverseTransformDirection(rb.linearVelocity - eau);
        Vector3 w = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 f = Vector3.zero, c = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            f[i] = -(traineeLineaire[i] + traineeQuadratique[i] * Mathf.Abs(v[i])) * v[i];
            c[i] = -(traineeRotation + traineeRotationQuadratique * Mathf.Abs(w[i])) * w[i];
        }
        rb.AddRelativeForce(f * immerge);
        rb.AddRelativeTorque(c * Mathf.Max(0.1f, immerge));

        Commander();
        Pousser(surface);
        Mesurer();
        Bordure();
    }

    private readonly float[] voulu = new float[16];

    private void Commander()
    {
        Vector4 cmd = Lire();
        float dt = Time.fixedDeltaTime;

        // Force verticale (N)
        float fy = cmd.y * gain * 2f * pousseeMax;
        if (Mathf.Abs(cmd.y) > 0.01f)
        {
            consigneProfondeur = Profondeur;
            integrale = 0f;
        }
        else if (maintienProfondeur)
        {
            float err = Profondeur - consigneProfondeur;      // positif : trop bas
            integrale = Mathf.Clamp(integrale + err * dt, -2f, 2f);
            fy = pidProfondeur.x * err + pidProfondeur.y * integrale - pidProfondeur.z * rb.linearVelocity.y;
        }

        // Couple de lacet (N.m)
        float bras = 0.185f * 4f * pousseeMax;
        float couple = cmd.w * gain * 0.3f * bras;
        if (Mathf.Abs(cmd.w) > 0.01f) consigneCap = transform.eulerAngles.y;
        else if (maintienCap)
            couple = pdCap.x * Mathf.DeltaAngle(transform.eulerAngles.y, consigneCap) * Mathf.Deg2Rad - pdCap.y * rb.angularVelocity.y;

        // Repartition sur les propulseurs, selon leur orientation reelle
        float maxi = 1f;
        for (int i = 0; i < props.Length; i++)
        {
            Vector3 d = transform.InverseTransformDirection(props[i].forward);
            Vector3 r = transform.InverseTransformPoint(props[i].position) - centreMasse;
            float u;
            if (Mathf.Abs(d.y) > 0.7f) u = fy / (2f * pousseeMax);
            else
            {
                float lacet = r.z * d.x - r.x * d.z;
                u = cmd.z * gain * Mathf.Sign(d.z) + cmd.x * gain * Mathf.Sign(d.x) + couple / bras * Mathf.Sign(lacet);
                maxi = Mathf.Max(maxi, Mathf.Abs(u));
            }
            voulu[i] = u;
        }
        for (int i = 0; i < props.Length; i++)
        {
            bool vertical = Mathf.Abs(transform.InverseTransformDirection(props[i].forward).y) > 0.7f;
            voulu[i] = Mathf.Clamp(vertical ? voulu[i] : voulu[i] / maxi, -1f, 1f);
        }
    }

    private void Pousser(float surface)
    {
        float lissage = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.01f, tempsReponse));
        for (int i = 0; i < props.Length; i++)
        {
            etat[i] = Mathf.Lerp(etat[i], voulu[i], lissage);
            if (helices[i] != null) helices[i].commande = etat[i];
            // Une helice hors de l'eau ne pousse plus.
            float n = Immersion(props[i].position.y, surface, 0.05f);
            Vector3 p = transform.InverseTransformPoint(props[i].position);
            p.y = centreMasse.y;
            rb.AddForceAtPosition(props[i].forward * (etat[i] * pousseeMax * n), transform.TransformPoint(p));
        }
    }

    private void Mesurer()
    {
        Altitude = Distance(Vector3.down, 60f);
        Obstacle = Distance(transform.forward, 30f);
    }

    private float Distance(Vector3 dir, float max)
    {
        float mieux = -1f;
        foreach (RaycastHit h in Physics.RaycastAll(transform.position, dir, max, ~0, QueryTriggerInteraction.Ignore))
            if (h.rigidbody != rb && (mieux < 0f || h.distance < mieux)) mieux = h.distance;
        return mieux;
    }

    private void Bordure()
    {
        Vector3 p = rb.position;
        var q = new Vector3(Mathf.Clamp(p.x, -limiteZone, limiteZone), p.y, Mathf.Clamp(p.z, -limiteZone, limiteZone));
        if (q != p) rb.AddForce((q - p) * 60f);
    }

    private static string Metres(float d) => d < 0f ? "--" : d.ToString("F1") + " m";

    private void OnGUI()
    {
        if (equipements != null && equipements.VueLibre) return;
        GUI.Box(new Rect(10, 10, 330, 92), GUIContent.none);
        GUI.Label(new Rect(20, 14, 310, 84),
            $"<b>Profondeur {Profondeur:F2} m    Cap {Cap:000}    Vitesse {Vitesse:F2} m/s</b>" +
            $"\nFond : {Metres(Altitude)}    Devant : {Metres(Obstacle)}    Gain : {gain * 100f:F0} % (1-4)" +
            $"\nProfondeur tenue : {(maintienProfondeur ? consigneProfondeur.ToString("F2") + " m" : "non")} (H)    Cap tenu : {(maintienCap ? "oui" : "non")} (G)" +
            $"\nCourant : {(courantActif ? courant.magnitude.ToString("F2") + " m/s" : "non")} (K)    Retour depart (R)");
        GUI.Box(new Rect(10, Screen.height - 54, 520, 44), GUIContent.none);
        GUI.Label(new Rect(20, Screen.height - 51, 500, 40),
            "Z/S : avant / arriere    Q/D : gauche / droite    <- / -> : tourner" +
            "\nE/A : monter / descendre    C : vue    L : phares");
    }
}
