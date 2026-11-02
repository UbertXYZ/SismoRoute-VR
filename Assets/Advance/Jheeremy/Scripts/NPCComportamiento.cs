using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Comportamiento de una persona del edificio.
// Normal: se queda parada, patrulla una ruta, sale por una puerta o deambula.
// Al sonar la alarma (EventosSismo.Alarma): huye a la salida más cercana o se paraliza.
[RequireComponent(typeof(NavMeshAgent))]
public class NPCComportamiento : MonoBehaviour
{
    // Deambular va al final para no cambiar los roles ya asignados en la escena
    public enum Rol { Parado, Patrulla, Saliente, Deambular }
    public enum Reaccion { Huir, Paralizarse }

    [Header("Comportamiento normal")]
    public Rol rol = Rol.Patrulla;
    [Tooltip("Puntos de la ruta (solo para Patrulla).")]
    public Transform[] puntosRuta;
    public float velocidadCaminar = 1.2f;
    public Vector2 esperaEnPunto = new Vector2(1f, 4f);
    [Tooltip("Radio en metros para elegir puntos al azar (solo para Deambular).")]
    public float radioDeambular = 6f;

    [Header("Reacción a la alarma")]
    public bool reaccionaAAlarma = true;
    public Reaccion reaccion = Reaccion.Huir;
    public float velocidadHuida = 3.5f;
    public Vector2 retrasoReaccion = new Vector2(0f, 1.5f);
    public bool desapareceAlSalir = true;

    [Header("Animator (nombres de los parámetros)")]
    public string paramVelocidad = "Velocidad";
    public string paramPanico = "Panico";
    [Tooltip("Estado que reproduce quien se paraliza.")]
    public string estadoParalizado = "Crying";

    NavMeshAgent agente;
    Animator animator;
    Vector3 origen;
    int indiceRuta;
    float esperaHasta;
    bool yendoASalida;
    bool reaccionando;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void OnEnable() { EventosSismo.Alarma += OnAlarma; }
    void OnDisable() { EventosSismo.Alarma -= OnAlarma; }

    void Start()
    {
        origen = transform.position;
        agente.speed = velocidadCaminar;

        switch (rol)
        {
            case Rol.Patrulla:
                if (puntosRuta != null && puntosRuta.Length > 0)
                    agente.SetDestination(puntosRuta[0].position);
                break;
            case Rol.Saliente:
                IrASalida();
                break;
            case Rol.Deambular:
                NuevoDestinoAleatorio();
                break;
        }
    }

    void Update()
    {
        if (animator != null)
            animator.SetFloat(paramVelocidad, agente.velocity.magnitude);

        if (yendoASalida)
        {
            if (LlegoADestino()) Salir();
            return;
        }

        if (reaccionando) return; // paralizado: no hace nada más

        if (rol == Rol.Patrulla) Patrullar();
        else if (rol == Rol.Deambular) Deambular();
    }

    // ---------- Comportamientos normales ----------

    void Patrullar()
    {
        if (puntosRuta == null || puntosRuta.Length == 0) return;

        if (Esperando()) return;

        if (LlegoADestino())
            esperaHasta = Time.time + Random.Range(esperaEnPunto.x, esperaEnPunto.y);
    }

    void Deambular()
    {
        if (Esperando()) return;

        if (LlegoADestino())
            esperaHasta = Time.time + Random.Range(esperaEnPunto.x, esperaEnPunto.y);
    }

    // Devuelve true mientras la persona está esperando en un punto.
    // Al terminar la espera, elige el siguiente destino.
    bool Esperando()
    {
        if (esperaHasta <= 0f) return false;
        if (Time.time < esperaHasta) return true;

        esperaHasta = 0f;
        if (rol == Rol.Patrulla)
        {
            indiceRuta = (indiceRuta + 1) % puntosRuta.Length;
            agente.SetDestination(puntosRuta[indiceRuta].position);
        }
        else
        {
            NuevoDestinoAleatorio();
        }
        return true;
    }

    void NuevoDestinoAleatorio()
    {
        Vector3 punto = origen + Random.insideUnitSphere * radioDeambular;
        if (NavMesh.SamplePosition(punto, out NavMeshHit hit, radioDeambular, NavMesh.AllAreas))
            agente.SetDestination(hit.position);
    }

    // ---------- Reacción a la alarma ----------

    void OnAlarma(Vector3 origenAlarma)
    {
        if (!reaccionaAAlarma || reaccionando) return;
        StartCoroutine(ReaccionarTrasRetraso());
    }

    IEnumerator ReaccionarTrasRetraso()
    {
        // Retraso aleatorio para que no todos reaccionen al mismo tiempo
        yield return new WaitForSeconds(Random.Range(retrasoReaccion.x, retrasoReaccion.y));

        reaccionando = true;
        esperaHasta = 0f;

        if (reaccion == Reaccion.Paralizarse)
        {
            // Se queda congelado en su sitio (perfil "El Paralizado")
            agente.isStopped = true;
            agente.ResetPath();
            if (animator != null) animator.CrossFade(estadoParalizado, 0.3f);
            yield break;
        }

        // Huir a la salida más cercana
        agente.speed = velocidadHuida;
        if (animator != null) animator.SetBool(paramPanico, true);
        IrASalida();
    }

    void IrASalida()
    {
        var salidas = FindObjectsByType<PuntoSalida>(FindObjectsSortMode.None);
        if (salidas.Length == 0)
        {
            Debug.LogWarning("No hay ningún PuntoSalida en la escena.");
            return;
        }

        PuntoSalida mejor = salidas[0];
        float mejorDist = float.MaxValue;
        foreach (var s in salidas)
        {
            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < mejorDist) { mejorDist = d; mejor = s; }
        }

        yendoASalida = true;
        agente.SetDestination(mejor.transform.position);
    }

    void Salir()
    {
        yendoASalida = false;
        agente.ResetPath();
        if (desapareceAlSalir) gameObject.SetActive(false);
    }

    bool LlegoADestino()
    {
        return !agente.pathPending
            && agente.remainingDistance <= agente.stoppingDistance + 0.2f
            && (!agente.hasPath || agente.velocity.sqrMagnitude < 0.01f);
    }
}