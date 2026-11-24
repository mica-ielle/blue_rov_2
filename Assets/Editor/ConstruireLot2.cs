using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu BLUE ROV : (re)construit seulement l'objet ParcEoliennes dans la scene.
public static class ConstruireLot2
{
    private const string Racine = "ParcEoliennes";
    private const float HautFondation = 14f, HauteurMoyeu = 70f, Pale = 38f;
    private static Material acier, jaune, blanc, sombre, cableMat;

    [MenuItem("BLUE ROV/Lot 2 - Reconstruire les eoliennes")]
    private static void Menu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Construire();
    }

    public static void Construire()
    {
        var scene = EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        foreach (GameObject o in scene.GetRootGameObjects())
            if (o.name == Racine) Object.DestroyImmediate(o);

        acier = ConstruireLot1.Materiau("AcierImmerge", "BlueROV/Immerge");
        acier.SetColor("_Couleur", new Color(0.34f, 0.33f, 0.24f));
        acier.SetColor("_Couleur2", new Color(0.13f, 0.18f, 0.11f));
        acier.SetFloat("_Echelle", 1.1f);
        jaune = ConstruireLot1.Materiau("JauneFondation", "BlueROV/Immerge");
        jaune.SetColor("_Couleur", new Color(0.95f, 0.68f, 0.05f));
        jaune.SetColor("_Couleur2", new Color(0.82f, 0.56f, 0.05f));
        blanc = Lit("BlancEolienne", new Color(0.88f, 0.90f, 0.91f), 0.35f);
        sombre = Lit("GrisFonce", new Color(0.20f, 0.21f, 0.23f), 0.3f);
        cableMat = ConstruireLot1.Materiau("Cable", "BlueROV/Immerge");
        cableMat.SetColor("_Couleur", new Color(0.10f, 0.10f, 0.10f));
        cableMat.SetColor("_Couleur2", new Color(0.45f, 0.38f, 0.06f));
        cableMat.SetFloat("_Echelle", 0.9f);

        var parc = new GameObject(Racine).transform;
        Mesh pale = ConstruireLot1.Sauver(MaillagePale(), "PaleMesh");
        float[] xs = { -60f, 110f };
        float[] zs = { -150f, 0f, 150f };
        var est = new Vector3[2, 3]; var ouest = new Vector3[2, 3];
        int n = 0;
        for (int c = 0; c < 2; c++)
            for (int l = 0; l < 3; l++)
                (est[c, l], ouest[c, l]) = Eolienne(++n, xs[c], zs[l], c == 1, parc, pale);

        for (int c = 0; c < 2; c++)
            for (int l = 0; l < 2; l++)
                Cable($"Cable_{c * 3 + l + 1}_{c * 3 + l + 2}", est[c, l], est[c, l + 1], parc);
        Cable("Cable_3_6", est[0, 2], ouest[1, 2], parc);
        Vector3 cote = new Vector3(-285f, 0f, -40f);
        cote.y = ConstruireLot1.Fond(cote.x, cote.z) + 0.12f;
        Cable("Cable_export", ouest[0, 1], cote, parc);

        var camera = Object.FindAnyObjectByType<CameraExploration>();
        if (camera != null) camera.altitudeMax = 160f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LOT2 OK eoliennes=" + n);
    }

    private static (Vector3, Vector3) Eolienne(int num, float x, float z, bool treillis, Transform parc, Mesh pale)
    {
        float sol = ConstruireLot1.Fond(x, z);
        var t = new GameObject($"Eolienne_{num}_{(treillis ? "treillis" : "monopieu")}").transform;
        t.SetParent(parc, false);
        t.position = new Vector3(x, 0f, z);

        var mAcier = new Forme();
        var mJaune = new Forme();
        if (treillis) Treillis(mAcier, mJaune, x, z, sol);
        else Monopieu(mAcier, mJaune, sol);
        float rj = treillis ? 7.6f : 2.95f;
        Vector3 jEst = TubeJ(mAcier, mJaune, x, z, rj, 1f), jOuest = TubeJ(mAcier, mJaune, x, z, rj, -1f);

        // Plateforme et garde-corps
        float rp = treillis ? 6f : 5f;
        mJaune.Tube(new Vector3(0, HautFondation - 0.5f, 0), new Vector3(0, HautFondation, 0), rp, rp, 32);
        for (int i = 0; i < 20; i++)
        {
            Vector3 a = Cercle(rp - 0.15f, i / 20f, HautFondation), b = Cercle(rp - 0.15f, (i + 1) / 20f, HautFondation);
            mJaune.Tube(a, a + Vector3.up * 1.1f, 0.04f, 0.04f, 6);
            mJaune.Tube(a + Vector3.up * 1.1f, b + Vector3.up * 1.1f, 0.04f, 0.04f, 6);
            mJaune.Tube(a + Vector3.up * 0.55f, b + Vector3.up * 0.55f, 0.03f, 0.03f, 6);
        }

        Poser(mAcier, acier, t, $"FondationImmergee_{num}", true);
        Poser(mJaune, jaune, t, $"FondationHaute_{num}", true);

        var mMat = new Forme();
        mMat.Tube(new Vector3(0, HautFondation, 0), new Vector3(0, HauteurMoyeu - 1.6f, 0), 2.2f, 1.4f, 32);
        Poser(mMat, blanc, t, $"Mat_{num}", true);
        var mPorte = new Forme();
        mPorte.Boite(new Vector3(0, HautFondation + 1.2f, -2.15f), new Vector3(0.9f, 2.2f, 0.2f));
        Poser(mPorte, sombre, t, $"Porte_{num}", false);

        // Nacelle face au vent (vers -Z local, tournee de 20 degres)
        var nac = new GameObject("Nacelle").transform;
        nac.SetParent(t, false);
        nac.localPosition = new Vector3(0, HauteurMoyeu, 0);
        nac.localRotation = Quaternion.Euler(0, 20f, 0);
        var mNac = new Forme();
        mNac.Boite(new Vector3(0, 0, 1.8f), new Vector3(3.4f, 3.4f, 9f));
        mNac.Tube(new Vector3(0, 0, -2.7f), new Vector3(0, 0, -3.6f), 1.3f, 1.3f, 24);
        Poser(mNac, blanc, nac, $"NacelleForme_{num}", false);

        var rotor = new GameObject("Rotor").transform;
        rotor.SetParent(nac, false);
        rotor.localPosition = new Vector3(0, 0, -4.8f);
        rotor.localRotation = Quaternion.Euler(0, 0, num * 37f);
        rotor.gameObject.AddComponent<RotorEolienne>().toursParMinute = 9f + num * 0.4f;
        var mMoyeu = new Forme();
        mMoyeu.Tube(new Vector3(0, 0, 1.2f), new Vector3(0, 0, -0.8f), 1.6f, 1.5f, 24);
        mMoyeu.Tube(new Vector3(0, 0, -0.8f), new Vector3(0, 0, -1.7f), 1.5f, 1.0f, 24);
        mMoyeu.Tube(new Vector3(0, 0, -1.7f), new Vector3(0, 0, -2.1f), 1.0f, 0.2f, 24);
        Poser(mMoyeu, blanc, rotor, $"Moyeu_{num}", false);
        for (int i = 0; i < 3; i++)
        {
            var p = ConstruireLot1.Objet("Pale" + (i + 1), pale, blanc, rotor);
            p.transform.localRotation = Quaternion.Euler(0, 0, i * 120f);
        }

        // Enrochement anti-affouillement
        var rng = new System.Random(num * 31);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var roche = ConstruireLot1.Materiau("Roche", "BlueROV/Immerge");
        for (int i = 0; i < 45; i++)
        {
            float ang = R(0f, 6.283f), r = treillis ? R(8f, 13f) : R(3.2f, 8.5f);
            float px = x + Mathf.Cos(ang) * r, pz = z + Mathf.Sin(ang) * r, s = R(0.35f, 0.9f);
            var m = AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/Generated/Rocher{i % 4}.asset");
            var o = ConstruireLot1.Objet("Enrochement", m, roche, t);
            o.transform.position = new Vector3(px, ConstruireLot1.Fond(px, pz), pz);
            o.transform.rotation = Quaternion.Euler(R(0f, 360f), R(0f, 360f), 0f);
            o.transform.localScale = new Vector3(s * 1.3f, s * 0.8f, s);
        }
        return (new Vector3(x, 0, z) + jEst, new Vector3(x, 0, z) + jOuest);
    }

    private static void Monopieu(Forme a, Forme j, float sol)
    {
        a.Tube(new Vector3(0, sol - 4f, 0), new Vector3(0, -2f, 0), 2.6f, 2.6f, 40);
        j.Tube(new Vector3(0, -2f, 0), new Vector3(0, HautFondation - 0.5f, 0), 2.7f, 2.7f, 40);
        // Soudures annulaires et anodes : points d'inspection
        for (float y = sol + 3f; y < -3f; y += 4f)
            a.Tube(new Vector3(0, y - 0.08f, 0), new Vector3(0, y + 0.08f, 0), 2.66f, 2.66f, 40);
        for (int i = 0; i < 6; i++)
        {
            Vector3 d = Cercle(2.85f, i / 6f, 0f);
            a.Tube(d + Vector3.up * (sol + 4.5f), d + Vector3.up * (sol + 6f), 0.14f, 0.14f, 10);
            a.Tube(d * 0.9f + Vector3.up * (sol + 5.2f), d + Vector3.up * (sol + 5.2f), 0.05f, 0.05f, 6);
        }
        Accostage(j, 2.7f);
    }

    private static void Treillis(Forme a, Forme j, float x, float z, float sol)
    {
        const float haut = 10f;
        float bas = sol - 1f;
        Vector3 Jambe(int i, float y)
        {
            float demi = Mathf.Lerp(7f, 4f, Mathf.InverseLerp(bas, haut, y));
            return new Vector3(i == 0 || i == 3 ? -demi : demi, y, i < 2 ? -demi : demi);
        }
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = Jambe(i, -2f);
            float fondJ = ConstruireLot1.Fond(x + Jambe(i, bas).x, z + Jambe(i, bas).z) - 2f;
            Vector3 pied = Jambe(i, bas);
            a.Tube(new Vector3(pied.x, fondJ - 2f, pied.z), new Vector3(pied.x, Mathf.Max(bas, fondJ) + 4f, pied.z), 1.0f, 1.0f, 20);
            a.Tube(pied, p, 0.6f, 0.6f, 20);
            j.Tube(p, Jambe(i, haut), 0.62f, 0.62f, 20);
            j.Tube(Jambe(i, haut), new Vector3(0, HautFondation - 0.6f, 0), 0.6f, 0.9f, 20);
            // Anodes
            for (float y = bas + 6f; y < -4f; y += 8f)
            {
                Vector3 q = Jambe(i, y) * 1f + new Vector3(Mathf.Sign(p.x) * 0.75f, 0, 0);
                a.Boite(q, new Vector3(0.22f, 1.3f, 0.22f));
            }
        }
        float[] niv = { bas + 3f, Mathf.Lerp(bas, -2f, 0.38f), Mathf.Lerp(bas, -2f, 0.7f), -2f, haut };
        for (int k = 0; k < niv.Length; k++)
            for (int i = 0; i < 4; i++)
            {
                int s = (i + 1) % 4;
                Forme f = niv[k] > -2.5f ? j : a;
                if (k == 0 || k >= 3) f.Tube(Jambe(i, niv[k]), Jambe(s, niv[k]), 0.28f, 0.28f, 14);
                if (k == niv.Length - 1) continue;
                f.Tube(Jambe(i, niv[k]), Jambe(s, niv[k + 1]), 0.3f, 0.3f, 14);
                f.Tube(Jambe(s, niv[k]), Jambe(i, niv[k + 1]), 0.3f, 0.3f, 14);
            }
        j.Tube(new Vector3(0, haut, 0), new Vector3(0, HautFondation - 0.4f, 0), 2.4f, 2.4f, 32);
        Accostage(j, 4.8f);
    }

    // Deux defenses verticales et une echelle.
    private static void Accostage(Forme j, float r)
    {
        foreach (float s in new[] { -0.9f, 0.9f })
        {
            j.Tube(new Vector3(s, -2.5f, -r - 1f), new Vector3(s, 8f, -r - 1f), 0.2f, 0.2f, 12);
            j.Tube(new Vector3(s, -1.5f, -r - 1f), new Vector3(s, -1.5f, -r + 0.5f), 0.14f, 0.14f, 10);
            j.Tube(new Vector3(s, 7f, -r - 1f), new Vector3(s, 7f, -r + 0.5f), 0.14f, 0.14f, 10);
            j.Tube(new Vector3(s * 0.33f, -2f, -r - 0.6f), new Vector3(s * 0.33f, HautFondation, -r - 0.6f), 0.05f, 0.05f, 8);
        }
        for (float y = -2f; y < HautFondation; y += 0.45f)
            j.Tube(new Vector3(-0.3f, y, -r - 0.6f), new Vector3(0.3f, y, -r - 0.6f), 0.03f, 0.03f, 6);
    }

    // Tube guide du cable, courbe vers l'exterieur en bas. Renvoie la sortie en coordonnees locales.
    private static Vector3 TubeJ(Forme a, Forme j, float x, float z, float r, float sens)
    {
        float sol = ConstruireLot1.Fond(x + sens * (r + 3f), z);
        const float rayon = 3f;
        j.Tube(new Vector3(sens * r, HautFondation - 0.5f, 0), new Vector3(sens * r, -2f, 0), 0.22f, 0.22f, 14);
        a.Tube(new Vector3(sens * r, -2f, 0), new Vector3(sens * r, sol + rayon + 0.3f, 0), 0.22f, 0.22f, 14);
        if (r > 5f) j.Tube(new Vector3(0, 12f, 0), new Vector3(sens * r, 12f, 0), 0.18f, 0.18f, 10);
        Vector3 prec = new Vector3(sens * r, sol + rayon + 0.3f, 0);
        for (int i = 1; i <= 8; i++)
        {
            float an = i / 8f * Mathf.PI / 2f;
            var p = new Vector3(sens * (r + rayon * (1f - Mathf.Cos(an))), sol + 0.3f + rayon * (1f - Mathf.Sin(an)), 0);
            a.Tube(prec, p, 0.22f, 0.22f, 14);
            prec = p;
        }
        return prec;
    }

    private static void Cable(string nom, Vector3 de, Vector3 vers, Transform parc)
    {
        Vector3 d = vers - de;
        d.y = 0f;
        Vector3 cote = new Vector3(-d.z, 0f, d.x).normalized;
        int n = Mathf.CeilToInt(d.magnitude / 1.5f);
        var pts = new List<Vector3>();
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n, env = Mathf.Sin(t * Mathf.PI);
            Vector3 p = Vector3.Lerp(de, vers, t) + cote * (env * (5f + 3f * Mathf.Sin(t * 14f)));
            float y = ConstruireLot1.Fond(p.x, p.z) + 0.05f;
            float b = Mathf.Min(t, 1f - t) * d.magnitude;
            // Depart dans le prolongement du tube guide.
            p.y = Mathf.Lerp(i < n / 2 ? de.y : vers.y, y, Mathf.SmoothStep(0f, 1f, b / 4f));
            pts.Add(p);
        }
        var f = new Forme();
        f.Courbe(pts, 0.12f, 10);
        Poser(f, cableMat, parc, nom, true);
    }

    private static Mesh MaillagePale()
    {
        const int ns = 28, np = 20;
        var v = new List<Vector3>();
        var tri = new List<int>();
        for (int s = 0; s <= ns; s++)
        {
            float t = s / (float)ns;
            float corde = t < 0.2f ? Mathf.Lerp(1.9f, 3.3f, Mathf.SmoothStep(0, 1, t / 0.2f)) : Mathf.Lerp(3.3f, 0.55f, Mathf.Pow((t - 0.2f) / 0.8f, 0.8f));
            float ep = corde * Mathf.Lerp(1f, 0.16f, Mathf.SmoothStep(0, 1, t / 0.25f));
            float vrille = Mathf.Lerp(16f, 0f, t) * Mathf.Deg2Rad;
            float aplati = Mathf.SmoothStep(0, 1, t / 0.25f);
            if (s == ns) { corde *= 0.3f; ep *= 0.3f; }
            for (int i = 0; i < np; i++)
            {
                float an = 2f * Mathf.PI * i / np;
                float cx = Mathf.Cos(an) * corde * 0.5f;
                // Bord de fuite plus fin que le bord d'attaque.
                float cz = Mathf.Sin(an) * ep * 0.5f * (1f - aplati * 0.45f * (1f + Mathf.Cos(an)) * 0.9f);
                cx += corde * 0.2f * aplati;
                v.Add(new Vector3(cx * Mathf.Cos(vrille) - cz * Mathf.Sin(vrille), 1.2f + t * Pale,
                                  cx * Mathf.Sin(vrille) + cz * Mathf.Cos(vrille)));
                if (s < ns)
                {
                    int a = s * np + i, b = s * np + (i + 1) % np, c = a + np, d = b + np;
                    tri.AddRange(new[] { a, c, b, b, c, d });
                }
            }
        }
        int bout = v.Count;
        v.Add(new Vector3(0.1f, 1.2f + Pale + 0.1f, 0f));
        for (int i = 0; i < np; i++)
            tri.AddRange(new[] { ns * np + i, bout, ns * np + (i + 1) % np });
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static Vector3 Cercle(float r, float t, float y) =>
        new Vector3(Mathf.Cos(t * 2f * Mathf.PI) * r, y, Mathf.Sin(t * 2f * Mathf.PI) * r);

    private static Material Lit(string nom, Color c, float lisse)
    {
        var m = ConstruireLot1.Materiau(nom, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", lisse);
        return m;
    }

    private static void Poser(Forme f, Material mat, Transform parent, string nom, bool collision)
    {
        Mesh m = ConstruireLot1.Sauver(f.Mesh(), nom + "_mesh");
        var o = ConstruireLot1.Objet(nom, m, mat, parent);
        if (collision) o.AddComponent<MeshCollider>().sharedMesh = m;
    }

    // Accumulateur de formes simples.
    internal class Forme
    {
        private readonly List<Vector3> v = new List<Vector3>(), no = new List<Vector3>();
        private readonly List<int> tri = new List<int>();

        public void Tube(Vector3 a, Vector3 b, float ra, float rb, int n)
        {
            Vector3 axe = (b - a).normalized;
            Vector3 u = Vector3.Cross(axe, Mathf.Abs(axe.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 w = Vector3.Cross(axe, u);
            float pente = (ra - rb) / Vector3.Distance(a, b);
            int s = v.Count;
            for (int i = 0; i <= n; i++)
            {
                float t = 2f * Mathf.PI * i / n;
                Vector3 d = u * Mathf.Cos(t) + w * Mathf.Sin(t);
                v.Add(a + d * ra); v.Add(b + d * rb);
                Vector3 nn = (d + axe * pente).normalized;
                no.Add(nn); no.Add(nn);
                if (i < n) tri.AddRange(new[] { s + 2 * i, s + 2 * i + 2, s + 2 * i + 1, s + 2 * i + 1, s + 2 * i + 2, s + 2 * i + 3 });
            }
            Bouchon(a, u, w, ra, n, -axe, true);
            Bouchon(b, u, w, rb, n, axe, false);
        }

        private void Bouchon(Vector3 c, Vector3 u, Vector3 w, float r, int n, Vector3 normale, bool inverse)
        {
            int s = v.Count;
            v.Add(c); no.Add(normale);
            for (int i = 0; i <= n; i++)
            {
                float t = 2f * Mathf.PI * i / n;
                v.Add(c + (u * Mathf.Cos(t) + w * Mathf.Sin(t)) * r); no.Add(normale);
                if (i < n) tri.AddRange(inverse ? new[] { s, s + i + 2, s + i + 1 } : new[] { s, s + i + 1, s + i + 2 });
            }
        }

        public void Courbe(List<Vector3> p, float r, int n)
        {
            int s = v.Count;
            for (int k = 0; k < p.Count; k++)
            {
                Vector3 axe = (p[Mathf.Min(k + 1, p.Count - 1)] - p[Mathf.Max(k - 1, 0)]).normalized;
                Vector3 u = Vector3.Cross(axe, Vector3.up).normalized, w = Vector3.Cross(axe, u);
                for (int i = 0; i <= n; i++)
                {
                    float t = 2f * Mathf.PI * i / n;
                    Vector3 d = u * Mathf.Cos(t) + w * Mathf.Sin(t);
                    v.Add(p[k] + d * r); no.Add(d);
                    if (k < p.Count - 1 && i < n)
                    {
                        int a = s + k * (n + 1) + i, c = a + n + 1;
                        tri.AddRange(new[] { a, a + 1, c, a + 1, c + 1, c });
                    }
                }
            }
        }

        public void Boite(Vector3 c, Vector3 t)
        {
            foreach (Vector3 d in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                Vector3 u = Mathf.Abs(d.y) > 0.5f ? Vector3.right : Vector3.up, w = Vector3.Cross(d, u);
                int s = v.Count;
                foreach (Vector2 k in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                {
                    v.Add(c + Vector3.Scale(d + u * k.x + w * k.y, t * 0.5f));
                    no.Add(d);
                }
                tri.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
        }

        // Profil (position sur l'axe, rayon), dans le sens qui donne des normales vers l'exterieur.
        public void Revolution(Vector3 c, Vector3 axe, List<Vector2> p, int n)
        {
            Vector3 u = Vector3.Cross(axe, Mathf.Abs(axe.y) > 0.9f ? Vector3.right : Vector3.up).normalized, w = Vector3.Cross(axe, u);
            for (int k = 0; k + 1 < p.Count; k++)
            {
                Vector2 seg = (p[k + 1] - p[k]).normalized;
                int s = v.Count;
                for (int i = 0; i <= n; i++)
                {
                    float t = 2f * Mathf.PI * i / n;
                    Vector3 d = u * Mathf.Cos(t) + w * Mathf.Sin(t);
                    Vector3 nn = d * seg.x - axe * seg.y;
                    v.Add(c + axe * p[k].x + d * p[k].y); v.Add(c + axe * p[k + 1].x + d * p[k + 1].y);
                    no.Add(nn); no.Add(nn);
                    if (i < n) tri.AddRange(new[] { s + 2 * i, s + 2 * i + 2, s + 2 * i + 1, s + 2 * i + 1, s + 2 * i + 2, s + 2 * i + 3 });
                }
            }
        }

        // Polygone convexe (z, y) etire de x0 a x1.
        public void Extrusion(Vector2[] p, float x0, float x1)
        {
            Vector2 centre = Vector2.zero;
            foreach (Vector2 q in p) centre += q / p.Length;
            for (int i = 0; i < p.Length; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Length], m = (a + b) / 2f - centre, nor = new Vector2(b.y - a.y, a.x - b.x).normalized;
                if (Vector2.Dot(nor, m) < 0f) nor = -nor;
                Face(new[] { new Vector3(x0, a.y, a.x), new Vector3(x1, a.y, a.x), new Vector3(x1, b.y, b.x), new Vector3(x0, b.y, b.x) }, new Vector3(0, nor.y, nor.x));
            }
            Face(System.Array.ConvertAll(p, q => new Vector3(x0, q.y, q.x)), Vector3.left);
            Face(System.Array.ConvertAll(p, q => new Vector3(x1, q.y, q.x)), Vector3.right);
        }

        // Face convexe, orientee selon la normale donnee.
        private void Face(Vector3[] p, Vector3 normale)
        {
            int s = v.Count;
            foreach (Vector3 q in p) { v.Add(q); no.Add(normale); }
            bool bon = Vector3.Dot(Vector3.Cross(p[1] - p[0], p[2] - p[0]), normale) > 0f;
            for (int i = 1; i + 1 < p.Length; i++)
                tri.AddRange(bon ? new[] { s, s + i, s + i + 1 } : new[] { s, s + i + 1, s + i });
        }

        // Pale vrillee, dessinee des deux cotes, axe Z.
        public void PaleHelice(float angle, float r0, float r1, float demiCorde, float sens)
        {
            Vector3 d = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0), t = new Vector3(-d.y, d.x, 0);
            const int n = 5;
            for (int k = 0; k < n; k++)
            {
                Vector3[] q = new Vector3[4];
                for (int e = 0; e < 2; e++)
                {
                    float f = (k + e) / (float)n, vr = Mathf.Lerp(50f, 22f, f) * Mathf.Deg2Rad * sens;
                    float l = demiCorde * Mathf.Sqrt(Mathf.Max(0.05f, 1f - f * f * 0.8f));
                    Vector3 corde = (t * Mathf.Cos(vr) + Vector3.forward * Mathf.Sin(vr)) * l;
                    Vector3 c = d * Mathf.Lerp(r0, r1, f) + Vector3.forward * 0.003f;
                    q[e == 0 ? 0 : 3] = c - corde;
                    q[e == 0 ? 1 : 2] = c + corde;
                }
                Vector3 nn = Vector3.Cross(q[1] - q[0], q[2] - q[0]).normalized;
                Face(q, nn);
                Face(q, -nn);
            }
        }

        public Mesh Mesh()
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetNormals(no);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
