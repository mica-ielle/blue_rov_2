using UnityEngine;
using UnityEngine.Rendering;

// Câble visuel attaché au ROV et au treuil, avec mise en tension à sa longueur de déploiement.
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public sealed class OmbilicalDynamique : MonoBehaviour
{
    public Transform depart;
    public Transform guideRampe;
    public Transform guidePont;
    public Transform ancrageTreuil;
    public Transform tambour;
    public Rigidbody rov;
    [Min(15f)] public float longueurTotale = 300f;
    [Min(15f)] public float longueurMax = 24f;
    [Min(0.1f)] public float vitesseRembobinage = 3f;
    [Min(0.05f)] public float rayonTambour = 0.36f;
    public float raideur = 22f;
    public float amortissement = 5f;

    private const int EchantillonsParTroncon = 10;
    private LineRenderer ligne;
    private float longueurInitiale;
    private float longueurUtilisee;
    private bool rappelActif;
    private Quaternion rotationTambourInitiale;

    public bool RappelActif => rappelActif;
    public float LongueurUtilisee => longueurUtilisee;
    private bool tambourInitialise;

    private void Awake()
    {
        ligne = GetComponent<LineRenderer>();
        ligne.useWorldSpace = true;
        ligne.numCornerVertices = 6;
        ligne.numCapVertices = 5;
        ligne.alignment = LineAlignment.View;
        ligne.shadowCastingMode = ShadowCastingMode.On;
        ligne.receiveShadows = true;
        longueurInitiale = longueurMax;
    }

    private void Start()
    {
        longueurInitiale = longueurMax;
        CapturerRotationTambour();
    }

    private void CapturerRotationTambour()
    {
        if (tambour == null || tambourInitialise) return;
        rotationTambourInitiale = tambour.localRotation;
        tambourInitialise = true;
    }

    public void BasculerRappel()
    {
        rappelActif = !rappelActif;
        if (rappelActif) longueurMax = Mathf.Min(longueurMax, CalculerLongueur() + 0.35f);
    }

    public void Reinitialiser()
    {
        rappelActif = false;
        longueurMax = longueurInitiale;
        CapturerRotationTambour();
        if (tambour != null && tambourInitialise) tambour.localRotation = rotationTambourInitiale;
    }

    private float CalculerLongueur()
    {
        if (depart == null || guideRampe == null || guidePont == null || ancrageTreuil == null) return 0f;
        return Vector3.Distance(depart.position, guideRampe.position)
             + Vector3.Distance(guideRampe.position, guidePont.position)
             + Vector3.Distance(guidePont.position, ancrageTreuil.position);
    }

    private void LateUpdate()
    {
        if (depart == null || guideRampe == null || guidePont == null || ancrageTreuil == null) return;
        Transform[] noeuds = { depart, guideRampe, guidePont, ancrageTreuil };
        ligne.positionCount = 1 + (noeuds.Length - 1) * EchantillonsParTroncon;
        int index = 0;
        for (int segment = 0; segment < noeuds.Length - 1; segment++)
        {
            Vector3 a = noeuds[segment].position;
            Vector3 b = noeuds[segment + 1].position;
            float mou = Mathf.Min(0.32f, Vector3.Distance(a, b) * 0.035f);
            int departEchantillon = segment == 0 ? 0 : 1;
            for (int i = departEchantillon; i <= EchantillonsParTroncon; i++)
            {
                float t = i / (float)EchantillonsParTroncon;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (4f * mou * t * (1f - t));
                ligne.SetPosition(index++, p);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying) return;
        if (rov == null || depart == null || guideRampe == null || guidePont == null || ancrageTreuil == null) return;
        float longueur = CalculerLongueur();
        longueurUtilisee = longueur;
        float longueurMin = Vector3.Distance(guideRampe.position, guidePont.position)
                          + Vector3.Distance(guidePont.position, ancrageTreuil.position) + 0.3f;

        if (rappelActif)
        {
            float rembobinee = Mathf.Min(vitesseRembobinage * Time.fixedDeltaTime, longueurMax - longueurMin);
            longueurMax = Mathf.Max(longueurMin, longueurMax - rembobinee);
            if (tambour != null && rembobinee > 0f)
            {
                CapturerRotationTambour();
                float degres = rembobinee / (2f * Mathf.PI * rayonTambour) * 360f;
                tambour.localRotation *= Quaternion.AngleAxis(degres, Vector3.up);
            }
            if (longueurMax <= longueurMin + 0.001f) rappelActif = false;
        }
        else if (longueur < longueurTotale)
        {
            // Le treuil laisse sortir du câble automatiquement quand le ROV s'éloigne.
            longueurMax = Mathf.Min(longueurTotale, Mathf.Max(longueurMax, longueur + 0.75f));
        }

        float extension = longueur - longueurMax;
        if (extension <= 0f) return;

        Vector3 versTreuil = (guideRampe.position - depart.position).normalized;
        float traction = Mathf.Clamp(raideur * extension - amortissement * Vector3.Dot(rov.linearVelocity, versTreuil), 0f, 180f);
        rov.AddForceAtPosition(versTreuil * traction, depart.position, ForceMode.Force);
    }
}
