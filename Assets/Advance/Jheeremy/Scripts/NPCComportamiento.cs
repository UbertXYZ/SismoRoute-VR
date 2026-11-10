using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Comportamiento de una persona del edificio.
// Normal: se queda parada, patrulla una ruta, sale por una puerta o deambula.
// Al sonar la alarma: huye a una salida, se paraliza o corre sin rumbo.
// Si un bloqueo le corta el camino, busca otra salida.
// Efecto rebaño: algunos no eligen salida, siguen a la persona que huye más cerca.
// Rescate: el jugador puede tocar a un paralizado o desorientado para que lo siga hasta la salida.
// Voz (solo después de la alarma): gritan al huir, lloran si se paralizan, conversan al salir.
// Al salir del edificio: se queda un momento en la salida, camina al punto de encuentro y celebra.
[RequireComponent(typeof(NavMeshAgent))]
public class NPCComportamiento : MonoBehaviour
{
    public enum Rol { Parado, Patrulla, Saliente, Deambular }
    public enum Reaccion { Huir, Paralizarse, Desorientarse }

    [Header("Comportamiento normal")]
    public Rol rol = Rol.Deambular;
    [Tooltip("Puntos de la ruta (solo para Patrulla).")]
    public Transform[] puntosRuta;
    public float velocidadCaminar = 1.2f;
    public Vector2 esperaEnPunto = new Vector2(1f, 4f);
    [Tooltip("Radio en metros para elegir puntos al azar (Deambular y Desorientarse).")]
    public float radioDeambular = 6f;

    [Header("Reacción a la alarma")]
    public bool reaccionaAAlarma = true;
    public Reaccion reaccion = Reaccion.Huir;
    public float velocidadHuida = 3.5f;
    public Vector2 retrasoReaccion = new Vector2(0f, 1.5f);
    [Tooltip("Si no hay Zona de Encuentro en la escena, desaparece al llegar a la salida.")]
    public bool desapareceAlSalir = true;
    [Tooltip("Desorientarse: no se acerca a menos de esta distancia de una salida.")]
    public float distanciaEvitarSalida = 6f;
    [Tooltip("Probabilidad de NO elegir la salida más cercana (ruta familiar / seguir a otros).")]
    [Range(0f, 1f)] public float probabilidadOtraSalida = 0.2f;

    [Header("Efecto rebaño")]
    [Tooltip("Probabilidad de que, al huir, siga a otra persona en vez de elegir salida.")]
    [Range(0f, 1f)] public float probabilidadSeguir = 0.3f;
    [Tooltip("Distancia máxima para buscar a quién seguir (mismo piso).")]
    public float radioBuscarLider = 10f;
    [Tooltip("Qué tan atrás del líder camina.")]
    public float distanciaAlLider = 1.5f;

    [Header("Rescate por el jugador")]
    [Tooltip("Distancia máxima (en metros) a la que el jugador puede tocarlo para rescatarlo.")]
    public float distanciaRescate = 2.5f;

    [Header("Después de salir")]
    [Tooltip("Distancia a la salida para considerar que ya salió (evita que se atasquen amontonados).")]
    public float radioLlegadaSalida = 2.5f;
    [Tooltip("Segundos que se queda en la salida antes de ir al punto de encuentro (varía ±20%).")]
    public float tiempoCansado = 10f;

    [Header("Voz (solo después de la alarma)")]
    [Tooltip("Gritos: se usan al huir o correr desorientado. Pon varios para que no suenen todos igual.")]
    public AudioClip[] gritos;
    [Tooltip("Llantos: se usan al paralizarse.")]
    public AudioClip[] llantos;
    [Tooltip("Conversaciones al llegar a la salida y en el punto de encuentro.")]
    public AudioClip[] charlas;
    [Tooltip("Segundos de silencio entre un grito y el siguiente.")]
    public Vector2 pausaEntreGritos = new Vector2(3f, 8f);
    [Range(0f, 1f)] public float volumenVoz = 0.8f;
    [Tooltip("Hasta qué distancia (metros) se escucha a esta persona.")]
    public float distanciaVoz = 12f;

    [Header("Animator (nombres de parámetros y estados)")]
    public string paramVelocidad = "Velocidad";
    public string paramPanico = "Panico";
    public string estadoLocomocion = "Locomocion";
    public string estadoParalizado = "Crying";
    public string estadoCansado = "Nervously Look Around";
    public string estadoFeliz = "Victory";

    NavMeshAgent agente;
    Animator animator;
    Vector3 origen;
    int indiceRuta;
    float esperaHasta;
    bool yendoASalida;
    bool reaccionando;
    float proximaRevision;
    bool siguiendo;
    bool salio;
    NPCComportamiento lider;
    bool rescatado;
    Transform jugador;
    AudioSource voz;
    Coroutine rutinaGritos;
    bool atascado;
    float inmuneHasta;
    Coroutine rutinaSinRumbo;

    // Contador global de personas rescatadas por el jugador
    public static int Rescatados { get; private set; }
    public static event System.Action<int> AlRescatar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarContador() { Rescatados = 0; AlRescatar = null; }

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Fuente de voz 3D (se crea sola si el prefab no la tiene)
        voz = GetComponent<AudioSource>();
        if (voz == null) voz = gameObject.AddComponent<AudioSource>();
        voz.playOnAwake = false;
        voz.spatialBlend = 1f;
        voz.rolloffMode = AudioRolloffMode.Linear;
        voz.minDistance = 1f;
        voz.maxDistance = distanciaVoz;
        voz.volume = volumenVoz;
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
        if (atascado) return;   // atascado en una aglomeración: espera a que se libere
        if (animator != null)
            animator.SetFloat(paramVelocidad, agente.velocity.magnitude);

        if (siguiendo)
        {
            Seguir();
            return;
        }

        if (yendoASalida)
        {
            // Si un bloqueo cortó el camino, buscar otra salida (revisa 1 vez por segundo)
            if (!agente.pathPending && agente.pathStatus != NavMeshPathStatus.PathComplete)
            {
                if (Time.time >= proximaRevision)
                {
                    proximaRevision = Time.time + 1f;
                    IrASalida();
                }
                return;
            }

            if (!agente.pathPending && agente.remainingDistance <= radioLlegadaSalida) Salir();
            return;
        }

        if (reaccionando) return; // paralizado, desorientado o ya evacuado: lo maneja su corrutina

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
        for (int i = 0; i < 10; i++)
        {
            Vector3 punto = origen + Random.insideUnitSphere * radioDeambular;
            punto.y = origen.y;
            if (NavMesh.SamplePosition(punto, out NavMeshHit hit, 2f, NavMesh.AllAreas)
                && Mathf.Abs(hit.position.y - origen.y) < 1f) // que no cambie de piso
            {
                agente.SetDestination(hit.position);
                return;
            }
        }
    }

    // ---------- Reacción a la alarma ----------

    void OnAlarma(Vector3 origenAlarma)
    {
        if (!reaccionaAAlarma || reaccionando) return;
        StartCoroutine(ReaccionarTrasRetraso());
    }

    IEnumerator ReaccionarTrasRetraso()
    {
        yield return new WaitForSeconds(Random.Range(retrasoReaccion.x, retrasoReaccion.y));

        reaccionando = true;
        esperaHasta = 0f;

        switch (reaccion)
        {
            case Reaccion.Paralizarse:
                agente.isStopped = true;
                agente.ResetPath();
                if (animator != null) animator.CrossFade(estadoParalizado, 0.3f);
                Llorar();
                break;

            case Reaccion.Desorientarse:
                agente.speed = velocidadHuida;
                if (animator != null) animator.SetBool(paramPanico, true);
                Gritar();
                rutinaSinRumbo = StartCoroutine(CorrerSinRumbo());
                break;

            default: // Huir
                agente.speed = velocidadHuida;
                if (animator != null) animator.SetBool(paramPanico, true);
                Gritar();
                if (Random.value < probabilidadSeguir && BuscarLider())
                    siguiendo = true;   // efecto rebaño: va detrás de otro
                else
                    IrASalida();
                break;
        }
    }

    // ---------- Efecto rebaño ----------

    // Busca a la persona más cercana de su piso que huye por su cuenta
    bool BuscarLider()
    {
        lider = null;
        float mejor = radioBuscarLider * radioBuscarLider;
        foreach (var otro in FindObjectsByType<NPCComportamiento>(FindObjectsSortMode.None))
        {
            if (otro == this || otro.siguiendo || otro.salio) continue;      // no seguir a otro seguidor
            if (otro.reaccion != Reaccion.Huir || !otro.reaccionaAAlarma) continue;
            Vector3 d = otro.transform.position - transform.position;
            if (Mathf.Abs(d.y) > 1.5f) continue;                             // mismo piso
            if (d.sqrMagnitude < mejor) { mejor = d.sqrMagnitude; lider = otro; }
        }
        return lider != null;
    }

    // Va detrás del líder (otro NPC) o del jugador si fue rescatado
    void Seguir()
    {
        Transform objetivo;
        if (rescatado)
        {
            objetivo = jugador;
            if (objetivo == null) { siguiendo = false; IrASalida(); return; }
        }
        else
        {
            // Si el líder ya no sirve (desapareció o ya salió), buscar la salida por su cuenta
            if (lider == null || !lider.isActiveAndEnabled || lider.salio)
            {
                siguiendo = false;
                IrASalida();
                return;
            }
            objetivo = lider.transform;
        }

        if (Time.time < proximaRevision) return;
        proximaRevision = Time.time + 0.4f;

        // El rescatado, al estar cerca de una salida, ya sale solo
        if (rescatado && HaySalidaCerca(radioLlegadaSalida + 3f))
        {
            siguiendo = false;
            IrASalida();
            return;
        }

        // Ponerse a 'distanciaAlLider' metros del objetivo, del lado donde está el NPC
        Vector3 objetivoPos = objetivo.position;
        if (rescatado) objetivoPos.y -= 1.5f;   // la cámara está a la altura de la cabeza
        Vector3 desdeObjetivo = transform.position - objetivoPos;
        desdeObjetivo.y = 0f;
        if (desdeObjetivo.sqrMagnitude < 0.01f) desdeObjetivo = -objetivo.forward;
        Vector3 destino = objetivoPos + desdeObjetivo.normalized * distanciaAlLider;

        if (NavMesh.SamplePosition(destino, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            agente.SetDestination(hit.position);
    }

    bool HaySalidaCerca(float distancia)
    {
        foreach (var s in FindObjectsByType<PuntoSalida>(FindObjectsSortMode.None))
            if (Vector3.Distance(s.transform.position, transform.position) < distancia) return true;
        return false;
    }

    // ---------- Rescate por el jugador ----------

    // Llamar desde el XR Simple Interactable del prefab: Select Entered → AlSerTocado()
    public void AlSerTocado()
    {
        if (!reaccionando || rescatado || salio || atascado) return;
        if (reaccion == Reaccion.Huir) return;              // los que ya huyen no necesitan ayuda

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 d = cam.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude > distanciaRescate) return;         // hay que acercarse para ayudarlo

        rescatado = true;
        jugador = cam.transform;
        StopAllCoroutines();                                // deja de correr sin rumbo
        Callar();                                           // se calma al ser rescatado
        agente.isStopped = false;
        agente.speed = velocidadHuida;                      // para poder seguir al jugador
        if (animator != null)
        {
            animator.SetBool(paramPanico, false);           // se calma: deja de llorar
            animator.CrossFade(estadoLocomocion, 0.3f);
        }
        siguiendo = true;
        proximaRevision = 0f;
    }

    // Corre de un lado a otro de su piso, evitando acercarse a las salidas
    IEnumerator CorrerSinRumbo()
    {
        var salidas = FindObjectsByType<PuntoSalida>(FindObjectsSortMode.None);
        while (true)
        {
            for (int intento = 0; intento < 15; intento++)
            {
                Vector3 punto = transform.position + Random.insideUnitSphere * radioDeambular * 2f;
                punto.y = transform.position.y;
                if (!NavMesh.SamplePosition(punto, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                if (Mathf.Abs(hit.position.y - transform.position.y) > 1f) continue;

                bool cercaDeSalida = false;
                foreach (var s in salidas)
                    if (Vector3.Distance(s.transform.position, hit.position) < distanciaEvitarSalida)
                    { cercaDeSalida = true; break; }
                if (cercaDeSalida) continue;

                agente.SetDestination(hit.position);
                break;
            }

            float limite = Time.time + 5f;
            yield return new WaitUntil(() => LlegoADestino() || Time.time > limite);
            yield return new WaitForSeconds(Random.Range(0.3f, 1.2f));
        }
    }

    void IrASalida()
    {
        var salidas = FindObjectsByType<PuntoSalida>(FindObjectsSortMode.None);
        if (salidas.Length == 0)
        {
            Debug.LogWarning("No hay ningún PuntoSalida en la escena.");
            return;
        }

        // Medir el camino REAL (caminando por el NavMesh) hasta cada salida
        var camino = new NavMeshPath();
        var opciones = new List<(PuntoSalida salida, float largo)>();
        foreach (var s in salidas)
        {
            if (!NavMesh.CalculatePath(transform.position, s.transform.position, NavMesh.AllAreas, camino)) continue;
            if (camino.status != NavMeshPathStatus.PathComplete) continue; // bloqueada o inalcanzable

            float largo = 0f;
            for (int i = 1; i < camino.corners.Length; i++)
                largo += Vector3.Distance(camino.corners[i - 1], camino.corners[i]);
            opciones.Add((s, largo));
        }

        PuntoSalida elegida;
        if (opciones.Count == 0)
        {
            // Ninguna salida alcanzable: acercarse a la más cercana en línea recta y esperar
            elegida = salidas[0];
            float mejor = float.MaxValue;
            foreach (var s in salidas)
            {
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (d < mejor) { mejor = d; elegida = s; }
            }
        }
        else
        {
            // La de camino más corto... salvo algunos que eligen otra
            opciones.Sort((a, b) => a.largo.CompareTo(b.largo));
            elegida = opciones[0].salida;
            if (opciones.Count > 1 && Random.value < probabilidadOtraSalida)
                elegida = opciones[Random.Range(1, opciones.Count)].salida;
        }

        yendoASalida = true;
        agente.SetDestination(elegida.transform.position);
    }

    void Salir()
    {
        salio = true;
        yendoASalida = false;

        if (rescatado)
        {
            Rescatados++;
            Debug.Log($"Persona rescatada. Total: {Rescatados}");
            AlRescatar?.Invoke(Rescatados);
        }
        agente.ResetPath();

        // Los que evacuaban por la alarma: reacción en la salida y luego al punto de encuentro
        var zonas = FindObjectsByType<ZonaEncuentro>(FindObjectsSortMode.None);
        if (reaccionando && zonas.Length > 0)
        {
            StartCoroutine(CansancioYEncuentro(zonas));
            return;
        }

        // Los que salían antes del sismo (Saliente) o si no hay zona de encuentro
        if (desapareceAlSalir) gameObject.SetActive(false);
    }

    IEnumerator CansancioYEncuentro(ZonaEncuentro[] zonas)
    {
        // 1) En la puerta de salida: se queda un momento conversando
        agente.isStopped = true;
        if (animator != null)
        {
            animator.SetBool(paramPanico, false);
            animator.CrossFade(estadoCansado, 0.3f);
        }
        Hablar();
        yield return new WaitForSeconds(tiempoCansado * Random.Range(0.8f, 1.2f));

        // 2) Caminar a la zona de encuentro más cercana
        ZonaEncuentro zona = zonas[0];
        float mejor = float.MaxValue;
        foreach (var z in zonas)
        {
            float d = (z.transform.position - transform.position).sqrMagnitude;
            if (d < mejor) { mejor = d; zona = z; }
        }

        agente.speed = velocidadCaminar;
        agente.isStopped = false;
        if (animator != null) animator.CrossFade(estadoLocomocion, 0.3f);
        agente.SetDestination(zona.PuntoLibre());

        yield return null;
        yield return new WaitUntil(() => LlegoADestino());

        // 3) Llegó a la zona segura: mirar al centro y celebrar (Victory)
        agente.isStopped = true;
        Vector3 dir = zona.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        if (animator != null) animator.CrossFade(estadoFeliz, 0.3f);
        // sigue conversando en el punto de encuentro
    }

    // ---------- Aglomeración ----------

    // Lo consulta el DetectorAglomeracion: solo gente que huye o corre sin rumbo, y una sola vez
    public bool PuedeAtascarse =>
        reaccionando && !atascado && Time.time >= inmuneHasta && !salio && !rescatado
        && reaccion != Reaccion.Paralizarse && agente.enabled;

    public void EntrarAtasco()
    {
        atascado = true;
        if (rutinaSinRumbo != null) { StopCoroutine(rutinaSinRumbo); rutinaSinRumbo = null; }
        agente.isStopped = true;
        agente.ResetPath();
        agente.enabled = false;                              // que el bloqueo no lo empuje fuera del NavMesh
        if (animator != null)
        {
            animator.SetBool(paramPanico, false);
            animator.CrossFade(estadoParalizado, 0.3f);      // llora (Crying)
        }
        Llorar();
    }

    public void SalirAtasco(float inmunidad)
    {
        if (!atascado) return;
        atascado = false;
        inmuneHasta = Time.time + inmunidad;                 // un tiempo sin poder atascarse otra vez
        agente.enabled = true;
        agente.Warp(transform.position);
        agente.isStopped = false;
        agente.speed = velocidadHuida;
        if (animator != null)
        {
            animator.SetBool(paramPanico, true);
            animator.CrossFade(estadoLocomocion, 0.3f);
        }
        Gritar();
        if (siguiendo) return;                               // sigue a su líder
        if (reaccion == Reaccion.Desorientarse) rutinaSinRumbo = StartCoroutine(CorrerSinRumbo());
        else IrASalida();
    }

    // ---------- Voz ----------

    // Grita de vez en cuando (no sin parar), con tono distinto cada vez
    void Gritar()
    {
        Callar();
        if (gritos == null || gritos.Length == 0) return;
        rutinaGritos = StartCoroutine(RutinaGritos());
    }

    IEnumerator RutinaGritos()
    {
        yield return new WaitForSeconds(Random.Range(0f, 1.5f));   // que no griten todos a la vez
        while (true)
        {
            AudioClip clip = gritos[Random.Range(0, gritos.Length)];
            voz.loop = false;
            voz.pitch = Random.Range(0.9f, 1.15f);
            voz.clip = clip;
            voz.Play();
            yield return new WaitForSeconds(clip.length / voz.pitch + Random.Range(pausaEntreGritos.x, pausaEntreGritos.y));
        }
    }

    // Llora en bucle (paralizados)
    void Llorar() { SonarEnBucle(llantos); }

    // Habla en bucle (en la salida y en el punto de encuentro)
    void Hablar() { SonarEnBucle(charlas); }

    void SonarEnBucle(AudioClip[] lista)
    {
        Callar();
        if (lista == null || lista.Length == 0) return;
        voz.clip = lista[Random.Range(0, lista.Length)];
        voz.loop = true;
        voz.pitch = Random.Range(0.92f, 1.08f);
        voz.time = Random.Range(0f, voz.clip.length * 0.5f);         // que no suenen todos sincronizados
        voz.Play();
    }

    void Callar()
    {
        if (rutinaGritos != null) { StopCoroutine(rutinaGritos); rutinaGritos = null; }
        if (voz != null) voz.Stop();
    }

    bool LlegoADestino()
    {
        return !agente.pathPending
            && agente.remainingDistance <= agente.stoppingDistance + 0.2f
            && (!agente.hasPath || agente.velocity.sqrMagnitude < 0.01f);
    }
}