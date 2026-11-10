using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Genera personas al azar en cada piso del edificio al dar Play.
// - El piso más alto tiene "personasPisoSuperior"; cada piso inferior tiene 1 o 2 menos.
// - La mayoría huye a la salida de su piso; una minoría se desorienta (corre a cualquier lado menos la salida).
// - Aparecen en lugares distintos del NavMesh de cada piso (habitaciones y pasillos), separados entre sí.
public class GeneradorPersonas : MonoBehaviour
{
    [Header("Prefab")]
    public NPCComportamiento prefabPersona;
    [Tooltip("Prefab de mujer. Si está vacío, todas las personas usan el prefab de arriba.")]
    public NPCComportamiento prefabMujer;
    [Tooltip("Probabilidad de que cada persona sea mujer (0.5 = mitad y mitad en promedio, pero varía en cada partida).")]
    [Range(0f, 1f)] public float probabilidadMujer = 0.5f;

    [Header("Pisos (altura del suelo de cada piso, de ARRIBA hacia ABAJO)")]
    public float[] alturasPisos = { 18.12f, 13.62f, 9.12f, 4.62f, 0.12f };
    [Tooltip("Área del edificio (X y Z mínimos y máximos) para no generar personas afuera.")]
    public Vector2 areaMin = new Vector2(-10f, -24f);
    public Vector2 areaMax = new Vector2(14f, 24f);

    [Header("Cantidad por piso")]
    [Tooltip("Personas en el piso más alto.")]
    public int personasPisoSuperior = 12;
    [Tooltip("Cuántas personas menos tiene cada piso inferior (al azar entre X e Y).")]
    public Vector2Int reduccionPorPiso = new Vector2Int(1, 2);
    public int minimoPorPiso = 2;

    [Header("Reacciones")]
    [Tooltip("Porcentaje que NO va a la salida (corre a cualquier lado).")]
    [Range(0f, 1f)] public float porcentajeDesorientados = 0.15f;
    public int minimoDesorientadosPorPiso = 1;
    [Tooltip("Porcentaje que empieza parado (el resto deambula).")]
    [Range(0f, 1f)] public float porcentajeParados = 0.3f;

    [Header("Ubicación")]
    [Tooltip("Distancia mínima entre personas al aparecer.")]
    public float separacionMinima = 3f;
    [Tooltip("0 = distinto cada vez. Otro número = siempre la misma distribución.")]
    public int semilla = 0;

    readonly List<GameObject> generadas = new List<GameObject>();

    void Start()
    {
        Generar();
    }

    [ContextMenu("Volver a generar (solo en Play)")]
    public void Generar()
    {
        if (!Application.isPlaying) { Debug.LogWarning("GeneradorPersonas: solo funciona en Play."); return; }
        if (prefabPersona == null) { Debug.LogWarning("GeneradorPersonas: asigna el prefab Persona."); return; }

        Limpiar();
        if (semilla != 0) Random.InitState(semilla);

        NavMeshTriangulation malla = NavMesh.CalculateTriangulation();
        if (malla.vertices.Length == 0) { Debug.LogWarning("GeneradorPersonas: no hay NavMesh horneado."); return; }

        int cantidad = personasPisoSuperior;
        int total = 0;
        for (int p = 0; p < alturasPisos.Length; p++)
        {
            int n = Mathf.Max(minimoPorPiso, cantidad);
            total += GenerarEnPiso(malla, alturasPisos[p], n, p + 1);
            cantidad -= Random.Range(reduccionPorPiso.x, reduccionPorPiso.y + 1);
        }
        Debug.Log($"GeneradorPersonas: {total} personas en {alturasPisos.Length} pisos.");
    }

    int GenerarEnPiso(NavMeshTriangulation malla, float altura, int n, int numPiso)
    {
        // Triángulos del NavMesh que están en este piso y dentro del edificio
        var tris = new List<int>();
        var areas = new List<float>();
        float areaTotal = 0f;
        for (int i = 0; i < malla.indices.Length; i += 3)
        {
            Vector3 a = malla.vertices[malla.indices[i]];
            Vector3 b = malla.vertices[malla.indices[i + 1]];
            Vector3 c = malla.vertices[malla.indices[i + 2]];
            Vector3 centro = (a + b + c) / 3f;
            if (Mathf.Abs(centro.y - altura) > 0.35f) continue;
            if (!DentroDelArea(centro)) continue;

            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            tris.Add(i); areas.Add(area); areaTotal += area;
        }
        if (tris.Count == 0) { Debug.LogWarning($"GeneradorPersonas: no hay NavMesh en el piso de altura {altura}."); return 0; }

        // Posiciones separadas entre sí
        var posiciones = new List<Vector3>();
        for (int k = 0; k < n; k++)
        {
            for (int intento = 0; intento < 60; intento++)
            {
                Vector3 punto = PuntoAleatorio(malla, tris, areas, areaTotal);
                if (!DentroDelArea(punto)) continue;
                bool muyCerca = false;
                foreach (var otra in posiciones)
                    if (Vector3.Distance(otra, punto) < separacionMinima) { muyCerca = true; break; }
                if (muyCerca) continue;
                posiciones.Add(punto);
                break;
            }
        }

        // Quiénes se desorientan (al azar)
        int desorientados = Mathf.Max(minimoDesorientadosPorPiso, Mathf.RoundToInt(posiciones.Count * porcentajeDesorientados));
        desorientados = Mathf.Min(desorientados, posiciones.Count);
        var orden = new List<int>();
        for (int i = 0; i < posiciones.Count; i++) orden.Add(i);
        for (int i = 0; i < orden.Count; i++) { int j = Random.Range(i, orden.Count); (orden[i], orden[j]) = (orden[j], orden[i]); }

        for (int i = 0; i < posiciones.Count; i++)
        {
            NPCComportamiento elegido = (prefabMujer != null && Random.value < probabilidadMujer) ? prefabMujer : prefabPersona;
            NPCComportamiento npc = Instantiate(elegido, posiciones[i],
                                                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            npc.name = $"Persona_P{numPiso}_{i + 1}";
            bool desorientado = orden.IndexOf(i) < desorientados;

            npc.rol = Random.value < porcentajeParados ? NPCComportamiento.Rol.Parado : NPCComportamiento.Rol.Deambular;
            npc.reaccion = desorientado ? NPCComportamiento.Reaccion.Desorientarse : NPCComportamiento.Reaccion.Huir;
            npc.radioDeambular = Random.Range(3f, 6f);
            npc.velocidadCaminar = Random.Range(1f, 1.4f);
            npc.velocidadHuida = Random.Range(3f, 4f);
            npc.retrasoReaccion = new Vector2(0f, 2f);
            generadas.Add(npc.gameObject);
        }
        return posiciones.Count;
    }

    Vector3 PuntoAleatorio(NavMeshTriangulation malla, List<int> tris, List<float> areas, float areaTotal)
    {
        // Elegir un triángulo según su área (así se reparten parejo por todo el piso)
        float r = Random.value * areaTotal;
        int t = 0;
        for (; t < tris.Count - 1; t++) { r -= areas[t]; if (r <= 0f) break; }

        int i = tris[t];
        Vector3 a = malla.vertices[malla.indices[i]];
        Vector3 b = malla.vertices[malla.indices[i + 1]];
        Vector3 c = malla.vertices[malla.indices[i + 2]];
        float u = Random.value, v = Random.value;
        if (u + v > 1f) { u = 1f - u; v = 1f - v; }
        return a + u * (b - a) + v * (c - a);
    }

    bool DentroDelArea(Vector3 p)
    {
        return p.x >= areaMin.x && p.x <= areaMax.x && p.z >= areaMin.y && p.z <= areaMax.y;
    }

    void Limpiar()
    {
        foreach (var g in generadas) if (g != null) Destroy(g);
        generadas.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (float y in alturasPisos)
        {
            Vector3 centro = new Vector3((areaMin.x + areaMax.x) / 2f, y, (areaMin.y + areaMax.y) / 2f);
            Gizmos.DrawWireCube(centro, new Vector3(areaMax.x - areaMin.x, 0.1f, areaMax.y - areaMin.y));
        }
    }
}