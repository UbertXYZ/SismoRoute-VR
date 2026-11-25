using UnityEditor;
using UnityEngine;
using static UtilEditorJosue;

// Menu: SismoRoute > Josue > Senales. Crea los prefabs de las 5 senales oficiales usando las imagenes de
// Assets/Advance/Josue/Senales/Texturas (Senal_<Nombre>.png), en dos versiones:
//   Senal_<Nombre>_Pared : solo el cartel (para pegar en una pared). Pivote = centro del cartel.
//   Senal_<Nombre>_Poste : cartel + poste (para clavar en el suelo). Pivote = pie del poste.
// El frente (la imagen) mira hacia el lado -Z: la flecha azul del gizmo debe apuntar HACIA la pared.
public static class GeneradorSenalesJosue
{
    const string Base = Raiz + "/Senales";
    const string Mats = Base + "/Materiales";
    const string Texturas = Base + "/Texturas";
    const string Prefabs = Base + "/Prefabs";
    const float AlturaPoste = 2.0f;   // altura del centro del cartel en la version con poste
    const float AltoCartel = 0.5f;    // alto del cartel en metros (el ancho sale de la proporcion de la imagen)

    public enum Forma { Botiquin, Extintor, RiesgoElectrico, ZonaSeguraSismos, Salida }

    [MenuItem("SismoRoute/Josue/Senales/1 - Crear prefabs de senales")]
    public static void CrearTodas()
    {
        int n = 0;
        foreach (Forma f in System.Enum.GetValues(typeof(Forma)))
        {
            if (Crear(f, true) != null) n++;
            if (Crear(f, false) != null) n++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Josue] " + n + " prefabs de senales creados en " + Prefabs + " (arrastralos a la escena o usa SismoRoute > Josue > Colocar prefab seleccionado...).");
    }

    public static string RutaPrefab(Forma f, bool pared) => Prefabs + "/Senal_" + f + (pared ? "_Pared" : "_Poste") + ".prefab";
    static string RutaTextura(Forma f) => Texturas + "/Senal_" + f + ".png";

    static Material MaterialDe(Forma f, Texture2D tex)
    {
        AsegurarCarpeta(Mats);
        string ruta = Mats + "/Senal_" + f + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;

        // Sin iluminacion (Unlit): la senal se ve con los colores de la imagen aunque el pasillo este oscuro.
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.mainTexture = tex;
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static GameObject Crear(Forma forma, bool pared)
    {
        string rutaTex = RutaTextura(forma);
        AssetDatabase.ImportAsset(rutaTex, ImportAssetOptions.ForceSynchronousImport);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
        if (tex == null)
        {
            Debug.LogError("[Josue] Falta la imagen " + rutaTex + "; no se crea la senal " + forma + ".");
            return null;
        }

        float ancho = AltoCartel * tex.width / tex.height;
        var raiz = new GameObject("Senal_" + forma + (pared ? "_Pared" : "_Poste"));
        float y = pared ? 0f : AlturaPoste;

        if (!pared)
            Pieza(PrimitiveType.Cylinder, "Poste", raiz.transform, new Vector3(0, y / 2f, 0.04f),
                  new Vector3(0.05f, y / 2f, 0.05f), CrearMat(Mats, "SenalGris", new Color(0.35f, 0.35f, 0.35f)), null, true);

        // La imagen queda en la cara -Z (el frente). Se gira 180 grados sobre Z (el eje de frente) porque
        // el cubo la mostraba boca abajo; asi se ve derecha y el frente sigue siendo -Z.
        Pieza(PrimitiveType.Cube, "Cartel", raiz.transform, new Vector3(0, y, 0), new Vector3(ancho, AltoCartel, 0.02f),
              MaterialDe(forma, tex), new Vector3(0f, 0f, 180f));
        return GuardarPrefab(raiz, RutaPrefab(forma, pared));
    }
}
