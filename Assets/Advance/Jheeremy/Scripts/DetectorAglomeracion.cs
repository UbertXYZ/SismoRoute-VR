using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Detecta cuando se juntan muchas personas huyendo y forma un atasco:
// esas personas se detienen llorando y se crea un bloqueo que ni el jugador ni otros NPC pueden cruzar.
// Pasado un tiempo, el atasco se deshace y todos siguen.
public class DetectorAglomeracion : MonoBehaviour
{
    [Header("Cuándo se forma un atasco")]
    [Tooltip("Cuántas personas juntas forman un atasco.")]
    [Min(2)] public int personasParaAtasco = 6;
    [Tooltip("Distancia (m) para considerar que dos personas están juntas.")]
    public float radioGrupo = 2.5f;
    [Tooltip("Cada cuántos segundos se revisa.")]
    public float intervaloRevision = 0.5f;

    [Header("El bloqueo")]
    [Tooltip("Segundos que dura el atasco.")]
    public float duracionBloqueo = 15f;
    [Tooltip("Tamaño del bloqueo (ancho, alto, largo) en metros.")]
    public Vector3 tamanoBloqueo = new Vector3(4f, 2.5f, 4f);
    [Tooltip("Máximo de atascos al mismo tiempo en todo el edificio.")]
    public int maximoAtascos = 2;

    [Header("Enfriamiento (evita atascos repetidos en el mismo lugar)")]
    [Tooltip("Segundos, después de liberarse, en que no puede formarse otro atasco cerca.")]
    public float enfriamiento = 20f;
    [Tooltip("Distancia (m) alrededor de un atasco donde no puede formarse otro.")]
    public float radioEnfriamiento = 8f;
    [Tooltip("Segundos en que una persona que salió de un atasco no puede formar parte de otro.")]
    public float inmunidadPersona = 30f;

    class Atasco
    {
        public Vector3 centro;
        public float finEnfriamiento;
        public GameObject bloqueo;
    }

    readonly List<Atasco> atascos = new List<Atasco>();
    float proximaRevision;

    void Update()
    {
        if (Time.time < proximaRevision) return;
        proximaRevision = Time.time + intervaloRevision;

        // Olvidar atascos que ya terminaron su enfriamiento
        atascos.RemoveAll(a => a.bloqueo == null && Time.time > a.finEnfriamiento);

        int activos = 0;
        foreach (var a in atascos) if (a.bloqueo != null) activos++;
        if (activos >= maximoAtascos) return;

        // Personas que pueden atascarse (huyendo, después de la alarma, que no se hayan atascado antes)
        var candidatos = new List<NPCComportamiento>();
        foreach (var n in FindObjectsByType<NPCComportamiento>(FindObjectsSortMode.None))
            if (n.PuedeAtascarse) candidatos.Add(n);
        if (candidatos.Count < personasParaAtasco) return;

        float r2 = radioGrupo * radioGrupo;
        foreach (var c in candidatos)
        {
            // Contar cuántas están cerca de esta persona, en el mismo piso
            var grupo = new List<NPCComportamiento> { c };
            foreach (var o in candidatos)
            {
                if (o == c) continue;
                Vector3 d = o.transform.position - c.transform.position;
                if (Mathf.Abs(d.y) > 1.5f) continue;
                d.y = 0f;
                if (d.sqrMagnitude <= r2) grupo.Add(o);
            }
            if (grupo.Count < personasParaAtasco) continue;

            Vector3 centro = Vector3.zero;
            foreach (var g in grupo) centro += g.transform.position;
            centro /= grupo.Count;
            if (EnEnfriamiento(centro)) continue;

            StartCoroutine(FormarAtasco(centro, grupo));
            return;   // máximo un atasco nuevo por revisión
        }
    }

    bool EnEnfriamiento(Vector3 p)
    {
        foreach (var a in atascos)
        {
            if (Mathf.Abs(a.centro.y - p.y) > 1.5f) continue;   // otro piso
            if (Vector3.Distance(a.centro, p) < radioEnfriamiento) return true;
        }
        return false;
    }

    IEnumerator FormarAtasco(Vector3 centro, List<NPCComportamiento> grupo)
    {
        var a = new Atasco { centro = centro, finEnfriamiento = Time.time + duracionBloqueo + enfriamiento };
        atascos.Add(a);

        // Las personas se detienen llorando
        foreach (var n in grupo) n.EntrarAtasco();

        // Bloqueo: el collider frena al jugador y el NavMeshObstacle a los demás NPC
        var b = new GameObject("AtascoPersonas");
        b.transform.position = centro + Vector3.up * (tamanoBloqueo.y / 2f);
        var col = b.AddComponent<BoxCollider>();
        col.size = tamanoBloqueo;
        var obs = b.AddComponent<NavMeshObstacle>();
        obs.shape = NavMeshObstacleShape.Box;
        obs.size = tamanoBloqueo;
        obs.carving = true;
        obs.carveOnlyStationary = false;
        a.bloqueo = b;
        Debug.Log($"Atasco de {grupo.Count} personas en {centro}");

        yield return new WaitForSeconds(duracionBloqueo);

        // Se deshace el atasco
        Destroy(b);
        a.bloqueo = null;
        yield return null;
        yield return null;   // esperar a que el NavMesh se recomponga

        foreach (var n in grupo) if (n != null) n.SalirAtasco(inmunidadPersona);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f);
        foreach (var a in atascos)
            if (a.bloqueo != null) Gizmos.DrawWireCube(a.bloqueo.transform.position, tamanoBloqueo);
    }
}