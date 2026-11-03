using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cerebro del NPC: una "maquina de estados". En cada momento esta en UN estado,
// y el estado decide que hace en Update().
public class NPCPersona : MonoBehaviour
{
    public enum Estado { Normal, Reaccionando, Protegiendose, Evacuando, Esperando, Seguro }

    [Header("Rutas de evacuacion")]
    public RutaEvacuacion rutaPrincipal;
    public RutaEvacuacion rutaAlterna;

    [Header("Movimiento")]
    public float velocidadBase = 1.4f;      // metros por segundo (caminata normal)
    public float velocidadGiro = 6f;
    public float distanciaLlegada = 0.4f;   // a que distancia se considera "llegue al punto"
    public LayerMask capasSuelo = ~0;       // ~0 = todas las capas

    [Header("Personalidad")]
    public Vector2 retardoReaccion = new Vector2(0.5f, 3f); // segundos (min, max) antes de reaccionar
    [Range(0f, 1f)] public float nerviosismo = 0.3f;        // 0 = calmado, 1 = muy nervioso
    public float radioPercepcionFuego = 5f;                 // a cuantos metros del fuego entra en panico

    [Header("Partes visuales (las asigna el generador)")]
    public Transform visual;
    public Transform brazoIzq;
    public Transform brazoDer;
    public Renderer rendererCuerpo;

    public Estado EstadoActual { get; private set; } = Estado.Normal;

    RutaEvacuacion _rutaActual;
    List<Vector3> _puntos = new List<Vector3>();
    int _indice;              // punto de la ruta al que voy
    float _fase;              // avanza al caminar; alimenta el balanceo de brazos
    float _proximaRevision;   // cada cuanto reviso si mi ruta esta bloqueada
    float _velocidadPersonal; // cada NPC camina un poco distinto
    bool _panico;

    void Awake()
    {
        _velocidadPersonal = velocidadBase * Random.Range(0.85f, 1.15f);
        nerviosismo = Mathf.Clamp01(nerviosismo + Random.Range(-0.2f, 0.2f));
        // Color de ropa aleatorio para que no se vean todos iguales.
        if (rendererCuerpo != null)
            rendererCuerpo.material.color = Color.HSVToRGB(Random.value, 0.55f, 0.85f);
    }

    void OnEnable()
    {
        EventosSismo.SismoIniciado += AlIniciarSismo;   // me suscribo al "altavoz"
        EventosSismo.NpcsTotales++;
        if (EventosSismo.SismoActivo) AlIniciarSismo(); // por si aparezco con el sismo en curso
    }

    void OnDisable()
    {
        EventosSismo.SismoIniciado -= AlIniciarSismo;   // siempre desuscribirse
        EventosSismo.NpcsTotales--;
    }

    void AlIniciarSismo()
    {
        if (EstadoActual != Estado.Normal) return;
        StartCoroutine(SecuenciaReaccion());
    }

    // Corrutina = funcion que puede "pausarse" con yield y continuar despues.
    IEnumerator SecuenciaReaccion()
    {
        EstadoActual = Estado.Reaccionando;
        yield return new WaitForSeconds(Random.Range(retardoReaccion.x, retardoReaccion.y));

        EstadoActual = Estado.Protegiendose;                 // agacharse y cubrirse
        yield return new WaitUntil(() => !EventosSismo.SismoActivo); // espera a que termine el sismo
        yield return new WaitForSeconds(Random.Range(0.3f, 1.5f));   // se recompone

        IniciarEvacuacion();
    }

    void IniciarEvacuacion()
    {
        _rutaActual = ElegirRuta();
        if (_rutaActual == null) { EstadoActual = Estado.Esperando; return; }
        CargarRuta(_rutaActual);
        EstadoActual = Estado.Evacuando;
    }

    void Update()
    {
        switch (EstadoActual)
        {
            case Estado.Evacuando: Evacuar(); break;
            case Estado.Esperando: Esperar(); break;
            case Estado.Reaccionando: // gira la cabeza buscando que pasa
                transform.Rotate(0f, Mathf.Sin(Time.time * 5f) * 70f * Time.deltaTime, 0f);
                break;
        }
        AjustarAlSuelo();
        AnimarCuerpo();
    }

    // ---------- Estado EVACUANDO ----------
    void Evacuar()
    {
        if (Time.time >= _proximaRevision)
        {
            _proximaRevision = Time.time + 0.5f;
            RevisarRuta();
            if (EstadoActual != Estado.Evacuando) return;
        }

        if (_indice >= _puntos.Count) { LlegarASalvo(); return; }

        Vector3 dir = _puntos[_indice] - transform.position;
        dir.y = 0f;
        if (dir.magnitude < distanciaLlegada) { _indice++; return; }  // paso al siguiente punto

        dir.Normalize();
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), velocidadGiro * Time.deltaTime);

        float v = _velocidadPersonal * MultiplicadorVelocidad();
        transform.position += dir * v * Time.deltaTime;
        _fase += v * Time.deltaTime * 5f;
    }

    void RevisarRuta()
    {
        if (!RutaBloqueada(_puntos, _indice)) return;
        RutaEvacuacion nueva = ElegirRuta();     // busca una ruta libre
        if (nueva == null) { EstadoActual = Estado.Esperando; return; }
        if (nueva != _rutaActual) { _rutaActual = nueva; CargarRuta(nueva); }
    }

    // ---------- Estado ESPERANDO (todas las rutas bloqueadas) ----------
    void Esperar()
    {
        if (Time.time < _proximaRevision) return;
        _proximaRevision = Time.time + 1f;
        RutaEvacuacion r = ElegirRuta();
        if (r != null) { _rutaActual = r; CargarRuta(r); EstadoActual = Estado.Evacuando; }
    }

    void LlegarASalvo()
    {
        EstadoActual = Estado.Seguro;
        EventosSismo.NpcLlegoASalvo();
    }

    // ---------- Rutas y bloqueos ----------
    RutaEvacuacion ElegirRuta()
    {
        foreach (var r in new[] { rutaPrincipal, rutaAlterna })
        {
            if (r == null) continue;
            var pts = r.ObtenerPuntos();
            if (pts.Count == 0) continue;
            if (!RutaBloqueada(pts, IndiceMasCercano(pts))) return r;   // la primera libre
        }
        return null;
    }

    void CargarRuta(RutaEvacuacion ruta)
    {
        _puntos = ruta.ObtenerPuntos();
        _indice = IndiceMasCercano(_puntos);
    }

    int IndiceMasCercano(List<Vector3> pts)
    {
        int mejor = 0; float mejorD = float.MaxValue;
        for (int i = 0; i < pts.Count; i++)
        {
            float d = (pts[i] - transform.position).sqrMagnitude;
            if (d < mejorD) { mejorD = d; mejor = i; }
        }
        return mejor;
    }

    // Recorre la ruta desde mi posicion y revisa si algun tramo cruza un bloqueo o fuego.
    bool RutaBloqueada(List<Vector3> pts, int desde)
    {
        Vector3 anterior = transform.position;
        for (int i = Mathf.Max(desde, 0); i < pts.Count; i++)
        {
            if (SegmentoBloqueado(anterior, pts[i])) return true;
            anterior = pts[i];
        }
        return false;
    }

    bool SegmentoBloqueado(Vector3 a, Vector3 b)
    {
        // Tomamos una muestra cada ~0.5 m a lo largo del tramo.
        int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.5f));
        for (int k = 0; k <= n; k++)
        {
            Vector3 p = Vector3.Lerp(a, b, k / (float)n);
            foreach (var z in ZonaPeligro.Activas)
                if (z.tipo != TipoZona.Aglomeracion && z.Contiene(p)) return true;
        }
        return false;
    }

    // Fuego cerca = panico (mas rapido). Aglomeracion = mas lento.
    float MultiplicadorVelocidad()
    {
        float m = 1f + nerviosismo * 0.5f;
        _panico = false;
        foreach (var z in ZonaPeligro.Activas)
        {
            Vector3 d = z.transform.position - transform.position; d.y = 0f;
            if (z.tipo == TipoZona.Fuego && d.magnitude < radioPercepcionFuego)
            { _panico = true; m = Mathf.Max(m, 1.6f); }
            if (z.tipo == TipoZona.Aglomeracion && z.Contiene(transform.position))
                m *= 0.4f;
        }
        return m;
    }

    // ---------- Suelo y animacion procedural (sin Animator) ----------
    void AjustarAlSuelo()
    {
        // Rayo hacia abajo: asi el NPC sube y baja por rampas y escaleras.
        Vector3 origen = transform.position + Vector3.up * 0.6f;
        if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 1.5f,
                            capasSuelo, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, hit.point.y, 12f * Time.deltaTime);
            transform.position = p;
        }
    }

    void AnimarCuerpo()
    {
        float escalaY = 1f, izq = 0f, der = 0f, rebote = 0f;
        switch (EstadoActual)
        {
            case Estado.Reaccionando: izq = der = -30f; break;
            case Estado.Protegiendose: escalaY = 0.55f; izq = der = -160f; break; // agachado, manos en la cabeza
            case Estado.Esperando: izq = der = -90f; break;                        // brazos al frente
            case Estado.Evacuando:
                float balanceo = Mathf.Sin(_fase) * (_panico ? 55f : 35f);
                float levantar = _panico ? -60f : 0f;
                izq = levantar + balanceo; der = levantar - balanceo;
                rebote = Mathf.Abs(Mathf.Sin(_fase)) * 0.04f;
                break;
        }
        float dt = Time.deltaTime;
        if (visual != null)
        {
            visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(1f, escalaY, 1f), 8f * dt);
            visual.localPosition = new Vector3(0f, rebote, 0f);
        }
        if (brazoIzq != null) brazoIzq.localRotation = Quaternion.Slerp(brazoIzq.localRotation, Quaternion.Euler(izq, 0f, 0f), 10f * dt);
        if (brazoDer != null) brazoDer.localRotation = Quaternion.Slerp(brazoDer.localRotation, Quaternion.Euler(der, 0f, 0f), 10f * dt);
    }
}