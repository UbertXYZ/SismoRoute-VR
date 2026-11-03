using UnityEngine;

// Pon este script en un Quad pegado a la pared (con un material Unlit).
// Al iniciar dibuja el plano de SU piso: paredes, puertas, escaleras,
// salidas, zonas de encuentro y un punto rojo "Usted está aquí".
// El mapa se orienta hacia donde mira la persona que lo lee (arriba del mapa = al frente).
[RequireComponent(typeof(MeshRenderer))]
public class MapaPiso : MonoBehaviour
{
    [Header("Área que cubre el mapa")]
    public Vector2 centro = new Vector2(4f, 0f);   // x, z del mundo
    public float tamano = 56f;                     // metros de ancho y alto
    public int resolucion = 200;                   // píxeles por lado

    [Header("Pisos del edificio")]
    public float alturaPrimerPiso = 0.12f;
    public float alturaEntrePisos = 4.5f;

    [Header("Colores")]
    public Color colorFondo = Color.white;
    public Color colorPiso = new Color(0.88f, 0.88f, 0.88f);
    public Color colorPared = new Color(0.15f, 0.15f, 0.15f);
    public Color colorPuerta = new Color(0.95f, 0.6f, 0.1f);
    public Color colorEscalera = new Color(0.1f, 0.65f, 0.25f);
    public Color colorSalida = new Color(0f, 0.8f, 0.2f);
    public Color colorEncuentro = new Color(0.1f, 0.5f, 1f);
    public Color colorUsted = Color.red;

    [Tooltip("Capas que NO se dibujan (por defecto la capa 6 = NPC).")]
    public LayerMask capasIgnoradas = 1 << 6;

    void Start() { Generar(); }

    [ContextMenu("Generar mapa (en Play)")]
    public void Generar()
    {
        // Piso en el que está este cartel
        int piso = Mathf.FloorToInt((transform.position.y - alturaPrimerPiso) / alturaEntrePisos + 0.01f);
        float pisoY = alturaPrimerPiso + piso * alturaEntrePisos;

        // Orientación: arriba del mapa = hacia donde mira el lector (frente de la pared)
        Vector3 adelante = transform.forward; adelante.y = 0f; adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        Vector3 c = new Vector3(centro.x, 0f, centro.y);
        float px = tamano / resolucion;

        var tex = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var colores = new Color[resolucion * resolucion];
        var hits = new Collider[8];
        int mascara = ~capasIgnoradas.value;

        for (int v = 0; v < resolucion; v++)
            for (int u = 0; u < resolucion; u++)
            {
                Vector3 w = c + derecha * ((u + 0.5f) * px - tamano / 2f) + adelante * ((v + 0.5f) * px - tamano / 2f);
                Color col = colorFondo;

                // ¿Hay piso aquí?
                if (Physics.Raycast(new Vector3(w.x, pisoY + 1f, w.z), Vector3.down, out RaycastHit rh, 1.4f, mascara, QueryTriggerInteraction.Ignore))
                    col = rh.collider.name.Contains("Escalera") ? colorEscalera : colorPiso;

                // ¿Pared, puerta o escalera a 1.2 m de altura?
                int n = Physics.OverlapSphereNonAlloc(new Vector3(w.x, pisoY + 1.2f, w.z), px * 0.6f, hits, mascara, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i];
                    if (h.GetComponentInParent<CharacterController>() != null) continue;      // el jugador
                    if (h.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) continue; // personas
                    if (h.GetComponentInParent<PuertaAutomatica>() != null) { col = colorPuerta; continue; }
                    if (h.name.Contains("Escalera")) { if (col != colorPared && col != colorPuerta) col = colorEscalera; continue; }
                    col = colorPared;
                }
                colores[v * resolucion + u] = col;
            }

        // Marcadores
        foreach (var s in FindObjectsByType<PuntoSalida>(FindObjectsSortMode.None))
            if (Mathf.Abs(s.transform.position.y - pisoY) < 1f)
                Circulo(colores, s.transform.position, 1.2f, colorSalida, c, derecha, adelante);

        if (piso == 0)
            foreach (var z in FindObjectsByType<ZonaEncuentro>(FindObjectsSortMode.None))
                Circulo(colores, z.transform.position, z.radio, colorEncuentro, c, derecha, adelante);

        Circulo(colores, transform.position - adelante * 0.8f, 0.9f, colorUsted, c, derecha, adelante);

        tex.SetPixels(colores);
        tex.Apply();
        GetComponent<MeshRenderer>().material.mainTexture = tex;
    }

    void Circulo(Color[] colores, Vector3 mundo, float radio, Color color, Vector3 c, Vector3 derecha, Vector3 adelante)
    {
        Vector3 d = mundo - c;
        float px = tamano / resolucion;
        float cu = Vector3.Dot(d, derecha) / px + resolucion / 2f;
        float cv = Vector3.Dot(d, adelante) / px + resolucion / 2f;
        float r = radio / px;
        for (int v = Mathf.FloorToInt(cv - r); v <= Mathf.CeilToInt(cv + r); v++)
            for (int u = Mathf.FloorToInt(cu - r); u <= Mathf.CeilToInt(cu + r); u++)
            {
                if (u < 0 || v < 0 || u >= resolucion || v >= resolucion) continue;
                if ((u - cu) * (u - cu) + (v - cv) * (v - cv) <= r * r)
                    colores[v * resolucion + u] = color;
            }
    }
}