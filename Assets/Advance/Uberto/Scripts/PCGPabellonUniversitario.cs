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
    public float grosorSueloHabitacion = 0f;

    [Tooltip("Grosor de la Losa Superior (Techo) de las habitaciones (0 = no se crea)")]
    public float grosorTechoHabitacion = 0f;

    // ──────────────────────────────────────────────────────────
    // PREFABS
    // ──────────────────────────────────────────────────────────
    [Header("Prefabs Requeridos")]
    public GameObject techoPisoPrefab;
    public GameObject losaPrefab;   // Assets/Advance/Uberto/Models/TechoPisoBase.fbx
    public GameObject bloqueHabitacionPrefab;
    public GameObject pasilloPrefab;
    public GameObject escaleraPrefab;
    public GameObject habitacionPrefab;
    public GameObject paredBasePrefab;
    public GameObject puertaPrefab;

    [Header("Dimensiones del Prefab Escalera (Unity: 7m x 5m)")]
    public float anchoEscalera = 7f;
    public float largoEscalera = 5f;

    [Header("Instancias Generadas")]
    public List<GameObject> pisosGenerados = new List<GameObject>();
    public GameObject contenedorParedesExteriores;

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
                GenerarEdificioCuadratico();
                break;
            default:
                Debug.LogWarning($"Tipo {tipoEdificio} en desarrollo. Generando Cuadrático.");
                GenerarEdificioCuadratico();
                break;
        }
    }

    // ──────────────────────────────────────────────────────────
    // EDIFICIO CUADRÁTICO
    // ──────────────────────────────────────────────────────────
    private void GenerarEdificioCuadratico()
    {
        float alturaNivel   = alturaPisos + grosorPisos;
        bool cuatroEscaleras = anchoPabellon >= 24f;

        // ── Suelo General (Piso 0) ── cuadrado completo, sin extender
        CrearLosaCompleta("TechoPiso_SueloGeneral",
            new Vector3(0f, -grosorPisos / 2f, 0f),
            anchoPabellon, largoPabellon, igualarMateriales: false);

        for (int piso = 0; piso < numeroPisos; piso++)
        {
            float elevacionY = piso * alturaNivel;
            GameObject contenedorPiso = new GameObject($"Piso_{piso + 1}");
            contenedorPiso.transform.SetParent(transform, false);
            contenedorPiso.transform.localPosition = new Vector3(0f, elevacionY, 0f);
            pisosGenerados.Add(contenedorPiso);

            GenerarDistribucionPiso(contenedorPiso.transform, piso, alturaNivel, cuatroEscaleras);

            // ── TechoPiso intermedio ──
            // Solo el 2do (piso==0) y el último (piso==numeroPisos-1) se extienden
            // en X hacia el Este por grosorParedVidrio para dar apoyo a la pared de vidrio.
            bool esTechoPisoEspecial = (piso == 0) || (piso == numeroPisos - 1);
            float extX   = esTechoPisoEspecial ? grosorParedVidrio : 0f;
            float offsetX = esTechoPisoEspecial ? grosorParedVidrio / 2f : 0f; // desplazar hacia +X

            float alturaDivisorLocal = alturaPisos + grosorPisos / 2f;
            bool esTechoFinal = (piso == numeroPisos - 1);

            GenerarTechoPisoIntermedio(contenedorPiso.transform,
                alturaDivisorLocal, largoPabellon, extX, offsetX,
                cuatroEscaleras, esTechoFinal);
        }

        // ── Paredes exteriores ──
        if (numeroPisos >= 2)
            GenerarParedesExteriores(alturaNivel);
    }

    // ──────────────────────────────────────────────────────────
    // DISTRIBUCIÓN DE CADA PISO
    // ──────────────────────────────────────────────────────────
    private void GenerarDistribucionPiso(Transform padrePiso, int indicePiso, float alturaNivel, bool cuatroEscaleras)
    {
        float minX = -anchoPabellon / 2f;
        float maxX =  anchoPabellon / 2f;
        float minZ = -largoPabellon / 2f;
        float maxZ =  largoPabellon / 2f;

        bool esPrimerPiso   = indicePiso == 0;
        bool enPisosConVidrio = indicePiso >= 1;

        // ─────────────────────────────────────────────────────
        // A. ESCALERAS
        //  • Inf. Izquierda → rot 0°
        //  • Sup. Izquierda → rot 180° en Y
        //  • Inf. Derecha   → rot 0°   (si cuatroEscaleras, en X=0)
        //  • Sup. Derecha   → rot 180° en Y (si cuatroEscaleras, en X=0)
        // ─────────────────────────────────────────────────────
        CrearEscalera(padrePiso,
            new Vector3(minX + anchoEscalera / 2f, 0f, minZ + largoEscalera / 2f),
            Quaternion.identity,
            "Escalera_Inferior_Izquierda");

        CrearEscalera(padrePiso,
            new Vector3(minX + anchoEscalera / 2f, 0f, maxZ - largoEscalera / 2f),
            Quaternion.Euler(0f, 180f, 0f),
            "Escalera_Superior_Izquierda");

        if (cuatroEscaleras)
        {
            // Las segundas escaleras arrancan en X = 0 (mitad del ancho)
            CrearEscalera(padrePiso,
                new Vector3(anchoEscalera / 2f, 0f, minZ + largoEscalera / 2f),
                Quaternion.identity,
                "Escalera_Inferior_Derecha");

            CrearEscalera(padrePiso,
                new Vector3(anchoEscalera / 2f, 0f, maxZ - largoEscalera / 2f),
                Quaternion.Euler(0f, 180f, 0f),
                "Escalera_Superior_Derecha");
        }

        // ─────────────────────────────────────────────────────
        // B. BLOQUES EN EXTREMOS (zona de escaleras, Z = largoEscalera)
        // ─────────────────────────────────────────────────────
        if (cuatroEscaleras)
        {
            // Mitad IZQUIERDA: [minX + anchoEscalera, 0]
            float anchoBlqIzq = (anchoPabellon / 2f) - anchoEscalera;
            if (anchoBlqIzq >= 1f)
            {
                float centroXBlqIzq = minX + anchoEscalera + anchoBlqIzq / 2f;

                CrearBloqueExtremo(padrePiso,
                    new Vector3(centroXBlqIzq, 0f, minZ + largoEscalera / 2f),
                    anchoBlqIzq, largoEscalera,
                    "BloqueHab_ExtremoInferior_Izq", haciaZPositiva: true);

                CrearBloqueExtremo(padrePiso,
                    new Vector3(centroXBlqIzq, 0f, maxZ - largoEscalera / 2f),
                    anchoBlqIzq, largoEscalera,
                    "BloqueHab_ExtremoSuperior_Izq", haciaZPositiva: false);
            }

            // Mitad DERECHA: [anchoEscalera, maxX]
            float anchoBlqDer = (anchoPabellon / 2f) - anchoEscalera;
            if (anchoBlqDer >= 1f)
            {
                float centroXBlqDer = anchoEscalera + anchoBlqDer / 2f;

                BloqueHabitacion blqID = CrearBloqueExtremo(padrePiso,
                    new Vector3(centroXBlqDer, 0f, minZ + largoEscalera / 2f),
                    anchoBlqDer, largoEscalera,
                    "BloqueHab_ExtremoInferior_Der", haciaZPositiva: true);
                if (enPisosConVidrio && blqID != null)
                    blqID.OcultarParedesPegadas(false, false, false, true);

                BloqueHabitacion blqSD = CrearBloqueExtremo(padrePiso,
                    new Vector3(centroXBlqDer, 0f, maxZ - largoEscalera / 2f),
                    anchoBlqDer, largoEscalera,
                    "BloqueHab_ExtremoSuperior_Der", haciaZPositiva: false);
                if (enPisosConVidrio && blqSD != null)
                    blqSD.OcultarParedesPegadas(false, false, false, true);
            }
        }
        else
        {
            // Solo escaleras a la izquierda; bloques rellenan hacia la derecha
            float anchoDispBloques = anchoPabellon - anchoEscalera;
            float centroXBloques   = minX + anchoEscalera + anchoDispBloques / 2f;

            BloqueHabitacion blqI = CrearBloqueExtremo(padrePiso,
                new Vector3(centroXBloques, 0f, minZ + largoEscalera / 2f),
                anchoDispBloques, largoEscalera,
                "BloqueHab_ExtremoInferior", haciaZPositiva: true);
            if (enPisosConVidrio && blqI != null)
                blqI.OcultarParedesPegadas(false, false, false, true);

            BloqueHabitacion blqS = CrearBloqueExtremo(padrePiso,
                new Vector3(centroXBloques, 0f, maxZ - largoEscalera / 2f),
                anchoDispBloques, largoEscalera,
                "BloqueHab_ExtremoSuperior", haciaZPositiva: false);
            if (enPisosConVidrio && blqS != null)
                blqS.OcultarParedesPegadas(false, false, false, true);
        }

        // ─────────────────────────────────────────────────────
        // C. DOS PASILLOS PARALELOS AL EJE X
        // ─────────────────────────────────────────────────────
        float posZPasilloXInf = minZ + largoEscalera + anchoPasillos / 2f;
        CrearPasilloX(padrePiso, new Vector3(0f, 0f, posZPasilloXInf), anchoPabellon, "Pasillo_X_Inferior");

        float posZPasilloXSup = maxZ - largoEscalera - anchoPasillos / 2f;
        CrearPasilloX(padrePiso, new Vector3(0f, 0f, posZPasilloXSup), anchoPabellon, "Pasillo_X_Superior");

        // ─────────────────────────────────────────────────────
        // D. PASILLO PARALELO AL EJE Z (Inferior, solo si ancho < 28)
        // ─────────────────────────────────────────────────────
        float inicioZ        = minZ + largoEscalera + anchoPasillos;
        float finZ           = maxZ - largoEscalera - anchoPasillos;
        float largoPasillosZ = Mathf.Max(1f, finZ - inicioZ);
        float centroZMedio   = (inicioZ + finZ) / 2f;

        bool hayPasilloZ = anchoPabellon < 28f;
        if (hayPasilloZ)
        {
            float posXPasilloZInf = minX + anchoPasillos / 2f;
            CrearPasilloZ(padrePiso, new Vector3(posXPasilloZInf, 0f, centroZMedio), largoPasillosZ, "Pasillo_Z_Inferior");
        }

        // ─────────────────────────────────────────────────────
        // E. BLOQUE CENTRAL DE HABITACIONES
        // ─────────────────────────────────────────────────────
        float anchoAreaCentral;
        float centroXCentral;

        if (hayPasilloZ)
        {
            anchoAreaCentral = anchoPabellon - anchoPasillos;
            centroXCentral   = minX + anchoPasillos + anchoAreaCentral / 2f;
        }
        else
        {
            anchoAreaCentral = anchoPabellon;
            centroXCentral   = 0f;
        }

        BloqueHabitacion blqCentral = CrearBloqueCentral(padrePiso,
            new Vector3(centroXCentral, 0f, centroZMedio),
            anchoAreaCentral, largoPasillosZ, hayPasilloZ);

        if (enPisosConVidrio && blqCentral != null)
            blqCentral.OcultarParedesPegadas(false, false, false, true);

        // ─────────────────────────────────────────────────────
        // F. VEREDA DE ENTRADA (solo en el PRIMER PISO, piso==0)
        //    Pasillo exterior en el lado Este (+X), sin luces, solo losa inferior
        // ─────────────────────────────────────────────────────
        if (esPrimerPiso)
        {
            float posXVereda = maxX + anchoPasillos / 2f;
            CrearPasilloVeredaEntrada(padrePiso,
                new Vector3(posXVereda, 0f, 0f),
                largoPabellon, "Vereda_Entrada_Este");
        }
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
    // TECHO PISO INTERMEDIO
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// Genera las losas del TechoPiso de cada piso.
    ///
    /// Cuando cuatroEscaleras == false: 2 escaleras a la izquierda → 3 piezas.
    /// Cuando cuatroEscaleras == true : 4 escaleras (2 izq + 2 en X=0) → 6 piezas,
    ///   divididas en dos grupos (izquierda y derecha) tal como lo piden los dos TechoPisos.
    ///
    /// extX   = extensión extra en X hacia el Este (0 para pisos normales).
    /// offsetX = desplazamiento del centro total para que la extensión quede al Este.
    /// </summary>
    private void GenerarTechoPisoIntermedio(Transform padrePiso, float alturaY, float largoLosa, float extX, float offsetX, bool cuatroEscaleras, bool igualarMats)
    {
        float anchoLosa = anchoPabellon + extX;
        // La losa se desplaza offsetX en X (hacia +X) para que el margen quede solo al Este
        float centroXLosa = offsetX;

        float halfAncho = anchoLosa / 2f;
        float halfLargo = largoLosa / 2f;

        // extremos de la losa en X y Z
        float losaMinX = centroXLosa - halfAncho;
        float losaMaxX = centroXLosa + halfAncho;

        if (!cuatroEscaleras)
        {
            // ── 2 escaleras a la izquierda ──
            // Esc Inf Izq: [losaMinX, losaMinX+anchoEscalera] × [-halfLargo, -halfLargo+largoEscalera]
            // Esc Sup Izq: [losaMinX, losaMinX+anchoEscalera] × [halfLargo-largoEscalera, halfLargo]

            float largoMedio = largoLosa - 2f * largoEscalera;

            // Pieza central (todo el ancho, franja del medio en Z)
            CrearLosaParte(padrePiso, "TechoPiso_Central",
                new Vector3(centroXLosa, alturaY, 0f),
                anchoLosa, largoMedio, igualarMats);

            // Pieza inferior (desde donde acaba la Esc izq hacia la derecha)
            float anchoLosaInf  = anchoLosa - anchoEscalera;
            float centroXInf    = losaMinX + anchoEscalera + anchoLosaInf / 2f;
            CrearLosaParte(padrePiso, "TechoPiso_Inferior",
                new Vector3(centroXInf, alturaY, -halfLargo + largoEscalera / 2f),
                anchoLosaInf, largoEscalera, igualarMats);

            // Pieza superior (ídem)
            CrearLosaParte(padrePiso, "TechoPiso_Superior",
                new Vector3(centroXInf, alturaY, halfLargo - largoEscalera / 2f),
                anchoLosaInf, largoEscalera, igualarMats);
        }
        else
        {
            // ── 4 escaleras: 2 izquierda (en minX) + 2 derecha (en X=0) ──
            // Bloque IZQUIERDO: desde losaMinX hasta 0
            float xEscDer_IzqBorde = 0f; // borde izq de la mitad derecha
            float anchoMitadIzq    = anchoPabellon / 2f; // = 0 - minX

            // → Central izq (entre largoEscalera y largoLosa-largoEscalera en Z)
            float largoMedio = largoLosa - 2f * largoEscalera;
            CrearLosaParte(padrePiso, "TechoPiso_IzqCentral",
                new Vector3(losaMinX + anchoMitadIzq / 2f, alturaY, 0f),
                anchoMitadIzq, largoMedio, igualarMats);

            // → Inferior izq: hueco entre Esc izq y la pared del bloque derecho (en X=0)
            float anchoHuecoInfIzq = anchoMitadIzq - anchoEscalera;
            if (anchoHuecoInfIzq > 0.05f)
            {
                float cxInfIzq = losaMinX + anchoEscalera + anchoHuecoInfIzq / 2f;
                CrearLosaParte(padrePiso, "TechoPiso_IzqInferior",
                    new Vector3(cxInfIzq, alturaY, -halfLargo + largoEscalera / 2f),
                    anchoHuecoInfIzq, largoEscalera, igualarMats);
            }
            // → Superior izq
            if (anchoHuecoInfIzq > 0.05f)
            {
                float cxSupIzq = losaMinX + anchoEscalera + anchoHuecoInfIzq / 2f;
                CrearLosaParte(padrePiso, "TechoPiso_IzqSuperior",
                    new Vector3(cxSupIzq, alturaY, halfLargo - largoEscalera / 2f),
                    anchoHuecoInfIzq, largoEscalera, igualarMats);
            }

            // Bloque DERECHO: desde 0 hasta losaMaxX
            float anchoMitadDer = anchoPabellon / 2f + extX;
            float cxDer         = xEscDer_IzqBorde + anchoMitadDer / 2f + offsetX;

            // → Central der
            CrearLosaParte(padrePiso, "TechoPiso_DerCentral",
                new Vector3(cxDer, alturaY, 0f),
                anchoMitadDer, largoMedio, igualarMats);

            // → Inferior der: desde borde der Esc der (en X = anchoEscalera) hasta losaMaxX
            float anchoHuecoInfDer = anchoMitadDer - anchoEscalera;
            if (anchoHuecoInfDer > 0.05f)
            {
                float cxInfDer = xEscDer_IzqBorde + anchoEscalera + anchoHuecoInfDer / 2f + offsetX;
                CrearLosaParte(padrePiso, "TechoPiso_DerInferior",
                    new Vector3(cxInfDer, alturaY, -halfLargo + largoEscalera / 2f),
                    anchoHuecoInfDer, largoEscalera, igualarMats);
            }
            // → Superior der
            if (anchoHuecoInfDer > 0.05f)
            {
                float cxSupDer = xEscDer_IzqBorde + anchoEscalera + anchoHuecoInfDer / 2f + offsetX;
                CrearLosaParte(padrePiso, "TechoPiso_DerSuperior",
                    new Vector3(cxSupDer, alturaY, halfLargo - largoEscalera / 2f),
                    anchoHuecoInfDer, largoEscalera, igualarMats);
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // HELPERS DE CREACIÓN — PASILLOS
    // ──────────────────────────────────────────────────────────
    private void CrearEscalera(Transform padre, Vector3 localPos, Quaternion localRot, string nombre)
    {
        GameObject go = ObtenerOInstanciar(escaleraPrefab, padre, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        Escaleras comp = go.GetComponent<Escaleras>();
        if (comp != null) comp.Configurar(anchoEscalera, largoEscalera, alturaPisos);
    }

    private void CrearPasilloX(Transform padre, Vector3 localPos, float longitudX, string nombre)
    {
        GameObject go = ObtenerOInstanciar(pasilloPrefab, padre, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        ConfigurarPasillo(go, longitudX);
    }

    private void CrearPasilloZ(Transform padre, Vector3 localPos, float longitudZ, string nombre)
    {
        GameObject go = ObtenerOInstanciar(pasilloPrefab, padre, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        ConfigurarPasillo(go, longitudZ);
    }

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

    private void ConfigurarPasillo(GameObject go, float longitud)
    {
        PasilloConfig pc = go.GetComponent<PasilloConfig>();
        if (pc != null)
        {
            float grosorInf = grosorLosaInferiorPasillo > 0 ? grosorLosaInferiorPasillo : grosorPisos;
            pc.tieneLuces = true;
            pc.Configurar(longitud, anchoPasillos, grosorInf, alturaPisos,
                          conTecho: grosorLosaSuperiorPasillo > 0,
                          conSuelo: grosorLosaInferiorPasillo > 0);
            if (losaPrefab != null) pc.losaPrefab = losaPrefab;
        }
    }

    // ──────────────────────────────────────────────────────────
    // HELPERS DE CREACIÓN — BLOQUES
    // ──────────────────────────────────────────────────────────
    private BloqueHabitacion CrearBloqueExtremo(Transform padre, Vector3 localPos, float anchoTotalX, float largoZ, string nombre, bool haciaZPositiva)
    {
        GameObject go = ObtenerOInstanciar(bloqueHabitacionPrefab, padre, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        BloqueHabitacion bloque = go.GetComponent<BloqueHabitacion>();
        if (bloque != null)
        {
            const float minHabX = 5f; // mínimo 5 m en X

            // Calcular cuántas habitaciones caben, respetando coste de pasillos Z internos
            int cantHabX = Mathf.Max(1, Mathf.FloorToInt(anchoTotalX / minHabX));
            while (cantHabX > 1)
            {
                int cortesZ = 0;
                for (int j = 2; j < cantHabX; j += 2) cortesZ++;
                if (cantHabX * minHabX + cortesZ * anchoPasillos <= anchoTotalX) break;
                cantHabX--;
            }

            // Distribuir espacio sobrante
            int cortesFinZ = 0;
            for (int j = 2; j < cantHabX; j += 2) cortesFinZ++;
            float tamHabX = Mathf.Max(minHabX, (anchoTotalX - cortesFinZ * anchoPasillos) / cantHabX);

            // Propagar configuración de losas
            bloque.grosorLosaInferiorPasillo = grosorLosaInferiorPasillo;
            bloque.grosorLosaSuperiorPasillo = grosorLosaSuperiorPasillo;
            bloque.grosorSuelo = grosorSueloHabitacion;
            bloque.grosorTecho = grosorTechoHabitacion;
            if (losaPrefab != null) bloque.techoPisoBasePrefab = losaPrefab;

            bloque.Configurar(
                1, cantHabX, tamHabX, largoZ, alturaPisos,
                false, false, anchoPasillos,
                BloqueHabitacion.TipoPuertasBloque.SoloUnaPuerta, false,
                haciaZPositiva, !haciaZPositiva, false, false
            );
        }
        return bloque;
    }

    private BloqueHabitacion CrearBloqueCentral(Transform padre, Vector3 localPos, float anchoTotalX, float largoTotalZ, bool hayPasilloZ)
    {
        GameObject go = ObtenerOInstanciar(bloqueHabitacionPrefab, padre, "BloqueHabitaciones_Central");
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        BloqueHabitacion bloque = go.GetComponent<BloqueHabitacion>();
        if (bloque != null)
        {
            // M: habitaciones en Z (min 10 m)
            int cantM = 1;
            for (int k = 2; k <= 8; k++)
            {
                int cortes = 0;
                for (int p = 6; p < k; p += 3) cortes++;
                if (k * 10f + cortes * anchoPasillos <= largoTotalZ) cantM = k;
                else break;
            }
            int cortesM = 0;
            for (int p = 6; p < cantM; p += 3) cortesM++;
            float tamZ = Mathf.Max(10f, (largoTotalZ - cortesM * anchoPasillos) / cantM);

            // N: habitaciones en X (min 8 m)
            int cantN = 1;
            for (int k = 2; k <= 6; k++)
            {
                int cortes = 0;
                for (int p = 2; p < k; p += 2) cortes++;
                if (k * 8f + cortes * anchoPasillos <= anchoTotalX) cantN = k;
                else break;
            }
            int cortesN = 0;
            for (int p = 2; p < cantN; p += 2) cortesN++;
            float tamX = Mathf.Max(8f, (anchoTotalX - cortesN * anchoPasillos) / cantN);

            bloque.grosorLosaInferiorPasillo = grosorLosaInferiorPasillo;
            bloque.grosorLosaSuperiorPasillo = grosorLosaSuperiorPasillo;
            bloque.grosorSuelo = grosorSueloHabitacion;
            bloque.grosorTecho = grosorTechoHabitacion;
            if (losaPrefab != null) bloque.techoPisoBasePrefab = losaPrefab;

            bloque.Configurar(
                cantM, cantN, tamX, tamZ, alturaPisos,
                true, true, anchoPasillos,
                BloqueHabitacion.TipoPuertasBloque.SoloUnaPuerta, false,
                true, true, hayPasilloZ, true
            );
        }
        return bloque;
    }

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
        pisosGenerados.Clear();

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
            escaleraPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Escaleras.prefab");
        if (materialVidrio == null)
            materialVidrio = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Advance/Uberto/Vidrio.mat");
#endif
    }
}
