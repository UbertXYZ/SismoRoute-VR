using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "SismoRoute/Bloqueos (Josue)". Solo existe en el Editor.
// Todo lo que crea queda dentro de Assets/Advance/Josue (regla del equipo).
public static class MenuBloqueosJosue
{
    const string EscenaPrincipal = "Assets/Advance/Uberto/Prueba01.unity";
    const string CarpetaEscenas = "Assets/Advance/Josue/Bloqueos/Escenas";
    const string EscenaPrueba = CarpetaEscenas + "/PruebaBloqueos.unity";

    [MenuItem("SismoRoute/Bloqueos (Josue)/1 - Crear escena de prueba (copia de Prueba01)")]
    public static void CrearEscenaPrueba()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaPrueba) != null &&
            !EditorUtility.DisplayDialog("Escena de prueba", "Ya existe " + EscenaPrueba + ". ¿Reemplazarla con una copia nueva de Prueba01?", "Reemplazar", "Solo abrirla"))
        {
            EditorSceneManager.OpenScene(EscenaPrueba);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaPrincipal) == null)
        {
            EditorUtility.DisplayDialog("Escena de prueba", "No se encontro " + EscenaPrincipal + ". Haz pull de main.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaEscenas))
            AssetDatabase.CreateFolder("Assets/Advance/Josue/Bloqueos", "Escenas");

        AssetDatabase.DeleteAsset(EscenaPrueba);
        AssetDatabase.DeleteAsset(CarpetaEscenas + "/PruebaBloqueos"); // NavMesh viejo de la copia anterior
        AssetDatabase.CopyAsset(EscenaPrincipal, EscenaPrueba); // copia: la escena de Uberto no se toca
        var escena = EditorSceneManager.OpenScene(EscenaPrueba);
        // Desde que Uberto guarda el edificio y su NavMesh dentro de Prueba01, no hace falta regenerarlos:
        // solo se hace si el edificio de la copia esta vacio.
        if (!EdificioYaGenerado()) GenerarEdificioYNavMesh();
        AgregarGenerador();
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Bloqueos Josue] Escena de prueba lista: " + EscenaPrueba + " -> presiona Play.");
    }

    static bool EdificioYaGenerado()
    {
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb != null && mb.GetType().Name == "PCGPabellonUniversitario") return mb.transform.childCount > 0;
        return false;
    }

    // (Antes) Prueba01 en main NO tenia guardado el edificio: el PCG de Uberto esta vacio
    // (pisosGenerados = None) y solo quedo el NavMesh horneado. Por eso el jugador caia
    // y los NPC caminaban en el aire. Aqui se genera el edificio en la COPIA y se vuelve
    // a hornear el NavMesh, guardandolo dentro de la carpeta de Josue.
    [MenuItem("SismoRoute/Bloqueos (Josue)/4 - Generar edificio + NavMesh en la escena abierta")]
    public static void GenerarEdificioYNavMesh()
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (!escena.path.StartsWith("Assets/Advance/Josue/"))
        {
            EditorUtility.DisplayDialog("Bloqueos Josue",
                "Solo se hace en escenas dentro de Assets/Advance/Josue (para no modificar escenas de otros).\n" +
                "Usa primero la opcion 1 - Crear escena de prueba.", "OK");
            return;
        }

        // OJO: no se escribe el tipo PCGPabellonUniversitario directamente porque en main existe
        // tambien dentro de Uberto/Scripts/BloqueHabitacion.dll y el Editor lo ve "duplicado" (CS0433).
        // Se busca por nombre y se llama GenerarEdificio() por reflexion.
        MonoBehaviour pcg = null;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb != null && mb.GetType().Name == "PCGPabellonUniversitario") { pcg = mb; break; }

        if (pcg == null)
            Debug.LogWarning("[Bloqueos Josue] No hay PCGPabellonUniversitario en la escena.");
        else
        {
            EditorUtility.DisplayProgressBar("Bloqueos Josue", "Generando el edificio de Uberto...", 0.3f);
            var metodo = pcg.GetType().GetMethod("GenerarEdificio");
            if (metodo == null)
                Debug.LogError("[Bloqueos Josue] PCGPabellonUniversitario no tiene GenerarEdificio().");
            else
            {
                metodo.Invoke(pcg, null);
                Debug.Log("[Bloqueos Josue] Edificio generado (" + pcg.transform.childCount + " objetos bajo '" + pcg.name + "').");
            }
        }

        try
        {
            string carpetaNav = CarpetaEscenas + "/PruebaBloqueos";
            if (!AssetDatabase.IsValidFolder(carpetaNav))
                AssetDatabase.CreateFolder(CarpetaEscenas, "PruebaBloqueos");

            var superficies = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
            for (int i = 0; i < superficies.Length; i++)
            {
                var s = superficies[i];
                EditorUtility.DisplayProgressBar("Bloqueos Josue", "Horneando NavMesh (" + s.name + ")...", 0.5f + 0.5f * i / superficies.Length);
                s.navMeshData = null;  // no tocar el asset de NavMesh de Uberto
                s.BuildNavMesh();
                if (s.navMeshData != null)
                {
                    AssetDatabase.CreateAsset(s.navMeshData, carpetaNav + "/NavMesh-" + s.name + "-" + i + ".asset");
                    EditorUtility.SetDirty(s);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Bloqueos Josue] NavMesh horneado de nuevo (" + superficies.Length + " superficie/s).");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    [MenuItem("SismoRoute/Bloqueos (Josue)/2 - Agregar generador a la escena abierta")]
    public static void AgregarGenerador()
    {
        var existente = Object.FindFirstObjectByType<GeneradorBloqueosAleatorios>();
        if (existente != null)
        {
            Selection.activeObject = existente.gameObject;
            Debug.Log("[Bloqueos Josue] La escena ya tiene un GeneradorBloqueosAleatorios.");
            return;
        }

        var go = new GameObject("GeneradorBloqueosJosue");
        Undo.RegisterCreatedObjectUndo(go, "Crear generador de bloqueos");
        var g = go.AddComponent<GeneradorBloqueosAleatorios>();
        g.prefabFuego = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Jheeremy/Prefabs/fuego.prefab");
        g.prefabRocas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Jheeremy/Prefabs/bloqueo.prefab");
        if (g.prefabFuego == null || g.prefabRocas == null)
            Debug.LogWarning("[Bloqueos Josue] No se encontraron los prefabs de Jheeremy (fuego / bloqueo). Asignalos a mano.");

        Selection.activeObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    [MenuItem("SismoRoute/Bloqueos (Josue)/3 - Agregar PuntoBloqueo (opcional)")]
    public static void AgregarPunto()
    {
        var go = new GameObject("PuntoBloqueo");
        Undo.RegisterCreatedObjectUndo(go, "Crear PuntoBloqueo");
        go.AddComponent<PuntoBloqueo>();
        if (SceneView.lastActiveSceneView != null)
            go.transform.position = SceneView.lastActiveSceneView.pivot;
        Selection.activeObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
