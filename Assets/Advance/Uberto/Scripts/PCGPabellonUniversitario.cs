using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TipoEdificio
{
    Cuadratico,
    L,
    Cruz
}

[ExecuteAlways]
public class PCGPabellonUniversitario : MonoBehaviour
{
    public struct UbicacionExtintorCompartida
    {
        public Vector3 posicionLocal;
        public float rotacionY;

        public UbicacionExtintorCompartida(Vector3 posicionLocal, float rotacionY)
        {
            this.posicionLocal = posicionLocal;
            this.rotacionY = rotacionY;
        }
    }

    [Header("Activación")]
    public bool generarCampus = false;
    public bool limpiarCampus = false;

    [Header("Configuración del Campus")]
    public TipoEdificio tipoEdificio = TipoEdificio.Cuadratico;

    [Tooltip("Cantidad de pisos que tiene el edificio")]
    [Min(1)] public int numeroPisos = 3;

    [Tooltip("El espacio vacío que hay entre cada piso (4.25 m)")]
    public float alturaPisos = 4.25f;

    [Tooltip("Ancho de los pasillos por defecto (4 m)")]
    public float anchoPasillos = 4f;

    [UnityEngine.Serialization.FormerlySerializedAs("espacioPupitres")]
    [Tooltip("Separación entre columnas de pupitres en salones y salas libres")]
    [Min(0f)] public float espacioColumnasPupitres = 0.8f;

    [Tooltip("Ancho de caminos en habitaciones y separación de pupitres respecto al profesor")]
    [Min(0f)] public float espacioCaminoHabitacion = 1f;

    [Tooltip("Grosor o espesor de las losas de piso (0.25 m)")]
    public float grosorPisos = 0.25f;

    [Tooltip("Grosor de las paredes interiores (0.20 m)")]
    public float grosorPared = 0.20f;

    [Header("Dimensiones Modificables del Pabellón")]
    [Tooltip("Ancho de pabellón en el eje X (límite menor: 12 m)")]
    public float anchoPabellon = 12f;

    [Tooltip("Largo de pabellón en el eje Z (límite menor: 48 m)")]
    public float largoPabellon = 48f;

    [Header("Área Disponible (Calculada: Ancho * Largo)")]
    [SerializeField] public float areaDisponible;

    // Alias de compatibilidad
    public float ladoMinimo { get => anchoPabellon; set => anchoPabellon = value; }

    // ──────────────────────────────────────────────────────────
    // PAREDES EXTERIORES
    // ──────────────────────────────────────────────────────────
    [Header("Paredes Exteriores")]
    [Tooltip("Grosor de las paredes exteriores sólidas (Norte, Sur, Oeste). Por defecto 0.25 m.")]
    public float grosorParedExterior = 0.25f;

    [Tooltip("Grosor de la pared de vidrio Este. Por defecto 0.05 m.")]
    public float grosorParedVidrio = 0.05f;

    public Material materialVidrio;

    // ──────────────────────────────────────────────────────────
    // ALTURAS DE LOSAS (0 = no se crea)
    // ──────────────────────────────────────────────────────────
    [Header("Alturas de Losas y Habitaciones (0 = sin objeto)")]
    [Tooltip("Grosor de la Losa Inferior de los pasillos (0 = no se crea)")]
    public float grosorLosaInferiorPasillo = 0f;

    [Tooltip("Grosor de la Losa Superior de los pasillos (0 = no se crea)")]
    public float grosorLosaSuperiorPasillo = 0f;

    [Tooltip("Grosor de la Losa Inferior (Suelo) de las habitaciones (0 = no se crea)")]
    public float grosorLosaInferiorHabitacion = 0f;

    [Tooltip("Grosor de la Losa Superior (Techo) de las habitaciones (0 = no se crea)")]
    public float grosorLosaSuperiorHabitacion = 0f;

    [Header("Límites de Espacio Interior Vacío para Habitaciones")]
    [Tooltip("Largo mínimo Z del espacio interior en habitaciones del bloque central")]
    public float limiteCentralHabitacionZ = 10f;
    [Tooltip("Ancho mínimo X del espacio interior en habitaciones del bloque central")]
    public float limiteCentralHabitacionX = 8f;

    [Tooltip("Largo Z del espacio interior en habitaciones de bloques de escalera (por defecto 5 m)")]
    public float limiteEscaleraHabitacionZ = 5f;
    [Tooltip("Ancho mínimo X del espacio interior en habitaciones de bloques de escalera (por defecto 7 m)")]
    public float limiteEscaleraHabitacionX = 7f;

    // ──────────────────────────────────────────────────────────
    // PREFABS
    // ──────────────────────────────────────────────────────────
    [Header("Prefabs Requeridos")]
    public GameObject pisoPrefab;
    public GameObject techoPisoPrefab;
    public GameObject losaPrefab;   // Assets/Advance/Uberto/Models/TechoPisoBase.fbx
    public GameObject bloqueHabitacionPrefab;
    public GameObject pasilloPrefab;
    public GameObject escaleraPrefab;
    public GameObject habitacionPrefab;
    public GameObject paredBasePrefab;
    public GameObject puertaPrefab;
    public GameObject pupitreProfesorPrefab;
    public GameObject pupitreEstudiantesPrefab;

    [Header("Dimensiones del Prefab Escalera (Unity: 7m x 5m)")]
    public float anchoEscalera = 7f;
    public float largoEscalera = 5f;

    [Header("Instancias Generadas")]
    public List<GameObject> pisosGenerados = new List<GameObject>();
    public GameObject contenedorParedesExteriores;

    [System.NonSerialized] public int[,] clasificacionHabitacionesCentral;
    [System.NonSerialized] private List<UbicacionExtintorCompartida> ubicacionesExtintoresCompartidas;
    [System.NonSerialized] private bool ubicacionesExtintoresCalculadas;

    public bool IntentarObtenerUbicacionesExtintorCompartidas(out List<UbicacionExtintorCompartida> ubicaciones)
    {
        if (!ubicacionesExtintoresCalculadas)
        {
            ubicaciones = null;
            return false;
        }

        ubicaciones = new List<UbicacionExtintorCompartida>(ubicacionesExtintoresCompartidas);
        return true;
    }

    public void GuardarUbicacionesExtintorCompartidas(IList<UbicacionExtintorCompartida> ubicaciones)
    {
        if (ubicacionesExtintoresCalculadas)
            return;

        ubicacionesExtintoresCompartidas = ubicaciones != null
            ? new List<UbicacionExtintorCompartida>(ubicaciones)
            : new List<UbicacionExtintorCompartida>();
        ubicacionesExtintoresCalculadas = true;
    }

    private void ReiniciarUbicacionesExtintorCompartidas()
    {
        ubicacionesExtintoresCompartidas = null;
        ubicacionesExtintoresCalculadas = false;
    }

    // ──────────────────────────────────────────────────────────
    // UNITY CALLBACKS
    // ──────────────────────────────────────────────────────────
    private void OnValidate()
    {
        CalcularDimensiones();
        CargarPrefabsSiEsNecesario();

        if (generarCampus) { generarCampus = false; GenerarEdificio(); }
        if (limpiarCampus) { limpiarCampus = false; LimpiarCampus();  }
    }

    private void Start() => CalcularDimensiones();

    // ──────────────────────────────────────────────────────────
    // DIMENSIONES
    // ──────────────────────────────────────────────────────────
    [ContextMenu("Calcular Dimensiones")]
    public void CalcularDimensiones()
    {
        numeroPisos = Mathf.Max(1, numeroPisos);
        anchoPabellon = Mathf.Max(12f, anchoPabellon);
        largoPabellon = Mathf.Max(48f, largoPabellon);

        float ratio = anchoPabellon / largoPabellon;
        if (ratio < 0.25f) largoPabellon = anchoPabellon / 0.25f;
        else if (ratio > 1.0f) largoPabellon = anchoPabellon;

        anchoPabellon = Mathf.Max(12f, anchoPabellon);
        largoPabellon = Mathf.Max(48f, largoPabellon);
        areaDisponible = anchoPabellon * largoPabellon;
    }

    // ──────────────────────────────────────────────────────────
    // GENERAR EDIFICIO
    // ──────────────────────────────────────────────────────────
    [ContextMenu("Generar Campus / Edificio")]
    public void GenerarEdificio()
    {
        CargarPrefabsSiEsNecesario();
        CalcularDimensiones();
        LimpiarCampus();

        switch (tipoEdificio)
        {
            case TipoEdificio.Cuadratico:
                if (Application.isPlaying)
                    StartCoroutine(GenerarEdificioCuadraticoEnParalelo());
                else
                    GenerarEdificioCuadratico();
                break;
            default:
                Debug.LogWarning($"Tipo {tipoEdificio} en desarrollo. Generando Cuadrático.");
                if (Application.isPlaying)
                    StartCoroutine(GenerarEdificioCuadraticoEnParalelo());
                else
                    GenerarEdificioCuadratico();
                break;
        }
    }

    // ──────────────────────────────────────────────────────────
    // EDIFICIO CUADRÁTICO
    // ──────────────────────────────────────────────────────────
    private void GenerarEdificioCuadratico()
    {
        List<PisoConfig> pisos = PrepararEdificioCuadratico();
        foreach (PisoConfig piso in pisos)
            piso.PrepararPiso();

        if (!PrepararClasificacionHabitaciones(pisos))
        {
            LimpiarCampus();
            return;
        }

        foreach (PisoConfig piso in pisos)
            piso.CompletarPiso();
    }

    private IEnumerator GenerarEdificioCuadraticoEnParalelo()
    {
        List<PisoConfig> pisos = PrepararEdificioCuadratico();
        // Las coroutines mantienen la creación de GameObjects en el hilo principal,
        // y escalonan el trabajo de cada piso entre frames para evitar una sola ráfaga.
        foreach (PisoConfig piso in pisos)
        {
            StartCoroutine(piso.PrepararPisoEnParalelo());
            yield return null;
        }

        while (!TodosLosPisosPreparados(pisos))
            yield return null;

        if (!PrepararClasificacionHabitaciones(pisos))
        {
            LimpiarCampus();
            yield break;
        }

        foreach (PisoConfig piso in pisos)
        {
            StartCoroutine(piso.CompletarPisoEnParalelo());
            yield return null;
        }

        while (!TodosLosPisosGenerados(pisos))
            yield return null;
    }

    private List<PisoConfig> PrepararEdificioCuadratico()
    {
        float alturaNivel = alturaPisos + grosorPisos;
        bool cuatroEscaleras = anchoPabellon >= 24f;
        List<PisoConfig> pisos = new List<PisoConfig>(numeroPisos);

        CrearLosaCompleta("TechoPiso_SueloGeneral",
            new Vector3(0f, -grosorPisos / 2f, 0f),
            anchoPabellon, largoPabellon, igualarMateriales: false);

        // Se crean/configuran primero desde el ultimo piso para fijar una clasificacion unica.
        for (int indice = numeroPisos - 1; indice >= 0; indice--)
        {
            GameObject instancia = pisoPrefab != null
                ? Instantiate(pisoPrefab, transform, false)
                : new GameObject();
            instancia.name = "Piso_" + indice;
            instancia.transform.SetParent(transform, false);
            instancia.transform.localPosition = new Vector3(0f, indice * alturaNivel, 0f);
            instancia.transform.localRotation = Quaternion.identity;

            PisoConfig configuracion = instancia.GetComponent<PisoConfig>();
            if (configuracion == null)
                configuracion = instancia.AddComponent<PisoConfig>();
            configuracion.ConfigurarDesde(this, indice, alturaNivel, cuatroEscaleras);

            pisosGenerados.Add(instancia);
            pisos.Add(configuracion);
        }

        // Estas piezas pertenecen al edificio y no a un piso individual.
        if (numeroPisos >= 2)
            GenerarParedesExteriores(alturaNivel);

        if (numeroPisos > 0)
        {
            float xVereda = anchoPabellon / 2f + anchoPasillos / 2f;
            CrearPasilloVeredaEntrada(transform, new Vector3(xVereda, 0f, 0f),
                largoPabellon, "Vereda_Entrada_Este");
        }

        return pisos;
    }

    private bool PrepararClasificacionHabitaciones(List<PisoConfig> pisos)
    {
        if (pisos == null || pisos.Count == 0)
            return false;

        // El piso superior crea la matriz una vez; la raiz la conserva y cada piso recibe su copia.
        if (clasificacionHabitacionesCentral == null)
        {
            int[,] clasificacion = pisos[0].PrepararClasificacionCentral();
            if (clasificacion == null)
            {
                Debug.LogError("No se pudo obtener la clasificacion central desde el piso superior.", this);
                return false;
            }

            clasificacionHabitacionesCentral = ClonarClasificacion(clasificacion);
        }

        foreach (PisoConfig piso in pisos)
            piso.AsignarClasificacionCompartida(clasificacionHabitacionesCentral);

        return true;
    }

    private static int[,] ClonarClasificacion(int[,] matriz)
    {
        if (matriz == null)
            return null;

        int[,] copia = new int[matriz.GetLength(0), matriz.GetLength(1)];
        System.Array.Copy(matriz, copia, matriz.Length);
        return copia;
    }

    private static bool TodosLosPisosPreparados(List<PisoConfig> pisos)
    {
        foreach (PisoConfig piso in pisos)
            if (piso != null && !piso.PreparacionFinalizada)
                return false;
        return true;
    }

    private static bool TodosLosPisosGenerados(List<PisoConfig> pisos)
    {
        foreach (PisoConfig piso in pisos)
            if (piso != null && !piso.GeneracionFinalizada)
                return false;
        return true;
    }


    // ──────────────────────────────────────────────────────────
    // PAREDES EXTERIORES
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// Paredes van desde la cara INFERIOR del 2do TechoPiso hasta la cara SUPERIOR del último.
    ///   yBase  = alturaPisos                       (cara inf. del 2do TechoPiso)
    ///   yTecho = numeroPisos * alturaNivel          (cara sup. del último TechoPiso)
    ///
    /// Norte y Sur tienen prioridad: se extienden para cerrar las esquinas.
    ///   ancho Norte/Sur = anchoPabellon + grosorParedExterior (hacia -X) + grosorParedVidrio (hacia +X)
    ///
    /// Oeste encaja entre Norte y Sur sin superponerse.
    /// Este (vidrio) encaja entre Norte y Sur sin superponerse.
    ///
    /// Nota: el vidrio (Este) sigue la misma extensión vertical.
    /// </summary>
    private void GenerarParedesExteriores(float alturaNivel)
    {
        contenedorParedesExteriores = new GameObject("ParedesExteriores");
        contenedorParedesExteriores.transform.SetParent(transform, false);

        float yBase  = -(grosorPisos/2f);                // cara inferior del 1er TechoPiso
        float yTecho = numeroPisos * alturaNivel;        // cara superior del último TechoPiso
        float ySegundo = yBase + alturaNivel;

        float alturaParedes = yTecho - yBase;
        float alturaVidrio = yTecho - ySegundo;

        float centroY       = (yBase + yTecho) / 2f;
        float centroVidrioY = (ySegundo + yTecho) / 2f;
        float gE = grosorParedExterior;
        float gV = grosorParedVidrio;

        // ── Norte (+Z) y Sur (-Z) ──
        // Se extienden en X: desde -anchoPabellon/2 - gE  hasta  anchoPabellon/2 + gV
        // Ancho total = anchoPabellon + gE + gV
        // Centro X = -gE/2 + gV/2 (desplazado para centrar el tramo extendido)
        float anchoNorteSur = anchoPabellon + gE + gV;
        float centroXns     = (gV - gE) / 2f;

        CrearPanelSolido(contenedorParedesExteriores.transform, "ParedExterior_Norte",
            new Vector3(centroXns, centroY, largoPabellon / 2f + gE / 2f),
            new Vector3(anchoNorteSur, alturaParedes, gE));

        CrearPanelSolido(contenedorParedesExteriores.transform, "ParedExterior_Sur",
            new Vector3(centroXns, centroY, -largoPabellon / 2f - gE / 2f),
            new Vector3(anchoNorteSur, alturaParedes, gE));

        // ── Oeste (-X) ── encaja entre Norte y Sur
        float largoEstOeste = largoPabellon;

        CrearPanelSolido(contenedorParedesExteriores.transform, "ParedExterior_Oeste",
            new Vector3(-anchoPabellon / 2f - gE / 2f, centroY, 0f),
            new Vector3(gE, alturaParedes, largoEstOeste));

        // ── Este (+X) ── VIDRIO, encaja entre Norte y Sur
        CrearPanelVidrio(contenedorParedesExteriores.transform, "ParedVidrio_Este",
            new Vector3(anchoPabellon / 2f + gV / 2f, centroVidrioY, 0f),
            new Vector3(gV, alturaVidrio, largoEstOeste));
    }

    // ──────────────────────────────────────────────────────────
    // PASILLO EXTERIOR
    // ──────────────────────────────────────────────────────────
    private void CrearPasilloVeredaEntrada(Transform padre, Vector3 localPos, float longitudZ, string nombre)
    {
        GameObject go = ObtenerOInstanciar(pasilloPrefab, padre, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        PasilloConfig pc = go.GetComponent<PasilloConfig>();
        if (pc != null)
        {
            float grosorInf = grosorLosaInferiorPasillo > 0 ? grosorLosaInferiorPasillo : grosorPisos;
            // Vereda: solo losa inferior, sin luces
            pc.tieneLuces = false;
            pc.Configurar(longitudZ, anchoPasillos, grosorInf, alturaPisos,
                          conTecho: false, conSuelo: true);
            if (losaPrefab != null) pc.losaPrefab = losaPrefab;
        }
    }
    // ──────────────────────────────────────────────────────────
    // HELPERS DE CREACIÓN — GEOMETRÍA EXTERIOR
    // ──────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────
    // HELPERS DE CREACIÓN — PANELES (PRIMITIVOS)
    // ──────────────────────────────────────────────────────────
    private void CrearPanelSolido(Transform padre, string nombre, Vector3 posLocal, Vector3 escala)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = escala;
    }

    private void CrearPanelVidrio(Transform padre, string nombre, Vector3 posLocal, Vector3 escala)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = escala;

        if (materialVidrio != null)
        {
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = materialVidrio;
        }
    }

    // ──────────────────────────────────────────────────────────
    // HELPERS DE CREACIÓN — LOSAS
    // ──────────────────────────────────────────────────────────
    private void CrearLosaCompleta(string nombre, Vector3 posLocal, float ancho, float largo, bool igualarMateriales)
    {
        CrearLosaParte(transform, nombre, posLocal, ancho, largo, igualarMateriales);
    }

    private void CrearLosaParte(Transform padre, string nombre, Vector3 posLocal, float ancho, float largo, bool igualarMats)
    {
        GameObject go = ObtenerOInstanciar(techoPisoPrefab, padre, nombre);
        go.transform.localPosition = posLocal;
        go.transform.localRotation = Quaternion.identity;

        TechoPisoConfig config = go.GetComponent<TechoPisoConfig>();
        if (config != null)
        {
            config.Configurar(ancho, grosorPisos, largo);
            if (igualarMats) config.IgualarMateriales();
        }
        else
        {
            go.transform.localScale = new Vector3(ancho, grosorPisos, largo);
        }
    }

    private GameObject ObtenerOInstanciar(GameObject prefab, Transform padre, string nombre)
    {
        GameObject go;
        if (prefab != null)
            go = Instantiate(prefab, padre);
        else
        {
            go = new GameObject();
            go.transform.SetParent(padre, false);
        }
        go.name = nombre;
        return go;
    }

    // ──────────────────────────────────────────────────────────
    // LIMPIAR
    // ──────────────────────────────────────────────────────────
    [ContextMenu("Limpiar Campus")]
    public void LimpiarCampus()
    {
        StopAllCoroutines();
        pisosGenerados.Clear();
        clasificacionHabitacionesCentral = null;
        ReiniciarUbicacionesExtintorCompartidas();

        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--)
            toDestroy.Add(transform.GetChild(i).gameObject);

        foreach (var go in toDestroy)
        {
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // CARGA AUTOMÁTICA DE PREFABS
    // ──────────────────────────────────────────────────────────
    private void CargarPrefabsSiEsNecesario()
    {
#if UNITY_EDITOR
        if (pisoPrefab == null)
            pisoPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/PisoConfig.prefab");
        if (techoPisoPrefab == null)
            techoPisoPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/BasePrefabs/TechoPiso.prefab");
        if (losaPrefab == null)
            losaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Models/TechoPisoBase.fbx");
        if (bloqueHabitacionPrefab == null)
            bloqueHabitacionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/BloqueHabitaciones.prefab");
        if (pasilloPrefab == null)
            pasilloPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Pasillo.prefab");
        if (habitacionPrefab == null)
            habitacionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Habitacion.prefab");
        if (paredBasePrefab == null)
            paredBasePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/BasePrefabs/ParedBase.prefab");
        if (puertaPrefab == null)
            puertaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Puerta.prefab");
        if (escaleraPrefab == null)
            escaleraPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/EscaleraEdificio.prefab");
        if (materialVidrio == null)
            materialVidrio = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Advance/Uberto/Vidrio.mat");
        if (pupitreProfesorPrefab == null)
            pupitreProfesorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/PupitreProfesor.prefab");
        if (pupitreEstudiantesPrefab == null)
            pupitreEstudiantesPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/PupitreEstudiantes.prefab");
#endif
    }
}
