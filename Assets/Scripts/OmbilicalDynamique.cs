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
    public Rigidbody rov;
    public float longueurMax = 25f;
    public float raideur = 22f;
    public float amortissement = 5f;

    private const int EchantillonsParTroncon = 10;
    private LineRenderer ligne;

    private void Awake()
    {
        ligne = GetComponent<LineRenderer>();
        ligne.useWorldSpace = true;
        ligne.numCornerVertices = 6;
        ligne.numCapVertices = 5;
        ligne.alignment = LineAlignment.View;
        ligne.shadowCastingMode = ShadowCastingMode.On;
        ligne.receiveShadows = true;
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
        float longueur = Vector3.Distance(depart.position, guideRampe.position)
                       + Vector3.Distance(guideRampe.position, guidePont.position)
                       + Vector3.Distance(guidePont.position, ancrageTreuil.position);
        float extension = longueur - longueurMax;
        if (extension <= 0f) return;

        Vector3 versTreuil = (guideRampe.position - depart.position).normalized;
        float traction = Mathf.Clamp(raideur * extension - amortissement * Vector3.Dot(rov.linearVelocity, versTreuil), 0f, 180f);
        rov.AddForceAtPosition(versTreuil * traction, depart.position, ForceMode.Force);
    }
}
