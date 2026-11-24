using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Genera bloqueos de FUEGO y de CAIDA DE ROCAS al azar, pero SIEMPRE deja al menos
// una ruta libre desde donde empieza el jugador hasta una salida.
//
// Usa los objetos de los compañeros (no los modifica):
//  - Prefabs de Jheeremy: fuego.prefab (FuegoAlto) y bloqueo.prefab (BloqueoRepentino).
//  - Pasillos del PCG de Uberto (PasilloConfig), FinNivel y ControladorNivel (DestinoSismo).
//
// Como garantiza la salida:
//  1) Junta puntos candidatos (pasillos, PuntoBloqueo, y bloqueos que ya estaban en la escena).
//  2) Los mezcla y prueba uno por uno: enciende el NavMeshObstacle del bloqueo (corta el NavMesh),
//     y pide a Unity una ruta desde el inicio hasta alguna salida. Si ya NO hay ruta, ese bloqueo
//     se descarta. Los aceptados se quedan "encendidos" mientras se prueban los siguientes, asi se
//     comprueba la COMBINACION (ej. rocas en un pasillo + fuego en el otro).
//  3) Al momento de activarse cada bloqueo se vuelve a comprobar desde la posicion ACTUAL del
//     jugador (para no encerrarlo si se metio en un aula o un pasillo sin salida).
public class GeneradorBloqueosAleatorios : MonoBehaviour
{
    public enum MomentoActivacion { AlIniciarSismo, AlAcercarseJugador, AlIniciarEscena }

    [Header("Prefabs de Jheeremy")]
    [Tooltip("Assets/Advance/Jheeremy/Prefabs/fuego.prefab")]
    public GameObject prefabFuego;
    [Tooltip("Assets/Advance/Jheeremy/Prefabs/bloqueo.prefab (caida de rocas)")]
    public GameObject prefabRocas;
    [Range(0f, 1f)] public float probabilidadFuego = 0.5f;

    [Header("Cantidad y azar")]
    [Tooltip("Minimo y maximo de bloqueos por partida (se sortea un numero entre ambos).")]
    public Vector2Int cantidadBloqueos = new Vector2Int(3, 6);
    [Tooltip("Distancia minima (m) entre dos bloqueos del mismo piso.")]
    public float separacionMinima = 7f;
    [Tooltip("0 = distinto en cada partida. Otro numero = siempre el mismo resultado (util para depurar).")]
    public int semilla = 0;
    [Tooltip("Desplazamiento al azar (m) a lo largo del pasillo.")]
    public float variacionPosicion = 0.75f;
    [Tooltip("Giro al azar (grados) para que no se vean todos iguales.")]
    public float variacionGiro = 8f;

    [Header("Donde pueden aparecer")]
    public bool usarPuntosBloqueo = true;
    [Tooltip("Reutiliza la posicion de los fuego/bloqueo que ya estan en la escena (los originales se apagan y aqui se sortea de nuevo su tipo).")]
    public bool usarBloqueosExistentes = true;
    [Tooltip("Genera puntos a lo largo de los pasillos del PCG de Uberto (PasilloConfig).")]
    public bool usarPasillosGenerados = true;
    public float espaciadoEnPasillo = 6f;
    public float margenExtremosPasillo = 2.5f;
    [Tooltip("La vereda de entrada es exterior; normalmente no se bloquea.")]
    public bool incluirVeredaExterior = false;

    [Header("Ruta garantizada")]
    [Tooltip("Donde empieza el jugador. Vacio = DestinoSismo del ControladorNivel, o el XR Origin.")]
    public Transform puntoInicio;
    [Tooltip("Salidas del edificio. Vacio = objetos con FinNivel (y PuntoSalida si se marca abajo).")]
    public List<Transform> salidas = new List<Transform>();
    [Tooltip("Tambien usar los PuntoSalida de Jheeremy como salidas (ojo: algunos pueden ser escaleras).")]
    public bool incluirPuntosSalidaNPC = false;
    public float distanciaMinimaAInicio = 5f;
    public float distanciaMinimaASalida = 5f;
    [Tooltip("Frames que se espera para que Unity recorte el NavMesh antes de calcular la ruta.")]
    public int framesEsperaCarving = 3;
    [Tooltip("Distancia (m) a cada lado del bloqueo donde se comprueba que el pasillo sigue conectado por otro camino.")]
    public float distanciaLados = 3.5f;

    [Header("Activacion")]
    public MomentoActivacion momento = MomentoActivacion.AlIniciarSismo;
    [Tooltip("Segundos (min, max) despues del inicio del sismo. Cada bloqueo sortea el suyo.")]
    public Vector2 retardoAleatorio = new Vector2(1f, 12f);
    [Tooltip("Solo para AlAcercarseJugador: distancia a la que se dispara.")]
    public float radioAcercamiento = 8f;
    [Tooltip("Nunca activar un bloqueo si el jugador esta a menos de esto (no le cae encima).")]
    public float distanciaMinimaJugador = 3.5f;
    [Tooltip("Antes de activar, comprobar que el jugador (donde este) aun tenga salida.")]
    public bool comprobarRutaDesdeJugador = true;
    [Tooltip("Apaga el ControladorBloqueos de Uberto para que no active bloqueos sin comprobar la ruta.")]
    public bool desactivarControladorBloqueosOriginal = true;

    [Header("Pruebas")]
    [Tooltip("F6 = volver a sortear los bloqueos, F7 = activarlos todos ya, F8 = iniciar el sismo real (TV + alarma). Solo en el Editor.")]
    public bool teclasDePrueba = true;
    public bool mostrarPanel = true;
    public bool mostrarLog = true;

    // ---------------------------------------------------------------------
    class Candidato
    {
        public Vector3 posicion;
        public Quaternion rotacion;
        public TipoBloqueoJosue tipo;
        public string origen;
    }

    class Bloqueo
    {
        public BloqueoBase instancia;
        public NavMeshObstacle obstaculo;
        public TipoBloqueoJosue tipo;
        public Vector3 ladoA, ladoB;  // un punto a cada lado del bloqueo (en el pasillo)
        public bool programado, activado;
        public float activarEn, proximoIntento;
    }

    readonly List<Candidato> _candidatos = new List<Candidato>();
    readonly List<Bloqueo> _bloqueos = new List<Bloqueo>();
    readonly List<Vector3> _ultimaRuta = new List<Vector3>();
    NavMeshPath _ruta;
    ControladorNivel _controladorNivel;
    Transform _jugador;
    bool _hayNavMesh, _listo, _programado, _verificando, _sismoDetectado, _rutaJugadorLibre = true;
    bool _rutaGlobal; // ¿el NavMesh conecta el inicio con una salida (sin bloqueos)?
    int _descartados, _sinVerificar;
    float _proximaRevisionPanel;

    // ---------------------------------------------------------------------
    void OnEnable()
    {
        EventosSismo.Alarma += AlDispararAlarma;                 // alarma de Jheeremy
    }

    void OnDisable()
    {
        EventosSismo.Alarma -= AlDispararAlarma;
    }

    void AlDispararAlarma(Vector3 origen) { _sismoDetectado = true; }

    void Start()
    {
        _ruta = new NavMeshPath();
        StartCoroutine(Generar());
    }

    // =====================================================================
    // 1) GENERAR
    // =====================================================================
    IEnumerator Generar()
    {
        yield return null; // deja que el resto de la escena termine su Awake/Start

        if (semilla != 0) Random.InitState(semilla);
        ResolverReferencias();

        if (desactivarControladorBloqueosOriginal)
            foreach (var c in FindObjectsByType<ControladorBloqueos>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                c.enabled = false;

        _hayNavMesh = NavMesh.CalculateTriangulation().vertices.Length > 0;

        RecolectarCandidatos();
        Mezclar(_candidatos);

        if (!_hayNavMesh)
        {
            Debug.LogError("[Bloqueos Josue] La escena no tiene NavMesh: no se puede garantizar " +
                           "una salida, asi que NO se colocan bloqueos. Hornea el NavMesh (NavMeshSurface) del edificio.", this);
            _listo = true;
            yield break;
        }

        // Garantia 1 (si el NavMesh lo permite): inicio -> salida debe seguir existiendo.
        _rutaGlobal = puntoInicio != null && salidas.Count > 0 && HayRutaDesde(puntoInicio.position);
        if (!_rutaGlobal) DiagnosticarRutaGlobal();
        // Garantia 2 (siempre): cada bloqueo deja sus dos lados conectados por OTRO camino,
        // asi nunca se pierde una conexion que existia (sirve aunque el NavMesh este incompleto).

        int objetivo = Random.Range(cantidadBloqueos.x, Mathf.Max(cantidadBloqueos.x, cantidadBloqueos.y) + 1);
        _descartados = 0;
        _sinVerificar = 0;

        foreach (var c in _candidatos)
        {
            if (_bloqueos.Count >= objetivo) break;
            if (MuyCercaDeOtro(c.posicion)) continue;

            TipoBloqueoJosue tipo = ElegirTipo(c.tipo);
            GameObject prefab = tipo == TipoBloqueoJosue.Fuego ? prefabFuego : prefabRocas;
            if (prefab == null) continue;

            // Posicion y giro finales (se sortean ANTES de probar, asi se prueba exactamente lo que aparecera)
            Vector3 pos = c.posicion + c.rotacion * Vector3.right * Random.Range(-variacionPosicion, variacionPosicion);
            float giro = (Random.value < 0.5f ? 0f : 180f) + Random.Range(-variacionGiro, variacionGiro);
            Quaternion rot = c.rotacion * Quaternion.Euler(0f, giro, 0f);

            GameObject go = Instantiate(prefab, pos, rot, transform);
            go.name = prefab.name + "_Josue_" + _bloqueos.Count;
            var instancia = go.GetComponentInChildren<BloqueoBase>(true);
            if (instancia == null) { Destroy(go); continue; }
            instancia.activarAlIniciar = false;

            NavMeshObstacle obstaculo = instancia.obstaculoNavMesh != null
                ? instancia.obstaculoNavMesh : go.GetComponentInChildren<NavMeshObstacle>(true);

            yield return null; // corre el Start() de BloqueoBase (apaga collider y obstaculo)

            if (obstaculo == null)
            {
                Debug.LogWarning("[Bloqueos Josue] " + prefab.name + " no tiene NavMeshObstacle; no se puede comprobar la ruta.", go);
                Destroy(go);
                continue;
            }

            // Puntos a cada lado del bloqueo, que HOY (sin el) estan conectados.
            if (!Lados(pos, rot, out Vector3 ladoA, out Vector3 ladoB) || !Conectados(ladoA, ladoB))
            {
                Destroy(go);
                _sinVerificar++;
                continue;
            }

            EncenderObstaculo(obstaculo, true);
            yield return EsperarCarving();

            var nuevo = new Bloqueo { instancia = instancia, obstaculo = obstaculo, tipo = tipo, ladoA = ladoA, ladoB = ladoB };
            string motivo = null;
            if (_rutaGlobal && !HayRutaDesde(puntoInicio.position))
                motivo = "cerraba la ultima ruta del inicio a la salida";
            else if (!Conectados(ladoA, ladoB))
                motivo = "era el unico paso por ese pasillo";
            else
                foreach (var otro in _bloqueos)
                    if (!Conectados(otro.ladoA, otro.ladoB)) { motivo = "junto con " + otro.instancia.name + " cerraba el paso"; break; }

            if (motivo != null)
            {
                EncenderObstaculo(obstaculo, false);
                Destroy(go);
                _descartados++;
                if (mostrarLog) Debug.Log("[Bloqueos Josue] Descartado " + tipo + " en " + c.origen + ": " + motivo + ".");
                continue;
            }

            var b = nuevo;
            // El fuego se puede apagar con el extintor de Josue (Extintor/). Si el prefab no lo trae, se agrega.
            if (tipo == TipoBloqueoJosue.Fuego && go.GetComponentInChildren<FuegoApagable>(true) == null)
                go.AddComponent<FuegoApagable>();
            _bloqueos.Add(b);

            if (mostrarLog) Debug.Log("[Bloqueos Josue] + " + tipo + " en " + c.origen + " " + pos.ToString("F1"));
        }

        // Ya elegidos: se apagan los obstaculos hasta que cada bloqueo se active de verdad.
        foreach (var b in _bloqueos)
            if (b.obstaculo != null) EncenderObstaculo(b.obstaculo, false);

        Debug.Log("[Bloqueos Josue] " + _bloqueos.Count + " bloqueos colocados (objetivo " + objetivo + ", " +
                  _candidatos.Count + " candidatos, " + _descartados + " descartados por cerrar el paso, " +
                  _sinVerificar + " sin pasillo verificable). Ruta inicio->salida en NavMesh: " +
                  (_rutaGlobal ? "SI" : "NO (se usa solo la garantia por pasillo)") + ". Modo de activacion: " + momento);

        _listo = true;
        _programado = false;
        if (momento == MomentoActivacion.AlIniciarEscena) ProgramarTodos();
    }

    // =====================================================================
    // 2) ACTIVAR (durante el juego)
    // =====================================================================
    void Update()
    {
        if (!_listo) return;

        if (teclasDePrueba && Application.isEditor && Keyboard.current != null)
        {
            if (Keyboard.current.f6Key.wasPressedThisFrame) { Regenerar(); return; }
            if (Keyboard.current.f7Key.wasPressedThisFrame) ProgramarTodos(true);
            if (Keyboard.current.f8Key.wasPressedThisFrame) IniciarSismoReal();
        }

        if (momento == MomentoActivacion.AlIniciarSismo && !_programado && SismoComenzo())
            ProgramarTodos();

        if (mostrarPanel && _hayNavMesh && _jugador != null && Time.time >= _proximaRevisionPanel)
        {
            _proximaRevisionPanel = Time.time + 1f;
            _rutaJugadorLibre = HayRutaDesde(_jugador.position);
        }

        if (_verificando) return; // se activa de uno en uno, asi cada comprobacion ve a los anteriores

        foreach (var b in _bloqueos)
        {
            if (b.activado || b.instancia == null || Time.time < b.proximoIntento) continue;

            bool toca = momento == MomentoActivacion.AlAcercarseJugador && !b.programado
                ? JugadorEnRango(b)
                : b.programado && Time.time >= b.activarEn;

            if (toca) { StartCoroutine(ActivarSeguro(b)); break; }
        }
    }

    bool SismoComenzo()
    {
        return _sismoDetectado
            || (_controladorNivel != null && _controladorNivel.SismoIniciado);
    }

    void ProgramarTodos(bool inmediato = false)
    {
        _programado = true;
        foreach (var b in _bloqueos)
        {
            if (b.activado) continue;
            b.programado = true;
            b.activarEn = Time.time + (inmediato ? 0f : Random.Range(retardoAleatorio.x, retardoAleatorio.y));
        }
    }

    bool JugadorEnRango(Bloqueo b)
    {
        if (_jugador == null) return false;
        Vector3 d = _jugador.position - b.instancia.transform.position;
        if (Mathf.Abs(d.y) > 3f) return false; // otro piso
        d.y = 0f;
        return d.magnitude <= radioAcercamiento && d.magnitude >= distanciaMinimaJugador;
    }

    IEnumerator ActivarSeguro(Bloqueo b)
    {
        _verificando = true;

        // No dejar caer rocas / explotar fuego justo encima del jugador.
        if (_jugador != null)
        {
            Vector3 d = _jugador.position - b.instancia.transform.position; d.y = 0f;
            if (d.magnitude < distanciaMinimaJugador && Mathf.Abs(_jugador.position.y - b.instancia.transform.position.y) < 3f)
            {
                b.proximoIntento = Time.time + 1f;
                _verificando = false;
                yield break;
            }
        }

        // Solo se exige si el jugador HOY tiene ruta a la salida (si el NavMesh no llega, vale la garantia por pasillo).
        if (comprobarRutaDesdeJugador && _hayNavMesh && _jugador != null && b.obstaculo != null && HayRutaDesde(_jugador.position))
        {
            EncenderObstaculo(b.obstaculo, true);
            yield return EsperarCarving();
            if (!HayRutaDesde(_jugador.position))
            {
                // El jugador quedaria encerrado: se pospone y se vuelve a intentar despues.
                EncenderObstaculo(b.obstaculo, false);
                b.proximoIntento = Time.time + 2f;
                if (mostrarLog) Debug.Log("[Bloqueos Josue] Pospuesto " + b.instancia.name + ": encerraria al jugador.");
                _verificando = false;
                yield break;
            }
            // Si pasa, el obstaculo se queda encendido: asi la proxima comprobacion ya lo tiene en cuenta.
        }

        b.activado = true;
        b.instancia.Activar();           // secuencia de Jheeremy (polvo+rocas o explosion+llamas)
        if (mostrarLog) Debug.Log("[Bloqueos Josue] ACTIVADO " + b.instancia.name);
        _verificando = false;
    }

    // =====================================================================
    // Puntos candidatos
    // =====================================================================
    void RecolectarCandidatos()
    {
        _candidatos.Clear();

        if (usarPuntosBloqueo)
            foreach (var p in FindObjectsByType<PuntoBloqueo>(FindObjectsSortMode.None))
                if (Random.value <= p.probabilidad)
                    Agregar(p.transform.position, p.transform.rotation, p.tipo, "PuntoBloqueo '" + p.name + "'");

        if (usarBloqueosExistentes)
        {
            foreach (var b in FindObjectsByType<BloqueoBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.transform.IsChildOf(transform)) continue;  // son mios
                if (!b.gameObject.scene.IsValid()) continue;      // es un asset, no de la escena
                Vector3 pos = b.transform.position;
                b.gameObject.SetActive(false);                     // el original se apaga; aqui se sortea de nuevo
                Agregar(pos, RotacionTransversal(pos, b.transform.rotation), TipoBloqueoJosue.Aleatorio,
                        "existente '" + b.name + "'");
            }
        }

        if (usarPasillosGenerados)
        {
            foreach (var pc in FindObjectsByType<PasilloConfig>(FindObjectsSortMode.None))
            {
                if (!incluirVeredaExterior && pc.name.Contains("Vereda")) continue;

                Vector3 eje = pc.transform.forward; eje.y = 0f;
                if (eje.sqrMagnitude < 0.01f) continue;
                eje.Normalize();
                // El eje Z local del bloqueo debe cruzar el pasillo (perpendicular a su largo).
                Quaternion rot = Quaternion.LookRotation(Vector3.Cross(Vector3.up, eje), Vector3.up);

                float mitad = pc.largo * 0.5f - margenExtremosPasillo;
                if (mitad <= 0f) { Agregar(pc.transform.position, rot, TipoBloqueoJosue.Aleatorio, "pasillo " + pc.name); continue; }

                // El primer punto se corre al azar: cada partida cambia donde caen los puntos.
                float paso = Mathf.Max(1f, espaciadoEnPasillo);
                for (float z = -mitad + Random.Range(0f, paso); z <= mitad; z += paso)
                    Agregar(pc.transform.position + eje * z, rot, TipoBloqueoJosue.Aleatorio, "pasillo " + pc.name);
            }
        }
    }

    void Agregar(Vector3 pos, Quaternion rot, TipoBloqueoJosue tipo, string origen)
    {
        if (_hayNavMesh)
        {
            if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return; // no es caminable
            pos = hit.position;
        }

        if (puntoInicio != null && Vector3.Distance(pos, puntoInicio.position) < distanciaMinimaAInicio) return;
        foreach (var s in salidas)
            if (s != null && Vector3.Distance(pos, s.position) < distanciaMinimaASalida) return;
        foreach (var c in _candidatos)
            if ((c.posicion - pos).sqrMagnitude < 1.5f * 1.5f) return; // repetido

        _candidatos.Add(new Candidato { posicion = pos, rotacion = rot, tipo = tipo, origen = origen });
    }

    // Lanza rayos a los lados para saber hacia donde es angosto el pasillo,
    // y gira el bloqueo para que lo cruce de pared a pared.
    static Quaternion RotacionTransversal(Vector3 pos, Quaternion porDefecto)
    {
        Vector3 o = pos + Vector3.up * 1.2f;
        float sx = Espacio(o, Vector3.right) + Espacio(o, Vector3.left);
        float sz = Espacio(o, Vector3.forward) + Espacio(o, Vector3.back);
        if (Mathf.Abs(sx - sz) < 0.5f) return porDefecto;
        return sx < sz ? Quaternion.LookRotation(Vector3.right) : Quaternion.identity;
    }

    static float Espacio(Vector3 o, Vector3 dir)
    {
        return Physics.Raycast(o, dir, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : 30f;
    }

    bool MuyCercaDeOtro(Vector3 pos)
    {
        foreach (var b in _bloqueos)
        {
            Vector3 d = b.instancia.transform.position - pos;
            if (Mathf.Abs(d.y) > 2.5f) continue; // otro piso
            d.y = 0f;
            if (d.magnitude < separacionMinima) return true;
        }
        return false;
    }

    TipoBloqueoJosue ElegirTipo(TipoBloqueoJosue pedido)
    {
        TipoBloqueoJosue t = pedido != TipoBloqueoJosue.Aleatorio ? pedido
            : (Random.value < probabilidadFuego ? TipoBloqueoJosue.Fuego : TipoBloqueoJosue.Rocas);
        if (t == TipoBloqueoJosue.Fuego && prefabFuego == null) t = TipoBloqueoJosue.Rocas;
        if (t == TipoBloqueoJosue.Rocas && prefabRocas == null) t = TipoBloqueoJosue.Fuego;
        return t;
    }

    static void Mezclar<T>(List<T> lista) // Fisher-Yates
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }

    // =====================================================================
    // NavMesh
    // =====================================================================
    static void EncenderObstaculo(NavMeshObstacle o, bool encendido)
    {
        o.carving = true;
        o.carveOnlyStationary = false; // recorta al instante, sin esperar a que "este quieto"
        o.enabled = encendido;
    }

    IEnumerator EsperarCarving()
    {
        for (int i = 0; i < Mathf.Max(1, framesEsperaCarving); i++) yield return null;
    }

    // Un punto a cada lado del bloqueo, a lo largo del pasillo (eje X local), sobre el NavMesh y en el mismo piso.
    bool Lados(Vector3 pos, Quaternion rot, out Vector3 a, out Vector3 b)
    {
        a = b = pos;
        Vector3 eje = rot * Vector3.right; eje.y = 0f; eje.Normalize();
        if (!NavMesh.SamplePosition(pos - eje * distanciaLados, out NavMeshHit ha, 1.5f, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(pos + eje * distanciaLados, out NavMeshHit hb, 1.5f, NavMesh.AllAreas)) return false;
        if (Mathf.Abs(ha.position.y - pos.y) > 1.2f || Mathf.Abs(hb.position.y - pos.y) > 1.2f) return false;
        a = ha.position; b = hb.position;
        return true;
    }

    bool Conectados(Vector3 a, Vector3 b)
    {
        return NavMesh.CalculatePath(a, b, NavMesh.AllAreas, _ruta) && _ruta.status == NavMeshPathStatus.PathComplete;
    }

    // Explica en la consola POR QUE no hay ruta del inicio a la salida.
    void DiagnosticarRutaGlobal()
    {
        if (puntoInicio == null || salidas.Count == 0)
        {
            Debug.LogWarning("[Bloqueos Josue] Falta " + (puntoInicio == null ? "el punto de inicio" : "una salida (FinNivel)") +
                             ". Se usa solo la garantia por pasillo.", this);
            return;
        }
        bool inicioOk = NavMesh.SamplePosition(puntoInicio.position, out NavMeshHit hi, 3f, NavMesh.AllAreas);
        string txt = "[Bloqueos Josue] El NavMesh NO conecta el inicio '" + puntoInicio.name + "' " + puntoInicio.position.ToString("F1") +
                     (inicioOk ? "" : " (el inicio NO esta sobre el NavMesh)") + " con ninguna salida:";
        foreach (var s in salidas)
        {
            if (s == null) continue;
            if (!NavMesh.SamplePosition(s.position, out NavMeshHit hs, 3f, NavMesh.AllAreas))
            { txt += "\n - " + s.name + " " + s.position.ToString("F1") + ": la salida NO esta sobre el NavMesh"; continue; }
            if (inicioOk)
            {
                NavMesh.CalculatePath(hi.position, hs.position, NavMesh.AllAreas, _ruta);
                var c = _ruta.corners;
                txt += "\n - " + s.name + ": " + _ruta.status + (c.Length > 0 ? ", el camino se corta en " + c[c.Length - 1].ToString("F1") : "");
            }
        }
        Debug.LogWarning(txt + "\nNo es un error de los bloqueos: se colocan igual con la garantia por pasillo " +
                         "(cada bloqueo deja otro camino entre sus dos lados). Para la garantia completa, hornea el NavMesh " +
                         "de modo que conecte puertas, escaleras y la vereda hasta los puntos de encuentro.", this);
    }

    // ¿Existe un camino COMPLETO desde 'desde' hasta alguna salida?
    bool HayRutaDesde(Vector3 desde)
    {
        if (!NavMesh.SamplePosition(desde, out NavMeshHit a, 3f, NavMesh.AllAreas)) return false;
        foreach (var s in salidas)
        {
            if (s == null) continue;
            if (!NavMesh.SamplePosition(s.position, out NavMeshHit b, 3f, NavMesh.AllAreas)) continue;
            if (NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, _ruta) &&
                _ruta.status == NavMeshPathStatus.PathComplete)
            {
                _ultimaRuta.Clear();
                _ultimaRuta.AddRange(_ruta.corners);
                return true;
            }
        }
        return false;
    }

    void ResolverReferencias()
    {
        _controladorNivel = FindFirstObjectByType<ControladorNivel>();
        var xr = FindFirstObjectByType<XROrigin>();
        _jugador = xr != null ? (xr.Camera != null ? xr.Camera.transform : xr.transform)
                              : (Camera.main != null ? Camera.main.transform : null);

        if (puntoInicio == null)
        {
            Transform destino = _controladorNivel != null ? _controladorNivel.transform.Find("DestinoSismo") : null;
            if (destino == null)
            {
                var g = GameObject.Find("DestinoSismo");
                if (g != null) destino = g.transform;
            }
            puntoInicio = destino != null ? destino : (xr != null ? xr.transform : _jugador);
        }

        salidas.RemoveAll(s => s == null);
        if (salidas.Count == 0)
        {
            foreach (var f in FindObjectsByType<FinNivel>(FindObjectsSortMode.None)) salidas.Add(f.transform);
            if (incluirPuntosSalidaNPC || salidas.Count == 0)
                foreach (var p in FindObjectsByType<PuntoSalida>(FindObjectsSortMode.None)) salidas.Add(p.transform);
        }
    }

    // =====================================================================
    // Pruebas
    // =====================================================================
    [ContextMenu("Volver a sortear (en Play)")]
    public void Regenerar()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        foreach (var b in _bloqueos)
            if (b.instancia != null) Destroy(b.instancia.gameObject);
        _bloqueos.Clear();
        _listo = _verificando = false;
        semilla = 0; // en la regeneracion siempre se sortea distinto
        StartCoroutine(Generar());
    }

    // Hace lo mismo que el flujo normal (TV -> alarma) para no esperar la noticia y la cuenta regresiva.
    void IniciarSismoReal()
    {
        var inicio = ControladorInicio.Instancia;
        if (inicio == null) { _sismoDetectado = true; Debug.Log("[Bloqueos Josue] No hay ControladorInicio; solo se avisa a los bloqueos."); return; }
        if (inicio.EstadoActual == ControladorInicio.Estado.EsperandoTV) inicio.TelevisorEncendido();
        if (inicio.EstadoActual == ControladorInicio.Estado.Noticia && !inicio.permitirAlarmaDuranteNoticia) inicio.NoticiaTerminada();
        inicio.ActivarAlarma();
        Debug.Log("[Bloqueos Josue] F8: sismo iniciado (estado " + inicio.EstadoActual + ").");
    }

    void OnGUI()
    {
        if (!mostrarPanel || !Application.isEditor) return;
        int activos = 0;
        foreach (var b in _bloqueos) if (b.activado) activos++;
        GUI.Box(new Rect(10, Screen.height - 110, 560, 100),
            "Bloqueos Josue: " + _bloqueos.Count + " colocados, " + activos + " activos, " +
            _descartados + " descartados (cerraban el paso)\n" +
            "Sismo: " + (SismoComenzo() ? "SI" : "no (F8 para iniciarlo)") + "   |   Ruta inicio->salida en NavMesh: " +
            (!_hayNavMesh ? "sin NavMesh" : _rutaGlobal ? "SI" : "no (garantia por pasillo)") + "\n" +
            "Ruta del jugador a la salida ahora: " + (_rutaJugadorLibre ? "LIBRE" : "no (o fuera del NavMesh)") + "\n" +
            "F6 = volver a sortear   F7 = activar bloqueos ya   F8 = iniciar sismo");
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        foreach (var c in _candidatos) Gizmos.DrawWireSphere(c.posicion + Vector3.up * 0.2f, 0.3f);

        foreach (var b in _bloqueos)
        {
            if (b.instancia == null) continue;
            Gizmos.color = b.tipo == TipoBloqueoJosue.Fuego ? new Color(1f, 0.35f, 0f) : new Color(0.6f, 0.45f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(b.instancia.transform.position, b.instancia.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.up * 1.5f, new Vector3(3f, 3f, 4f));
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.magenta; // los dos lados que deben seguir conectados
            Gizmos.DrawSphere(b.ladoA + Vector3.up * 0.3f, 0.25f);
            Gizmos.DrawSphere(b.ladoB + Vector3.up * 0.3f, 0.25f);
        }

        Gizmos.color = Color.green; // ultima ruta libre encontrada
        for (int i = 1; i < _ultimaRuta.Count; i++)
            Gizmos.DrawLine(_ultimaRuta[i - 1] + Vector3.up * 0.3f, _ultimaRuta[i] + Vector3.up * 0.3f);

        if (puntoInicio != null) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(puntoInicio.position, 0.4f); }
        Gizmos.color = Color.green;
        foreach (var s in salidas) if (s != null) Gizmos.DrawSphere(s.position, 0.5f);
    }

#if UNITY_EDITOR
    // Al agregar el componente, carga solos los prefabs de Jheeremy.
    void Reset()
    {
        prefabFuego = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Jheeremy/Prefabs/fuego.prefab");
        prefabRocas = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Jheeremy/Prefabs/bloqueo.prefab");
    }
#endif
}
