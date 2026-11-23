using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UtilEditorJosue;

// Crea Assets/Advance/Josue/Escenas/PruebaExtintorSenales.unity: un cuarto sencillo con
// el fuego de Jheeremy (que se enciende solo), estaciones y extintores sueltos, y todas las senales en fila.
// Usa el XR Origin y el XR Device Simulator de los Samples para poder probar sin visor.
public static class EscenaPruebaJosue
{
    const string RutaEscena = Raiz + "/Escenas/PruebaExtintorSenales.unity";
    const string RigRuta = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    const string SimuladorRuta = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab";

    [MenuItem("SismoRoute/Josue/2 - Crear escena de prueba (extintor + fuego + senales)")]
    public static void Crear()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Prefabs (se crean si aun no existen)
        var extintor = AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorExtintorJosue.RutaExtintor);
        var estacion = AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorExtintorJosue.RutaEstacion);
        if (extintor == null || estacion == null) { GeneradorExtintorJosue.CrearTodo(); extintor = AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorExtintorJosue.RutaExtintor); estacion = AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorExtintorJosue.RutaEstacion); }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorSenalesJosue.RutaPrefab(GeneradorSenalesJosue.Forma.Salida, true)) == null)
            GeneradorSenalesJosue.CrearTodas();
        var fuego = AssetDatabase.LoadAssetAtPath<GameObject>(FuegoPrefabJheeremy);
        if (fuego == null) { EditorUtility.DisplayDialog("Escena de prueba", "No se encontro " + FuegoPrefabJheeremy + ". Haz pull de main.", "OK"); return; }

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        string mats = Raiz + "/Escenas/Materiales";
        var matSuelo = CrearMat(mats, "Prueba_Suelo", new Color(0.45f, 0.45f, 0.47f));
        var matPared = CrearMat(mats, "Prueba_Pared", new Color(0.82f, 0.8f, 0.76f));

        // Suelo y paredes (con collider: el jugador camina y las senales/estaciones se pegan)
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "Suelo"; suelo.transform.position = new Vector3(0, -0.05f, 1f); suelo.transform.localScale = new Vector3(14f, 0.1f, 26f);
        suelo.GetComponent<Renderer>().sharedMaterial = matSuelo;
        Pared("Pared_Izquierda", new Vector3(-7f, 2f, 1f), new Vector3(0.2f, 4f, 26f), matPared);
        Pared("Pared_Derecha", new Vector3(7f, 2f, 1f), new Vector3(0.2f, 4f, 26f), matPared);
        Pared("Pared_Fondo", new Vector3(0, 2f, 14f), new Vector3(14f, 4f, 0.2f), matPared);

        // Jugador: XR Origin + simulador de dispositivos
        var camaraVieja = GameObject.Find("Main Camera");
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigRuta);
        if (rig != null)
        {
            if (camaraVieja != null) Object.DestroyImmediate(camaraVieja);
            var r = (GameObject)PrefabUtility.InstantiatePrefab(rig);
            r.transform.SetPositionAndRotation(new Vector3(0, 0, -9f), Quaternion.identity);
            var sim = AssetDatabase.LoadAssetAtPath<GameObject>(SimuladorRuta);
            if (sim != null) PrefabUtility.InstantiatePrefab(sim);
        }
        else
        {
            Debug.LogWarning("[Josue] No se encontro el XR Origin de los Samples; se deja la camara normal (no podras agarrar con VR).");
            if (camaraVieja != null) camaraVieja.transform.position = new Vector3(0, 1.6f, -9f);
        }

        // Fuego de Jheeremy: se enciende solo a los 3 s, ahora con FuegoApagable
        var f = (GameObject)PrefabUtility.InstantiatePrefab(fuego);
        f.name = "Fuego_Prueba";
        f.transform.SetPositionAndRotation(new Vector3(0f, 0.283f, 6f), Quaternion.Euler(0, 90f, 0));
        var fa = f.GetComponent<FuegoAlto>();
        fa.activarAlIniciar = true; fa.retardo = 3f;
        f.AddComponent<FuegoApagable>().sonidoApagado = AssetDatabase.LoadAssetAtPath<AudioClip>(ClicJheeremy);

        // Estaciones de extintor pegadas a la pared izquierda (frente hacia +X) y un extintor suelto
        Colocar(estacion, new Vector3(-6.88f, 0f, -4f), Quaternion.LookRotation(Vector3.left));
        Colocar(estacion, new Vector3(-6.88f, 0f, 3f), Quaternion.LookRotation(Vector3.left));
        Colocar(extintor, new Vector3(1.5f, 0.02f, -6f), Quaternion.identity);

        // Senales: en la pared derecha (_Pared) y con poste delante (_Poste)
        int i = 0;
        foreach (GeneradorSenalesJosue.Forma forma in System.Enum.GetValues(typeof(GeneradorSenalesJosue.Forma)))
        {
            float z = -8f + i * 2.1f;
            var pared = AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorSenalesJosue.RutaPrefab(forma, true));
            var poste = AssetDatabase.LoadAssetAtPath<GameObject>(GeneradorSenalesJosue.RutaPrefab(forma, false));
            Colocar(pared, new Vector3(6.88f, 2f, z), Quaternion.LookRotation(Vector3.right));   // +Z hacia la pared, frente (-Z) hacia el cuarto
            Colocar(poste, new Vector3(4.5f, 0f, z), Quaternion.LookRotation(Vector3.right));
            i++;
        }

        new GameObject("AyudaPrueba").AddComponent<AyudaPruebaExtintor>();

        AsegurarCarpeta(Raiz + "/Escenas");
        EditorSceneManager.SaveScene(escena, RutaEscena);
        AssetDatabase.SaveAssets();
        Debug.Log("[Josue] Escena de prueba lista: " + RutaEscena + " -> presiona Play (el fuego aparece a los 3 s).");
    }

    static void Pared(string nombre, Vector3 pos, Vector3 escala, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nombre; g.transform.position = pos; g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static GameObject Colocar(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }
}
