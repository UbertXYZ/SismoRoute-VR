using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Esta clase vive en una carpeta "Editor": solo funciona dentro del Editor de Unity
// (no entra al build). Crea el menu "SismoRoute" arriba.
public static class GeneradorNPC
{
    const string Base = "Assets/Advance/Josue/NPC";

    // ---------- Utilidades ----------
    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }

    static Material CrearMaterial(string nombre, Color color)
    {
        AsegurarCarpeta(Base + "/Materiales");
        string ruta = Base + "/Materiales/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); // el proyecto usa URP
        m.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static GameObject Parte(PrimitiveType tipo, string nombre, Transform padre,
                            Vector3 pos, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // sin colision: no empuja al jugador VR
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Transform CrearBrazo(string nombre, Transform padre, float x, Material mat)
    {
        // El "pivote" esta en el hombro; la capsula cuelga debajo. Al girar el pivote, gira el brazo.
        var pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = new Vector3(x, 1.4f, 0f);
        Parte(PrimitiveType.Capsule, "Brazo", pivote.transform,
              new Vector3(0f, -0.3f, 0f), new Vector3(0.12f, 0.3f, 0.12f), mat);
        return pivote.transform;
    }

    // ---------- Menu 1: prefab del NPC ----------
    [MenuItem("SismoRoute/1 - Crear prefab NPC")]
    public static GameObject CrearPrefabNPC()
    {
        var ropa = CrearMaterial("NPC_Ropa", new Color(0.2f, 0.5f, 0.9f));
        var piel = CrearMaterial("NPC_Piel", new Color(0.9f, 0.72f, 0.58f));
        var pantalon = CrearMaterial("NPC_Pantalon", new Color(0.15f, 0.15f, 0.2f));

        var raiz = new GameObject("NPCPersona");        // el pivote de la raiz esta en los PIES
        var visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        Parte(PrimitiveType.Capsule, "Piernas", visual.transform, new Vector3(0, 0.15f, 0), new Vector3(0.3f, 0.15f, 0.3f), pantalon);
        var cuerpo = Parte(PrimitiveType.Capsule, "Cuerpo", visual.transform, new Vector3(0, 0.9f, 0), new Vector3(0.45f, 0.6f, 0.45f), ropa);
        Parte(PrimitiveType.Sphere, "Cabeza", visual.transform, new Vector3(0, 1.7f, 0), Vector3.one * 0.3f, piel);
        var izq = CrearBrazo("BrazoIzq", visual.transform, -0.3f, ropa);
        var der = CrearBrazo("BrazoDer", visual.transform, 0.3f, ropa);

        var npc = raiz.AddComponent<NPCPersona>();
        npc.visual = visual.transform;
        npc.brazoIzq = izq;
        npc.brazoDer = der;
        npc.rendererCuerpo = cuerpo.GetComponent<Renderer>();

        AsegurarCarpeta(Base + "/Prefabs");
        string ruta = Base + "/Prefabs/NPCPersona.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Prefab creado: " + ruta);
        return prefab;
    }

    // ---------- Menu 2: escena de prueba ----------
    [MenuItem("SismoRoute/2 - Crear escena de prueba NPC")]
    public static void CrearEscenaPrueba()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Prefabs/NPCPersona.prefab");
        if (prefab == null) prefab = CrearPrefabNPC();

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camara en vista aerea para ver todo.
        Camera.main.transform.position = new Vector3(-14f, 12f, -12f);
        Camera.main.transform.LookAt(new Vector3(0f, 0f, 2f));

        // Suelo (un Plane de 3x3 mide 30x30 m).
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "Suelo";
        suelo.transform.localScale = new Vector3(3f, 1f, 3f);
        suelo.GetComponent<Renderer>().sharedMaterial = CrearMaterial("NPC_Suelo", new Color(0.4f, 0.4f, 0.4f));

        // Salida (marcador verde, sin colision).
        var salida = GameObject.CreatePrimitive(PrimitiveType.Cube);
        salida.name = "Salida";
        Object.DestroyImmediate(salida.GetComponent<Collider>());
        salida.transform.position = new Vector3(0f, 1f, 12.5f);
        salida.transform.localScale = new Vector3(3f, 2f, 0.3f);
        salida.GetComponent<Renderer>().sharedMaterial = CrearMaterial("NPC_Salida", new Color(0.1f, 0.8f, 0.2f));

        // Dos rutas: principal (recta por el centro) y alterna (rodea por la derecha).
        var principal = CrearRuta("RutaPrincipal", Color.cyan,
            new Vector3(0, 0, -3), new Vector3(0, 0, 3), new Vector3(0, 0, 8), new Vector3(0, 0, 12));
        var alterna = CrearRuta("RutaAlterna", Color.green,
            new Vector3(6, 0, -4), new Vector3(6, 0, 4), new Vector3(6, 0, 9), new Vector3(0, 0, 12));

        // Zonas (empiezan APAGADAS; las enciendes con las teclas).
        var bloqueo = CrearZona("Bloqueo", TipoZona.Bloqueo, new Vector3(0, 0, 5), 1.8f, new Color(0.5f, 0.35f, 0.2f));
        var fuego = CrearZona("Fuego", TipoZona.Fuego, new Vector3(6, 0, 4), 2f, new Color(1f, 0.3f, 0.05f));
        var multitud = CrearZona("Aglomeracion", TipoZona.Aglomeracion, new Vector3(0, 0, -1), 2.5f, new Color(0.8f, 0.2f, 0.8f));

        // 6 NPC al inicio del pasillo.
        for (int i = 0; i < 6; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "NPC_" + i;
            go.transform.position = new Vector3(-5f + i * 2f, 0f, -9f);
            var npc = go.GetComponent<NPCPersona>();
            npc.rutaPrincipal = principal;
            npc.rutaAlterna = alterna;
        }

        // Probador de eventos con teclado.
        var pg = new GameObject("ProbadorEventos");
        var pe = pg.AddComponent<ProbadorEventos>();
        pe.bloqueo = bloqueo; pe.fuego = fuego; pe.aglomeracion = multitud;

        AsegurarCarpeta(Base + "/Escenas");
        string ruta = Base + "/Escenas/PruebaNPC.unity";
        EditorSceneManager.SaveScene(escena, ruta);
        Debug.Log("Escena creada: " + ruta + "  -> presiona Play");
    }

    static RutaEvacuacion CrearRuta(string nombre, Color color, params Vector3[] puntos)
    {
        var go = new GameObject(nombre);
        var r = go.AddComponent<RutaEvacuacion>();
        r.colorGizmo = color;
        for (int i = 0; i < puntos.Length; i++)
        {
            var p = new GameObject("Punto_" + i);
            p.transform.SetParent(go.transform, false);
            p.transform.position = puntos[i];
        }
        return r;
    }

    static GameObject CrearZona(string nombre, TipoZona tipo, Vector3 pos, float radio, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = pos + Vector3.up * 0.25f;
        go.transform.localScale = new Vector3(radio * 2f, 0.25f, radio * 2f);
        go.GetComponent<Renderer>().sharedMaterial = CrearMaterial("NPC_Zona_" + nombre, color);
        var z = go.AddComponent<ZonaPeligro>();
        z.tipo = tipo; z.radio = radio;
        go.SetActive(false);
        return go;
    }
}