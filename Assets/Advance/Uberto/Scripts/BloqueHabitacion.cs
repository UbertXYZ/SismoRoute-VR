using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BloqueHabitacion : MonoBehaviour
{
    [Header("Activación")]
    public bool crearBloque = false;

    [Header("Configuración de Cuadrícula (M x N = Z x X)")]
    [Tooltip("Cantidad de habitaciones a lo largo del eje Z (local)")]
    [Min(1)] public int m = 4;

    [Tooltip("Cantidad de habitaciones a lo largo del eje X (local)")]
    [Min(1)] public int n = 2;

    [Header("Control de Pasillos")]
    [Tooltip("Permite pasillos paralelos al eje Z (cada múltiplo de 2 en N)")]
    public bool permitirPasillosZ = true;

    [Tooltip("Permite pasillos paralelos al eje X (cada múltiplo de 3 en M)")]
    public bool permitirPasillosX = true;

    [Tooltip("Ancho de los pasillos")]
    public float anchoPasillo = 4f;
    [Tooltip("Espesor de las losas inferiores de pasillo (0 = no se crea)")]
    public float grosorLosaInferiorPasillo = 0f;
    [Tooltip("Espesor de las losas superiores de pasillo (0 = no se crea)")]
    public float grosorLosaSuperiorPasillo = 0f;

    [Header("Dimensiones de las Habitaciones (Espacio Vacío Interior)")]
    [Tooltip("Dimensión interior útil en el eje X de cada habitación")]
    public float tamanoHabitacionX = 8f;
    [Tooltip("Dimensión interior útil en el eje Z de cada habitación")]
    public float tamanoHabitacionZ = 10f;
    [Tooltip("Altura en el eje Y de cada habitación")]
    public float altoHabitacion = 4.25f;

    [Min(0.01f)] public float grosorPared = 0.20f;
    [Min(0f)]    public float grosorSuelo  = 0f;
    [Min(0f)]    public float grosorTecho  = 0f;

    [Tooltip("Generar losa TechoPiso debajo del bloque (desactivado por defecto si las losas del campus lo cubren)")]
    public bool generarTechoPisoBase = false;

    [Header("Borde con Pared de Vidrio")]
    [Tooltip("Indica si en el borde Este (+X) hay pared de vidrio (no se crea pared exterior Este)")]
    public bool pegadoAParedVidrio = false;
    [Tooltip("Indica si en el borde frontal (+Z) hay pared de vidrio")]
    public bool pegadoAParedVidrioNorte = false;
    [Tooltip("Indica si en el borde trasero (-Z) hay pared de vidrio")]
    public bool pegadoAParedVidrioSur = false;
    [Tooltip("Indica si en el borde izquierdo (-X) hay pared de vidrio")]
    public bool pegadoAParedVidrioOeste = false;

    // Compatibilidad previa
    public float tamanoHabitacion
    {
        get => Mathf.Max(tamanoHabitacionX, tamanoHabitacionZ);
        set { tamanoHabitacionX = value; tamanoHabitacionZ = value; }
    }

    public enum TipoPuertasBloque
    {
        Ninguna,
        HaciaPasillosZ,
        HaciaPasillosX,
        SoloUnaPuerta,
        Personalizado
    }

    [Header("Reglas de Puertas")]
    public TipoPuertasBloque tipoPuertas = TipoPuertasBloque.SoloUnaPuerta;
    [Tooltip("Compatibilidad con configuraciones antiguas; el número de puertas se determina por el tipo y altura del pasillo")]
    public bool doblePuerta = false;

    [Header("Pasillos en Bordes Exteriores (del Edificio)")]
    [Tooltip("Indica si en el borde frontal (+Z) hay un pasillo del edificio")]
    public bool pasilloEnBordeFrontal = false;
    [Tooltip("Indica si en el borde trasero (-Z) hay un pasillo del edificio")]
    public bool pasilloEnBordeTrasero = false;
    [Tooltip("Indica si en el borde izquierdo (-X) hay un pasillo del edificio")]
    public bool pasilloEnBordeIzquierdo = false;
    [Tooltip("Indica si en el borde derecho (+X) hay un pasillo del edificio")]
    public bool pasilloEnBordeDerecho = false;

    [Header("Prefabs")]
    public GameObject habitacionPrefab;
    public GameObject pasilloPrefab;
    public GameObject techoPisoPrefab;
    public GameObject techoPisoBasePrefab;  // Prefab para losas de pasillos internos
    public GameObject paredPrefab;          // Prefab para las paredes generadas en el bloque
    public GameObject pupitreProfesorPrefab;
    public GameObject pupitreEstudiantesPrefab;

    [Header("Contexto de Bloque")]
    [Tooltip("Indica si este bloque corresponde a un extremo junto a las escaleras")]
    public bool esBloqueEscalera = false;
    [Tooltip("Indica si este bloque está en el último piso")]
    public bool esUltimoPiso = false;
    [Tooltip("Indica si este bloque pertenece al primer piso")]
    public bool esPrimerPiso = false;

    [Header("Distribución de pupitres")]
    [UnityEngine.Serialization.FormerlySerializedAs("espacioPupitres")]
    [Min(0f)] public float espacioColumnasPupitres = 0.8f;
    [Min(0f)] public float espacioCaminoHabitacion = 1f;

    private int[,] clasificacionCompartida;
    private int[,] clasificacionHabitacionesGenerada;

    [Header("Instancias Generadas")]
    public TechoPisoConfig techoPisoBase;
    public List<HabitacionConfig> habitaciones = new List<HabitacionConfig>();
    public List<PasilloConfig>    pasillosZ    = new List<PasilloConfig>();
    public List<PasilloConfig>    pasillosX    = new List<PasilloConfig>();
    public List<GameObject>       paredesGeneradas = new List<GameObject>();

    [Header("Dimensiones Totales Calculadas")]
    [SerializeField] private float totalAnchoX;
    [SerializeField] private float totalLargoZ;
    public float TotalAnchoX => totalAnchoX;
    public float TotalLargoZ => totalLargoZ;

    private void OnValidate()
    {
        if (crearBloque)
        {
            crearBloque = false;
            GenerarBloque();
        }
        CargarPrefabsSiEsNecesario();
    }

    private void Start()
    {
        if (habitaciones.Count == 0 && transform.childCount > 0)
            AutoDetectarElementos();
    }

    // ── Sobrecarga de compatibilidad sin grosor losas ──
    public void Configurar(int habitacionesZ, int habitacionesX, float habX, float habZ, float altura,
        bool pasillosZ, bool pasillosX, float anchoPas,
        TipoPuertasBloque puertas, bool esDoblePuerta)
    {
        Configurar(habitacionesZ, habitacionesX, habX, habZ, altura,
            pasillosZ, pasillosX, anchoPas, puertas, esDoblePuerta,
            false, false, false, false, false);
    }

    public void Configurar(int habitacionesZ, int habitacionesX, float habX, float habZ, float altura,
        bool pasillosZFlag, bool pasillosXFlag, float anchoPas,
        TipoPuertasBloque puertas, bool esDoblePuerta,
        bool tienePasilloFrontal, bool tienePasilloTrasero,
        bool tienePasilloIzquierdo, bool tienePasilloDerecho,
        bool esParedVidrioEste = false,
        bool esExtremoEscalera = false,
        bool esPisoFinal = false,
        float nuevoEspacioColumnasPupitres = 0.8f,
        float nuevoEspacioCaminoHabitacion = 1f,
        int[,] tiposHabitacionesCompartidos = null,
        bool esPisoInicial = false)
    {
        m                    = Mathf.Max(1, habitacionesZ);
        n                    = Mathf.Max(1, habitacionesX);
        tamanoHabitacionX    = Mathf.Max(1f, habX);
        tamanoHabitacionZ    = Mathf.Max(1f, habZ);
        altoHabitacion       = Mathf.Max(1f, altura);
        permitirPasillosZ    = pasillosZFlag;
        permitirPasillosX    = pasillosXFlag;
        anchoPasillo         = Mathf.Max(0.5f, anchoPas);
        tipoPuertas          = puertas;
        doblePuerta          = esDoblePuerta;
        pasilloEnBordeFrontal    = tienePasilloFrontal;
        pasilloEnBordeTrasero    = tienePasilloTrasero;
        pasilloEnBordeIzquierdo  = tienePasilloIzquierdo;
        pasilloEnBordeDerecho    = tienePasilloDerecho;
        pegadoAParedVidrio       = esParedVidrioEste;
        esBloqueEscalera         = esExtremoEscalera;
        esUltimoPiso             = esPisoFinal;
        esPrimerPiso             = esPisoInicial;
        espacioColumnasPupitres  = Mathf.Max(0f, nuevoEspacioColumnasPupitres);
        espacioCaminoHabitacion  = Mathf.Max(0f, nuevoEspacioCaminoHabitacion);
        clasificacionCompartida  = ClonarMatriz(tiposHabitacionesCompartidos);
        GenerarBloque();
    }

    public int[,] ObtenerClasificacionHabitaciones()
    {
        return ClonarMatriz(clasificacionHabitacionesGenerada);
    }

    [ContextMenu("Generar / Regenerar Bloque (PCG)")]
    public void GenerarBloque()
    {
        CargarPrefabsSiEsNecesario();
        LimpiarBloque();

        m = Mathf.Max(1, m);
        n = Mathf.Max(1, n);

        List<int> cortesM = CalcularCortesM();
        List<int> cortesN = CalcularCortesN();

        int numParedesX = (n + 1) + cortesN.Count;
        int numParedesZ = (m + 1) + cortesM.Count;

        totalAnchoX = n * tamanoHabitacionX + numParedesX * grosorPared + cortesN.Count * anchoPasillo;
        totalLargoZ = m * tamanoHabitacionZ + numParedesZ * grosorPared + cortesM.Count * anchoPasillo;

        if (generarTechoPisoBase)
            CrearTechoPisoBase();

        // 1. Crear Pasillos Z y X
        CrearPasillosZ(cortesN);
        CrearPasillosX(cortesM, cortesN);

        // 2. Crear Habitaciones y Paredes Compartidas
        CrearHabitacionesYParedes(cortesM, cortesN);
    }

    public List<int> CalcularCortesM()
    {
        List<int> cortes = new List<int>();
        if (!permitirPasillosX || m <= 1) return cortes;

        for (int i = 3; i < m; i += 3)
            cortes.Add(i);

        if (cortes.Count > 0)
        {
            int ultimoCorte = cortes[cortes.Count - 1];
            if (m - ultimoCorte == 1)
                cortes[cortes.Count - 1] = ultimoCorte - 1;
        }
        else if (m == 4)
        {
            cortes.Add(2);
        }

        return cortes;
    }

    public List<int> CalcularCortesN()
    {
        List<int> cortes = new List<int>();
        if (!permitirPasillosZ || n <= 1) return cortes;

        for (int j = 2; j < n; j += 2)
            cortes.Add(j);

        return cortes;
    }

    private void CrearTechoPisoBase()
    {
        if (techoPisoPrefab == null) return;

        GameObject go = Instantiate(techoPisoPrefab, transform);
        go.name        = "TechoPiso_Base";
        techoPisoBase  = go.GetComponent<TechoPisoConfig>();
        if (techoPisoBase != null)
            techoPisoBase.Configurar(totalAnchoX, grosorLosaInferiorPasillo > 0 ? grosorLosaInferiorPasillo : 0.25f, totalLargoZ);
        else
            go.transform.localScale = new Vector3(totalAnchoX, 0.25f, totalLargoZ);

        go.transform.localPosition = new Vector3(0f, -0.125f, 0f);
        go.transform.localRotation = Quaternion.identity;
    }

    private void CrearPasillosZ(List<int> cortesN)
    {
        if (cortesN == null || cortesN.Count == 0 || pasilloPrefab == null) return;

        for (int k = 0; k < cortesN.Count; k++)
        {
            int corteCol = cortesN[k];

            float bordeIzq = CalcularPosicionXBordeDerechoCelda(corteCol - 1, cortesN);
            float centerX = bordeIzq + anchoPasillo / 2f;

            GameObject go = Instantiate(pasilloPrefab, transform);
            go.name = $"Pasillo_Z_{k + 1}";
            go.transform.localPosition = new Vector3(centerX, 0f, 0f);
            go.transform.localRotation = Quaternion.identity;

            PasilloConfig pc = go.GetComponent<PasilloConfig>();
            if (pc != null)
            {
                float grosorInf = grosorLosaInferiorPasillo > 0 ? grosorLosaInferiorPasillo : 0.25f;
                pc.Configurar(totalLargoZ, anchoPasillo, grosorInf, altoHabitacion,
                              conTecho: grosorLosaSuperiorPasillo > 0,
                              conSuelo: grosorLosaInferiorPasillo > 0);
                if (techoPisoBasePrefab != null) pc.losaPrefab = techoPisoBasePrefab;
                pasillosZ.Add(pc);
            }
        }
    }

    private void CrearPasillosX(List<int> cortesM, List<int> cortesN)
    {
        if (cortesM == null || cortesM.Count == 0 || pasilloPrefab == null) return;

        for (int r = 0; r < cortesM.Count; r++)
        {
            int corteFila = cortesM[r];

            float bordeAtras = CalcularPosicionZBordeFrontalCelda(corteFila - 1, cortesM);
            float centerZ = bordeAtras + anchoPasillo / 2f;

            float grosorInf = grosorLosaInferiorPasillo > 0 ? grosorLosaInferiorPasillo : 0.25f;

            if (cortesN.Count == 0)
            {
                GameObject go = Instantiate(pasilloPrefab, transform);
                go.name = $"Pasillo_X_{r + 1}";
                go.transform.localPosition = new Vector3(0f, 0f, centerZ);
                go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                PasilloConfig pc = go.GetComponent<PasilloConfig>();
                if (pc != null)
                {
                    pc.Configurar(totalAnchoX, anchoPasillo, grosorInf, altoHabitacion,
                                  conTecho: grosorLosaSuperiorPasillo > 0,
                                  conSuelo: grosorLosaInferiorPasillo > 0);
                    if (techoPisoBasePrefab != null) pc.losaPrefab = techoPisoBasePrefab;
                    pasillosX.Add(pc);
                }
            }
            else
            {
                int segIndex = 0;
                int colInicio = 0;

                for (int k = 0; k <= cortesN.Count; k++)
                {
                    int colFin = (k < cortesN.Count) ? cortesN[k] : n;
                    int numHabSeg = colFin - colInicio;

                    if (numHabSeg > 0)
                    {
                        float minXSeg = CalcularPosicionXBordeIzquierdoCelda(colInicio, cortesN);
                        float maxXSeg = CalcularPosicionXBordeDerechoCelda(colFin - 1, cortesN);
                        float segWidth = maxXSeg - minXSeg;
                        float segCenterX = (minXSeg + maxXSeg) / 2f;

                        GameObject go = Instantiate(pasilloPrefab, transform);
                        go.name = $"Pasillo_X_{r + 1}_Seg_{segIndex + 1}";
                        go.transform.localPosition = new Vector3(segCenterX, 0f, centerZ);
                        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                        PasilloConfig pc = go.GetComponent<PasilloConfig>();
                        if (pc != null)
                        {
                            pc.Configurar(segWidth, anchoPasillo, grosorInf, altoHabitacion,
                                          conTecho: grosorLosaSuperiorPasillo > 0,
                                          conSuelo: grosorLosaInferiorPasillo > 0);
                            if (techoPisoBasePrefab != null) pc.losaPrefab = techoPisoBasePrefab;
                            pasillosX.Add(pc);
                        }
                        segIndex++;
                    }

                    colInicio = colFin;
                }
            }
        }
    }

    // ── Funciones de Posicionamiento Exacto de Celdas ──
    private float CalcularPosicionXBordeIzquierdoCelda(int col, List<int> cortesN)
    {
        int pasillosPrevios = 0;
        foreach (int c in cortesN) { if (c <= col) pasillosPrevios++; }
        int paredesPrevias = col + pasillosPrevios;
        return -totalAnchoX / 2f + col * tamanoHabitacionX + paredesPrevias * grosorPared + pasillosPrevios * anchoPasillo;
    }

    private float CalcularPosicionXBordeDerechoCelda(int col, List<int> cortesN)
    {
        return CalcularPosicionXBordeIzquierdoCelda(col, cortesN) + grosorPared + tamanoHabitacionX;
    }

    private float CalcularPosicionZBordeTraseroCelda(int fila, List<int> cortesM)
    {
        int pasillosPrevios = 0;
        foreach (int c in cortesM) { if (c <= fila) pasillosPrevios++; }
        int paredesPrevias = fila + pasillosPrevios;
        return -totalLargoZ / 2f + fila * tamanoHabitacionZ + paredesPrevias * grosorPared + pasillosPrevios * anchoPasillo;
    }

    private float CalcularPosicionZBordeFrontalCelda(int fila, List<int> cortesM)
    {
        return CalcularPosicionZBordeTraseroCelda(fila, cortesM) + grosorPared + tamanoHabitacionZ;
    }

    /// <summary>
    /// Crea las habitaciones en sus centros y genera una sola pared compartida por límite entre celdas.
    /// Si hay pared de vidrio en dirección de esa pared (borde exterior que da al vidrio), NO SE CREA.
    /// </summary>
    private void CrearHabitacionesYParedes(List<int> cortesM, List<int> cortesN)
    {
        if (habitacionPrefab == null) return;

        HabitacionConfig[,] gridHabs = new HabitacionConfig[m, n];
        Vector3[,] cellCenters = new Vector3[m, n];

        // 1. Calcular centros, sin crear todavía el interior de las habitaciones.
        for (int fila = 0; fila < m; fila++)
        {
            float zMin = CalcularPosicionZBordeTraseroCelda(fila, cortesM) + grosorPared;
            float zMax = zMin + tamanoHabitacionZ;
            float centerZ = (zMin + zMax) / 2f;

            for (int col = 0; col < n; col++)
            {
                float xMin = CalcularPosicionXBordeIzquierdoCelda(col, cortesN) + grosorPared;
                float xMax = xMin + tamanoHabitacionX;
                float centerX = (xMin + xMax) / 2f;

                cellCenters[fila, col] = new Vector3(centerX, 0f, centerZ);
            }
        }

        // 2. Determinar puertas requeridas para cada celda
        int[,] pFrontal   = new int[m, n];
        int[,] pTrasera   = new int[m, n];
        int[,] pIzquierda = new int[m, n];
        int[,] pDerecha   = new int[m, n];

        for (int fila = 0; fila < m; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                DeterminarPuertasParaHabitacion(fila, col, cortesM, cortesN,
                    out pFrontal[fila, col], out pTrasera[fila, col],
                    out pIzquierda[fila, col], out pDerecha[fila, col]);
            }
        }

        // 3. Crear PAREDES HORIZONTALES (Paralelas al eje X)
        Transform[,] wallHFrontalDe = new Transform[m, n];
        Transform[,] wallHTraseraDe = new Transform[m, n];

        float largoParedXBase = tamanoHabitacionX + 2f * grosorPared;

        for (int fila = 0; fila < m; fila++)
        {
            // Trasera de esta fila
            bool esParedCompartidaAtras = (fila > 0) && !cortesM.Contains(fila);
            for (int col = 0; col < n; col++)
            {
                if (esParedCompartidaAtras)
                {
                    wallHTraseraDe[fila, col] = wallHFrontalDe[fila - 1, col];
                }
                else
                {
                    // Si este borde trasero da al exterior y hay paredVidrioSur, NO se crea
                    bool daAVidrioSur = (fila == 0) && pegadoAParedVidrioSur;
                    if (daAVidrioSur)
                    {
                        wallHTraseraDe[fila, col] = null;
                    }
                    else
                    {
                        int puertas = pTrasera[fila, col];
                        float posZ = CalcularPosicionZBordeTraseroCelda(fila, cortesM) + grosorPared / 2f;
                        float posX = cellCenters[fila, col].x;

                        Transform paredT = CrearPared(new Vector3(posX, grosorSuelo, posZ),
                            Quaternion.Euler(0f, 180f, 0f), largoParedXBase, altoHabitacion - grosorSuelo - grosorTecho, grosorPared,
                            puertas, $"ParedH_Fila{fila}_Col{col}_Trasera");

                        wallHTraseraDe[fila, col] = paredT;
                    }
                }
            }

            // Frontal de esta fila
            for (int col = 0; col < n; col++)
            {
                bool esCompartidaConSiguiente = (fila < m - 1) && !cortesM.Contains(fila + 1);
                bool daAVidrioNorte = (fila == m - 1) && pegadoAParedVidrioNorte;

                if (daAVidrioNorte)
                {
                    wallHFrontalDe[fila, col] = null;
                }
                else
                {
                    int puertas = pFrontal[fila, col];
                    if (esCompartidaConSiguiente)
                    {
                        puertas = Mathf.Max(puertas, pTrasera[fila + 1, col]);
                    }

                    float posZ = CalcularPosicionZBordeFrontalCelda(fila, cortesM) + grosorPared / 2f;
                    float posX = cellCenters[fila, col].x;

                    Transform paredT = CrearPared(new Vector3(posX, grosorSuelo, posZ),
                        Quaternion.identity, largoParedXBase, altoHabitacion - grosorSuelo - grosorTecho, grosorPared,
                        puertas, $"ParedH_Fila{fila}_Col{col}_Frontal");

                    wallHFrontalDe[fila, col] = paredT;
                }
            }
        }

        // 4. Crear PAREDES VERTICALES (Paralelas al eje Z)
        Transform[,] wallVIzquierdaDe = new Transform[m, n];
        Transform[,] wallVDerechaDe   = new Transform[m, n];

        float largoZReal = tamanoHabitacionZ;

        for (int fila = 0; fila < m; fila++)
        {
            float habCenterZ = cellCenters[fila, 0].z;

            for (int col = 0; col < n; col++)
            {
                // Pared Izquierda de (fila, col)
                bool esCompartidaIzquierda = (col > 0) && !cortesN.Contains(col);
                if (esCompartidaIzquierda)
                {
                    wallVIzquierdaDe[fila, col] = wallVDerechaDe[fila, col - 1];
                }
                else
                {
                    bool daAVidrioOeste = (col == 0) && pegadoAParedVidrioOeste;
                    if (daAVidrioOeste)
                    {
                        wallVIzquierdaDe[fila, col] = null;
                    }
                    else
                    {
                        int puertas = pIzquierda[fila, col];
                        float posX = CalcularPosicionXBordeIzquierdoCelda(col, cortesN) + grosorPared / 2f;

                        Transform paredT = CrearPared(new Vector3(posX, grosorSuelo, habCenterZ),
                            Quaternion.Euler(0f, 270f, 0f), largoZReal, altoHabitacion - grosorSuelo - grosorTecho, grosorPared,
                            puertas, $"ParedV_Fila{fila}_Col{col}_Izq");

                        wallVIzquierdaDe[fila, col] = paredT;
                    }
                }

                // Pared Derecha de (fila, col)
                bool esCompartidaDerecha = (col < n - 1) && !cortesN.Contains(col + 1);
                if (esCompartidaDerecha)
                {
                    // Si ya fue creada o se creará, aquí se enlaza con la izquierda del siguiente
                }

                bool daAVidrioEste = (col == n - 1) && pegadoAParedVidrio;
                if (daAVidrioEste)
                {
                    // "Si hay paredVidrio en direccion de esa pared, no se crea. No es que se oculta, sino que no se crea."
                    wallVDerechaDe[fila, col] = null;
                }
                else
                {
                    int puertasDer = pDerecha[fila, col];
                    if (esCompartidaDerecha)
                    {
                        puertasDer = Mathf.Max(puertasDer, pIzquierda[fila, col + 1]);
                    }

                    float posX = CalcularPosicionXBordeDerechoCelda(col, cortesN) + grosorPared / 2f;

                    Transform paredT = CrearPared(new Vector3(posX, grosorSuelo, habCenterZ),
                        Quaternion.Euler(0f, 90f, 0f), largoZReal, altoHabitacion - grosorSuelo - grosorTecho, grosorPared,
                        puertasDer, $"ParedV_Fila{fila}_Col{col}_Der");

                    wallVDerechaDe[fila, col] = paredT;
                }
            }
        }

        // 5. Con las paredes y puertas ya construidas, clasificar interiores.
        // Esta fase solo lee las cantidades de puertas; no modifica las paredes.
        int[,] tiposPorHabitacion = ClasificarHabitaciones(pFrontal, pTrasera, pIzquierda, pDerecha);
        if (!esBloqueEscalera && esPrimerPiso)
        {
            ReconciliarClasificacionPrimerPiso(
                tiposPorHabitacion, pFrontal, pTrasera, pIzquierda, pDerecha);
        }

        clasificacionHabitacionesGenerada = esBloqueEscalera
            ? null
            : ClonarMatriz(tiposPorHabitacion);

        if (!esBloqueEscalera && esUltimoPiso)
        {
            var habitacionesSinPasillos = new List<Vector2Int>();
            var habitacionesLibres = new List<Vector2Int>();
            var habitacionesAulaOLaboratorio = new List<Vector2Int>();
            for (int fila = 0; fila < m; fila++)
            {
                for (int col = 0; col < n; col++)
                {
                    Vector2Int coord = new Vector2Int(fila, col);
                    if (!TienePasilloCercano(fila, col, cortesM, cortesN))
                        habitacionesSinPasillos.Add(coord);

                    if (tiposPorHabitacion[fila, col] == 3)
                        habitacionesLibres.Add(coord);
                    else if (tiposPorHabitacion[fila, col] == 1 || tiposPorHabitacion[fila, col] == 2)
                        habitacionesAulaOLaboratorio.Add(coord);
                }
            }

            List<Vector2Int> opciones = habitacionesSinPasillos.Count > 0
                ? habitacionesSinPasillos
                : habitacionesLibres.Count > 0
                    ? habitacionesLibres
                    : habitacionesAulaOLaboratorio;

            if (opciones.Count > 0)
            {
                Vector2Int direccion = opciones[Random.Range(0, opciones.Count)];
                tiposPorHabitacion[direccion.x, direccion.y] = 4;
            }
        }

        // 6. Crear habitaciones vacías después de fijar puertas y clasificación.
        for (int fila = 0; fila < m; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                GameObject go = Instantiate(habitacionPrefab, transform);
                go.name = $"Habitacion_{fila}_{col}";
                go.transform.localPosition = cellCenters[fila, col];
                go.transform.localRotation = Quaternion.identity;

                HabitacionConfig hab = go.GetComponent<HabitacionConfig>();
                if (hab == null) continue;

                hab.paredesGestionadasExternamente = true;
                hab.losaPrefab = techoPisoBasePrefab;
                hab.extenderLosaDerecha = pegadoAParedVidrio && col == n - 1;
                hab.ConfigurarDimensiones(
                    tamanoHabitacionX, tamanoHabitacionZ, altoHabitacion,
                    grosorPared, grosorSuelo, grosorTecho, nuevoTipo: 0);
                gridHabs[fila, col] = hab;
                habitaciones.Add(hab);
            }
        }

        // 7. Asociar paredes y puertas y crear el mobiliario interior.
        for (int fila = 0; fila < m; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                HabitacionConfig hab = gridHabs[fila, col];
                if (hab != null)
                {
                    hab.pupitreProfesorPrefab    = pupitreProfesorPrefab;
                    hab.pupitreEstudiantesPrefab = pupitreEstudiantesPrefab;
                    hab.tipo                     = tiposPorHabitacion[fila, col];
                    hab.espacioColumnasPupitres  = espacioColumnasPupitres;
                    hab.espacioCaminoHabitacion  = espacioCaminoHabitacion;

                    hab.paredFrontal   = wallHFrontalDe[fila, col];
                    hab.paredTrasera   = wallHTraseraDe[fila, col];
                    hab.paredIzquierda = wallVIzquierdaDe[fila, col];
                    hab.paredDerecha   = wallVDerechaDe[fila, col];

                    hab.puertasFrontal   = pFrontal[fila, col];
                    hab.puertasTrasera   = pTrasera[fila, col];
                    hab.puertasIzquierda = pIzquierda[fila, col];
                    hab.puertasDerecha   = pDerecha[fila, col];

                    hab.ActualizarTodo();
                }
            }
        }
    }

    /// <summary>
    /// Determina las puertas para una habitación según el estricto orden de prioridad:
    /// 1. Pasillos Z dentro del bloque -> pared que mira a ese pasillo tiene 2 puertas
    /// 2. Pasillos X dentro del bloque -> pared que mira a ese pasillo tiene 1 puerta
    /// 3. Pasillos Z fuera del bloque  -> pared que mira a ese pasillo tiene 2 puertas
    /// 4. Pasillos X fuera del bloque  -> pared que mira a ese pasillo tiene 1 puerta
    /// 5. Si no hay pasillo en ningún lado -> habitaciones cercanas en eje Z, paredes compartidas tienen 1 puerta
    /// </summary>
    private void DeterminarPuertasParaHabitacion(int fila, int col, List<int> cortesM, List<int> cortesN,
        out int pFrontal, out int pTrasera, out int pIzquierda, out int pDerecha)
    {
        pFrontal = 0; pTrasera = 0; pIzquierda = 0; pDerecha = 0;

        // Los cortes y pasillos externos pertenecen a este objeto BloqueHabitacion,
        // que representa exclusivamente el piso actual.
        bool pasilloZDerInterno     = cortesN.Contains(col + 1);
        bool pasilloZIzqInterno     = cortesN.Contains(col);
        bool pasilloXFrontalInterno = cortesM.Contains(fila + 1);
        bool pasilloXTraseroInterno = cortesM.Contains(fila);

        // Pasillos EXTERNOS
        bool pasilloZDerExterno     = (col == n - 1) && pasilloEnBordeDerecho;
        bool pasilloZIzqExterno     = (col == 0)     && pasilloEnBordeIzquierdo;
        bool pasilloXFrontalExterno = (fila == m - 1) && pasilloEnBordeFrontal;
        bool pasilloXTraseroExterno = (fila == 0)     && pasilloEnBordeTrasero;

        // 1. Pasillos que crecen en dirección Z dentro del bloque (pared mira a ese pasillo -> derecha o izquierda)
        if (pasilloZDerInterno)
        {
            pDerecha = 2;
            return;
        }
        if (pasilloZIzqInterno)
        {
            pIzquierda = 2;
            return;
        }

        // 2. Pasillos que crecen en dirección X dentro del bloque (pared frontal o trasera)
        if (pasilloXFrontalInterno)
        {
            pFrontal = 1;
            return;
        }
        if (pasilloXTraseroInterno)
        {
            pTrasera = 1;
            return;
        }

        // 3. Pasillos externos en Z
        if (pasilloZDerExterno)
        {
            pDerecha = 2;
            return;
        }
        if (pasilloZIzqExterno)
        {
            pIzquierda = 2;
            return;
        }

        // 4. Pasillos que crecen en dirección X fuera del bloque
        if (pasilloXFrontalExterno)
        {
            pFrontal = 1;
            return;
        }
        if (pasilloXTraseroExterno)
        {
            pTrasera = 1;
            return;
        }

        // 5. Si no hay pasillo alguno en ningún lado:
        // "se busca habitaciones cercanas en el eje Z y las paredes compartidas con estas dos se convierte en pared con 1 puerta."
        if (fila < m - 1 && !cortesM.Contains(fila + 1)) pFrontal = 1;
        if (fila > 0     && !cortesM.Contains(fila))     pTrasera = 1;
    }

    private int[,] ClasificarHabitaciones(int[,] frontal, int[,] trasera, int[,] izquierda, int[,] derecha)
    {
        int[,] tipos = new int[m, n];

        if (esBloqueEscalera)
        {
            for (int fila = 0; fila < m; fila++)
                for (int col = 0; col < n; col++)
                    tipos[fila, col] = 5;
            return tipos;
        }

        bool mapaCompartidoValido = clasificacionCompartida != null &&
                                    clasificacionCompartida.GetLength(0) == m &&
                                    clasificacionCompartida.GetLength(1) == n;
        if (mapaCompartidoValido)
        {
            for (int fila = 0; fila < m; fila++)
            {
                for (int col = 0; col < n; col++)
                {
                    int tipoCompartido = clasificacionCompartida[fila, col];
                    tipos[fila, col] = tipoCompartido == 1 || tipoCompartido == 2 ? tipoCompartido : 3;
                }
            }
            return tipos;
        }

        var salones = new List<Vector2Int>();
        for (int fila = 0; fila < m; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                tipos[fila, col] = 3;
                if (TieneUnaParedDoblePuerta(
                    frontal[fila, col], trasera[fila, col],
                    izquierda[fila, col], derecha[fila, col]))
                {
                    salones.Add(new Vector2Int(fila, col));
                    tipos[fila, col] = 1;
                }
            }
        }

        int cantidadLaboratorios = salones.Count / 3;
        for (int i = 0; i < cantidadLaboratorios; i++)
        {
            int indice = Random.Range(0, salones.Count);
            Vector2Int laboratorio = salones[indice];
            tipos[laboratorio.x, laboratorio.y] = 2;
            salones.RemoveAt(indice);
        }

        return tipos;
    }

    private static bool TieneUnaParedDoblePuerta(int frontal, int trasera, int izquierda, int derecha)
    {
        int paredesConPuerta = 0;
        int puertasEnUnaPared = 0;
        int[] puertas = { frontal, trasera, izquierda, derecha };
        foreach (int cantidad in puertas)
        {
            if (cantidad <= 0) continue;
            paredesConPuerta++;
            if (cantidad >= 2) puertasEnUnaPared++;
        }

        return paredesConPuerta == 1 && puertasEnUnaPared == 1;
    }

    private void ReconciliarClasificacionPrimerPiso(
        int[,] tipos, int[,] frontal, int[,] trasera, int[,] izquierda, int[,] derecha)
    {
        var habitacionesConDoblePuerta = new List<Vector2Int>();
        var habitacionesPromovidas = new List<Vector2Int>();
        int laboratoriosActuales = 0;

        for (int fila = 0; fila < m; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                bool calificaComoAula = TieneUnaParedDoblePuerta(
                    frontal[fila, col], trasera[fila, col],
                    izquierda[fila, col], derecha[fila, col]);

                if (!calificaComoAula)
                {
                    // Sin una única pared con dos puertas, debe ser Sala Libre.
                    tipos[fila, col] = 3;
                    continue;
                }

                var coord = new Vector2Int(fila, col);
                habitacionesConDoblePuerta.Add(coord);

                if (tipos[fila, col] == 2)
                {
                    laboratoriosActuales++;
                }
                else if (tipos[fila, col] != 1)
                {
                    // Una Sala Libre del mapa compartido que sí cumple la
                    // regla de puertas se promueve a Salón antes de repartir labs.
                    tipos[fila, col] = 1;
                    habitacionesPromovidas.Add(coord);
                }
            }
        }

        int laboratoriosObjetivo = habitacionesConDoblePuerta.Count / 3;
        int laboratoriosPorAsignar = Mathf.Max(0, laboratoriosObjetivo - laboratoriosActuales);

        // Se asignan primero entre las habitaciones que debieron cambiar desde
        // Sala Libre; así las categorías compartidas compatibles se conservan.
        while (laboratoriosPorAsignar > 0 && habitacionesPromovidas.Count > 0)
        {
            int indice = Random.Range(0, habitacionesPromovidas.Count);
            Vector2Int coord = habitacionesPromovidas[indice];
            tipos[coord.x, coord.y] = 2;
            habitacionesPromovidas.RemoveAt(indice);
            laboratoriosPorAsignar--;
        }

        // Si la clasificación base tenía más laboratorios de los que corresponden
        // a este piso, o faltan candidatos promovidos, ajusta solo los necesarios.
        while (laboratoriosActuales > laboratoriosObjetivo)
        {
            var laboratorios = habitacionesConDoblePuerta.FindAll(
                coord => tipos[coord.x, coord.y] == 2);
            if (laboratorios.Count == 0) break;
            Vector2Int coord = laboratorios[Random.Range(0, laboratorios.Count)];
            tipos[coord.x, coord.y] = 1;
            laboratoriosActuales--;
        }

        while (laboratoriosPorAsignar > 0)
        {
            var salones = habitacionesConDoblePuerta.FindAll(
                coord => tipos[coord.x, coord.y] == 1);
            if (salones.Count == 0) break;
            Vector2Int coord = salones[Random.Range(0, salones.Count)];
            tipos[coord.x, coord.y] = 2;
            laboratoriosPorAsignar--;
        }
    }

    private bool TienePasilloCercano(int fila, int col, List<int> cortesM, List<int> cortesN)
    {
        return cortesN.Contains(col + 1) || cortesN.Contains(col) ||
               cortesM.Contains(fila + 1) || cortesM.Contains(fila) ||
               (col == n - 1 && pasilloEnBordeDerecho) ||
               (col == 0 && pasilloEnBordeIzquierdo) ||
               (fila == m - 1 && pasilloEnBordeFrontal) ||
               (fila == 0 && pasilloEnBordeTrasero);
    }

    private static int[,] ClonarMatriz(int[,] origen)
    {
        if (origen == null) return null;
        int[,] copia = new int[origen.GetLength(0), origen.GetLength(1)];
        for (int fila = 0; fila < origen.GetLength(0); fila++)
            for (int col = 0; col < origen.GetLength(1); col++)
                copia[fila, col] = origen[fila, col];
        return copia;
    }

    private Transform CrearPared(Vector3 posLocal, Quaternion rotLocal, float largo, float alto, float espesor, int numPuertas, string nombre)
    {
        GameObject go;
        if (paredPrefab != null)
        {
            go = Instantiate(paredPrefab, transform);
        }
        else
        {
            go = new GameObject();
            go.transform.SetParent(transform, false);
        }

        go.name = nombre;
        go.transform.localPosition = posLocal;
        go.transform.localRotation = rotLocal;

        ParedConfig pConfig = go.GetComponent<ParedConfig>();
        if (pConfig != null)
        {
            pConfig.Configurar(largo, alto, espesor, numPuertas);
        }
        else
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "ParedVisual";
            cubo.transform.SetParent(go.transform, false);
            cubo.transform.localScale = new Vector3(largo, alto, espesor);
            cubo.transform.localPosition = new Vector3(0f, alto / 2f, 0f);
        }

        paredesGeneradas.Add(go);
        return go.transform;
    }

    [ContextMenu("Limpiar Bloque")]
    public void LimpiarBloque()
    {
        foreach (var p in paredesGeneradas)
        {
            if (p != null)
            {
                if (Application.isPlaying) Destroy(p);
                else DestroyImmediate(p);
            }
        }
        paredesGeneradas.Clear();

        foreach (var hab in habitaciones)
        {
            if (hab != null)
            {
                if (Application.isPlaying) Destroy(hab.gameObject);
                else DestroyImmediate(hab.gameObject);
            }
        }
        habitaciones.Clear();

        foreach (var p in pasillosZ)
        {
            if (p != null)
            {
                if (Application.isPlaying) Destroy(p.gameObject);
                else DestroyImmediate(p.gameObject);
            }
        }
        pasillosZ.Clear();

        foreach (var p in pasillosX)
        {
            if (p != null)
            {
                if (Application.isPlaying) Destroy(p.gameObject);
                else DestroyImmediate(p.gameObject);
            }
        }
        pasillosX.Clear();

        if (techoPisoBase != null)
        {
            if (Application.isPlaying) Destroy(techoPisoBase.gameObject);
            else DestroyImmediate(techoPisoBase.gameObject);
            techoPisoBase = null;
        }
    }

    /// <summary>
    /// Configura si colinda con pared de vidrio en alguno de sus bordes exteriores.
    /// Si ya fue generado, desactiva las paredes que coincidan.
    /// </summary>
    public void OcultarParedesPegadas(bool ocultarFrontal, bool ocultarTrasera, bool ocultarIzquierda, bool ocultarDerecha)
    {
        pegadoAParedVidrio = ocultarDerecha;
        pegadoAParedVidrioNorte = ocultarFrontal;
        pegadoAParedVidrioSur = ocultarTrasera;
        pegadoAParedVidrioOeste = ocultarIzquierda;

        for (int fila = 0; fila < m; fila++)
        {
            for (int col = 0; col < n; col++)
            {
                int index = fila * n + col;
                if (index < 0 || index >= habitaciones.Count) continue;

                HabitacionConfig hab = habitaciones[index];
                if (hab == null) continue;

                if (ocultarFrontal   && fila == m - 1 && hab.paredFrontal   != null) hab.paredFrontal.gameObject.SetActive(false);
                if (ocultarTrasera   && fila == 0     && hab.paredTrasera   != null) hab.paredTrasera.gameObject.SetActive(false);
                if (ocultarIzquierda && col  == 0     && hab.paredIzquierda != null) hab.paredIzquierda.gameObject.SetActive(false);
                if (ocultarDerecha   && col  == n - 1 && hab.paredDerecha   != null) hab.paredDerecha.gameObject.SetActive(false);
            }
        }
    }

    [ContextMenu("Auto Detectar Elementos")]
    public void AutoDetectarElementos()
    {
        habitaciones.Clear();
        pasillosZ.Clear();
        pasillosX.Clear();
        paredesGeneradas.Clear();

        techoPisoBase = GetComponentInChildren<TechoPisoConfig>();

        HabitacionConfig[] todasHab = GetComponentsInChildren<HabitacionConfig>();
        habitaciones.AddRange(todasHab);

        PasilloConfig[] todosPas = GetComponentsInChildren<PasilloConfig>();
        foreach (var p in todosPas)
        {
            if (p.name.Contains("_Z_")) pasillosZ.Add(p);
            else pasillosX.Add(p);
        }

        ParedConfig[] todasParedes = GetComponentsInChildren<ParedConfig>();
        foreach (var pared in todasParedes)
        {
            if (pared.transform.parent == transform)
                paredesGeneradas.Add(pared.gameObject);
        }
    }

    private void CargarPrefabsSiEsNecesario()
    {
#if UNITY_EDITOR
        if (habitacionPrefab == null)
            habitacionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Habitacion.prefab");
        if (pasilloPrefab == null)
            pasilloPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Pasillo.prefab");
        if (techoPisoPrefab == null)
            techoPisoPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/BasePrefabs/TechoPiso.prefab");
        if (techoPisoBasePrefab == null)
            techoPisoBasePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Models/TechoPisoBase.fbx");
        if (paredPrefab == null)
            paredPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Pared.prefab");
        if (pupitreProfesorPrefab == null)
            pupitreProfesorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/PupitreProfesor.prefab");
        if (pupitreEstudiantesPrefab == null)
            pupitreEstudiantesPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/PupitreEstudiantes.prefab");
#endif
    }
}
