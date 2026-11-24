using UnityEngine;
using UnityEngine.InputSystem;

// Phares (L) et vues (C) : poursuite, camera du ROV, camera libre.
public class EquipementsROV : MonoBehaviour
{
    public enum TypeVue { Poursuite, ROV, Libre }

    public Light[] phares;
    public Camera cameraROV;
    public Camera cameraLibre;
    public bool pharesAllumes = true;
    public Vector3 reculPoursuite = new Vector3(0f, 0.45f, -1.5f);

    public TypeVue Vue { get; private set; }
    public bool VueLibre => Vue == TypeVue.Libre;

    private void Start()
    {
        Phares(pharesAllumes);
        ChangerVue(GetComponent<PilotageROV>() != null ? TypeVue.Poursuite : TypeVue.Libre);
        Suivre(1f);
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.lKey.wasPressedThisFrame) Phares(!pharesAllumes);
        if (k.cKey.wasPressedThisFrame) ChangerVue((TypeVue)(((int)Vue + 1) % 3));
    }

    private void LateUpdate()
    {
        if (Vue == TypeVue.Poursuite) Suivre(1f - Mathf.Exp(-4f * Time.deltaTime));
    }

    private void Suivre(float t)
    {
        if (cameraLibre == null || Vue != TypeVue.Poursuite) return;
        Quaternion lacet = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 cible = transform.position, but = cible + lacet * reculPoursuite;
        // Ne pas laisser la camera traverser une structure.
        Vector3 d = but - cible;
        foreach (RaycastHit h in Physics.SphereCastAll(cible, 0.1f, d.normalized, d.magnitude))
            if (h.collider.transform.root != transform && h.distance > 0f)
                but = cible + d.normalized * Mathf.Min((but - cible).magnitude, h.distance);
        Transform c = cameraLibre.transform;
        c.position = Vector3.Lerp(c.position, but, t);
        c.rotation = Quaternion.Slerp(c.rotation, Quaternion.LookRotation(cible + lacet * new Vector3(0f, 0.1f, 0.6f) - c.position), t);
    }

    public void Phares(bool allumes)
    {
        pharesAllumes = allumes;
        foreach (Light l in phares) if (l != null) l.enabled = allumes;
    }

    public void ChangerVue(TypeVue vue)
    {
        if (cameraROV == null || cameraLibre == null) return;
        Vue = vue;
        bool rov = vue == TypeVue.ROV;
        Activer(cameraROV, rov);
        Activer(cameraLibre, !rov);
        var libre = cameraLibre.GetComponent<CameraExploration>();
        if (libre != null)
        {
            libre.enabled = VueLibre;
            if (VueLibre) libre.Recaler();
        }
        // Les particules suivent la camera utilisee.
        var a = cameraLibre.GetComponent<AmbianceSousMarine>();
        if (a != null && a.particules != null)
            a.particules.transform.SetParent((rov ? cameraROV : cameraLibre).transform, false);
        Suivre(1f);
    }

    private static void Activer(Camera c, bool actif)
    {
        c.enabled = actif;
        var a = c.GetComponent<AmbianceSousMarine>();
        if (a != null) a.enabled = actif;
    }

    private void OnGUI()
    {
        string nom = Vue == TypeVue.ROV ? "camera du ROV" : VueLibre ? "camera libre" : "poursuite";
        GUI.Box(new Rect(10, 108, 330, 46), GUIContent.none);
        GUI.Label(new Rect(20, 112, 310, 40),
            $"<b>BlueROV2</b> - vue : {nom} (C)\nPhares : {(pharesAllumes ? "allumes" : "eteints")} (L)");
    }
}
