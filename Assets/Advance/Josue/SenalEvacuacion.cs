using UnityEngine;
using TMPro;

public class SenalEvacuacion : MonoBehaviour
{
    public enum Tipo { Salida, RutaEvacuacion, ZonaSegura, PuntoReunion, PrimerosAuxilios, Extintor, NoUsarAscensor }
    public enum Direccion { Derecha, Izquierda, Arriba, Abajo }

    public Tipo tipo = Tipo.Salida;
    public Direccion direccion = Direccion.Derecha;

    [Header("Montaje")]
    public bool conPoste = true;          // true = con poste para el suelo, false = solo el panel (para pegar en una pared)
    public float alturaMontaje = 2.0f;    // altura del centro del panel (metros)

    static readonly Color Verde = new Color(0f, 0.55f, 0.25f);
    static readonly Color Rojo = new Color(0.8f, 0.05f, 0.05f);
    static readonly Color Blanco = Color.white;
    static readonly Color Gris = new Color(0.35f, 0.35f, 0.35f);

    // Crea (una sola vez) un material de color guardado en Assets/Advance/Josue/Materiales
    Material Mat(string nombre, Color color)
    {
#if UNITY_EDITOR
        string carpeta = "Assets/Advance/Josue/Materiales";
        if (!UnityEditor.AssetDatabase.IsValidFolder(carpeta))
            UnityEditor.AssetDatabase.CreateFolder("Assets/Advance/Josue", "Materiales");
        string ruta = carpeta + "/" + nombre + ".mat";
        var existente = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        m.color = color;
        UnityEditor.AssetDatabase.CreateAsset(m, ruta);
        return m;
#else
        return null;
#endif
    }

    GameObject Pieza(string nombre, Transform padre, Vector3 pos, Vector3 escala, Color color, string matNombre,
                     float rotZ = 0f, PrimitiveType prim = PrimitiveType.Cube, bool conColision = false)
    {
        var g = GameObject.CreatePrimitive(prim);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = Mat(matNombre, color);
        if (!conColision) DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    void Texto(string txt, Vector3 pos, float ancho, float alto)
    {
        var g = new GameObject("Texto");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = pos;
        var t = g.AddComponent<TextMeshPro>();
        t.text = txt;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Blanco;
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.5f;
        t.fontSizeMax = 3f;
        t.rectTransform.sizeDelta = new Vector2(ancho, alto);
    }

    // Panel de color + poste opcional. Devuelve nada: los símbolos se ponen delante (z negativo).
    void Panel(float ancho, float alto, Color color, string matNombre)
    {
        float y = conPoste ? alturaMontaje : alturaMontaje;
        if (conPoste)
            Pieza("Poste", transform, new Vector3(0, alturaMontaje / 2f, 0.03f),
                  new Vector3(0.05f, alturaMontaje / 2f, 0.05f), Gris, "SenalGris", 0f, PrimitiveType.Cylinder, true);
        Pieza("Panel", transform, new Vector3(0, y, 0), new Vector3(ancho, alto, 0.03f), color, matNombre);
    }

    void Flecha(Vector3 centro, float escala, float anguloZ)
    {
        var pivote = new GameObject("Flecha");
        pivote.transform.SetParent(transform, false);
        pivote.transform.localPosition = centro;
        pivote.transform.localRotation = Quaternion.Euler(0, 0, anguloZ);
        float s = escala;
        Pieza("Cuerpo", pivote.transform, Vector3.zero, new Vector3(0.30f * s, 0.06f * s, 0.01f), Blanco, "SenalBlanco");
        Pieza("Punta_A", pivote.transform, new Vector3(0.108f * s, 0.042f * s, 0), new Vector3(0.12f * s, 0.06f * s, 0.01f), Blanco, "SenalBlanco", -45f);
        Pieza("Punta_B", pivote.transform, new Vector3(0.108f * s, -0.042f * s, 0), new Vector3(0.12f * s, 0.06f * s, 0.01f), Blanco, "SenalBlanco", 45f);
    }

    [ContextMenu("Generar señal")]
    public void Generar()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        float y = alturaMontaje;
        float zf = -0.021f; // delante del panel

        switch (tipo)
        {
            case Tipo.Salida:
                Panel(0.6f, 0.25f, Verde, "SenalVerde");
                Texto("SALIDA", new Vector3(0, y, zf), 0.55f, 0.2f);
                break;

            case Tipo.RutaEvacuacion:
                Panel(0.7f, 0.3f, Verde, "SenalVerde");
                float ang = direccion == Direccion.Derecha ? 0f : direccion == Direccion.Izquierda ? 180f : direccion == Direccion.Arriba ? 90f : -90f;
                Flecha(new Vector3(-0.22f, y, zf), 0.8f, ang);
                Texto("RUTA DE\nEVACUACION", new Vector3(0.12f, y, zf), 0.42f, 0.26f);
                break;

            case Tipo.ZonaSegura:
                Panel(0.5f, 0.5f, Verde, "SenalVerde");
                Texto("ZONA SEGURA\nEN CASO\nDE SISMO", new Vector3(0, y, zf), 0.44f, 0.44f);
                break;

            case Tipo.PuntoReunion:
                Panel(0.6f, 0.4f, Verde, "SenalVerde");
                Texto("PUNTO DE\nREUNION", new Vector3(0, y, zf), 0.54f, 0.34f);
                break;

            case Tipo.PrimerosAuxilios:
                Panel(0.4f, 0.4f, Verde, "SenalVerde");
                Pieza("Cruz_H", transform, new Vector3(0, y, zf), new Vector3(0.24f, 0.08f, 0.01f), Blanco, "SenalBlanco");
                Pieza("Cruz_V", transform, new Vector3(0, y, zf), new Vector3(0.08f, 0.24f, 0.01f), Blanco, "SenalBlanco");
                break;

            case Tipo.Extintor:
                Panel(0.5f, 0.3f, Rojo, "SenalRoja");
                Texto("EXTINTOR", new Vector3(0, y, zf), 0.46f, 0.2f);
                break;

            case Tipo.NoUsarAscensor:
                Panel(0.5f, 0.35f, Rojo, "SenalRoja");
                Texto("NO USAR\nASCENSOR", new Vector3(0, y, zf), 0.46f, 0.3f);
                break;
        }
    }
}
