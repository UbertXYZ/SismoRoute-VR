using TMPro;
using UnityEditor;
using UnityEngine;
using static UtilEditorJosue;

// Menu: SismoRoute > Josue > Senales. Crea un prefab por cada senal de evacuacion, en dos versiones:
//   *_Pared : solo el panel (para pegar en una pared). Pivote = centro del panel.
//   *_Poste : panel + poste (para clavar en el suelo). Pivote = pie del poste.
// El frente (donde se lee el texto) es el lado -Z: la flecha azul del gizmo debe apuntar HACIA la pared.
public static class GeneradorSenalesJosue
{
    const string Base = Raiz + "/Senales";
    const string Mats = Base + "/Materiales";
    const string Prefabs = Base + "/Prefabs";
    const float AlturaPoste = 2.0f;
    const float Z = -0.021f; // un poco delante del panel

    static readonly Color Verde = new Color(0f, 0.55f, 0.25f);
    static readonly Color Rojo = new Color(0.8f, 0.05f, 0.05f);
    static readonly Color Gris = new Color(0.35f, 0.35f, 0.35f);

    public enum Forma
    {
        Salida, RutaDerecha, RutaIzquierda, RutaArriba, RutaAbajo, ZonaSegura, PuntoReunion,
        PrimerosAuxilios, Extintor, NoUsarAscensor, EscaleraEmergencia
    }

    [MenuItem("SismoRoute/Josue/Senales/1 - Crear prefabs de senales")]
    public static void CrearTodas()
    {
        int n = 0;
        foreach (Forma f in System.Enum.GetValues(typeof(Forma)))
        {
            Crear(f, true);
            Crear(f, false);
            n += 2;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Josue] " + n + " prefabs de senales creados en " + Prefabs + " (arrastralos a la escena o usa SismoRoute > Josue > Senales > Colocar).");
    }

    public static string RutaPrefab(Forma f, bool pared) => Prefabs + "/Senal_" + f + (pared ? "_Pared" : "_Poste") + ".prefab";

    static GameObject Crear(Forma forma, bool pared)
    {
        var raiz = new GameObject("Senal_" + forma + (pared ? "_Pared" : "_Poste"));
        float y = pared ? 0f : AlturaPoste;          // altura del centro del panel respecto al pivote

        if (!pared)
            Pieza(PrimitiveType.Cylinder, "Poste", raiz.transform, new Vector3(0, y / 2f, 0.03f),
                  new Vector3(0.05f, y / 2f, 0.05f), CrearMat(Mats, "SenalGris", Gris), null, true);

        switch (forma)
        {
            case Forma.Salida:
                Panel(raiz, y, 0.6f, 0.25f, Verde, "SenalVerde");
                Texto(raiz, "SALIDA", new Vector3(0, y, Z), 0.55f, 0.2f);
                break;
            case Forma.RutaDerecha: Ruta(raiz, y, 0f); break;
            case Forma.RutaIzquierda: Ruta(raiz, y, 180f); break;
            case Forma.RutaArriba: Ruta(raiz, y, 90f); break;
            case Forma.RutaAbajo: Ruta(raiz, y, -90f); break;
            case Forma.ZonaSegura:
                Panel(raiz, y, 0.5f, 0.5f, Verde, "SenalVerde");
                Texto(raiz, "ZONA SEGURA\nEN CASO\nDE SISMO", new Vector3(0, y, Z), 0.44f, 0.44f);
                break;
            case Forma.PuntoReunion:
                Panel(raiz, y, 0.6f, 0.4f, Verde, "SenalVerde");
                Texto(raiz, "PUNTO DE\nREUNIÓN", new Vector3(0, y, Z), 0.54f, 0.34f);
                break;
            case Forma.PrimerosAuxilios:
                Panel(raiz, y, 0.4f, 0.4f, Verde, "SenalVerde");
                Pieza(PrimitiveType.Cube, "Cruz_H", raiz.transform, new Vector3(0, y, Z), new Vector3(0.24f, 0.08f, 0.01f), Blanco());
                Pieza(PrimitiveType.Cube, "Cruz_V", raiz.transform, new Vector3(0, y, Z), new Vector3(0.08f, 0.24f, 0.01f), Blanco());
                break;
            case Forma.Extintor:
                Panel(raiz, y, 0.5f, 0.3f, Rojo, "SenalRoja");
                Texto(raiz, "EXTINTOR", new Vector3(0, y, Z), 0.46f, 0.2f);
                break;
            case Forma.NoUsarAscensor:
                Panel(raiz, y, 0.5f, 0.35f, Rojo, "SenalRoja");
                Texto(raiz, "NO USAR\nASCENSOR", new Vector3(0, y, Z), 0.46f, 0.3f);
                break;
            case Forma.EscaleraEmergencia:
                Panel(raiz, y, 0.6f, 0.3f, Verde, "SenalVerde");
                Texto(raiz, "ESCALERA DE\nEMERGENCIA", new Vector3(0, y, Z), 0.54f, 0.26f);
                break;
        }
        return GuardarPrefab(raiz, RutaPrefab(forma, pared));
    }

    static Material Blanco() => CrearMat(Mats, "SenalBlanco", Color.white);

    static void Panel(GameObject raiz, float y, float ancho, float alto, Color color, string nombreMat)
    {
        // Con collider fino: asi el rayo del jugador/herramienta puede apuntarle.
        Pieza(PrimitiveType.Cube, "Panel", raiz.transform, new Vector3(0, y, 0), new Vector3(ancho, alto, 0.03f),
              CrearMat(Mats, nombreMat, color));
    }

    static void Ruta(GameObject raiz, float y, float anguloZ)
    {
        Panel(raiz, y, 0.7f, 0.3f, Verde, "SenalVerde");
        Flecha(raiz.transform, new Vector3(-0.22f, y, Z), 0.8f, anguloZ);
        Texto(raiz, "RUTA DE\nEVACUACIÓN", new Vector3(0.12f, y, Z), 0.42f, 0.26f);
    }

    static void Flecha(Transform padre, Vector3 centro, float s, float anguloZ)
    {
        var pivote = new GameObject("Flecha").transform;
        pivote.SetParent(padre, false);
        pivote.localPosition = centro;
        pivote.localRotation = Quaternion.Euler(0, 0, anguloZ);
        var b = Blanco();
        Pieza(PrimitiveType.Cube, "Cuerpo", pivote, Vector3.zero, new Vector3(0.30f * s, 0.06f * s, 0.01f), b);
        Pieza(PrimitiveType.Cube, "Punta_A", pivote, new Vector3(0.108f * s, 0.042f * s, 0), new Vector3(0.12f * s, 0.06f * s, 0.01f), b, new Vector3(0, 0, -45f));
        Pieza(PrimitiveType.Cube, "Punta_B", pivote, new Vector3(0.108f * s, -0.042f * s, 0), new Vector3(0.12f * s, 0.06f * s, 0.01f), b, new Vector3(0, 0, 45f));
    }

    static void Texto(GameObject raiz, string txt, Vector3 pos, float ancho, float alto)
    {
        var g = new GameObject("Texto");
        g.transform.SetParent(raiz.transform, false);
        g.transform.localPosition = pos;
        var t = g.AddComponent<TextMeshPro>();
        t.text = txt;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.5f;
        t.fontSizeMax = 3f;
        t.rectTransform.sizeDelta = new Vector2(ancho, alto);
    }
}
