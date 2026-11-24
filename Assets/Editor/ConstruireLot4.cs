using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu BLUE ROV : ajoute physique et pilotage au BlueROV2 (construit au besoin).
public static class ConstruireLot4
{
    [MenuItem("BLUE ROV/Lot 4 - Activer le pilotage")]
    private static void Menu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("BLUE ROV", "Arrete d'abord le mode Play.", "OK");
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Construire();
    }

    public static void Construire()
    {
        EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        GameObject rov = GameObject.Find("BlueROV2");
        if (rov == null)
        {
            ConstruireLot3.Construire();
            rov = GameObject.Find("BlueROV2");
        }
        if (rov.GetComponent<Rigidbody>() == null) rov.AddComponent<Rigidbody>().useGravity = false;
        if (rov.GetComponent<PilotageROV>() == null) rov.AddComponent<PilotageROV>();
        var scene = rov.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LOT4 OK");
    }
}
