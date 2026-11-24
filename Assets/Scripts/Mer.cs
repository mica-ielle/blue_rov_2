using UnityEngine;

// Houle de Gerstner : le meme calcul est fait ici (physique) et dans Ocean.shader (image).
[ExecuteAlways]
public class Mer : MonoBehaviour
{
    public static Mer Instance { get; private set; }

    [Header("Houle : direction X, direction Z, amplitude (m), longueur (m)")]
    public Vector4[] vagues =
    {
        new Vector4(1f, 0.2f, 0.45f, 62f),
        new Vector4(0.8f, -0.6f, 0.25f, 37f),
        new Vector4(0.5f, 0.9f, 0.14f, 21f),
        new Vector4(-0.3f, 1f, 0.08f, 13f)
    };
    [Range(0f, 2f)] public float forceHoule = 1f;
    [Range(0f, 1f)] public float cambrure = 0.6f;

    private static readonly int IdVagues = Shader.PropertyToID("_MerVagues");
    private static readonly int IdReglages = Shader.PropertyToID("_MerReglages");
    private readonly Vector4[] envoi = new Vector4[4];

    public float Temps => Application.isPlaying ? Time.time : 0f;

    private void OnEnable()
    {
        Instance = this;
        Envoyer();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update() => Envoyer();

    public void Envoyer()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector4 v = i < vagues.Length ? vagues[i] : new Vector4(1f, 0f, 0f, 10f);
            Vector2 d = new Vector2(v.x, v.y).normalized;
            envoi[i] = new Vector4(d.x, d.y, v.z * forceHoule, Mathf.Max(1f, v.w));
        }
        Shader.SetGlobalVectorArray(IdVagues, envoi);
        Shader.SetGlobalVector(IdReglages, new Vector4(Temps, cambrure, transform.position.y, 0f));
    }

    private Vector3 Deplacement(float x, float z)
    {
        Vector3 d = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            Vector4 v = envoi[i];
            float k = 2f * Mathf.PI / v.w;
            float phase = k * (v.x * x + v.y * z) - Mathf.Sqrt(9.81f * k) * Temps;
            float c = Mathf.Cos(phase) * v.z * cambrure;
            d += new Vector3(v.x * c, Mathf.Sin(phase) * v.z, v.y * c);
        }
        return d;
    }

    // Altitude de la surface au-dessus d'un point du monde.
    public float Hauteur(Vector3 position)
    {
        float x = position.x, z = position.z;
        Vector3 d = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            d = Deplacement(x, z);
            x = position.x - d.x;
            z = position.z - d.z;
        }
        return transform.position.y + d.y;
    }
}
