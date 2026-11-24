using UnityEngine;

// Change brouillard, lumiere et fond quand la camera passe sous la surface.
[RequireComponent(typeof(Camera))]
public class AmbianceSousMarine : MonoBehaviour
{
    public Light soleil;
    public ParticleSystem particules;

    [Header("Air")]
    public Color brumeAir = new Color(0.67f, 0.77f, 0.86f);
    public float densiteAir = 0.0012f;
    public Color ambianteAir = new Color(0.50f, 0.56f, 0.63f);
    public float intensiteSoleil = 1.4f;

    [Header("Eau")]
    public Color eauSurface = new Color(0.04f, 0.34f, 0.43f);
    public Color eauProfonde = new Color(0.008f, 0.075f, 0.13f);
    public float densiteEau = 0.032f;
    public float profondeurSombre = 45f;

    public float Profondeur { get; private set; }
    public bool SousEau { get; private set; }

    private Camera cam;

    private void LateUpdate() => Appliquer();

    public void Appliquer()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Mer mer = Mer.Instance;
        Vector3 p = transform.position;
        Profondeur = (mer != null ? mer.Hauteur(p) : 0f) - p.y;
        SousEau = Profondeur > 0f;

        float t = Mathf.Clamp01(Profondeur / Mathf.Max(1f, profondeurSombre));
        Color eau = Color.Lerp(eauSurface, eauProfonde, Mathf.Sqrt(t));

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = SousEau ? eau : brumeAir;
        RenderSettings.fogDensity = SousEau ? densiteEau : densiteAir;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = SousEau ? eau * 1.1f + new Color(0.03f, 0.05f, 0.06f) : ambianteAir;

        cam.clearFlags = SousEau ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        cam.backgroundColor = eau;
        if (soleil != null)
            soleil.intensity = SousEau ? intensiteSoleil * Mathf.Lerp(0.85f, 0.2f, t) : intensiteSoleil;

        if (particules != null && Application.isPlaying)
        {
            var emission = particules.emission;
            emission.enabled = SousEau;
            if (!SousEau) particules.Clear();
        }
    }
}
