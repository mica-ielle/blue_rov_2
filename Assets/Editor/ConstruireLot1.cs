using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Menu BLUE ROV : reconstruit la scene du lot 1 (mer, fond marin, lumiere).
public static class ConstruireLot1
{
    private const string Gen = "Assets/Generated";
    public const string CheminScene = "Assets/Scenes/BlueROV.unity";

    [MenuItem("BLUE ROV/Lot 1 - Reconstruire la mer")]
    private static void Menu()
    {
        if (!File.Exists(CheminScene) || EditorUtility.DisplayDialog("BLUE ROV",
                "Remplacer la scene BlueROV ? Les changements faits dans cette scene seront perdus.", "Remplacer", "Annuler"))
            Construire();
    }

    public static void Construire()
    {
        Directory.CreateDirectory(Gen);
        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();

        // La mer a besoin des textures de profondeur et de couleur.
        foreach (string g in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(g));
            rp.supportsCameraDepthTexture = true;
            rp.supportsCameraOpaqueTexture = true;
            rp.shadowDistance = 90f;
            EditorUtility.SetDirty(rp);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ciel et soleil
        var ciel = Materiau("Ciel", "Skybox/Procedural");
        ciel.SetFloat("_SunSize", 0.03f);
        ciel.SetFloat("_AtmosphereThickness", 0.8f);
        ciel.SetFloat("_Exposure", 1.15f);
        RenderSettings.skybox = ciel;
        var soleil = new GameObject("Soleil").AddComponent<Light>();
        soleil.type = LightType.Directional;
        soleil.color = new Color(1f, 0.95f, 0.86f);
        soleil.shadows = LightShadows.Soft;
        soleil.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.sun = soleil;

        // Mer
        var mer = Objet("Mer", Sauver(GrilleMer(400, 1500f), "MerMesh"), Materiau("Mer", "BlueROV/Ocean"), null);
        mer.AddComponent<Mer>();
        var mr = mer.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Fond
        var sable = Materiau("Sable", "BlueROV/Immerge");
        sable.SetFloat("_Rides", 0.8f);
        Mesh fondMesh = Sauver(GrilleFond(260, 300f), "FondMesh");
        var fond = Objet("FondMarin", fondMesh, sable, null);
        fond.AddComponent<MeshCollider>().sharedMesh = fondMesh;

        Decor();

        // Camera
        var camObj = new GameObject("Main Camera") { tag = "MainCamera" };
        camObj.transform.SetPositionAndRotation(new Vector3(0f, 4f, -30f), Quaternion.Euler(6f, 0f, 0f));
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 65f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 2500f;
        camObj.AddComponent<AudioListener>();
        var amb = camObj.AddComponent<AmbianceSousMarine>();
        amb.soleil = soleil;
        amb.particules = Particules(camObj.transform);
        camObj.AddComponent<CameraExploration>();
        amb.Appliquer();

        EditorSceneManager.SaveScene(scene, CheminScene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CheminScene, true) };
        PlayerSettings.productName = "BLUE ROV";
        AssetDatabase.SaveAssets();
        Debug.Log("LOT1 OK");
    }

    // Profondeur du fond : haut-fond a l'ouest, large a l'est.
    public static float Fond(float x, float z)
    {
        float pente = Mathf.Lerp(-11f, -38f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-130f, 130f, x)));
        float n = (Mathf.PerlinNoise(x * 0.012f + 50f, z * 0.012f + 50f) - 0.5f) * 8f;
        n += (Mathf.PerlinNoise(x * 0.05f + 9f, z * 0.05f + 9f) - 0.5f) * 2f;
        return pente + n;
    }

    private static void Decor()
    {
        var rng = new System.Random(7);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        var rocheMat = Materiau("Roche", "BlueROV/Immerge");
        rocheMat.SetColor("_Couleur", new Color(0.30f, 0.30f, 0.28f));
        rocheMat.SetColor("_Couleur2", new Color(0.12f, 0.14f, 0.12f));
        rocheMat.SetFloat("_Echelle", 1.6f);
        var rochers = new GameObject("Rochers").transform;
        var formes = new Mesh[4];
        for (int i = 0; i < formes.Length; i++) formes[i] = Sauver(Rocher(i), "Rocher" + i);
        for (int i = 0; i < 160; i++)
        {
            float x = R(-150f, 150f), z = R(-150f, 150f), t = Mathf.Pow(R(0f, 1f), 3f) * 3f + 0.35f;
            var r = Objet("Rocher", formes[i % formes.Length], rocheMat, rochers);
            r.transform.position = new Vector3(x, Fond(x, z) - t * 0.15f, z);
            r.transform.localScale = new Vector3(t * R(0.8f, 1.5f), t * R(0.6f, 1f), t);
            r.transform.rotation = Quaternion.Euler(R(-10f, 10f), R(0f, 360f), R(-10f, 10f));
            var c = r.AddComponent<MeshCollider>();
            c.sharedMesh = formes[i % formes.Length];
            c.convex = true;
        }

        var algueMat = Materiau("Algue", "BlueROV/Immerge");
        algueMat.SetColor("_Couleur", new Color(0.18f, 0.34f, 0.10f));
        algueMat.SetColor("_Couleur2", new Color(0.07f, 0.17f, 0.06f));
        var algues = new GameObject("Algues").transform;
        Mesh touffe = Sauver(Algue(rng), "AlgueMesh");
        for (int b = 0; b < 35; b++)
        {
            float cx = R(-140f, 60f), cz = R(-130f, 130f);
            for (int i = 0; i < 8; i++)
            {
                float x = cx + R(-5f, 5f), z = cz + R(-5f, 5f);
                var a = Objet("Algue", touffe, algueMat, algues);
                a.transform.position = new Vector3(x, Fond(x, z) - 0.1f, z);
                a.transform.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                a.transform.localScale = Vector3.one * R(0.5f, 1.3f);
            }
        }
    }

    private static ParticleSystem Particules(Transform parent)
    {
        var o = new GameObject("ParticulesEau");
        o.transform.SetParent(parent, false);
        var ps = o.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 8f;
        main.startSpeed = 0.08f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.035f);
        main.startColor = new Color(0.8f, 0.95f, 1f, 0.45f);
        main.maxParticles = 1500;
        var em = ps.emission;
        em.rateOverTime = 170f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(18f, 12f, 18f);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var mat = Materiau("Particule", "Universal Render Pipeline/Particles/Unlit");
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = 3100;
        mat.SetTexture("_BaseMap", Point());
        o.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    private static Texture2D Point()
    {
        string chemin = Gen + "/Point.asset";
        var t = new Texture2D(32, 32, TextureFormat.RGBA32, true);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f)));
            }
        t.Apply();
        AssetDatabase.DeleteAsset(chemin);
        AssetDatabase.CreateAsset(t, chemin);
        return t;
    }

    // ---------- Maillages ----------
    private static Mesh Grille(int n, System.Func<float, float> pas, System.Func<float, float, float> hauteur)
    {
        var v = new Vector3[(n + 1) * (n + 1)];
        var tri = new int[n * n * 6];
        for (int j = 0, i = 0; j <= n; j++)
            for (int k = 0; k <= n; k++, i++)
            {
                float x = pas(2f * k / n - 1f), z = pas(2f * j / n - 1f);
                v[i] = new Vector3(x, hauteur(x, z), z);
            }
        for (int j = 0, t = 0; j < n; j++)
            for (int k = 0; k < n; k++)
            {
                int a = j * (n + 1) + k, b = a + n + 1;
                tri[t++] = a; tri[t++] = b; tri[t++] = a + 1;
                tri[t++] = a + 1; tri[t++] = b; tri[t++] = b + 1;
            }
        var m = new Mesh { indexFormat = IndexFormat.UInt32, vertices = v, triangles = tri };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static Mesh GrilleMer(int n, float demi)
    {
        Mesh m = Grille(n, u => u * Mathf.Abs(u) * demi, (x, z) => 0f);
        m.bounds = new Bounds(Vector3.zero, new Vector3(demi * 2f, 20f, demi * 2f));
        return m;
    }

    private static Mesh GrilleFond(int n, float demi) => Grille(n, u => u * demi, Fond);

    private static Mesh Rocher(int graine)
    {
        const int n = 20;
        var v = new List<Vector3>();
        var tri = new List<int>();
        for (int j = 0; j <= n; j++)
            for (int i = 0; i <= n; i++)
            {
                float th = Mathf.PI * j / n, ph = 2f * Mathf.PI * (i % n) / n;
                var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                float o = graine * 13f + 20f;
                float r = 0.65f + 0.45f * Mathf.PerlinNoise(d.x * 1.4f + o, d.y * 1.4f + d.z + o)
                        + 0.18f * Mathf.PerlinNoise(d.z * 4f + o, d.x * 3f - d.y * 3f + o);
                v.Add(d * r);
                if (j < n && i < n)
                {
                    int a = j * (n + 1) + i, b = a + n + 1;
                    tri.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        // Coutures : meme normale des deux cotes.
        var no = m.normals;
        for (int j = 0; j <= n; j++)
        {
            int a = j * (n + 1), b = a + n;
            no[a] = no[b] = (no[a] + no[b]).normalized;
        }
        m.normals = no;
        return m;
    }

    private static Mesh Algue(System.Random rng)
    {
        var v = new List<Vector3>();
        var tri = new List<int>();
        for (int b = 0; b < 9; b++)
        {
            float a = (float)rng.NextDouble() * 6.28f, h = 0.8f + (float)rng.NextDouble() * 1.6f;
            float ecart = (float)rng.NextDouble() * 0.35f, phase = (float)rng.NextDouble() * 6f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var cote = new Vector3(-dir.z, 0f, dir.x);
            for (int s = 0; s <= 6; s++)
            {
                float t = s / 6f;
                Vector3 c = dir * (ecart + t * t * 0.5f) + cote * (Mathf.Sin(t * 4f + phase) * 0.12f * t) + Vector3.up * (h * t);
                float l = 0.06f * (1f - t * 0.8f);
                v.Add(c - cote * l);
                v.Add(c + cote * l);
                if (s < 6)
                {
                    int i = v.Count - 2;
                    tri.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
                }
            }
        }
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        return m;
    }

    // ---------- Outils ----------
    internal static GameObject Objet(string nom, Mesh mesh, Material mat, Transform parent)
    {
        var o = new GameObject(nom);
        o.transform.SetParent(parent, false);
        o.AddComponent<MeshFilter>().sharedMesh = mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return o;
    }

    internal static Mesh Sauver(Mesh m, string nom)
    {
        string chemin = $"{Gen}/{nom}.asset";
        AssetDatabase.DeleteAsset(chemin);
        AssetDatabase.CreateAsset(m, chemin);
        return m;
    }

    internal static Material Materiau(string nom, string shader)
    {
        string chemin = $"{Gen}/{nom}.mat";
        Shader s = Shader.Find(shader);
        if (s == null) throw new System.Exception("Shader introuvable : " + shader);
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (m == null)
        {
            m = new Material(s);
            AssetDatabase.CreateAsset(m, chemin);
        }
        m.shader = s;
        return m;
    }
}
