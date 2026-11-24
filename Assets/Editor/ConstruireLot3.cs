using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu BLUE ROV : (re)construit seulement l'objet BlueROV2. 1 unite = 1 metre, avant = +Z.
public static class ConstruireLot3
{
    private const string Racine = "BlueROV2";
    private static Material noir, bleu, alu, verre, helice, lampe, jaune;

    [MenuItem("BLUE ROV/Lot 3 - Reconstruire le BlueROV2")]
    private static void Menu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Construire();
    }

    public static void Construire()
    {
        var scene = EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        foreach (GameObject o in scene.GetRootGameObjects())
            if (o.name == Racine) Object.DestroyImmediate(o);

        noir = Lit("ROV_Noir", new Color(0.035f, 0.035f, 0.04f), 0.35f, 0f);
        bleu = Lit("ROV_Bleu", new Color(0.02f, 0.24f, 0.72f), 0.45f, 0f);
        alu = Lit("ROV_Alu", new Color(0.55f, 0.57f, 0.60f), 0.6f, 0.6f);
        helice = Lit("ROV_Helice", new Color(0.10f, 0.11f, 0.13f), 0.4f, 0f);
        jaune = Lit("ROV_Ombilical", new Color(0.95f, 0.75f, 0.05f), 0.3f, 0f);
        lampe = Lit("ROV_Lampe", Color.white, 0.5f, 0f);
        lampe.EnableKeyword("_EMISSION");
        lampe.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        lampe.SetColor("_EmissionColor", new Color(1f, 0.97f, 0.88f) * 2.5f);
        verre = Lit("ROV_Verre", new Color(0.6f, 0.85f, 0.95f, 0.16f), 0.95f, 0f);
        verre.SetFloat("_Surface", 1f);
        verre.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        verre.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        verre.SetFloat("_ZWrite", 0f);
        verre.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        verre.SetOverrideTag("RenderType", "Transparent");
        verre.renderQueue = 3050;

        var rov = new GameObject(Racine).transform;
        float sol = ConstruireLot1.Fond(-60f, -6.5f);
        rov.position = new Vector3(-60f, sol + 5f, -6.5f);
        Chassis(rov);
        var boite = rov.gameObject.AddComponent<BoxCollider>();
        boite.size = new Vector3(0.338f, 0.254f, 0.457f);

        // 4 propulseurs horizontaux a 45 degres, 2 verticaux. L'axe +Z local est le sens de poussee.
        var parent = new GameObject("Propulseurs").transform;
        parent.SetParent(rov, false);
        Propulseur(parent, "T1_avant_droit", new Vector3(0.112f, -0.055f, 0.150f), new Vector3(-1, 0, 1), 1f);
        Propulseur(parent, "T2_avant_gauche", new Vector3(-0.112f, -0.055f, 0.150f), new Vector3(1, 0, 1), -1f);
        Propulseur(parent, "T3_arriere_droit", new Vector3(0.112f, -0.055f, -0.150f), new Vector3(1, 0, 1), -1f);
        Propulseur(parent, "T4_arriere_gauche", new Vector3(-0.112f, -0.055f, -0.150f), new Vector3(-1, 0, 1), 1f);
        Propulseur(parent, "T5_vertical_droit", new Vector3(0.112f, 0.085f, 0f), Vector3.up, 1f);
        Propulseur(parent, "T6_vertical_gauche", new Vector3(-0.112f, 0.085f, 0f), Vector3.up, -1f);

        // Phares
        var phares = new List<Light>();
        var fl = new ConstruireLot2.Forme();
        var fa = new ConstruireLot2.Forme();
        foreach (float sx in new[] { -1f, 1f })
            foreach (float y in new[] { 0.015f, -0.11f })
            {
                float x = sx * (y > 0 ? 0.135f : 0.075f);
                var pos = new Vector3(x, y, 0.215f);
                fa.Tube(pos - Vector3.forward * 0.05f, pos, 0.018f, 0.02f, 16);
                fl.Tube(pos, pos + Vector3.forward * 0.002f, 0.016f, 0.016f, 16);
                var l = new GameObject("Phare" + (phares.Count + 1)).AddComponent<Light>();
                l.transform.SetParent(rov, false);
                l.transform.localPosition = pos + Vector3.forward * 0.02f;
                l.transform.localRotation = Quaternion.Euler(5f, sx * 6f, 0f);
                l.type = LightType.Spot;
                l.color = new Color(1f, 0.97f, 0.88f);
                l.intensity = 9f;
                l.range = 18f;
                l.spotAngle = 85f;
                l.innerSpotAngle = 35f;
                l.shadows = LightShadows.None;
                phares.Add(l);
            }
        Poser(fa, alu, rov, "PharesCorps");
        Poser(fl, lampe, rov, "PharesVerre");

        // Camera embarquee, dans le dome
        var libre = Object.FindAnyObjectByType<CameraExploration>(FindObjectsInactive.Include);
        var ambLibre = libre.GetComponent<AmbianceSousMarine>();
        var co = new GameObject("CameraROV");
        co.transform.SetParent(rov, false);
        co.transform.localPosition = new Vector3(0f, 0.04f, 0.235f);
        var cam = co.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 800f;
        cam.enabled = false;
        var amb = co.AddComponent<AmbianceSousMarine>();
        amb.soleil = ambLibre.soleil;
        amb.particules = ambLibre.particules;
        amb.enabled = false;

        var eq = rov.gameObject.AddComponent<EquipementsROV>();
        eq.phares = phares.ToArray();
        eq.cameraROV = cam;
        eq.cameraLibre = libre.GetComponent<Camera>();

        // La camera libre demarre pres du robot.
        libre.transform.position = rov.position + new Vector3(-1.1f, 0.45f, 1.1f);
        libre.transform.rotation = Quaternion.LookRotation(rov.position - libre.transform.position);
        libre.GetComponent<Camera>().nearClipPlane = 0.05f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LOT3 OK");
    }

    private static void Chassis(Transform rov)
    {
        // Flasques lateraux et traverses
        var n = new ConstruireLot2.Forme();
        foreach (float x in new[] { -0.163f, 0.163f })
        {
            Plaque(n, x, 0.008f, 0.035f, 0.065f, -0.205f, 0.205f);
            Plaque(n, x, 0.008f, -0.127f, -0.105f, -0.215f, 0.215f);
            foreach (float z in new[] { -0.205f, -0.0f, 0.205f })
                Plaque(n, x, 0.008f, -0.11f, 0.04f, z - 0.014f, z + 0.014f);
            n.Tube(new Vector3(x, -0.11f, 0.205f), new Vector3(x, 0.04f, 0.0f), 0.008f, 0.008f, 8);
            n.Tube(new Vector3(x, -0.11f, -0.205f), new Vector3(x, 0.04f, 0.0f), 0.008f, 0.008f, 8);
            n.Tube(new Vector3(x * 0.93f, -0.127f, -0.22f), new Vector3(x * 0.93f, -0.127f, 0.22f), 0.009f, 0.009f, 12);
        }
        foreach (float z in new[] { -0.19f, -0.07f, 0.07f, 0.19f })
            n.Boite(new Vector3(0, -0.122f, z), new Vector3(0.33f, 0.008f, 0.03f));
        foreach (float z in new[] { -0.11f, 0.11f })
            n.Boite(new Vector3(0, -0.02f, z), new Vector3(0.33f, 0.006f, 0.02f));
        // Capots du tube principal et de la batterie
        n.Tube(new Vector3(0, 0.045f, -0.185f), new Vector3(0, 0.045f, -0.165f), 0.062f, 0.062f, 32);
        n.Tube(new Vector3(0, 0.045f, 0.150f), new Vector3(0, 0.045f, 0.168f), 0.062f, 0.062f, 32);
        n.Tube(new Vector3(0, -0.075f, -0.165f), new Vector3(0, -0.075f, -0.150f), 0.047f, 0.047f, 28);
        n.Tube(new Vector3(0, -0.075f, 0.150f), new Vector3(0, -0.075f, 0.165f), 0.047f, 0.047f, 28);
        // Camera visible dans le dome
        n.Boite(new Vector3(0, 0.04f, 0.18f), new Vector3(0.035f, 0.035f, 0.02f));
        n.Tube(new Vector3(0, 0.04f, 0.19f), new Vector3(0, 0.04f, 0.208f), 0.012f, 0.014f, 16);
        Poser(n, noir, rov, "Chassis");

        // Tubes etanches
        var a = new ConstruireLot2.Forme();
        a.Tube(new Vector3(0, 0.045f, -0.165f), new Vector3(0, 0.045f, 0.150f), 0.057f, 0.057f, 36);
        a.Tube(new Vector3(0, -0.075f, -0.150f), new Vector3(0, -0.075f, 0.150f), 0.043f, 0.043f, 32);
        // Presse-etoupes a l'arriere
        for (int i = 0; i < 6; i++)
        {
            float an = i / 6f * 2f * Mathf.PI;
            var p = new Vector3(Mathf.Cos(an) * 0.038f, 0.045f + Mathf.Sin(an) * 0.038f, -0.185f);
            a.Tube(p, p - Vector3.forward * 0.014f, 0.008f, 0.006f, 10);
        }
        Poser(a, alu, rov, "TubesEtanches");

        // Flotteurs bleus, coupes autour des propulseurs verticaux
        var b = new ConstruireLot2.Forme();
        foreach (float sx in new[] { -1f, 1f })
        {
            float x0 = sx > 0 ? 0.068f : -0.168f;
            foreach (float sz in new[] { -1f, 1f })
            {
                var pts = new[] { new Vector2(0.056f, 0.065f), new Vector2(0.215f, 0.065f), new Vector2(0.226f, 0.078f),
                                  new Vector2(0.218f, 0.105f), new Vector2(0.185f, 0.127f), new Vector2(0.066f, 0.127f), new Vector2(0.056f, 0.117f) };
                for (int i = 0; i < pts.Length; i++) pts[i].x *= sz;
                b.Extrusion(pts, x0, x0 + 0.10f);
            }
        }
        // Pont central
        b.Extrusion(new[] { new Vector2(-0.2f, 0.112f), new Vector2(0.14f, 0.112f), new Vector2(0.13f, 0.124f), new Vector2(-0.19f, 0.124f) }, -0.07f, 0.07f);
        Poser(b, bleu, rov, "Flotteurs");

        // Dome
        var d = new ConstruireLot2.Forme();
        var profil = new List<Vector2>();
        for (int i = 0; i <= 12; i++)
        {
            float t = i / 12f * Mathf.PI / 2f;
            profil.Add(new Vector2(Mathf.Sin(t) * 0.056f, Mathf.Cos(t) * 0.056f));
        }
        d.Revolution(new Vector3(0, 0.045f, 0.168f), Vector3.forward, profil, 32);
        Poser(d, verre, rov, "Dome");

        // Depart de l'ombilical
        var c = new List<Vector3>();
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            c.Add(new Vector3(0.02f * Mathf.Sin(t * 5f), 0.045f + 0.35f * t * t, -0.195f - 0.35f * t));
        }
        var o = new ConstruireLot2.Forme();
        o.Courbe(c, 0.005f, 8);
        Poser(o, jaune, rov, "Ombilical");
    }

    private static void Plaque(ConstruireLot2.Forme f, float x, float ep, float y0, float y1, float z0, float z1) =>
        f.Boite(new Vector3(x, (y0 + y1) / 2f, (z0 + z1) / 2f), new Vector3(ep, y1 - y0, z1 - z0));

    private static void Propulseur(Transform parent, string nom, Vector3 pos, Vector3 axe, float sens)
    {
        var t = new GameObject(nom).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.LookRotation(axe, Mathf.Abs(axe.y) > 0.9f ? Vector3.forward : Vector3.up);

        // Tuyere creuse
        var f = new ConstruireLot2.Forme();
        f.Revolution(Vector3.zero, Vector3.forward, new List<Vector2> {
            new Vector2(-0.024f, 0.043f), new Vector2(-0.022f, 0.047f), new Vector2(0.018f, 0.050f), new Vector2(0.025f, 0.047f),
            new Vector2(0.022f, 0.042f), new Vector2(-0.018f, 0.040f), new Vector2(-0.024f, 0.043f) }, 32);
        // Bras et corps moteur
        for (int i = 0; i < 3; i++)
        {
            float a = i / 3f * 2f * Mathf.PI;
            var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            f.Tube(new Vector3(0, 0, -0.02f), d * 0.042f + new Vector3(0, 0, -0.02f), 0.004f, 0.004f, 8);
        }
        f.Tube(new Vector3(0, 0, -0.045f), new Vector3(0, 0, -0.005f), 0.011f, 0.017f, 20);
        Poser(f, noir, t, "Tuyere_" + nom);

        var h = new GameObject("Helice").transform;
        h.SetParent(t, false);
        h.gameObject.AddComponent<HeliceROV>();
        var m = new ConstruireLot2.Forme();
        m.Revolution(Vector3.zero, Vector3.forward, new List<Vector2> {
            new Vector2(-0.004f, 0f), new Vector2(-0.004f, 0.016f), new Vector2(0.008f, 0.016f), new Vector2(0.018f, 0.009f), new Vector2(0.022f, 0f) }, 20);
        for (int i = 0; i < 3; i++) m.PaleHelice(i / 3f * 2f * Mathf.PI, 0.014f, 0.038f, 0.016f, sens);
        Poser(m, helice, h, "Helice_" + nom);
    }

    private static Material Lit(string nom, Color c, float lisse, float metal)
    {
        var m = ConstruireLot1.Materiau(nom, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", lisse);
        m.SetFloat("_Metallic", metal);
        return m;
    }

    private static void Poser(ConstruireLot2.Forme f, Material mat, Transform parent, string nom) =>
        ConstruireLot1.Objet(nom, ConstruireLot1.Sauver(f.Mesh(), "ROV_" + nom), mat, parent);
}
