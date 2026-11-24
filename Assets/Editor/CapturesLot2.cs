using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CapturesLot2
{
    public static void ConstruireEtCapturer()
    {
        ConstruireLot2.Construire();
        Capturer();
    }

    public static void Capturer()
    {
        string[] args = Environment.GetCommandLineArgs();
        string dossier = args[Array.IndexOf(args, "-captures") + 1];
        Directory.CreateDirectory(dossier);
        EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        ShaderUtil.allowAsyncCompilation = false;
        Func<float, float, float> F = ConstruireLot1.Fond;
        CapturesLot1.Vue(dossier, "1_parc", new Vector3(-190, 35, -330), new Vector3(0, 35, 0));
        CapturesLot1.Vue(dossier, "2_eolienne", new Vector3(-100, 25, -90), new Vector3(-10, 25, 0));
        CapturesLot1.Vue(dossier, "3_plateforme", new Vector3(-72, 12, -18), new Vector3(5, 33, 0));
        CapturesLot1.Vue(dossier, "4_monopieu", new Vector3(-50, F(-50, -14) + 5, -14), new Vector3(0, -35, 0));
        CapturesLot1.Vue(dossier, "5_treillis", new Vector3(128, -22, -22), new Vector3(5, -40, 0));
        CapturesLot1.Vue(dossier, "6_cable", new Vector3(-48, F(-48, 5) + 2.5f, 5), new Vector3(18, 5, 0));
        Debug.Log("CAPTURES2 OK rotors=" + UnityEngine.Object.FindObjectsByType<RotorEolienne>().Length);
    }
}
