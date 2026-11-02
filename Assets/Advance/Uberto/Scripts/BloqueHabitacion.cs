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

    [Header("Dimensiones de las Habitaciones")]
    [Tooltip("Dimensión en el eje X de cada habitación")]
    public float tamanoHabitacionX = 8f;
    [Tooltip("Dimensión en el eje Z de cada habitación")]
    public float tamanoHabitacionZ = 10f;
    [Tooltip("Altura en el eje Y de cada habitación")]
    public float altoHabitacion = 4.25f;

    [Min(0.01f)] public float grosorPared = 0.20f;
    [Min(0f)]    public float grosorSuelo  = 0f;
    [Min(0f)]    public float grosorTecho  = 0f;

    [Tooltip("Generar losa TechoPiso debajo del bloque (desactivado por defecto si las losas del campus lo cubren)")]
    public bool generarTechoPisoBase = false;

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
    public TipoPuertasBloque tipoPuertas = TipoPuertasBloque.HaciaPasillosZ;
    [Tooltip("Si está activo, las puertas que se generen hacia pasillos serán dobles")]
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

    [Header("Instancias Generadas")]
    public TechoPisoConfig techoPisoBase;
    public List<HabitacionConfig> habitaciones = new List<HabitacionConfig>();
    public List<PasilloConfig>    pasillosZ    = new List<PasilloConfig>();
    public List<PasilloConfig>    pasillosX    = new List<PasilloConfig>();

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
            false, false, false, false);
    }

    public void Configurar(int habitacionesZ, int habitacionesX, float habX, float habZ, float altura,
        bool pasillosZFlag, bool pasillosXFlag, float anchoPas,
        TipoPuertasBloque puertas, bool esDoblePuerta,
        bool tienePasilloFrontal, bool tienePasilloTrasero,
        bool tienePasilloIzquierdo, bool tienePasilloDerecho)
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
        GenerarBloque();
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

        totalAnchoX = n * tamanoHabitacionX + cortesN.Count * anchoPasillo;
        totalLargoZ = m * tamanoHabitacionZ + cortesM.Count * anchoPasillo;

        if (generarTechoPisoBase)
            CrearTechoPisoBase();

        // Pasillos Z tienen prioridad
        CrearPasillosZ(cortesN);
        CrearPasillosX(cortesM, cortesN);
        CrearHabitaciones(cortesM, cortesN);
    }

    /// <summary>
    /// Calcula los cortes en M (pasillos X): cada múltiplo de 3 en M.
    /// Si la última división deja 1 habitación, el último pasillo se mueve 1 antes.
    /// </summary>
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

    /// <summary>
    /// Calcula los cortes en N (pasillos Z): cada múltiplo de 2 en N.
    /// </summary>
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
            int   corteCol = cortesN[k];
            float startX   = corteCol * tamanoHabitacionX + k * anchoPasillo;
            float centerX  = -totalAnchoX / 2f + startX + anchoPasillo / 2f;

            GameObject go = Instantiate(pasilloPrefab, transform);
            go.name = $"Pasillo_Z_{k + 1}";
            go.transform.localPosition = new Vector3(centerX, 0f, 0f);
            go.transform.localRotation = Quaternion.identity;

            PasilloConfig pc = go.GetComponent<PasilloConfig>();
            if (pc != null)
            {
                // Misma configuración de losas que los pasillos del PCG
                pc.tieneLuces = true;
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
            int   corteFila = cortesM[r];
            float startZ    = corteFila * tamanoHabitacionZ + r * anchoPasillo;
            float centerZ   = -totalLargoZ / 2f + startZ + anchoPasillo / 2f;

            float grosorInf = grosorLosaInferiorPasillo > 0 ? grosorLosaInferiorPasillo : 0.25f;

            if (cortesN.Count == 0)
            {
                // Sin pasillos Z: X se extiende completo
                GameObject go = Instantiate(pasilloPrefab, transform);
                go.name = $"Pasillo_X_{r + 1}";
                go.transform.localPosition = new Vector3(0f, 0f, centerZ);
                go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                PasilloConfig pc = go.GetComponent<PasilloConfig>();
                if (pc != null)
                {
                    pc.tieneLuces = true;
                    pc.Configurar(totalAnchoX, anchoPasillo, grosorInf, altoHabitacion,
                                  conTecho: grosorLosaSuperiorPasillo > 0,
                                  conSuelo: grosorLosaInferiorPasillo > 0);
                    if (techoPisoBasePrefab != null) pc.losaPrefab = techoPisoBasePrefab;
                    pasillosX.Add(pc);
                }
            }
            else
            {
                // Con pasillos Z: X se divide para no superponerse
                int segIndex = 0;
                int colInicio = 0;

                for (int k = 0; k <= cortesN.Count; k++)
                {
                    int colFin       = (k < cortesN.Count) ? cortesN[k] : n;
                    int numHabSeg    = colFin - colInicio;

                    if (numHabSeg > 0)
                    {
                        float segWidth  = numHabSeg * tamanoHabitacionX;
                        float segStartX = colInicio * tamanoHabitacionX + k * anchoPasillo;
                        float segCenterX = -totalAnchoX / 2f + segStartX + segWidth / 2f;

                        GameObject go = Instantiate(pasilloPrefab, transform);
                        go.name = $"Pasillo_X_{r + 1}_Seg_{segIndex + 1}";
                        go.transform.localPosition = new Vector3(segCenterX, 0f, centerZ);
                        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                        PasilloConfig pc = go.GetComponent<PasilloConfig>();
                        if (pc != null)
                        {
                            pc.tieneLuces = true;
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

    private void CrearHabitaciones(List<int> cortesM, List<int> cortesN)
    {
        if (habitacionPrefab == null) return;

        for (int fila = 0; fila < m; fila++)
        {
            int pasillosXPrevios = 0;
            foreach (int c in cortesM) { if (c <= fila) pasillosXPrevios++; }

            float startZ  = fila * tamanoHabitacionZ + pasillosXPrevios * anchoPasillo;
            float centerZ = -totalLargoZ / 2f + startZ + tamanoHabitacionZ / 2f;

            for (int col = 0; col < n; col++)
            {
                int pasillosZPrevios = 0;
                foreach (int c in cortesN) { if (c <= col) pasillosZPrevios++; }

                float startX  = col * tamanoHabitacionX + pasillosZPrevios * anchoPasillo;
                float centerX = -totalAnchoX / 2f + startX + tamanoHabitacionX / 2f;

                GameObject go = Instantiate(habitacionPrefab, transform);
                go.name = $"Habitacion_{fila}_{col}";
                go.transform.localPosition = new Vector3(centerX, 0f, centerZ);
                go.transform.localRotation = Quaternion.identity;

                HabitacionConfig hab = go.GetComponent<HabitacionConfig>();
                if (hab != null)
                {
                    hab.ConfigurarDimensiones(tamanoHabitacionX, tamanoHabitacionZ, altoHabitacion, grosorPared, grosorSuelo, grosorTecho);
                    ConfigurarPuertasHabitacion(hab, fila, col, cortesM, cortesN);
                    habitaciones.Add(hab);
                }
            }
        }
    }

    private void ConfigurarPuertasHabitacion(HabitacionConfig hab, int fila, int col,
        List<int> cortesM, List<int> cortesN)
    {
        int pFrontal = 0, pTrasera = 0, pIzquierda = 0, pDerecha = 0;

        // ── Pasillos INTERNOS del bloque ──
        bool pasilloXFrontalInterno = cortesM.Contains(fila + 1);
        bool pasilloXTraseroInterno = cortesM.Contains(fila);
        bool pasilloZIzqInterno     = cortesN.Contains(col);
        bool pasilloZDerInterno     = cortesN.Contains(col + 1);

        // ── Pasillos EXTERNOS del edificio ──
        bool pasilloFrontalExterno = (fila == m - 1) && pasilloEnBordeFrontal;
        bool pasilloTraseroExterno = (fila == 0)     && pasilloEnBordeTrasero;
        bool pasilloIzqExterno     = (col == 0)      && pasilloEnBordeIzquierdo;
        bool pasilloDerExterno     = (col == n - 1)  && pasilloEnBordeDerecho;

        // ── Hay pasillo en ese lado (combinado) ──
        bool tienePasilloFrontal   = pasilloXFrontalInterno || pasilloFrontalExterno;
        bool tienePasilloTrasero   = pasilloXTraseroInterno || pasilloTraseroExterno;
        bool tienePasilloIzquierdo = pasilloZIzqInterno     || pasilloIzqExterno;
        bool tienePasilloDerecho   = pasilloZDerInterno     || pasilloDerExterno;

        // ── Solo 1 puerta con preferencia: INTERNO > EXTERNO ──
        // Preferencia: pasillos Z internos → pasillos X internos → externos
        if      (pasilloZDerInterno)     pDerecha   = 1;
        else if (pasilloZIzqInterno)     pIzquierda = 1;
        else if (pasilloXFrontalInterno) pFrontal   = 1;
        else if (pasilloXTraseroInterno) pTrasera   = 1;
        else if (pasilloDerExterno)      pDerecha   = 1;
        else if (pasilloIzqExterno)      pIzquierda = 1;
        else if (pasilloFrontalExterno)  pFrontal   = 1;
        else if (pasilloTraseroExterno)  pTrasera   = 1;

        // ── SEGURIDAD: nunca puerta sin pasillo ──
        if (!tienePasilloFrontal)   pFrontal   = 0;
        if (!tienePasilloTrasero)   pTrasera   = 0;
        if (!tienePasilloIzquierdo) pIzquierda = 0;
        if (!tienePasilloDerecho)   pDerecha   = 0;

        hab.ConfigurarPuertas(pFrontal, pTrasera, pIzquierda, pDerecha);
    }

    [ContextMenu("Limpiar Bloque")]
    public void LimpiarBloque()
    {
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
    /// Oculta las paredes exteriores de las habitaciones que colindan con la Pared de Vidrio.
    /// </summary>
    public void OcultarParedesPegadas(bool ocultarFrontal, bool ocultarTrasera, bool ocultarIzquierda, bool ocultarDerecha)
    {
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

        techoPisoBase = GetComponentInChildren<TechoPisoConfig>();

        HabitacionConfig[] todasHab = GetComponentsInChildren<HabitacionConfig>();
        habitaciones.AddRange(todasHab);

        PasilloConfig[] todosPas = GetComponentsInChildren<PasilloConfig>();
        foreach (var p in todosPas)
        {
            if (p.name.Contains("_Z_")) pasillosZ.Add(p);
            else pasillosX.Add(p);
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
#endif
    }
}
