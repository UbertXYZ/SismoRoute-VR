using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coloca mobiliario en una habitación según su tipo.
///
/// Dimensiones de los prefabs (espacio local, centro en 0,0,0):
///   PupitreProfesor  : 2.0 m en X (-1 a +1), 1.4 m en Z (-0.7 a +0.7). Frente = +Z local.
///   PupitreEstudiantes (grupo del prefab): 1.2 m en X y 1.4 m en Z local.
///   Las columnas se distribuyen siguiendo el eje Z local tras aplicar su giro.
///
/// Regla de generación:
///   PRIMERO se calcula todo (posiciones, rotaciones, conteos).
///   LUEGO se instancian los GameObjects.
///   Esto evita el bug de creación múltiple por re-entrada de OnValidate.
/// </summary>
[ExecuteAlways]
public class PupitresConfig : MonoBehaviour
{
    [Header("Dimensiones y Parámetros")]
    [Tooltip("Ancho útil libre (eje X local de la habitación)")]
    public float anchoLibre = 8f;

    [Tooltip("Largo útil libre (eje Z local de la habitación)")]
    public float largoLibre = 10f;

    [Tooltip("Altura libre vertical")]
    public float altura = 4.25f;

    [Tooltip("Tipo de habitación: 1=Salon, 2=Laboratorio, 3=Sala Libre, 4=Direccion, 5=Almacen")]
    [Range(0, 5)]
    public int tipo = 0;

    [UnityEngine.Serialization.FormerlySerializedAs("espacioPupitres")]
    [Min(0f)] public float espacioColumnasPupitres = 0.8f;
    [Min(0f)] public float espacioCaminoHabitacion = 1f;

    [Tooltip("Grosor del suelo")]
    public float grosorSuelo = 0f;

    [Header("Orientación de la Pared con Doble Puerta / Puerta Principal")]
    [Tooltip("Indica si la puerta principal está en la pared derecha (+X)")]
    public bool puertaEnDerecha   = false;
    [Tooltip("Indica si la puerta principal está en la pared izquierda (-X)")]
    public bool puertaEnIzquierda = false;
    [Tooltip("Indica si la puerta principal está en la pared frontal (+Z)")]
    public bool puertaEnFrontal   = false;
    [Tooltip("Indica si la puerta principal está en la pared trasera (-Z)")]
    public bool puertaEnTrasera   = false;

    [Range(0, 2)] public int cantidadPuertasFrontal;
    [Range(0, 2)] public int cantidadPuertasTrasera;
    [Range(0, 2)] public int cantidadPuertasIzquierda;
    [Range(0, 2)] public int cantidadPuertasDerecha;

    [Header("Existencia de Paredes (para Tipo 4)")]
    public bool tieneParedFrontal = true;
    public bool tieneParedTrasera = true;
    public bool tieneParedIzquierda = true;
    public bool tieneParedDerecha   = true;

    [Header("Prefabs")]
    public GameObject pupitreProfesorPrefab;
    public GameObject pupitreEstudiantesPrefab;

    [Header("Contenedores de Instancias")]
    public Transform   contenedorEstudiantes;
    public GameObject  instanciaProfesor;

    // ── Dimensiones fijas de los prefabs ──────────────────────────
    // PupitreProfesor: 2.0 × 1.4 m (X × Z). Frente = +Z local.
    private const float PROF_HALF_X = 1.0f;   // ±1 m en X local
    private const float PROF_HALF_Z = 0.7f;   // ±0.7 m en Z local

    // El prefab agrupa mesas y sillas: huella local de 1.2 × 1.4 m.
    private const float EST_HALF_X  = 0.6f;   // ±0.6 m en X local
    private const float EST_HALF_Z  = 0.7f;   // ±0.7 m en Z local

    // ── Guard contra re-entrada (OnValidate cascada) ──────────────
    private bool _generando = false;

    // ─────────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────────
    public void Configurar(float nuevoAncho, float nuevoLargo, float nuevaAltura, float nuevoGrosorSuelo, int nuevoTipo,
                           bool tienePuertaDer, bool tienePuertaIzq, bool tienePuertaFront, bool tienePuertaTras,
                           bool existeParedIzq = true, bool existeParedDer = true,
                           GameObject prefabProf = null, GameObject prefabEst = null,
                           float espacioColumnasPupitres = 0.8f,
                           float espacioCaminoHabitacion = 1f,
                           int cantidadPuertasDerecha = -1, int cantidadPuertasIzquierda = -1,
                           int cantidadPuertasFrontal = -1, int cantidadPuertasTrasera = -1,
                           bool existeParedFrontal = true, bool existeParedTrasera = true)
    {
        anchoLibre  = nuevoAncho;
        largoLibre  = nuevoLargo;
        altura      = nuevaAltura;
        grosorSuelo = nuevoGrosorSuelo;
        tipo        = nuevoTipo;

        puertaEnDerecha   = tienePuertaDer;
        puertaEnIzquierda = tienePuertaIzq;
        puertaEnFrontal   = tienePuertaFront;
        puertaEnTrasera   = tienePuertaTras;

        this.cantidadPuertasDerecha = cantidadPuertasDerecha >= 0
            ? Mathf.Clamp(cantidadPuertasDerecha, 0, 2) : (tienePuertaDer ? 1 : 0);
        this.cantidadPuertasIzquierda = cantidadPuertasIzquierda >= 0
            ? Mathf.Clamp(cantidadPuertasIzquierda, 0, 2) : (tienePuertaIzq ? 1 : 0);
        this.cantidadPuertasFrontal = cantidadPuertasFrontal >= 0
            ? Mathf.Clamp(cantidadPuertasFrontal, 0, 2) : (tienePuertaFront ? 1 : 0);
        this.cantidadPuertasTrasera = cantidadPuertasTrasera >= 0
            ? Mathf.Clamp(cantidadPuertasTrasera, 0, 2) : (tienePuertaTras ? 1 : 0);

        this.espacioColumnasPupitres = Mathf.Max(0f, espacioColumnasPupitres);
        this.espacioCaminoHabitacion = Mathf.Max(0f, espacioCaminoHabitacion);

        tieneParedIzquierda = existeParedIzq;
        tieneParedDerecha   = existeParedDer;
        tieneParedFrontal   = existeParedFrontal;
        tieneParedTrasera   = existeParedTrasera;

        if (prefabProf != null) pupitreProfesorPrefab    = prefabProf;
        if (prefabEst  != null) pupitreEstudiantesPrefab = prefabEst;

        GenerarPupitres();
    }

    // ─────────────────────────────────────────────────────────────
    // GENERACIÓN PRINCIPAL
    // Patrón: calcular TODO primero → luego instanciar.
    // ─────────────────────────────────────────────────────────────
    [ContextMenu("Generar Pupitres")]
    public void GenerarPupitres()
    {
        if (_generando) return;
        _generando = true;
        try
        {
            CargarPrefabsSiEsNecesario();
            LimpiarPupitres();

            if (tipo == 5 || tipo == 0) return; // Almacén: sin mobiliario

            switch (tipo)
            {
                case 1: GenerarSalonNormal();    break;
                case 2: GenerarLaboratorio();    break;
                case 3: GenerarSalaLibre();      break;
                case 4: GenerarDireccion();      break;
            }
        }
        finally
        {
            _generando = false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 1: SALÓN NORMAL
    //   • Profesor en una esquina libre de puertas.
    //   • Un espacio de 1 m separa al profesor de los estudiantes.
    //   • Las columnas usan espacioColumnasPupitres para separarse.
    // ─────────────────────────────────────────────────────────────
    private void GenerarSalonNormal()
    {
        GenerarAula(gapColumnas: espacioColumnasPupitres, gapPupitresEnColumna: 0f);
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 2: LABORATORIO
    //   • Usa espacioCaminoHabitacion frente al profesor y junto a las paredes.
    //   • Los pupitres de estudiantes quedan pegados entre sí.
    // ─────────────────────────────────────────────────────────────
    private void GenerarLaboratorio()
    {
        GenerarAula(gapColumnas: 0f, gapPupitresEnColumna: 0f);
    }

    private void GenerarAula(float gapColumnas, float gapPupitresEnColumna)
    {
        if (ContarParedesConPuerta() != 1 || ContarParedesConDoblePuerta() != 1)
            return;

        // Salón Normal reserva el camino de la pared con doble puerta y el
        // espacio frente al profesor. Laboratorio añade dos márgenes de pared.
        float margenPared = tipo == 2 ? espacioCaminoHabitacion : 0f;
        CalcularLayoutAula(
            out Vector3 posProf, out float rotYProf,
            out float rotYEst, out float minX, out float maxX,
            out float minZ, out float maxZ, margenPared, espacioCaminoHabitacion);

        if (pupitreProfesorPrefab != null)
            InstanciarProfesor(posProf, rotYProf);

        if (pupitreEstudiantesPrefab == null)
            return;

        var estudiantes = new List<(Vector3 pos, float rotY)>();
        AgregarGrillaEnRectangulo(
            minX, maxX, minZ, maxZ, rotYEst,
            gapColumnas, gapPupitresEnColumna, estudiantes,
            centrarEnArea: tipo == 2);
        InstanciarEstudiantes(estudiantes);
    }

    private void CalcularLayoutAula(
        out Vector3 posProf, out float rotYProf, out float rotYEst,
        out float minX, out float maxX, out float minZ, out float maxZ,
        float margenPared, float espacioCamino)
    {
        float medioX = anchoLibre / 2f;
        float medioZ = largoLibre / 2f;

        if (puertaEnDerecha || puertaEnIzquierda)
        {
            // El profesor se ubica en la pared opuesta a la puerta, en una
            // esquina sin puertas, y mira a lo largo de esa misma pared.
            float paredSinPuertaX = puertaEnDerecha ? -medioX : medioX;
            posProf = new Vector3(
                paredSinPuertaX + (puertaEnDerecha ? PROF_HALF_X : -PROF_HALF_X),
                grosorSuelo,
                -medioZ + PROF_HALF_Z);
            rotYProf = 0f;  // Frente local +Z, hacia la otra esquina sin puerta.
            rotYEst = 180f; // El frente local +Z queda orientado hacia la esquina del profesor.

            float margenIzquierdo = margenPared;
            float margenDerecho = margenPared;
            if (tipo != 2)
            {
                // En el salón normal solo se reserva el camino junto a la
                // pared que tiene la doble puerta.
                if (puertaEnIzquierda) margenIzquierdo = espacioCamino;
                else margenDerecho = espacioCamino;
            }

            minX = -medioX + margenIzquierdo;
            maxX =  medioX - margenDerecho;
            minZ = posProf.z + PROF_HALF_Z + espacioCamino;
            // La pared frente al profesor no lleva margen.
            maxZ = medioZ;
        }
        else
        {
            // Puerta frontal o trasera: la pared opuesta no tiene puerta.
            // El profesor queda en una esquina de esa pared y mira hacia la
            // segunda esquina libre, siguiendo el eje local +Z de su pupitre.
            bool puertaFrontal = puertaEnFrontal;
            posProf = new Vector3(
                -medioX + PROF_HALF_Z,
                grosorSuelo,
                puertaFrontal ? -medioZ + PROF_HALF_X : medioZ - PROF_HALF_X);
            rotYProf = 90f; // Frente local +Z apunta a +X.
            rotYEst = 270f;

            minX = posProf.x + PROF_HALF_Z + espacioCamino;
            // La pared frente al profesor no lleva margen.
            maxX = medioX;

            float margenTrasero = margenPared;
            float margenFrontal = margenPared;
            if (tipo != 2)
            {
                // En el salón normal solo se reserva el camino junto a la
                // pared que tiene la doble puerta.
                if (puertaFrontal) margenFrontal = espacioCamino;
                else margenTrasero = espacioCamino;
            }

            minZ = -medioZ + margenTrasero;
            maxZ =  medioZ - margenFrontal;
        }

        minX = Mathf.Max(-medioX, minX);
        maxX = Mathf.Min(medioX, maxX);
        minZ = Mathf.Max(-medioZ, minZ);
        maxZ = Mathf.Min(medioZ, maxZ);
    }

    private void AgregarGrillaEnRectangulo(
        float minX, float maxX, float minZ, float maxZ, float rotY,
        float gapColumnas, float gapPupitresEnColumna,
        List<(Vector3 pos, float rotY)> resultado, bool centrarEnArea = true)
    {
        if (resultado == null || maxX <= minX || maxZ <= minZ)
            return;

        Quaternion rotacion = Quaternion.Euler(0f, rotY, 0f);
        Quaternion rotacionInversa = Quaternion.Inverse(rotacion);
        Vector3[] esquinas =
        {
            new Vector3(minX, 0f, minZ),
            new Vector3(minX, 0f, maxZ),
            new Vector3(maxX, 0f, minZ),
            new Vector3(maxX, 0f, maxZ),
        };

        float minLocalX = float.PositiveInfinity;
        float maxLocalX = float.NegativeInfinity;
        float minLocalZ = float.PositiveInfinity;
        float maxLocalZ = float.NegativeInfinity;
        foreach (Vector3 esquina in esquinas)
        {
            Vector3 local = rotacionInversa * esquina;
            minLocalX = Mathf.Min(minLocalX, local.x);
            maxLocalX = Mathf.Max(maxLocalX, local.x);
            minLocalZ = Mathf.Min(minLocalZ, local.z);
            maxLocalZ = Mathf.Max(maxLocalZ, local.z);
        }

        // Primero se define la orientación del pupitre; luego cada columna
        // avanza sobre su eje Z local. El eje X local separa las columnas.
        List<float> columnas = CalcularCentros(
            minLocalX + EST_HALF_X, maxLocalX - EST_HALF_X,
            2f * EST_HALF_X + gapColumnas, centrarEnArea);
        List<float> pupitresColumna = CalcularCentros(
            minLocalZ + EST_HALF_Z, maxLocalZ - EST_HALF_Z,
            2f * EST_HALF_Z + gapPupitresEnColumna, centrarEnArea);

        foreach (float xLocal in columnas)
        {
            foreach (float zLocal in pupitresColumna)
            {
                Vector3 posLocal = rotacion * new Vector3(xLocal, grosorSuelo, zLocal);
                resultado.Add((new Vector3(posLocal.x, grosorSuelo, posLocal.z), rotY));
            }
        }
    }

    private static List<float> CalcularCentros(float minCentro, float maxCentro, float paso, bool centrarEnArea = true)
    {
        var centros = new List<float>();
        if (paso <= 0f || maxCentro < minCentro)
            return centros;

        int cantidad = Mathf.FloorToInt((maxCentro - minCentro) / paso) + 1;
        float espacioUsado = (cantidad - 1) * paso;
        float inicio = centrarEnArea
            ? (minCentro + maxCentro - espacioUsado) / 2f
            : minCentro;
        for (int i = 0; i < cantidad; i++)
            centros.Add(inicio + i * paso);

        return centros;
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 3: SALA LIBRE
    //   • Sin pupitre de profesor.
    //   • Deja una cruz central y un margen libre junto a las paredes.
    //   • Cada una de las cuatro zonas mira al brazo de la cruz a su lado.
    // ─────────────────────────────────────────────────────────────
    private void GenerarSalaLibre()
    {
        if (pupitreEstudiantesPrefab == null) return;

        float mitadPasillo = Mathf.Max(0f, espacioCaminoHabitacion) / 2f;
        float margenPared = Mathf.Max(0f, espacioCaminoHabitacion);
        float minX = -anchoLibre / 2f + margenPared;
        float maxX =  anchoLibre / 2f - margenPared;
        float minZ = -largoLibre / 2f + margenPared;
        float maxZ =  largoLibre / 2f - margenPared;

        var posiciones = new List<(Vector3 pos, float rotY)>();

        // El camino perimetral y la cruz mantienen su ancho configurable.
        // La cruz mantiene libres sus cuatro brazos y su cuadrado central.
        // Las zonas de la izquierda miran hacia +X y las de la derecha hacia -X,
        // orientadas al camino que queda junto a cada una.
        AgregarZonaSalaLibre(minX, -mitadPasillo, minZ, -mitadPasillo, 90f, posiciones);
        AgregarZonaSalaLibre(minX, -mitadPasillo, mitadPasillo, maxZ, 90f, posiciones);
        AgregarZonaSalaLibre(mitadPasillo, maxX, minZ, -mitadPasillo, 270f, posiciones);
        AgregarZonaSalaLibre(mitadPasillo, maxX, mitadPasillo, maxZ, 270f, posiciones);

        InstanciarEstudiantes(posiciones, "SalaLibre");
    }

    private void AgregarZonaSalaLibre(
        float minX, float maxX, float minZ, float maxZ, float rotY,
        List<(Vector3 pos, float rotY)> posiciones)
    {
        AgregarGrillaEnRectangulo(
            minX, maxX, minZ, maxZ, rotY,
            gapColumnas: espacioColumnasPupitres,
            gapPupitresEnColumna: 0f,
            resultado: posiciones);
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 4: DIRECCIÓN
    //   • Solo 1 pupitre profesor, centrado en una pared disponible sin puerta.
    // ─────────────────────────────────────────────────────────────
    private void GenerarDireccion()
    {
        if (pupitreProfesorPrefab == null) return;
        const float espacioPared = 0.05f;

        // Busca una pared existente sin puerta; el escritorio queda centrado
        // contra ella y orientado hacia el interior.
        if (tieneParedFrontal && cantidadPuertasFrontal == 0 && anchoLibre >= 2f * PROF_HALF_X)
        {
            CrearProfesorDireccion(new Vector3(0f, grosorSuelo, largoLibre / 2f - PROF_HALF_Z - espacioPared), 180f);
        }
        else if (tieneParedTrasera && cantidadPuertasTrasera == 0 && anchoLibre >= 2f * PROF_HALF_X)
        {
            CrearProfesorDireccion(new Vector3(0f, grosorSuelo, -largoLibre / 2f + PROF_HALF_Z + espacioPared), 0f);
        }
        else if (tieneParedIzquierda && cantidadPuertasIzquierda == 0 && largoLibre >= 2f * PROF_HALF_X)
        {
            CrearProfesorDireccion(new Vector3(-anchoLibre / 2f + PROF_HALF_Z + espacioPared, grosorSuelo, 0f), 90f);
        }
        else if (tieneParedDerecha && cantidadPuertasDerecha == 0 && largoLibre >= 2f * PROF_HALF_X)
        {
            CrearProfesorDireccion(new Vector3(anchoLibre / 2f - PROF_HALF_Z - espacioPared, grosorSuelo, 0f), 270f);
        }
    }

    private void CrearProfesorDireccion(Vector3 posicion, float rotY)
    {
        instanciaProfesor = Instantiate(pupitreProfesorPrefab, transform);
        instanciaProfesor.name = "PupitreProfesor_Direccion";
        instanciaProfesor.transform.localPosition = posicion;
        instanciaProfesor.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS DE INSTANCIACIÓN
    // ─────────────────────────────────────────────────────────────
    private void InstanciarProfesor(Vector3 posicion, float rotY)
    {
        if (pupitreProfesorPrefab == null) return;
        instanciaProfesor = Instantiate(pupitreProfesorPrefab, transform);
        instanciaProfesor.name = "PupitreProfesor";
        instanciaProfesor.transform.localPosition = posicion;
        instanciaProfesor.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
    }

    private void InstanciarEstudiantes(List<(Vector3 pos, float rotY)> posiciones, string prefijo = "Pupitre")
    {
        if (pupitreEstudiantesPrefab == null || posiciones == null || posiciones.Count == 0) return;
        AsegurarContenedorEstudiantes();
        for (int i = 0; i < posiciones.Count; i++)
        {
            GameObject p = Instantiate(pupitreEstudiantesPrefab, contenedorEstudiantes);
            p.name = $"{prefijo}_{i:D3}";
            p.transform.localPosition = posiciones[i].pos;
            p.transform.localRotation = Quaternion.Euler(0f, posiciones[i].rotY, 0f);
        }
    }

    private void AsegurarContenedorEstudiantes()
    {
        if (contenedorEstudiantes != null) return;
        Transform t = transform.Find("Pupitre de Estudiantes");
        if (t != null)
        {
            contenedorEstudiantes = t;
        }
        else
        {
            GameObject go = new GameObject("Pupitre de Estudiantes");
            go.transform.SetParent(transform, false);
            contenedorEstudiantes = go.transform;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // LIMPIEZA
    // Destruye TODOS los GameObjects de pupitres (no solo el trackeado),
    // para evitar duplicados en caso de llamadas cascada de OnValidate.
    // ─────────────────────────────────────────────────────────────
    [ContextMenu("Limpiar Pupitres")]
    public void LimpiarPupitres()
    {
        Transform contenedor = contenedorEstudiantes != null
            ? contenedorEstudiantes
            : transform.Find("Pupitre de Estudiantes");
        if (contenedor != null)
        {
            for (int i = contenedor.childCount - 1; i >= 0; i--)
                DestruirInstancia(contenedor.GetChild(i).gameObject);
        }

        // El contenedor queda disponible para la siguiente regeneración.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name.StartsWith("PupitreProfesor"))
                DestruirInstancia(hijo.gameObject);
        }

        instanciaProfesor      = null;
        contenedorEstudiantes  = contenedor;
    }

    private int ContarParedesConPuerta()
    {
        int cantidad = 0;
        if (cantidadPuertasFrontal > 0) cantidad++;
        if (cantidadPuertasTrasera > 0) cantidad++;
        if (cantidadPuertasIzquierda > 0) cantidad++;
        if (cantidadPuertasDerecha > 0) cantidad++;
        return cantidad;
    }

    private int ContarParedesConDoblePuerta()
    {
        int cantidad = 0;
        if (cantidadPuertasFrontal >= 2) cantidad++;
        if (cantidadPuertasTrasera >= 2) cantidad++;
        if (cantidadPuertasIzquierda >= 2) cantidad++;
        if (cantidadPuertasDerecha >= 2) cantidad++;
        return cantidad;
    }

    private static void DestruirInstancia(GameObject objeto)
    {
        if (objeto == null) return;
        if (Application.isPlaying) Destroy(objeto);
        else DestroyImmediate(objeto);
    }

    // ─────────────────────────────────────────────────────────────
    // CARGA AUTOMÁTICA DE PREFABS
    // ─────────────────────────────────────────────────────────────
    private void CargarPrefabsSiEsNecesario()
    {
#if UNITY_EDITOR
        if (pupitreProfesorPrefab == null)
            pupitreProfesorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Advance/Uberto/Prefabs/PupitreProfesor.prefab");
        if (pupitreEstudiantesPrefab == null)
            pupitreEstudiantesPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Advance/Uberto/Prefabs/PupitreEstudiantes.prefab");
#endif
    }
}
