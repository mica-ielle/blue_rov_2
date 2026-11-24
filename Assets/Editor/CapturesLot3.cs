using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CapturesLot3
{
    public static void ConstruireEtCapturer()
    {
        ConstruireLot3.Construire();
        Capturer();
    }

    public static void Capturer()
    {
        string[] args = Environment.GetCommandLineArgs();
        string dossier = args[Array.IndexOf(args, "-captures") + 1];
        Directory.CreateDirectory(dossier);
        EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        ShaderUtil.allowAsyncCompilation = false;
        Transform r = GameObject.Find("BlueROV2").transform;
        void V(string n, Vector3 d)
        {
            Vector3 p = r.position + d;
            CapturesLot1.Vue(dossier, n, p, Quaternion.LookRotation(r.position - p).eulerAngles);
        }
        V("1_avant", new Vector3(-0.45f, 0.25f, 0.6f));
        V("2_arriere", new Vector3(0.5f, 0.35f, -0.65f));
        V("3_dessous", new Vector3(-0.5f, -0.35f, -0.3f));
        V("4_contexte", new Vector3(-2.2f, 0.8f, -3f));
        V("5_dessus", new Vector3(0.01f, 0.8f, 0.02f));
        Transform c = r.Find("CameraROV");
        CapturesLot1.Vue(dossier, "6_vueROV_allume", c.position, c.eulerAngles);
        var eq = r.GetComponent<EquipementsROV>();
        eq.Phares(false);
        CapturesLot1.Vue(dossier, "7_vueROV_eteint", c.position, c.eulerAngles);
        Bounds b = new Bounds(r.position, Vector3.zero);
        foreach (Renderer x in r.GetComponentsInChildren<Renderer>())
            if (x.name != "Ombilical") b.Encapsulate(x.bounds);
        Debug.Log($"CAPTURES3 OK taille={b.size.ToString("F3")} helices={r.GetComponentsInChildren<HeliceROV>().Length}");
    }
}
