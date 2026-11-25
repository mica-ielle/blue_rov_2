using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ajoute un ponton, sa rampe de mise a l'eau et l'ombilical raccorde au treuil.
public static class ConstruireLot5
{
    private const string RootName = "PlateformeLancement";
    private static readonly Vector3 Origin = new Vector3(-125f, 0f, -118f);
    private static readonly Vector3 RampTop = new Vector3(0f, .95f, .6f);
    private static readonly Vector3 RampEnd = new Vector3(0f, -6.2f, 18f);
    private const float RampWidth = 3.6f;
    // Met à niveau une scène Lot 5 ancienne après l'import des nouveaux scripts.
    [InitializeOnLoadMethod]
    private static void SurveillerSceneChargee()
    {
        EditorApplication.update -= VerifierSceneChargee;
        EditorApplication.update += VerifierSceneChargee;
    }

    private static void VerifierSceneChargee()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != ConstruireLot1.CheminScene) return;
        GameObject rov = GameObject.Find("BlueROV2");
        if (rov == null) return;
        bool ancienModele = GameObject.Find("Modele_BlueROV2_ROS") == null;
        bool incomplet = rov.GetComponent<Rigidbody>() == null
            || rov.GetComponent<PilotageROV>() == null
            || GameObject.Find("Ombilical_BlueROV_Treuil")?.GetComponent<OmbilicalDynamique>() == null
            || ancienModele;
        if (!incomplet)
        {
            EditorApplication.update -= VerifierSceneChargee;
            return;
        }
        EditorApplication.update -= VerifierSceneChargee;
        if (active.isDirty) EditorSceneManager.SaveScene(active);
        if (ancienModele) ConstruireLot3.Construire();
        ConstruireLot4.Construire();
        Build();
    }
    [MenuItem("BLUE ROV/Lot 5 - Ajouter la rampe de lancement")]
    private static void Menu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
    }

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ConstruireLot1.CheminScene);
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == RootName) Object.DestroyImmediate(go);

        GameObject rov = GameObject.Find("BlueROV2");
        if (rov == null) throw new System.Exception("Construis d'abord le BlueROV2 avec le menu BLUE ROV > Lot 3.");

        // Lot 5 doit rester pilotable même si l'utilisateur n'a pas lancé le lot 4.
        Rigidbody corps = rov.GetComponent<Rigidbody>();
        if (corps == null) corps = rov.AddComponent<Rigidbody>();
        corps.useGravity = false;
        corps.isKinematic = false;
        if (rov.GetComponent<PilotageROV>() == null) rov.AddComponent<PilotageROV>();

        Transform root = new GameObject(RootName).transform;
        root.position = Origin;
        Material navy = Mat("Plateforme_Bleu", new Color(.055f, .19f, .25f), .62f, .55f);
        Material steel = Mat("Plateforme_Acier", new Color(.33f, .39f, .40f), .42f, .72f);
        Material grating = Mat("Plateforme_Grille", new Color(.105f, .15f, .17f), .3f, .48f);
        Material yellow = Mat("Plateforme_Securite", new Color(1f, .57f, .055f), .32f, .12f);
        Material tether = Mat("ROV_Ombilical", new Color(1f, .69f, .06f), .3f, .08f);

        // Ponton flottant.
        Beam("Flotteur_Gauche", new Vector3(-3.3f,.35f,-8f), new Vector3(-3.3f,.35f,1.4f), .52f, navy, root);
        Beam("Flotteur_Droit", new Vector3(3.3f,.35f,-8f), new Vector3(3.3f,.35f,1.4f), .52f, navy, root);
        Box("Pont", new Vector3(0,1.05f,-3.35f), new Vector3(8.5f,.38f,9f), Quaternion.identity, grating, root);
        Box("Surface_grille", new Vector3(0,1.26f,-3.35f), new Vector3(7.9f,.06f,8.35f), Quaternion.identity, steel, root, false);

        // Garde-corps jaunes autour du pont, sauf l'ouverture de la rampe.
        Beam("Garde_arriere", new Vector3(-4.05f,2.55f,-7.75f), new Vector3(4.05f,2.55f,-7.75f), .085f, yellow, root);
        Beam("Garde_arriere_bas", new Vector3(-4.05f,1.72f,-7.75f), new Vector3(4.05f,1.72f,-7.75f), .055f, steel, root);
        for (int i=0;i<=4;i++)
        {
            float x=Mathf.Lerp(-4.05f,4.05f,i/4f);
            Beam("Poteau_arriere_"+i,new Vector3(x,1.35f,-7.75f),new Vector3(x,2.55f,-7.75f),.055f,yellow,root);
        }
        for (int s=-1;s<=1;s+=2)
        {
            float x=s*4.05f;
            Beam("Garde_lateral_"+s,new Vector3(x,2.48f,-7.75f),new Vector3(x,2.48f,.55f),.075f,yellow,root);
            Beam("Garde_lateral_bas_"+s,new Vector3(x,1.72f,-7.75f),new Vector3(x,1.72f,.55f),.05f,steel,root);
            for (int i=0;i<=4;i++)
            {
                float z=Mathf.Lerp(-7.75f,.55f,i/4f);
                Beam("Poteau_lateral_"+s+"_"+i,new Vector3(x,1.35f,z),new Vector3(x,2.48f,z),.05f,yellow,root);
            }
        }

        // Treuil, axe, tambour et guide-cable.
        Beam("Treuil_montant_G",new Vector3(-3.15f,1.35f,-6.45f),new Vector3(-3.15f,3.15f,-6.45f),.12f,steel,root);
        Beam("Treuil_montant_D",new Vector3(-1.85f,1.35f,-6.45f),new Vector3(-1.85f,3.15f,-6.45f),.12f,steel,root);
        Beam("Treuil_traverse",new Vector3(-3.15f,3.15f,-6.45f),new Vector3(-1.85f,3.15f,-6.45f),.14f,yellow,root);
        Beam("Tambour_treuil",new Vector3(-3f,2.2f,-6.45f),new Vector3(-2f,2.2f,-6.45f),.34f,navy,root);
        Beam("Axe_treuil",new Vector3(-3.35f,2.2f,-6.45f),new Vector3(-1.65f,2.2f,-6.45f),.09f,steel,root);
        Beam("Guide_ombilical",new Vector3(-2.5f,3.1f,-6.45f),new Vector3(-2.5f,2.7f,-6.45f),.075f,yellow,root);

        // Rampe immergee en caillebotis avec longerons, mains courantes et supports.
        Vector3 dir=(RampEnd-RampTop).normalized;
        Quaternion rampRot=Quaternion.LookRotation(dir,Vector3.up);
        Vector3 sideAxis=rampRot*Vector3.right;
        float rampLength=Vector3.Distance(RampTop,RampEnd);
        for (int i=0;i<13;i++)
        {
            Vector3 p=Vector3.Lerp(RampTop,RampEnd,(i+.5f)/13f);
            Box("Caillebotis_"+(i+1).ToString("00"),p,new Vector3(RampWidth,.16f,rampLength/13f+.04f),rampRot,grating);
        }
        for (int s=-1;s<=1;s+=2)
        {
            Vector3 off=sideAxis*(s*(RampWidth*.5f+.28f));
            Beam("Rail_rampe_"+s,RampTop+off+Vector3.up*.9f,RampEnd+off+Vector3.up*.9f,.12f,yellow,root);
            Beam("Longeron_rampe_"+s,RampTop+off,RampEnd+off,.16f,steel,root);
            for(int i=1;i<=5;i++)
            {
                Vector3 p=Vector3.Lerp(RampTop,RampEnd,i/6f)+off;
                Beam("Support_rampe_"+s+"_"+i,p,p-Vector3.up*(.75f+i*.12f),.075f,steel,root);
            }
        }
        for(int i=0;i<7;i++)
        {
            Vector3 p=Vector3.Lerp(RampTop,RampEnd,Mathf.Lerp(.12f,.88f,i/6f));
            Beam("Entretoise_"+i,p+sideAxis*RampWidth*.5f,p-sideAxis*RampWidth*.5f,.065f,yellow,root);
        }

        // Le ROV attend sur la partie immergée haute de la rampe, orienté vers la sortie.
        Vector3 normale = rampRot * Vector3.up;
        const float departRampe = 3.4f;
        Vector3 rovLocal = RampTop + dir * departRampe + normale * .43f;
        rov.transform.SetPositionAndRotation(root.TransformPoint(rovLocal),root.rotation*rampRot);
        Transform old=rov.transform.Find("Ombilical");
        if(old!=null) Object.DestroyImmediate(old.gameObject);
        Vector3 cradle=rovLocal-rampRot*Vector3.up*.19f;
        Box("Berceau_BlueROV",cradle,new Vector3(.78f,.12f,.92f),rampRot,navy,false);
        for(int s=-1;s<=1;s+=2)
            Box("Patin_"+s,cradle+rampRot*new Vector3(s*.35f,.12f,0),new Vector3(.09f,.22f,.75f),rampRot,yellow,false);

        // Connecteur de coque : l'extrémité mobile du câble est réellement enfant du ROV.
        var connecteur = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        connecteur.name = "Connecteur_Ombilical";
        connecteur.transform.SetParent(rov.transform, false);
        connecteur.transform.localPosition = new Vector3(0f, .045f, -.205f);
        connecteur.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        connecteur.transform.localScale = new Vector3(.052f, .035f, .052f);
        connecteur.GetComponent<MeshRenderer>().sharedMaterial = tether;
        Object.DestroyImmediate(connecteur.GetComponent<Collider>());
        var departCable = new GameObject("Attache_Cable_ROV").transform;
        departCable.SetParent(rov.transform, false);
        departCable.localPosition = new Vector3(0f, .045f, -.24f);

        // Le câble suit le guide extérieur de la rampe puis rejoint le treuil.
        var guideRampe = new GameObject("Guide_Cable_Rampe").transform;
        guideRampe.SetParent(root, false);
        guideRampe.localPosition = RampTop + dir * departRampe
            + sideAxis * (RampWidth * .5f + .48f) + normale * .78f;
        var guidePont = new GameObject("Guide_Cable_Ponton").transform;
        guidePont.SetParent(root, false);
        guidePont.localPosition = RampTop + sideAxis * (RampWidth * .5f + .48f) + normale * .78f;
        var ancrageTreuil = new GameObject("Ancrage_Cable_Treuil").transform;
        ancrageTreuil.SetParent(root, false);
        ancrageTreuil.localPosition = new Vector3(-2.5f, 2.72f, -6.45f);

        var cableObject = new GameObject("Ombilical_BlueROV_Treuil");
        cableObject.transform.SetParent(root, false);
        var ligne = cableObject.AddComponent<LineRenderer>();
        ligne.sharedMaterial = tether;
        ligne.startWidth = ligne.endWidth = .028f;
        ligne.startColor = ligne.endColor = Color.white;
        var cable = cableObject.AddComponent<OmbilicalDynamique>();
        cable.depart = departCable;
        cable.guideRampe = guideRampe;
        cable.guidePont = guidePont;
        cable.ancrageTreuil = ancrageTreuil;
        cable.rov = corps;

        // Deux projecteurs de pont donnent une lecture claire du ROV et de la rampe.
        for (int s = -1; s <= 1; s += 2)
        {
            var phare = new GameObject("Projecteur_Rampe_" + s).AddComponent<Light>();
            phare.transform.SetParent(root, false);
            phare.transform.localPosition = new Vector3(s * 3.65f, 2.5f, .1f);
            phare.transform.rotation = Quaternion.LookRotation(root.TransformDirection(dir));
            phare.type = LightType.Spot;
            phare.color = new Color(.60f, .86f, 1f);
            phare.intensity = 18f;
            phare.range = 26f;
            phare.spotAngle = 72f;
            phare.innerSpotAngle = 36f;
            phare.shadows = LightShadows.None;
        }

        // Aperçu cadré sur le ROV et la rampe.
        CameraExploration cam=Object.FindAnyObjectByType<CameraExploration>(FindObjectsInactive.Include);
        if(cam!=null)
        {
            Vector3 target=root.TransformPoint(new Vector3(0f,-2.6f,6.8f));
            cam.transform.position=root.TransformPoint(new Vector3(16f,12f,8.5f));
            cam.transform.rotation=Quaternion.LookRotation(target-cam.transform.position);
            Camera preview = cam.GetComponent<Camera>();
            if (preview != null) preview.fieldOfView = 58f;
            cam.Recaler();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LOT5 OK - rampe et ombilical raccordes au treuil.");
    }

    private static void Box(string name,Vector3 pos,Vector3 size,Quaternion rot,Material mat,Transform parent,bool collider=true)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localRotation=rot;o.transform.localScale=size;
        o.GetComponent<MeshRenderer>().sharedMaterial=mat;
        if(!collider) Object.DestroyImmediate(o.GetComponent<Collider>());
    }
    private static void Box(string name,Vector3 pos,Vector3 size,Quaternion rot,Material mat,bool collider=true)
        => Box(name,pos,size,rot,mat,GameObject.Find(RootName).transform,collider);
    private static void Beam(string name,Vector3 a,Vector3 b,float radius,Material mat,Transform parent)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        o.name=name;o.transform.SetParent(parent,false);
        o.transform.localPosition=(a+b)*.5f;
        o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        o.transform.localScale=new Vector3(radius*2f,Vector3.Distance(a,b)*.5f,radius*2f);
        o.GetComponent<MeshRenderer>().sharedMaterial=mat;
        Object.DestroyImmediate(o.GetComponent<Collider>());
    }
    private static Material Mat(string name,Color color,float smooth,float metallic)
    {
        Material m=ConstruireLot1.Materiau(name,"Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);
        return m;
    }
}


