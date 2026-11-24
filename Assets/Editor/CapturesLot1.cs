using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Captures de controle, en mode batch : -captures <dossier>
public static class CapturesLot1
{
    public static void ConstruireEtCapturer()
    {
        ConstruireLot1.Construire();
        Capturer();
    }

    public static void Capturer()
    {
        string[] args = Environment.GetCommandLineArgs();
        string dossier = args[Array.IndexOf(args, "-captures") + 1];
        Directory.CreateDirectory(dossier);
        EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        ShaderUtil.allowAsyncCompilation = false;

        Vue(dossier, "1_surface", new Vector3(0, 4, -30), new Vector3(6, 0, 0));
        Vue(dossier, "2_aerienne", new Vector3(-60, 30, -60), new Vector3(25, 45, 0));
        Vue(dossier, "3_hautfond", new Vector3(-110, ConstruireLot1.Fond(-110, 0) + 3, 0), new Vector3(8, 60, 0));
        Vue(dossier, "4_large", new Vector3(90, ConstruireLot1.Fond(90, 0) + 3, 0), new Vector3(8, 20, 0));
        Vue(dossier, "5_vers_surface", new Vector3(0, -8, 0), new Vector3(-55, -35, 0));

        foreach (string nom in new[] { "BlueROV/Ocean", "BlueROV/Immerge" })
        {
            Shader s = Shader.Find(nom);
            Debug.Log($"SHADER {nom} erreurs={ShaderUtil.ShaderHasError(s)}");
            foreach (var m in ShaderUtil.GetShaderMessages(s)) Debug.Log($"SHADERMSG {m.severity} {m.message} ligne {m.line}");
        }
        Debug.Log("CAPTURES OK");
    }

    internal static void Vue(string dossier, string nom, Vector3 pos, Vector3 rot)
    {
        Camera cam = Camera.main;
        cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        Mer.Instance.Envoyer();
        var amb = cam.GetComponent<AmbianceSousMarine>();
        amb.Appliquer();
        amb.particules.Simulate(6f, true, true);
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        var t = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        File.WriteAllBytes(Path.Combine(dossier, nom + ".png"), t.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Debug.Log($"VUE {nom} profondeur={amb.Profondeur:F2} sousEau={amb.SousEau}");
    }
}
