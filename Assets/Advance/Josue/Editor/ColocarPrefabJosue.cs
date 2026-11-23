using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Pone el prefab seleccionado (en la ventana Project) justo donde mira la camara de la vista Scene,
// girado para que su FRENTE (lado -Z, donde se lee el texto) quede hacia ti, pegado a la pared.
// Necesita que la pared/suelo tengan Collider (los pasillos del edificio de Uberto ya lo tienen).
public static class ColocarPrefabJosue
{
    [MenuItem("SismoRoute/Josue/Colocar prefab seleccionado donde mira la vista Scene")]
    public static void Colocar()
    {
        var prefab = Selection.activeObject as GameObject;
        if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            EditorUtility.DisplayDialog("Colocar prefab",
                "Primero selecciona un PREFAB en la ventana Project\n(por ejemplo Josue/Senales/Prefabs/Senal_Salida_Pared).", "OK");
            return;
        }

        var vista = SceneView.lastActiveSceneView;
        if (vista == null) { Debug.LogWarning("[Josue] Abre la vista Scene para poder apuntar."); return; }

        Transform cam = vista.camera.transform;
        if (!Physics.Raycast(new Ray(cam.position, cam.forward), out RaycastHit golpe, 80f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("[Josue] La mira de la vista Scene no toca ningun Collider. Acercate a una pared o al suelo y vuelve a intentar.");
            return;
        }

        bool esPared = Mathf.Abs(golpe.normal.y) < 0.5f;
        Vector3 pos = golpe.point;
        Quaternion rot;
        if (esPared)
        {
            rot = Quaternion.LookRotation(-golpe.normal);     // +Z hacia la pared -> el frente (-Z) mira hacia afuera
            pos += golpe.normal * 0.02f;
        }
        else
        {
            Vector3 plano = cam.forward; plano.y = 0f;
            rot = Quaternion.LookRotation(plano.sqrMagnitude > 0.001f ? plano.normalized : Vector3.forward);
        }

        // Los prefabs con pivote al piso (estacion, poste) deben apoyarse en el suelo, no flotar a la altura de la mira.
        bool pivoteEnSuelo = prefab.name.StartsWith("EstacionExtintor") || prefab.name.EndsWith("_Poste") || prefab.name == "Extintor";
        if (esPared && pivoteEnSuelo &&
            Physics.Raycast(pos + golpe.normal * 0.3f + Vector3.up * 0.1f, Vector3.down, out RaycastHit suelo, 6f, ~0, QueryTriggerInteraction.Ignore))
            pos.y = suelo.point.y;
        // Las senales "_Pared" tienen el pivote en el centro del panel: si apuntaste al suelo, se suben a altura de vista.
        if (!esPared && prefab.name.EndsWith("_Pared")) pos.y += 1.6f;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, EditorSceneManager.GetActiveScene());
        Undo.RegisterCreatedObjectUndo(go, "Colocar " + prefab.name);
        go.transform.SetPositionAndRotation(pos, rot);
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    [MenuItem("SismoRoute/Josue/Colocar prefab seleccionado donde mira la vista Scene", true)]
    static bool ValidarColocar() => Selection.activeObject is GameObject;
}
