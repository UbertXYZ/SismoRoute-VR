using UnityEditor;
using UnityEngine;

// Utilidades compartidas por los generadores de Josue (solo Editor).
// Todo lo que se crea queda dentro de Assets/Advance/Josue/ (regla del equipo).
public static class UtilEditorJosue
{
    public const string Raiz = "Assets/Advance/Josue";
    public const string FuegoPrefabJheeremy = "Assets/Advance/Jheeremy/Prefabs/fuego.prefab";
    public const string ClicJheeremy = "Assets/Advance/Jheeremy/Video/click-sound-effect_CGa27ayq.mp3";

    public static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }

    // Material URP/Lit guardado en la carpeta; si ya existe se reutiliza.
    public static Material CrearMat(string carpeta, string nombre, Color color, float metalico = 0f, float brillo = 0.35f)
    {
        AsegurarCarpeta(carpeta);
        string ruta = carpeta + "/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    // Pieza primitiva (sin collider por defecto: asi no estorba al jugador VR).
    public static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala,
                                   Material mat, Vector3? rotacion = null, bool conCollider = false)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        if (rotacion.HasValue) go.transform.localRotation = Quaternion.Euler(rotacion.Value);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!conCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Guarda el objeto de la escena como prefab y lo destruye de la escena.
    public static GameObject GuardarPrefab(GameObject raiz, string ruta)
    {
        AsegurarCarpeta(System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/'));
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        return prefab;
    }
}
