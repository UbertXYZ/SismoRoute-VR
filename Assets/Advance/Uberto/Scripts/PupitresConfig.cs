using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coloca mobiliario en una habitación según su tipo.
///
/// Dimensiones de los prefabs (espacio local, centro en 0,0,0):
///   PupitreProfesor  : 2.0 m en X (-1 a +1), 1.4 m en Z (-0.7 a +0.7). Frente = +Z local.
///   PupitreEstudiante: 1.2 m en X (-0.6 a +0.6), 1.8 m en Z (-0.9 a +0.9). Frente = +Z local.
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

    [Header("Existencia de Paredes (para Tipo 4)")]
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

    // PupitreEstudiante: 1.2 × 1.8 m (X × Z). Frente = +Z local.
    private const float EST_HALF_X  = 0.6f;   // ±0.6 m en X local
    private const float EST_HALF_Z  = 0.9f;   // ±0.9 m en Z local

    private const float MARGEN_PARED = 1.0f;  // distancia mínima a las paredes
    private const float MARGEN_PROF  = 1.0f;  // distancia mínima entre estudiantes y profesor
    private const float GAP_FILA     = 0.4f;  // hueco entre pupitres dentro de la misma fila

    // ── Guard contra re-entrada (OnValidate cascada) ──────────────
    private bool _generando = false;

    // ─────────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────────
    public void Configurar(float nuevoAncho, float nuevoLargo, float nuevaAltura, float nuevoGrosorSuelo, int nuevoTipo,
                           bool tienePuertaDer, bool tienePuertaIzq, bool tienePuertaFront, bool tienePuertaTras,
                           bool existeParedIzq = true, bool existeParedDer = true,
                           GameObject prefabProf = null, GameObject prefabEst = null)
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

        tieneParedIzquierda = existeParedIzq;
        tieneParedDerecha   = existeParedDer;

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
            //LimpiarPupitres();

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
    //   • 1 pupitre profesor en la esquina del frente (+Z) de la pared con doble puerta
    //   • Columnas de estudiantes separadas 1 m entre sí (en la dirección de profundidad)
    // ─────────────────────────────────────────────────────────────
    private void GenerarSalonNormal()
    {
        CalcularLayoutAula(gapColumnas: MARGEN_PROF,
            out Vector3 posProf, out float rotYProf,
            out List<Vector3> posEst, out float rotYEst);

        // ── Instanciar UNA SOLA vez ──────────────────────────────
        InstanciarProfesor(posProf, rotYProf);
        InstanciarEstudiantes(posEst, rotYEst);
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 2: LABORATORIO
    //   • Idéntico al Salón pero las columnas de estudiantes están
    //     pegadas entre sí (gap = 0 en la dirección de profundidad).
    // ─────────────────────────────────────────────────────────────
    private void GenerarLaboratorio()
    {
        CalcularLayoutAula(gapColumnas: 0f,
            out Vector3 posProf, out float rotYProf,
            out List<Vector3> posEst, out float rotYEst);

        InstanciarProfesor(posProf, rotYProf);
        InstanciarEstudiantes(posEst, rotYEst);
    }

    // ─────────────────────────────────────────────────────────────
    // CÁLCULO UNIFICADO DEL LAYOUT AULA (Tipo 1 y 2)
    //
    // Convención de rotaciones en Unity (mano izquierda, Y arriba):
    //   rotY =   0° → frente local +Z apunta a +Z mundo
    //   rotY =  90° → frente local +Z apunta a +X mundo
    //   rotY = 180° → frente local +Z apunta a -Z mundo
    //   rotY = 270° → frente local +Z apunta a -X mundo
    //
    // Tras la rotación, los semiejes del prefab en el mundo:
    //   rotY =  90° : local X (2m) → mundo -Z (±1m Z), local Z (1.4m) → mundo +X (±0.7m X)
    //   rotY = 270° : local X (2m) → mundo +Z (±1m Z), local Z (1.4m) → mundo -X (±0.7m X)
    //   rotY =   0° : local X (2m) → mundo +X (±1m X), local Z (1.4m) → mundo +Z (±0.7m Z)
    //   rotY = 180° : local X (2m) → mundo -X (±1m X), local Z (1.4m) → mundo -Z (±0.7m Z)
    //
    // Posición del profesor:
    //   • La pared con doble puerta define el lado del aula.
    //   • El profesor está en la esquina de esa misma pared al +Z (frente del aula).
    //   • Mira hacia el interior del aula (perpendicular a la pared).
    // ─────────────────────────────────────────────────────────────
    private void CalcularLayoutAula(float gapColumnas,
        out Vector3 posProf,  out float rotYProf,
        out List<Vector3> posEst, out float rotYEst)
    {
        posEst = new List<Vector3>();

        // ── Determinar orientación según la pared con doble puerta ──
        // Prioridad: Derecha > Izquierda > Frontal > Trasera
        bool paredX = puertaEnDerecha || puertaEnIzquierda; // pared paralela a Z, door en X-side
        bool paredZ = !paredX && (puertaEnFrontal || puertaEnTrasera);

        if (paredX)
        {
            // ── Puerta en pared DERECHA (+X) o IZQUIERDA (-X) ────────
            // El profesor se coloca en esa misma pared, en la esquina +Z (frente del aula).
            // Tras la rotación del escritorio:
            //   Door RIGHT: prof gira 270° → local Z (1.4m) queda en -X → pegado a pared +X
            //                                local X (2m) queda en +Z → esquina +Z
            //   Door LEFT:  prof gira  90° → local Z (1.4m) queda en +X → pegado a pared -X
            //                                local X (2m) queda en -Z → usamos +Z esquina también

            if (puertaEnDerecha)
            {
                // Profesor en esquina (+X, +Z)
                // Rotado 270°: frente → -X (mira hacia el interior)
                // World extents: ±PROF_HALF_Z en X, ±PROF_HALF_X en Z
                rotYProf = 270f;
                float cx = anchoLibre / 2f - PROF_HALF_Z;         // pegado a pared derecha
                float cz = largoLibre / 2f - PROF_HALF_X;         // pegado a pared frontal
                posProf = new Vector3(cx, grosorSuelo, cz);

                // Profesor ocupa en mundo: X ∈ [cx - PROF_HALF_Z, cx + PROF_HALF_Z]
                //                          Z ∈ [cz - PROF_HALF_X, cz + PROF_HALF_X]
                // Borde izquierdo del prof (en X): cx - PROF_HALF_Z = anchoLibre/2 - 2*PROF_HALF_Z

                rotYEst = 90f;  // estudiantes miran hacia +X (hacia el profesor)
                // Tras rotY=90° del pupitre estudiante:
                //   local X (1.2m) → mundo -Z (±EST_HALF_X en Z)
                //   local Z (1.8m) → mundo +X (±EST_HALF_Z en X, profundidad hacia el prof)

                // Área de estudiantes:
                float estMaxX = (cx - PROF_HALF_Z) - MARGEN_PROF - EST_HALF_Z; // frente: tras borde izq prof + 1m
                float estMinX = -anchoLibre / 2f + MARGEN_PARED + EST_HALF_Z;   // fondo: 1m de pared izq
                float estMaxZ =  largoLibre / 2f - MARGEN_PARED - EST_HALF_X;   // 1m de pared frontal
                float estMinZ = -largoLibre / 2f + MARGEN_PARED + EST_HALF_X;   // 1m de pared trasera

                // Columnas corren en Z (ancho de pupitres en Z), separadas en X (profundidad)
                // Paso en X: profundidad del pupitre (2*EST_HALF_Z) + gap
                float stepX = 2f * EST_HALF_Z + gapColumnas;
                float stepZ = 2f * EST_HALF_X + GAP_FILA;         // paso dentro de la columna

                posEst = GenerarGrilla(
                    minDepth: estMinX, maxDepth: estMaxX, stepDepth: stepX,
                    minFila: estMinZ,  maxFila: estMaxZ,  stepFila: stepZ,
                    ejeDepthEnX: true);
            }
            else // puertaEnIzquierda
            {
                // Profesor en esquina (-X, +Z)
                // Rotado 90°: frente → +X (mira hacia el interior)
                // World extents: ±PROF_HALF_Z en X, ±PROF_HALF_X en Z
                rotYProf = 90f;
                float cx = -anchoLibre / 2f + PROF_HALF_Z;
                float cz =  largoLibre / 2f - PROF_HALF_X;
                posProf = new Vector3(cx, grosorSuelo, cz);

                rotYEst = 270f; // estudiantes miran hacia -X (hacia el profesor en izquierda)
                // Tras rotY=270°: local X → mundo +Z (±EST_HALF_X), local Z → mundo -X (±EST_HALF_Z)

                float estMinX = (cx + PROF_HALF_Z) + MARGEN_PROF + EST_HALF_Z; // desde borde dcho prof
                float estMaxX =  anchoLibre / 2f - MARGEN_PARED - EST_HALF_Z;
                float estMaxZ =  largoLibre / 2f - MARGEN_PARED - EST_HALF_X;
                float estMinZ = -largoLibre / 2f + MARGEN_PARED + EST_HALF_X;

                float stepX = 2f * EST_HALF_Z + gapColumnas;
                float stepZ = 2f * EST_HALF_X + GAP_FILA;

                // Para pared izquierda, las columnas crecen desde la derecha hacia la izquierda.
                // Invertimos minDepth y maxDepth para que la iteración vaya de maxX a minX.
                posEst = GenerarGrillaInversa(
                    maxDepth: estMaxX, minDepth: estMinX, stepDepth: stepX,
                    minFila: estMinZ,  maxFila: estMaxZ,  stepFila: stepZ,
                    ejeDepthEnX: true);
            }
        }
        else if (paredZ)
        {
            // ── Puerta en pared FRONTAL (+Z) o TRASERA (-Z) ───────────
            if (!puertaEnFrontal)
            {
                // Profesor en esquina (+Z, -X):  pared frontal, lado izquierdo
                // Rotado 180°: frente → -Z (mira hacia el interior del aula)
                // World extents: ±PROF_HALF_X en X, ±PROF_HALF_Z en Z
                rotYProf = 180f;
                float cx = -anchoLibre / 2f + PROF_HALF_X;
                float cz =  largoLibre / 2f - PROF_HALF_Z;
                posProf = new Vector3(cx, grosorSuelo, cz);

                rotYEst = 0f; // estudiantes miran hacia +Z (hacia el profesor en pared frontal)
                // Tras rotY=0°: local X → mundo +X (±EST_HALF_X), local Z → mundo +Z (±EST_HALF_Z)

                float estMaxZ = (cz - PROF_HALF_Z) - MARGEN_PROF - EST_HALF_Z;
                float estMinZ = -largoLibre / 2f + MARGEN_PARED + EST_HALF_Z;
                float estMaxX =  anchoLibre / 2f - MARGEN_PARED - EST_HALF_X;
                float estMinX = -anchoLibre / 2f + MARGEN_PARED + EST_HALF_X;

                float stepZ = 2f * EST_HALF_Z + gapColumnas; // profundidad en Z (hacia prof)
                float stepX = 2f * EST_HALF_X + GAP_FILA;    // filas en X

                // Columnas corren en X, separadas en Z; desde el fondo hacia el frente (+Z)
                posEst = GenerarGrillaInversa(
                    maxDepth: estMaxZ, minDepth: estMinZ, stepDepth: stepZ,
                    minFila: estMinX,  maxFila: estMaxX,  stepFila: stepX,
                    ejeDepthEnX: false);
            }
            else // puertaEnTrasera
            {
                // Profesor en esquina (-Z, -X): pared trasera, lado izquierdo
                // Rotado 0°: frente → +Z (mira hacia el interior del aula)
                // World extents: ±PROF_HALF_X en X, ±PROF_HALF_Z en Z
                rotYProf = 0f;
                float cx = -anchoLibre / 2f + PROF_HALF_X;
                float cz = -largoLibre / 2f + PROF_HALF_Z;
                posProf = new Vector3(cx, grosorSuelo, cz);

                rotYEst = 180f; // estudiantes miran hacia -Z (hacia el profesor en pared trasera)
                // Tras rotY=180°: local X → mundo -X (±EST_HALF_X), local Z → mundo -Z (±EST_HALF_Z)

                float estMinZ = (cz + PROF_HALF_Z) + MARGEN_PROF + EST_HALF_Z;
                float estMaxZ =  largoLibre / 2f - MARGEN_PARED - EST_HALF_Z;
                float estMaxX =  anchoLibre / 2f - MARGEN_PARED - EST_HALF_X;
                float estMinX = -anchoLibre / 2f + MARGEN_PARED + EST_HALF_X;

                float stepZ = 2f * EST_HALF_Z + gapColumnas;
                float stepX = 2f * EST_HALF_X + GAP_FILA;

                posEst = GenerarGrilla(
                    minDepth: estMinZ, maxDepth: estMaxZ, stepDepth: stepZ,
                    minFila: estMinX,  maxFila: estMaxX,  stepFila: stepX,
                    ejeDepthEnX: false);
            }
        }
        else
        {
            // Sin puerta definida: usar defaults (profesor en esquina +X, +Z mirando -X)
            rotYProf = 270f;
            posProf = new Vector3(anchoLibre / 2f - PROF_HALF_Z, grosorSuelo, largoLibre / 2f - PROF_HALF_X);
            rotYEst = 90f;
            float estMaxX = anchoLibre / 2f - 2f * PROF_HALF_Z - MARGEN_PROF - EST_HALF_Z;
            float estMinX = -anchoLibre / 2f + MARGEN_PARED + EST_HALF_Z;
            float estMaxZ =  largoLibre / 2f - MARGEN_PARED - EST_HALF_X;
            float estMinZ = -largoLibre / 2f + MARGEN_PARED + EST_HALF_X;
            float stepX = 2f * EST_HALF_Z + gapColumnas;
            float stepZ = 2f * EST_HALF_X + GAP_FILA;
            posEst = GenerarGrilla(estMinX, estMaxX, stepX, estMinZ, estMaxZ, stepZ, ejeDepthEnX: true);
        }
    }

    /// <summary>
    /// Genera posiciones en una grilla 2D.
    /// "Depth" = dirección hacia el profesor (columnas separadas en esta dirección).
    /// "Fila"  = dirección perpendicular (pupitres dentro de cada columna).
    /// Avanza desde minDepth hasta maxDepth.
    /// </summary>
    private List<Vector3> GenerarGrilla(
        float minDepth, float maxDepth, float stepDepth,
        float minFila,  float maxFila,  float stepFila,
        bool ejeDepthEnX)
    {
        var lista = new List<Vector3>();
        if (maxDepth < minDepth || maxFila < minFila || stepDepth <= 0f || stepFila <= 0f)
            return lista;

        for (float d = minDepth; d <= maxDepth + 0.001f; d += stepDepth)
        {
            for (float f = minFila; f <= maxFila + 0.001f; f += stepFila)
            {
                float px = ejeDepthEnX ? d : f;
                float pz = ejeDepthEnX ? f : d;
                lista.Add(new Vector3(px, grosorSuelo, pz));
            }
        }
        return lista;
    }

    /// <summary>
    /// Igual que GenerarGrilla pero la profundidad avanza desde maxDepth hasta minDepth
    /// (útil cuando el profesor está a la izquierda y las columnas se alejan hacia +X).
    /// </summary>
    private List<Vector3> GenerarGrillaInversa(
        float maxDepth, float minDepth, float stepDepth,
        float minFila,  float maxFila,  float stepFila,
        bool ejeDepthEnX)
    {
        var lista = new List<Vector3>();
        if (maxDepth < minDepth || maxFila < minFila || stepDepth <= 0f || stepFila <= 0f)
            return lista;

        for (float d = maxDepth; d >= minDepth - 0.001f; d -= stepDepth)
        {
            for (float f = minFila; f <= maxFila + 0.001f; f += stepFila)
            {
                float px = ejeDepthEnX ? d : f;
                float pz = ejeDepthEnX ? f : d;
                lista.Add(new Vector3(px, grosorSuelo, pz));
            }
        }
        return lista;
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 3: SALA LIBRE
    //   • Sin pupitre de profesor.
    //   • Pasillos libres de 1 m junto a: pared con puerta, pared opuesta y pasillo central.
    //   • Los dos espacios resultantes se llenan con pupitres mirando al pasillo central.
    // ─────────────────────────────────────────────────────────────
    private void GenerarSalaLibre()
    {
        if (pupitreEstudiantesPrefab == null) return;

        const float anchoPasilloCentral = 1f;
        const float margenPaso = 1f;

        bool puertaEnEjeZ = puertaEnFrontal || puertaEnTrasera;

        var posiciones = new List<(Vector3 pos, float rotY)>();

        if (puertaEnEjeZ)
        {
            // Pasillo central corre en Z (en X ≈ 0, ancho 1 m)
            // Dos bloques: Izquierdo (X < 0) y Derecho (X > 0)
            float minZ = -largoLibre / 2f + margenPaso + EST_HALF_X;
            float maxZ =  largoLibre / 2f - margenPaso - EST_HALF_X;
            if (maxZ < minZ) return;
            float stepZ = 2f * EST_HALF_X + GAP_FILA;

            // Bloque Izquierdo: desde (-anchoLibre/2 + 1m) hasta (-anchoPasilloCentral/2)
            float xMaxIzq = -anchoPasilloCentral / 2f - EST_HALF_Z;
            float xMinIzq = -anchoLibre / 2f + margenPaso + EST_HALF_Z;
            float stepX = 2f * EST_HALF_Z + GAP_FILA;
            if (xMaxIzq >= xMinIzq)
            {
                for (float x = xMaxIzq; x >= xMinIzq - 0.001f; x -= stepX)
                    for (float z = minZ; z <= maxZ + 0.001f; z += stepZ)
                        posiciones.Add((new Vector3(x, grosorSuelo, z), 90f)); // mira +X (hacia pasillo)
            }

            // Bloque Derecho: desde (+anchoPasilloCentral/2) hasta (+anchoLibre/2 - 1m)
            float xMinDer = anchoPasilloCentral / 2f + EST_HALF_Z;
            float xMaxDer = anchoLibre / 2f - margenPaso - EST_HALF_Z;
            if (xMaxDer >= xMinDer)
            {
                for (float x = xMinDer; x <= xMaxDer + 0.001f; x += stepX)
                    for (float z = minZ; z <= maxZ + 0.001f; z += stepZ)
                        posiciones.Add((new Vector3(x, grosorSuelo, z), 270f)); // mira -X (hacia pasillo)
            }
        }
        else
        {
            // Pasillo central corre en X (en Z ≈ 0, ancho 1 m)
            // Dos bloques: Trasero (Z < 0) y Frontal (Z > 0)
            float minX = -anchoLibre / 2f + margenPaso + EST_HALF_X;
            float maxX =  anchoLibre / 2f - margenPaso - EST_HALF_X;
            if (maxX < minX) return;
            float stepX = 2f * EST_HALF_X + GAP_FILA;

            // Bloque Trasero (Z < 0): mira +Z (hacia pasillo central)
            float zMaxTras = -anchoPasilloCentral / 2f - EST_HALF_Z;
            float zMinTras = -largoLibre / 2f + margenPaso + EST_HALF_Z;
            float stepZ = 2f * EST_HALF_Z + GAP_FILA;
            if (zMaxTras >= zMinTras)
            {
                for (float z = zMaxTras; z >= zMinTras - 0.001f; z -= stepZ)
                    for (float x = minX; x <= maxX + 0.001f; x += stepX)
                        posiciones.Add((new Vector3(x, grosorSuelo, z), 0f)); // mira +Z
            }

            // Bloque Frontal (Z > 0): mira -Z (hacia pasillo central)
            float zMinFront = anchoPasilloCentral / 2f + EST_HALF_Z;
            float zMaxFront = largoLibre / 2f - margenPaso - EST_HALF_Z;
            if (zMaxFront >= zMinFront)
            {
                for (float z = zMinFront; z <= zMaxFront + 0.001f; z += stepZ)
                    for (float x = minX; x <= maxX + 0.001f; x += stepX)
                        posiciones.Add((new Vector3(x, grosorSuelo, z), 180f)); // mira -Z
            }
        }

        // ── Instanciar estudiantes ──────────────────────────────────
        AsegurarContenedorEstudiantes();
        int idx = 0;
        foreach (var (pos, rotY) in posiciones)
        {
            if (pupitreEstudiantesPrefab == null) break;
            GameObject p = Instantiate(pupitreEstudiantesPrefab, contenedorEstudiantes);
            p.name = $"SalaLibre_{idx++}";
            p.transform.localPosition = pos;
            p.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // TIPO 4: DIRECCIÓN
    //   • Solo 1 pupitre profesor, pegado y centrado en la pared
    //     izquierda (o derecha si no hay izquierda), sin superponer.
    // ─────────────────────────────────────────────────────────────
    private void GenerarDireccion()
    {
        if (pupitreProfesorPrefab == null) return;

        // El escritorio en rotY=0 / 180 ocupa ±PROF_HALF_X en X, ±PROF_HALF_Z en Z.
        // Para pegarlo a una pared lateral (en X), lo giramos 90° o 270°:
        //   rotY=90°: local X (2m) → -Z, local Z (1.4m) → +X → se pega a la pared +X
        //   rotY=270°: local X (2m) → +Z, local Z (1.4m) → -X → se pega a la pared -X
        // Después de la rotación, el escritorio ocupa ±PROF_HALF_Z en X, ±PROF_HALF_X en Z.

        bool usarIzquierda = tieneParedIzquierda;
        if (!tieneParedIzquierda && tieneParedDerecha) usarIzquierda = false;

        float posX, rotY;
        if (usarIzquierda)
        {
            // Pegado a pared izquierda (-X), mirando hacia +X (rotY = 90°)
            posX = -anchoLibre / 2f + PROF_HALF_Z + 0.05f;
            rotY = 90f;
        }
        else
        {
            // Pegado a pared derecha (+X), mirando hacia -X (rotY = 270°)
            posX = anchoLibre / 2f - PROF_HALF_Z - 0.05f;
            rotY = 270f;
        }

        // Centro en Z = 0 (centro del lado)
        instanciaProfesor = Instantiate(pupitreProfesorPrefab, transform);
        instanciaProfesor.name = "PupitreProfesor_Direccion";
        instanciaProfesor.transform.localPosition = new Vector3(posX, grosorSuelo, 0f);
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

    private void InstanciarEstudiantes(List<Vector3> posiciones, float rotY)
    {
        if (pupitreEstudiantesPrefab == null || posiciones == null || posiciones.Count == 0) return;
        AsegurarContenedorEstudiantes();
        for (int i = 0; i < posiciones.Count; i++)
        {
            GameObject p = Instantiate(pupitreEstudiantesPrefab, contenedorEstudiantes);
            p.name = $"Pupitre_{i:D3}";
            p.transform.localPosition = posiciones[i];
            p.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
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
        // Recolectar hijos a destruir antes de iterar (evitar modificar la jerarquía mientras se itera)
        var aDestruir = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name.StartsWith("PupitreProfesor") || hijo.name == "Pupitre de Estudiantes")
                aDestruir.Add(hijo.gameObject);
        }

        foreach (var go in aDestruir)
        {
            if (go == null) continue;
            if (Application.isPlaying) DestroyImmediate(go);
            else DestroyImmediate(go);
        }

        instanciaProfesor      = null;
        contenedorEstudiantes  = null;
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
