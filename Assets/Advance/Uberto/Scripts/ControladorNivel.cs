using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControladorNivel : MonoBehaviour
{
    [System.Serializable]
    private class GrupoBloqueosPiso
    {
        public int clavePiso;
        public float alturaPiso;
        public PisoConfig piso;
        public List<BloqueoEscalera> bloqueos = new List<BloqueoEscalera>();
    }

    [Header("Pantalla de carga")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject imagenCargando;
    [SerializeField, Min(0f)] private float esperaAntesDeDesvanecer = 3f;
    [SerializeField, Min(0.01f)] private float duracionDesvanecimiento = 1f;
    [HideInInspector, SerializeField] private List<GameObject> bloqueos = new List<GameObject>();
    [Header("Sismo")]
    [SerializeField] private ControladorInicio controladorInicio;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform destinoSismo;
    [SerializeField] private Text cronometro;
    [SerializeField] private bool sismoActivo;
    [SerializeField, Min(0f)] private float intensidadTraslacion = 0.035f;
    [SerializeField, Min(0f)] private float intensidadRotacion = 0.8f;
    [SerializeField, Min(0.1f)] private float frecuenciaSismo = 8f;
    [Header("Focos y bloqueos por piso")]
    [SerializeField] private List<FocoConfig> focos = new List<FocoConfig>();
    [SerializeField] private List<GrupoBloqueosPiso> bloqueosPorPiso = new List<GrupoBloqueosPiso>();

    private CanvasGroup grupoImagenCargando;
    private Vector3 desplazamientoSismoAplicado;
    private Quaternion rotacionSismoAplicada = Quaternion.identity;
    private bool temblorAplicado;
    private bool sismoIniciado;
    private float tiempoSismo;
    private float alturaNivelRecopilacion = 4.5f;

    public bool SismoActivo
    {
        get => sismoActivo;
        set => sismoActivo = value;
    }

    public bool SismoIniciado => sismoIniciado;
    public float TiempoSismo => tiempoSismo;

    private void Awake()
    {
        if (canvas == null)
        {
            GameObject encontrado = GameObject.Find("Canvas");
            if (encontrado != null)
                canvas = encontrado;
        }

        if (imagenCargando == null && canvas != null)
        {
            Transform encontrado = canvas.transform.Find("ImagenCargando");
            if (encontrado != null)
                imagenCargando = encontrado.gameObject;
        }

        if (xrOrigin == null)
        {
            GameObject encontrado = GameObject.Find("XR Origin (XR Rig)");
            if (encontrado != null)
                xrOrigin = encontrado.transform;
        }

        if (destinoSismo == null)
        {
            Transform encontrado = transform.Find("DestinoSismo");
            if (encontrado != null)
                destinoSismo = encontrado;
        }

        if (cronometro == null && canvas != null)
        {
            Transform encontrado = canvas.transform.Find("Cronometro");
            if (encontrado != null)
                cronometro = encontrado.GetComponent<Text>();
        }

        if (canvas != null)
            canvas.SetActive(true);

        if (imagenCargando != null)
        {
            imagenCargando.SetActive(true);
            grupoImagenCargando = imagenCargando.GetComponent<CanvasGroup>();
            if (grupoImagenCargando == null)
                grupoImagenCargando = imagenCargando.AddComponent<CanvasGroup>();

            grupoImagenCargando.alpha = 1f;
        }

        if (cronometro != null)
        {
            cronometro.text = "00:00";
            cronometro.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        sismoActivo = false;
        sismoIniciado = false;
        RecopilarFocosYBloqueos();
        EstablecerFocosNormales();
        DesactivarTodosLosBloqueos();

        if (imagenCargando != null && grupoImagenCargando != null)
            StartCoroutine(DesvanecerImagenCargando());
    }

    private void Update()
    {
        if (!sismoIniciado && controladorInicio != null && (int)controladorInicio.EstadoActual == 3)
        {
            IniciarSismo();
        }
        if (sismoActivo)
        {
            tiempoSismo += Time.deltaTime;
            ActualizarCronometro();
        }

        if (xrOrigin == null)
            return;

        RetirarTemblorAnterior();
        if (!sismoActivo)
            return;

        float tiempo = Time.time * frecuenciaSismo;
        Vector3 desplazamiento = new Vector3(
            RuidoCentrado(tiempo, 0.17f),
            RuidoCentrado(0.43f, tiempo),
            RuidoCentrado(tiempo, 7.31f)) * intensidadTraslacion;

        Vector3 angulos = new Vector3(
            RuidoCentrado(tiempo + 13.2f, 0.29f),
            RuidoCentrado(0.71f, tiempo + 4.8f),
            RuidoCentrado(tiempo + 27.4f, 0.53f)) * intensidadRotacion;

        desplazamientoSismoAplicado = desplazamiento;
        rotacionSismoAplicada = Quaternion.Euler(angulos);
        xrOrigin.localPosition += desplazamientoSismoAplicado;
        xrOrigin.localRotation = rotacionSismoAplicada * xrOrigin.localRotation;
        temblorAplicado = true;
    }

    public void IniciarSismo()
    {
        if (sismoIniciado)
            return;

        sismoIniciado = true;
        if (xrOrigin != null && destinoSismo != null)
        {
            RetirarTemblorAnterior();
            xrOrigin.SetPositionAndRotation(destinoSismo.position, destinoSismo.rotation);
            Physics.SyncTransforms();
        }
        else if (destinoSismo == null)
        {
            Debug.LogWarning("Asigna un DestinoSismo para teletransportar el XR Origin.", this);
        }

        RecopilarFocosYBloqueos();
        AveriarLaMitadDeFocos();
        PermitirActivacionBloqueosIntercalados();

        tiempoSismo = 0f;
        if (cronometro != null)
            cronometro.text = "00:00";

        sismoActivo = true;
    }

    private void RecopilarFocosYBloqueos()
    {
        focos.Clear();
        FocoConfig[] focosEncontrados = FindObjectsByType<FocoConfig>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (FocoConfig foco in focosEncontrados)
        {
            if (foco != null && !focos.Contains(foco))
                focos.Add(foco);
        }

        bloqueosPorPiso.Clear();
        alturaNivelRecopilacion = ObtenerAlturaNivel();
        Dictionary<int, GrupoBloqueosPiso> grupos = new Dictionary<int, GrupoBloqueosPiso>();
        HashSet<BloqueoEscalera> bloqueosIncluidos = new HashSet<BloqueoEscalera>();

        PCGPabellonUniversitario[] generadores = FindObjectsByType<PCGPabellonUniversitario>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PCGPabellonUniversitario generador in generadores)
        {
            if (generador == null)
                continue;

            for (int indice = 0; indice < Mathf.Max(1, generador.numeroPisos); indice++)
            {
                float altura = generador.transform.position.y + indice * alturaNivelRecopilacion;
                int clave = CalcularClavePiso(altura);
                ObtenerOCrearGrupo(grupos, clave, altura, null);
            }
        }

        PisoConfig[] pisosEncontrados = FindObjectsByType<PisoConfig>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PisoConfig piso in pisosEncontrados)
        {
            if (piso == null)
                continue;

            float altura = piso.transform.position.y;
            int clave = CalcularClavePiso(altura);
            GrupoBloqueosPiso grupo = ObtenerOCrearGrupo(grupos, clave, altura, piso);
            foreach (BloqueoEscalera bloqueo in piso.ObtenerBloqueosEscalera())
            {
                if (bloqueo != null && bloqueo.gameObject.activeInHierarchy && bloqueosIncluidos.Add(bloqueo))
                    grupo.bloqueos.Add(bloqueo);
            }
        }

        // Conserva la compatibilidad con raíces asignadas manualmente que sí están activas.
        foreach (GameObject raiz in bloqueos)
        {
            if (raiz == null || !raiz.activeInHierarchy)
                continue;

            BloqueoEscalera[] hijos = raiz.GetComponentsInChildren<BloqueoEscalera>(true);
            foreach (BloqueoEscalera bloqueo in hijos)
                AgregarBloqueoSinPiso(bloqueo, grupos, bloqueosIncluidos);
        }

        BloqueoEscalera[] bloqueosEncontrados = FindObjectsByType<BloqueoEscalera>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (BloqueoEscalera bloqueo in bloqueosEncontrados)
            AgregarBloqueoSinPiso(bloqueo, grupos, bloqueosIncluidos);

        foreach (GrupoBloqueosPiso grupo in grupos.Values)
        {
            grupo.bloqueos.Sort(CompararPosicionDeEscalera);
            bloqueosPorPiso.Add(grupo);
        }
        bloqueosPorPiso.Sort((a, b) => a.clavePiso.CompareTo(b.clavePiso));
    }

    private float ObtenerAlturaNivel()
    {
        PCGPabellonUniversitario[] generadores = FindObjectsByType<PCGPabellonUniversitario>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PCGPabellonUniversitario generador in generadores)
        {
            if (generador != null)
                return Mathf.Max(0.1f, generador.alturaPisos + generador.grosorPisos);
        }

        PisoConfig[] pisosEncontrados = FindObjectsByType<PisoConfig>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PisoConfig piso in pisosEncontrados)
        {
            if (piso != null && piso.alturaNivel > 0.1f)
                return piso.alturaNivel;
        }
        return 4.5f;
    }

    private int CalcularClavePiso(float altura)
    {
        return Mathf.RoundToInt(altura / Mathf.Max(0.1f, alturaNivelRecopilacion));
    }

    private static GrupoBloqueosPiso ObtenerOCrearGrupo(
        Dictionary<int, GrupoBloqueosPiso> grupos, int clave, float altura, PisoConfig piso)
    {
        if (!grupos.TryGetValue(clave, out GrupoBloqueosPiso grupo))
        {
            grupo = new GrupoBloqueosPiso
            {
                clavePiso = clave,
                alturaPiso = altura,
                piso = piso
            };
            grupos.Add(clave, grupo);
        }
        else if (grupo.piso == null && piso != null)
        {
            grupo.piso = piso;
        }
        return grupo;
    }

    private void AgregarBloqueoSinPiso(
        BloqueoEscalera bloqueo,
        Dictionary<int, GrupoBloqueosPiso> grupos,
        HashSet<BloqueoEscalera> bloqueosIncluidos)
    {
        if (bloqueo == null || !bloqueo.gameObject.activeInHierarchy || !bloqueosIncluidos.Add(bloqueo))
            return;

        float altura = bloqueo.transform.position.y;
        int clave = CalcularClavePiso(altura);
        ObtenerOCrearGrupo(grupos, clave, altura, null).bloqueos.Add(bloqueo);
    }

    private void EstablecerFocosNormales()
    {
        foreach (FocoConfig foco in focos)
        {
            if (foco != null)
                foco.EstablecerNormal();
        }
    }

    private void AveriarLaMitadDeFocos()
    {
        if (focos.Count == 0)
            return;

        EstablecerFocosNormales();
        for (int i = focos.Count - 1; i > 0; i--)
        {
            int indiceAleatorio = Random.Range(0, i + 1);
            (focos[i], focos[indiceAleatorio]) = (focos[indiceAleatorio], focos[i]);
        }

        int cantidadAveriada = Mathf.CeilToInt(focos.Count / 2f);
        for (int i = 0; i < cantidadAveriada; i++)
        {
            if (focos[i] != null)
                focos[i].EstablecerAveriado();
        }
    }

    private void DesactivarTodosLosBloqueos()
    {
        foreach (GrupoBloqueosPiso grupo in bloqueosPorPiso)
        {
            foreach (BloqueoEscalera bloqueo in grupo.bloqueos)
                DesactivarBloqueo(bloqueo);
        }
    }

    private void PermitirActivacionBloqueosIntercalados()
    {
        DesactivarTodosLosBloqueos();
        if (bloqueosPorPiso.Count < 2)
            return;

        GrupoBloqueosPiso pisoSuperior = bloqueosPorPiso[bloqueosPorPiso.Count - 1];
        int cantidadEscaleras = pisoSuperior.bloqueos.Count;
        if (cantidadEscaleras == 0)
            return;

        int cantidadSeleccionada = cantidadEscaleras == 2 || cantidadEscaleras == 4
            ? cantidadEscaleras / 2
            : Mathf.Clamp(Mathf.CeilToInt(cantidadEscaleras / 2f), 1, cantidadEscaleras);

        List<int> posiciones = new List<int>(cantidadEscaleras);
        for (int i = 0; i < cantidadEscaleras; i++)
            posiciones.Add(i);
        for (int i = posiciones.Count - 1; i > 0; i--)
        {
            int indiceAleatorio = Random.Range(0, i + 1);
            (posiciones[i], posiciones[indiceAleatorio]) = (posiciones[indiceAleatorio], posiciones[i]);
        }

        HashSet<int> seleccionadas = new HashSet<int>();
        for (int i = 0; i < cantidadSeleccionada; i++)
            seleccionadas.Add(posiciones[i]);

        // Se recorre desde el último piso hasta el segundo; la planta baja queda libre.
        int alternancia = 0;
        for (int indicePiso = bloqueosPorPiso.Count - 1; indicePiso >= 1; indicePiso--)
        {
            List<BloqueoEscalera> escaleras = bloqueosPorPiso[indicePiso].bloqueos;
            for (int posicion = 0; posicion < escaleras.Count; posicion++)
            {
                bool usarSeleccion = (alternancia % 2) == 0;
                bool permitirActivacion = seleccionadas.Contains(posicion) == usarSeleccion;
                if (permitirActivacion)
                    PermitirActivacionBloqueo(escaleras[posicion]);
                else
                    DesactivarBloqueo(escaleras[posicion]);
            }
            alternancia++;
        }
    }

    private static void PermitirActivacionBloqueo(BloqueoEscalera bloqueo)
    {
        if (bloqueo == null)
            return;

        bloqueo.activarAlIniciar = false;
        bloqueo.PermitirActivacionPorSismo();
        bloqueo.enabled = true;
    }

    private static void DesactivarBloqueo(BloqueoEscalera bloqueo)
    {
        if (bloqueo == null)
            return;

        bloqueo.activarAlIniciar = false;
        bloqueo.RevocarActivacionPorSismo();
        if (bloqueo.colliderBloqueo != null)
            bloqueo.colliderBloqueo.enabled = false;
        if (bloqueo.obstaculoNavMesh != null)
            bloqueo.obstaculoNavMesh.enabled = false;
        bloqueo.enabled = false;
    }

    private static int CompararPosicionDeEscalera(BloqueoEscalera a, BloqueoEscalera b)
    {
        string nombreA = NombreEscalera(a);
        string nombreB = NombreEscalera(b);
        int comparacion = string.CompareOrdinal(nombreA, nombreB);
        if (comparacion != 0)
            return comparacion;

        int comparacionX = a.transform.position.x.CompareTo(b.transform.position.x);
        return comparacionX != 0
            ? comparacionX
            : a.transform.position.z.CompareTo(b.transform.position.z);
    }

    private static string NombreEscalera(BloqueoEscalera bloqueo)
    {
        if (bloqueo == null)
            return string.Empty;

        EscaleraConfig escalera = bloqueo.GetComponentInParent<EscaleraConfig>();
        return escalera != null ? escalera.name : bloqueo.transform.root.name;
    }

    private void ActualizarCronometro()
    {
        if (cronometro == null)
            return;

        int segundosTotales = Mathf.FloorToInt(tiempoSismo);
        cronometro.text = string.Format("{0:00}:{1:00}", segundosTotales / 60, segundosTotales % 60);
    }

    private IEnumerator DesvanecerImagenCargando()
    {
        yield return new WaitForSecondsRealtime(esperaAntesDeDesvanecer);

        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionDesvanecimiento)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime;
            grupoImagenCargando.alpha = Mathf.Lerp(1f, 0f, tiempoTranscurrido / duracionDesvanecimiento);
            yield return null;
        }

        grupoImagenCargando.alpha = 0f;
        imagenCargando.SetActive(false);
    }

    private void RetirarTemblorAnterior()
    {
        if (!temblorAplicado)
            return;

        xrOrigin.localPosition -= desplazamientoSismoAplicado;
        xrOrigin.localRotation = Quaternion.Inverse(rotacionSismoAplicada) * xrOrigin.localRotation;
        desplazamientoSismoAplicado = Vector3.zero;
        rotacionSismoAplicada = Quaternion.identity;
        temblorAplicado = false;
    }

    private static float RuidoCentrado(float x, float y)
    {
        return (Mathf.PerlinNoise(x, y) - 0.5f) * 2f;
    }

    private void OnDisable()
    {
        if (xrOrigin != null)
            RetirarTemblorAnterior();
    }

    private void OnValidate()
    {
        esperaAntesDeDesvanecer = Mathf.Max(0f, esperaAntesDeDesvanecer);
        duracionDesvanecimiento = Mathf.Max(0.01f, duracionDesvanecimiento);
        intensidadTraslacion = Mathf.Max(0f, intensidadTraslacion);
        intensidadRotacion = Mathf.Max(0f, intensidadRotacion);
        frecuenciaSismo = Mathf.Max(0.1f, frecuenciaSismo);
    }
}
