using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>Configura y construye de forma independiente la geometría de un piso.</summary>

public class PisoConfig : MonoBehaviour
{
    [System.Serializable]
    private class EstadoBloqueo
    {
        public BloqueoBase bloqueo;
        public bool yaActivado;
        public bool puedeActivarse;
    }
    [Header("Piso Config")]
    public GameObject techoPisoPrefab;
    public GameObject losaPrefab;
    public int indicePiso;
    public float alturaNivel;
    public float alturaPisos;
    public float grosorPisos = 0.25f;
    public float grosorPared = 0.20f;
    public float grosorParedVidrio = 0.05f;
    public float anchoPabellon = 12f;
    public float largoPabellon = 48f;
    public bool hayPasilloZ = false;
    public bool esPrimerPiso = false;
    public bool esUltimoPiso = false;
    [Header("Bloque Habitacion Config")]
    public GameObject bloqueHabitacionPrefab;
    public GameObject pasilloPrefab;
    public float anchoPasillos = 4f;
    public float grosorLosaInferiorPasillo = 0f;
    public float grosorLosaSuperiorPasillo = 0f;
    [Header("Habitacion Config")]
    public GameObject habitacionPrefab;
    public GameObject pupitreProfesorPrefab;
    public GameObject pupitreEstudiantesPrefab;
    public float limiteCentralHabitacionZ = 10f;
    public float limiteCentralHabitacionX = 8f;
    public float grosorLosaInferiorHabitacion = 0f;
    public float grosorLosaSuperiorHabitacion = 0f;
    [Min(0f)] public float espacioColumnasPupitres = 0.8f;
    [Min(0f)] public float espacioCaminoHabitacion = 1f;
    [Header("Extintores")]
    public GameObject cajaExtintorPrefab;
    [Min(0f)] public float alturaBaseExtintor = 1f;
    [Header("Escalera Config")]
    public GameObject escaleraPrefab;
    public bool cuatroEscaleras;
    public float anchoEscalera = 7f;
    public float largoEscalera = 5f;
    public float limiteEscaleraHabitacionX = 7f;
    public float limiteEscaleraHabitacionZ = 5f;
    [Header("Bloqueos de Piso")]
    [SerializeField] private List<EstadoBloqueo> bloqueos = new List<EstadoBloqueo>();

    private int[,] clasificacionHabitacionesCompartida;
    private int[,] clasificacionCentralGenerada;
    [System.NonSerialized] private PCGPabellonUniversitario generadorEdificio;
    private bool preparacionIniciada;
    private bool preparacionFinalizada;
    private bool elementosEscaleraYBloquesGenerados;
    private bool pasillosGenerados;
    private bool bloqueCentralGenerado;
    private bool losasGeneradas;
    private bool finalizacionIniciada;
    private bool generacionFinalizada;

    public bool PreparacionFinalizada => preparacionFinalizada;
    public bool GeneracionFinalizada => generacionFinalizada;

    public List<BloqueoEscalera> ObtenerBloqueosEscalera()
    {
        List<BloqueoEscalera> resultado = new List<BloqueoEscalera>();
        HashSet<BloqueoEscalera> unicos = new HashSet<BloqueoEscalera>();

        if (bloqueos != null)
        {
            foreach (EstadoBloqueo estado in bloqueos)
            {
                BloqueoEscalera escalera = estado != null ? estado.bloqueo as BloqueoEscalera : null;
                if (escalera != null && unicos.Add(escalera))
                    resultado.Add(escalera);
            }
        }

        // También recoge escaleras colocadas manualmente o instanciadas por una escena antigua.
        BloqueoEscalera[] encontrados = GetComponentsInChildren<BloqueoEscalera>(true);
        foreach (BloqueoEscalera escalera in encontrados)
        {
            if (escalera != null && unicos.Add(escalera))
                resultado.Add(escalera);
        }

        resultado.Sort((a, b) => string.CompareOrdinal(NombreEscaleraEnPiso(a), NombreEscaleraEnPiso(b)));
        return resultado;
    }

    private string NombreEscaleraEnPiso(BloqueoEscalera bloqueo)
    {
        Transform raiz = bloqueo.transform;
        while (raiz.parent != null && raiz.parent != transform)
            raiz = raiz.parent;
        return raiz.name;
    }

    public void Configurar(int indicePiso, float alturaNivel, float alturaPisos, bool cuatroEscaleras, bool esPrimero, bool esUltimo)
    {
        this.indicePiso = indicePiso;
        this.alturaNivel = alturaNivel;
        this.alturaPisos = alturaPisos;
        this.cuatroEscaleras = cuatroEscaleras;
        generadorEdificio = null;
        esPrimerPiso = esPrimero;
        esUltimoPiso = esUltimo;
        ReiniciarEstadoGeneracion();
    }

    /// <summary>Recibe una copia de la configuración compartida del edificio.</summary>
    public void ConfigurarDesde(PCGPabellonUniversitario generador, int indicePiso, float alturaNivel, bool cuatroEscaleras)
    {
        if (generador == null)
        {
            Debug.LogError("PisoConfig necesita un PCGPabellonUniversitario para configurarse.", this);
            return;
        }

        Configurar(indicePiso, alturaNivel, generador.alturaPisos, cuatroEscaleras,
            indicePiso == 0, indicePiso == generador.numeroPisos - 1);
        generadorEdificio = generador;

        techoPisoPrefab = generador.techoPisoPrefab;
        losaPrefab = generador.losaPrefab;
        bloqueHabitacionPrefab = generador.bloqueHabitacionPrefab;
        pasilloPrefab = generador.pasilloPrefab;
        escaleraPrefab = generador.escaleraPrefab;
        habitacionPrefab = generador.habitacionPrefab;
        pupitreProfesorPrefab = generador.pupitreProfesorPrefab;
        pupitreEstudiantesPrefab = generador.pupitreEstudiantesPrefab;

        grosorPisos = generador.grosorPisos;
        grosorPared = generador.grosorPared;
        grosorParedVidrio = generador.grosorParedVidrio;
        anchoPabellon = generador.anchoPabellon;
        largoPabellon = generador.largoPabellon;
        hayPasilloZ = anchoPabellon < 28f;
        anchoPasillos = generador.anchoPasillos;
        grosorLosaInferiorPasillo = generador.grosorLosaInferiorPasillo;
        grosorLosaSuperiorPasillo = generador.grosorLosaSuperiorPasillo;
        grosorLosaInferiorHabitacion = generador.grosorLosaInferiorHabitacion;
        grosorLosaSuperiorHabitacion = generador.grosorLosaSuperiorHabitacion;
        limiteCentralHabitacionZ = generador.limiteCentralHabitacionZ;
        limiteCentralHabitacionX = generador.limiteCentralHabitacionX;
        espacioColumnasPupitres = generador.espacioColumnasPupitres;
        espacioCaminoHabitacion = generador.espacioCaminoHabitacion;
        anchoEscalera = generador.anchoEscalera;
        largoEscalera = generador.largoEscalera;
        limiteEscaleraHabitacionX = generador.limiteEscaleraHabitacionX;
        limiteEscaleraHabitacionZ = generador.limiteEscaleraHabitacionZ;

        AsignarClasificacionCompartida(null);
        if (bloqueos == null)
            bloqueos = new List<EstadoBloqueo>();
        else
            bloqueos.Clear();
    }

    public void AsignarClasificacionCompartida(int[,] clasificacion)
    {
        clasificacionHabitacionesCompartida = ClonarMatriz(clasificacion);
    }

    public int[,] PrepararClasificacionCentral()
    {
        if (!preparacionFinalizada)
        {
            if (preparacionIniciada)
            {
                Debug.LogError("No se puede clasificar un piso mientras su preparacion sigue en curso.", this);
                return null;
            }
            PrepararPiso();
        }

        if (!bloqueCentralGenerado)
            CrearBloqueCentral();

        return ClonarMatriz(clasificacionCentralGenerada);
    }

    public void PrepararPiso()
    {
        if (preparacionFinalizada || preparacionIniciada)
            return;

        preparacionIniciada = true;
        GenerarEscalerasYBloquesExtremos();
        GenerarPasillosInteriores();
        preparacionFinalizada = true;
    }

    public IEnumerator PrepararPisoEnParalelo()
    {
        if (preparacionFinalizada || preparacionIniciada)
            yield break;

        preparacionIniciada = true;
        GenerarEscalerasYBloquesExtremos();
        yield return null;

        GenerarPasillosInteriores();
        yield return null;
        preparacionFinalizada = true;
    }

    public void CompletarPiso()
    {
        if (generacionFinalizada || finalizacionIniciada)
            return;

        finalizacionIniciada = true;
        if (!preparacionFinalizada)
        {
            if (preparacionIniciada)
            {
                finalizacionIniciada = false;
                return;
            }
            PrepararPiso();
        }
        if (!bloqueCentralGenerado)
            CrearBloqueCentral();
        if (!losasGeneradas)
            GenerarTechoPisoIntermedio();
        generacionFinalizada = true;
    }

    public IEnumerator CompletarPisoEnParalelo()
    {
        if (generacionFinalizada || finalizacionIniciada)
            yield break;

        finalizacionIniciada = true;
        if (!preparacionIniciada)
            yield return PrepararPisoEnParalelo();
        while (!preparacionFinalizada)
            yield return null;
        if (!bloqueCentralGenerado)
            CrearBloqueCentral();
        yield return null;

        if (!losasGeneradas)
            GenerarTechoPisoIntermedio();
        yield return null;
        generacionFinalizada = true;
    }

    private void ReiniciarEstadoGeneracion()
    {
        preparacionIniciada = false;
        preparacionFinalizada = false;
        elementosEscaleraYBloquesGenerados = false;
        pasillosGenerados = false;
        bloqueCentralGenerado = false;
        losasGeneradas = false;
        finalizacionIniciada = false;
        generacionFinalizada = false;
        clasificacionCentralGenerada = null;
    }

    private static int[,] ClonarMatriz(int[,] origen)
    {
        if (origen == null)
            return null;

        int[,] copia = new int[origen.GetLength(0), origen.GetLength(1)];
        System.Array.Copy(origen, copia, origen.Length);
        return copia;
    }
    private GameObject ObtenerOInstanciar(GameObject prefab, string nombre)
    {
        GameObject go;
        if (prefab != null)
            go = Instantiate(prefab, transform);
        else
        {
            go = new GameObject();
            go.transform.SetParent(transform, false);
        }
        go.name = nombre;
        return go;
    }
    private void CrearEscalera(Vector3 localPos, Quaternion localRot, string nombre, bool esPrimerPiso)
    {
        GameObject go = ObtenerOInstanciar(escaleraPrefab, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        EscaleraConfig comp = go.GetComponent<EscaleraConfig>();
        if (comp != null) comp.Configurar(esPrimerPiso);

        BloqueoEscalera bloqueoEscalera = go.GetComponentInChildren<BloqueoEscalera>(true);
        if (bloqueos == null)
            bloqueos = new List<EstadoBloqueo>();
        if (bloqueoEscalera != null && !bloqueos.Exists(estado => estado != null && estado.bloqueo == bloqueoEscalera))
            bloqueos.Add(new EstadoBloqueo { bloqueo = bloqueoEscalera });
    }
    private BloqueHabitacion CrearBloqueExtremo(Vector3 localPos, float anchoTotalX, float largoZ, string nombre, bool haciaZPositiva, bool esParedVidrioEste = false, bool tienePasilloDerecho = false, bool esUltimoPiso = false)
    {
        GameObject go = ObtenerOInstanciar(bloqueHabitacionPrefab, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        BloqueHabitacion bloque = go.GetComponent<BloqueHabitacion>();
        if (bloque != null)
        {
            float minHabX = Mathf.Max(1f, limiteEscaleraHabitacionX);
            float minHabZ = Mathf.Max(1f, limiteEscaleraHabitacionZ);

            // Largo interior útil
            float tamHabZ = Mathf.Max(1f, largoZ - 2f * grosorPared);

            // Calcular cuántas habitaciones caben en anchoTotalX respetando minHabX y paredes compartidas
            int cantHabX = 1;
            for (int k = 2; k <= 10; k++)
            {
                int cortesZ = 0;
                for (int j = 2; j < k; j += 2) cortesZ++;
                int paredesX = (k + 1) + cortesZ;
                float espacioRequerido = k * minHabX + paredesX * grosorPared + cortesZ * anchoPasillos;
                if (anchoTotalX >= espacioRequerido) cantHabX = k;
                else break;
            }

            int cortesFinZ = 0;
            for (int j = 2; j < cantHabX; j += 2) cortesFinZ++;
            int numParedesXFin = (cantHabX + 1) + cortesFinZ;
            float espacioDisponibleHabs = anchoTotalX - numParedesXFin * grosorPared - cortesFinZ * anchoPasillos;
            // Si la habitación es inferior al límite y solo es una, se crea de igual forma:
            float tamHabX = (cantHabX == 1) ? Mathf.Max(0.5f, espacioDisponibleHabs) : Mathf.Max(minHabX, espacioDisponibleHabs / cantHabX);

            AplicarConfiguracionBloque(bloque);

            bloque.Configurar(
                1, cantHabX, tamHabX, tamHabZ, alturaPisos,
                false, false, anchoPasillos,
                BloqueHabitacion.TipoPuertasBloque.SoloUnaPuerta, false,
                haciaZPositiva, !haciaZPositiva, false, tienePasilloDerecho,
                esParedVidrioEste,
                esExtremoEscalera: true,
                esPisoFinal: esUltimoPiso,
                nuevoEspacioColumnasPupitres: espacioColumnasPupitres,
                nuevoEspacioCaminoHabitacion: espacioCaminoHabitacion
            );
        }
        return bloque;
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

    private void AplicarConfiguracionBloque(BloqueHabitacion bloque)
    {
        bloque.grosorLosaInferiorPasillo = grosorLosaInferiorPasillo;
        bloque.grosorLosaSuperiorPasillo = grosorLosaSuperiorPasillo;
        bloque.grosorSuelo = grosorLosaInferiorHabitacion;
        bloque.grosorTecho = grosorLosaSuperiorHabitacion;
        bloque.grosorPared = grosorPared;
        if (habitacionPrefab != null) bloque.habitacionPrefab = habitacionPrefab;
        if (pasilloPrefab != null) bloque.pasilloPrefab = pasilloPrefab;
        if (techoPisoPrefab != null) bloque.techoPisoPrefab = techoPisoPrefab;
        if (losaPrefab != null) bloque.techoPisoBasePrefab = losaPrefab;
        if (pupitreProfesorPrefab != null) bloque.pupitreProfesorPrefab = pupitreProfesorPrefab;
        if (pupitreEstudiantesPrefab != null) bloque.pupitreEstudiantesPrefab = pupitreEstudiantesPrefab;
    }

    private void CrearPasilloX(Vector3 localPos, float longitudX, string nombre)
    {
        GameObject go = ObtenerOInstanciar(pasilloPrefab, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        ConfigurarPasillo(go, longitudX);
    }
    private void CrearPasilloZ(Vector3 localPos, float longitudZ, string nombre)
    {
        GameObject go = ObtenerOInstanciar(pasilloPrefab, nombre);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        ConfigurarPasillo(go, longitudZ);
    }
    private BloqueHabitacion CrearBloqueCentral(Vector3 localPos, float anchoTotalX, float largoTotalZ, bool esParedVidrioEste, bool tienePasilloDerecho = false)
    {
        GameObject go = ObtenerOInstanciar(bloqueHabitacionPrefab, "BloqueHabitaciones_Central");
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        BloqueHabitacion bloque = go.GetComponent<BloqueHabitacion>();
        if (bloque != null)
        {
            float minHabZ = Mathf.Max(1f, limiteCentralHabitacionZ);
            float minHabX = Mathf.Max(1f, limiteCentralHabitacionX);

            // M: habitaciones en Z
            int cantM = 1;
            for (int k = 2; k <= 8; k++)
            {
                int cortes = 0;
                for (int p = 6; p < k; p += 3) cortes++;
                int numParedesZ = (k + 1) + cortes;
                float espacioNecesario = k * minHabZ + numParedesZ * grosorPared + cortes * anchoPasillos;
                if (largoTotalZ >= espacioNecesario) cantM = k;
                else break;
            }
            int cortesM = 0;
            for (int p = 6; p < cantM; p += 3) cortesM++;
            int numParedesZFin = (cantM + 1) + cortesM;
            float espacioDisponibleZ = largoTotalZ - numParedesZFin * grosorPared - cortesM * anchoPasillos;
            float tamZ = (cantM == 1) ? Mathf.Max(0.5f, espacioDisponibleZ) : Mathf.Max(minHabZ, espacioDisponibleZ / cantM);

            // N: habitaciones en X
            int cantN = 1;
            for (int k = 2; k <= 6; k++)
            {
                int cortes = 0;
                for (int p = 2; p < k; p += 2) cortes++;
                int numParedesX = (k + 1) + cortes;
                float espacioNecesario = k * minHabX + numParedesX * grosorPared + cortes * anchoPasillos;
                if (anchoTotalX >= espacioNecesario) cantN = k;
                else break;
            }
            int cortesN = 0;
            for (int p = 2; p < cantN; p += 2) cortesN++;
            int numParedesXFin = (cantN + 1) + cortesN;
            float espacioDisponibleX = anchoTotalX - numParedesXFin * grosorPared - cortesN * anchoPasillos;
            float tamX = (cantN == 1) ? Mathf.Max(0.5f, espacioDisponibleX) : Mathf.Max(minHabX, espacioDisponibleX / cantN);

            AplicarConfiguracionBloque(bloque);

            bloque.Configurar(
                cantM, cantN, tamX, tamZ, alturaPisos,
                true, true, anchoPasillos,
                BloqueHabitacion.TipoPuertasBloque.SoloUnaPuerta, false,
                true, true, hayPasilloZ, tienePasilloDerecho,
                esParedVidrioEste,
                esExtremoEscalera: false,
                esPisoFinal: esUltimoPiso,
                nuevoEspacioColumnasPupitres: espacioColumnasPupitres,
                nuevoEspacioCaminoHabitacion: espacioCaminoHabitacion,
                tiposHabitacionesCompartidos: clasificacionHabitacionesCompartida,
                esPisoInicial: esPrimerPiso
            );

            if (clasificacionHabitacionesCompartida == null)
                clasificacionCentralGenerada = ObtenerClasificacionRealGenerada(bloque);
        }
        bloqueCentralGenerado = true;
        return bloque;
    }

    private static int[,] ObtenerClasificacionRealGenerada(BloqueHabitacion bloque)
    {
        int[,] clasificacion = bloque.ObtenerClasificacionHabitaciones();
        if (bloque.habitaciones == null || bloque.habitaciones.Count != bloque.m * bloque.n)
            return clasificacion;

        int[,] clasificacionReal = new int[bloque.m, bloque.n];
        for (int indice = 0; indice < bloque.habitaciones.Count; indice++)
        {
            HabitacionConfig habitacion = bloque.habitaciones[indice];
            if (habitacion == null)
                return clasificacion;

            clasificacionReal[indice / bloque.n, indice % bloque.n] = habitacion.tipo;
        }
        return clasificacionReal;
    }
    public void GenerarPiso()
    {
        PrepararPiso();
        PrepararClasificacionCentral();
        CompletarPiso();
    }

    private void GenerarEscalerasYBloquesExtremos()
    {
        if (elementosEscaleraYBloquesGenerados)
            return;

        float minX = -anchoPabellon / 2f;
        float maxX = anchoPabellon / 2f;
        float minZ = -largoPabellon / 2f;
        float maxZ = largoPabellon / 2f;
        bool enPisosConVidrio = indicePiso >= 1;

        CrearEscalera(new Vector3(minX + anchoEscalera / 2f, 0f, minZ + largoEscalera / 2f),
            Quaternion.Euler(0f, 270f, 0f), "Escalera_Inferior_Izquierda", esPrimerPiso);
        CrearEscalera(new Vector3(minX + anchoEscalera / 2f, 0f, maxZ - largoEscalera / 2f),
            Quaternion.Euler(0f, 90f, 0f), "Escalera_Superior_Izquierda", esPrimerPiso);

        if (cuatroEscaleras)
        {
            CrearEscalera(new Vector3(anchoEscalera / 2f, 0f, minZ + largoEscalera / 2f),
                Quaternion.Euler(0f, 270f, 0f), "Escalera_Inferior_Derecha", esPrimerPiso);
            CrearEscalera(new Vector3(anchoEscalera / 2f, 0f, maxZ - largoEscalera / 2f),
                Quaternion.Euler(0f, 90f, 0f), "Escalera_Superior_Derecha", esPrimerPiso);

            float anchoBlqIzq = anchoPabellon / 2f - anchoEscalera;
            if (anchoBlqIzq >= 1f)
            {
                float centroXBlqIzq = minX + anchoEscalera + anchoBlqIzq / 2f;
                CrearBloqueExtremo(new Vector3(centroXBlqIzq, 0f, minZ + largoEscalera / 2f),
                    anchoBlqIzq, largoEscalera, "BloqueHab_ExtremoInferior_Izq", true,
                    esUltimoPiso: esUltimoPiso);
                CrearBloqueExtremo(new Vector3(centroXBlqIzq, 0f, maxZ - largoEscalera / 2f),
                    anchoBlqIzq, largoEscalera, "BloqueHab_ExtremoSuperior_Izq", false,
                    esUltimoPiso: esUltimoPiso);
            }

            float anchoBlqDer = anchoPabellon / 2f - anchoEscalera;
            if (anchoBlqDer >= 1f)
            {
                float centroXBlqDer = anchoEscalera + anchoBlqDer / 2f;
                CrearBloqueExtremo(new Vector3(centroXBlqDer, 0f, minZ + largoEscalera / 2f),
                    anchoBlqDer, largoEscalera, "BloqueHab_ExtremoInferior_Der", true,
                    esParedVidrioEste: enPisosConVidrio, esUltimoPiso: esUltimoPiso);
                CrearBloqueExtremo(new Vector3(centroXBlqDer, 0f, maxZ - largoEscalera / 2f),
                    anchoBlqDer, largoEscalera, "BloqueHab_ExtremoSuperior_Der", false,
                    esParedVidrioEste: enPisosConVidrio, esUltimoPiso: esUltimoPiso);
            }
        }
        else
        {
            float anchoDispBloques = anchoPabellon - anchoEscalera;
            float centroXBloques = minX + anchoEscalera + anchoDispBloques / 2f;
            CrearBloqueExtremo(new Vector3(centroXBloques, 0f, minZ + largoEscalera / 2f),
                anchoDispBloques, largoEscalera, "BloqueHab_ExtremoInferior", true,
                esParedVidrioEste: enPisosConVidrio, esUltimoPiso: esUltimoPiso);
            CrearBloqueExtremo(new Vector3(centroXBloques, 0f, maxZ - largoEscalera / 2f),
                anchoDispBloques, largoEscalera, "BloqueHab_ExtremoSuperior", false,
                esParedVidrioEste: enPisosConVidrio, esUltimoPiso: esUltimoPiso);
        }

        GenerarExtintoresBloquesExtremos();
        elementosEscaleraYBloquesGenerados = true;
    }

    private void GenerarExtintoresBloquesExtremos()
    {
        if (cajaExtintorPrefab == null)
        {
            Debug.LogWarning("PisoConfig no tiene asignado el prefab de CajaExtintor.", this);
            return;
        }

        List<PCGPabellonUniversitario.UbicacionExtintorCompartida> ubicaciones;
        if (generadorEdificio != null
            && generadorEdificio.IntentarObtenerUbicacionesExtintorCompartidas(out ubicaciones))
        {
            InstanciarExtintoresCompartidos(ubicaciones);
            return;
        }

        List<BloqueHabitacion> bloquesNorte = new List<BloqueHabitacion>();
        List<BloqueHabitacion> bloquesSur = new List<BloqueHabitacion>();
        BloqueHabitacion[] bloques = GetComponentsInChildren<BloqueHabitacion>(true);
        foreach (BloqueHabitacion bloque in bloques)
        {
            if (bloque == null || !bloque.esBloqueEscalera || bloque.habitaciones == null || bloque.habitaciones.Count == 0)
                continue;

            float zLocal = transform.InverseTransformPoint(bloque.transform.position).z;
            if (zLocal > 0f)
                bloquesNorte.Add(bloque);
            else if (zLocal < 0f)
                bloquesSur.Add(bloque);
        }

        bool crearEnNorte = cuatroEscaleras || largoPabellon > 32f;
        ubicaciones = new List<PCGPabellonUniversitario.UbicacionExtintorCompartida>(2);
        CalcularExtintorEnBloqueAleatorio(bloquesSur, "Sur", ubicaciones);
        if (crearEnNorte)
            CalcularExtintorEnBloqueAleatorio(bloquesNorte, "Norte", ubicaciones);

        if (generadorEdificio != null)
        {
            generadorEdificio.GuardarUbicacionesExtintorCompartidas(ubicaciones);
            generadorEdificio.IntentarObtenerUbicacionesExtintorCompartidas(out ubicaciones);
        }

        InstanciarExtintoresCompartidos(ubicaciones);
    }

    private void CalcularExtintorEnBloqueAleatorio(
        List<BloqueHabitacion> candidatos,
        string extremo,
        List<PCGPabellonUniversitario.UbicacionExtintorCompartida> ubicaciones)
    {
        if (candidatos == null || candidatos.Count == 0)
        {
            Debug.LogWarning($"No se encontró un bloque de escalera válido en el extremo {extremo} del piso {indicePiso + 1}.", this);
            return;
        }

        BloqueHabitacion bloque = candidatos[Random.Range(0, candidatos.Count)];
        HabitacionConfig habitacion = ObtenerHabitacionMasIzquierdaYSuperior(bloque);
        if (habitacion == null)
            return;

        bool pasilloEnFrontal = bloque.pasilloEnBordeFrontal;
        bool pasilloEnTrasera = bloque.pasilloEnBordeTrasero;
        if (pasilloEnFrontal == pasilloEnTrasera)
        {
            Debug.LogWarning($"El bloque {bloque.name} no tiene un único pasillo X exterior identificable.", bloque);
            return;
        }

        Transform paredTransform = pasilloEnFrontal ? habitacion.paredFrontal : habitacion.paredTrasera;
        ParedConfig pared = paredTransform != null ? paredTransform.GetComponent<ParedConfig>() : null;
        if (pared == null || pared.cantidadPuertas != 1 || pared.puerta1 == null)
        {
            Debug.LogWarning($"La pared del pasillo X de {habitacion.name} no tiene exactamente una puerta utilizable.", habitacion);
            return;
        }

        Vector3 puertaLocal = transform.InverseTransformPoint(pared.puerta1.transform.position);
        Vector3 paredLocal = transform.InverseTransformPoint(pared.transform.position);
        Vector3 bordeIzquierdoMundo = habitacion.transform.TransformPoint(new Vector3(-habitacion.ancho / 2f, 0f, 0f));
        Vector3 bordeIzquierdoLocal = transform.InverseTransformPoint(bordeIzquierdoMundo);
        Vector3 direccionPasilloMundo = pasilloEnFrontal ? habitacion.transform.forward : -habitacion.transform.forward;
        Vector3 direccionPasilloLocal = transform.InverseTransformDirection(direccionPasilloMundo).normalized;

        float x = (puertaLocal.x + bordeIzquierdoLocal.x) * 0.5f;
        float y = transform.InverseTransformPoint(habitacion.transform.position).y
            + alturaBaseExtintor;
        // El pivote del gabinete está en su cara trasera: apóyala en la cara
        // exterior de la pared y deja que el cuerpo sobresalga hacia el pasillo.
        float z = paredLocal.z + direccionPasilloLocal.z * (pared.grosor / 2f);

        float rotacionY = direccionPasilloLocal.z >= 0f ? 0f : 180f;
        ubicaciones.Add(new PCGPabellonUniversitario.UbicacionExtintorCompartida(
            new Vector3(x, y, z), rotacionY));
    }

    private void InstanciarExtintoresCompartidos(
        List<PCGPabellonUniversitario.UbicacionExtintorCompartida> ubicaciones)
    {
        if (ubicaciones == null)
            return;

        for (int i = 0; i < ubicaciones.Count; i++)
        {
            GameObject instancia = Instantiate(cajaExtintorPrefab, transform);
            instancia.name = $"Extintor_Piso{indicePiso + 1}_{i + 1}";
            instancia.transform.localPosition = ubicaciones[i].posicionLocal;
            instancia.transform.localRotation = Quaternion.Euler(0f, ubicaciones[i].rotacionY, 0f);
        }
    }

    private HabitacionConfig ObtenerHabitacionMasIzquierdaYSuperior(BloqueHabitacion bloque)
    {
        HabitacionConfig resultado = null;
        float menorX = float.PositiveInfinity;
        float mayorZ = float.NegativeInfinity;

        foreach (HabitacionConfig habitacion in bloque.habitaciones)
        {
            if (habitacion == null)
                continue;

            Vector3 posicionLocal = transform.InverseTransformPoint(habitacion.transform.position);
            if (posicionLocal.x < menorX - 0.001f
                || (Mathf.Abs(posicionLocal.x - menorX) <= 0.001f && posicionLocal.z > mayorZ))
            {
                resultado = habitacion;
                menorX = posicionLocal.x;
                mayorZ = posicionLocal.z;
            }
        }

        return resultado;
    }

    private void GenerarPasillosInteriores()
    {
        if (pasillosGenerados)
            return;

        float minX = -anchoPabellon / 2f;
        float maxZ = largoPabellon / 2f;
        float minZ = -largoPabellon / 2f;
        float posZPasilloXInf = minZ + largoEscalera + anchoPasillos / 2f;
        float posZPasilloXSup = maxZ - largoEscalera - anchoPasillos / 2f;
        CrearPasilloX(new Vector3(0f, 0f, posZPasilloXInf), anchoPabellon, "Pasillo_X_Inferior");
        CrearPasilloX(new Vector3(0f, 0f, posZPasilloXSup), anchoPabellon, "Pasillo_X_Superior");

        if (hayPasilloZ)
        {
            float inicioZ = minZ + largoEscalera + anchoPasillos;
            float finZ = maxZ - largoEscalera - anchoPasillos;
            float centroZMedio = (inicioZ + finZ) / 2f;
            float posXPasilloZInf = minX + anchoPasillos / 2f;
            CrearPasilloZ(new Vector3(posXPasilloZInf, 0f, centroZMedio),
                Mathf.Max(1f, finZ - inicioZ), "Pasillo_Z_Inferior");
        }

        pasillosGenerados = true;
    }

    private void ObtenerGeometriaCentral(out Vector3 posicion, out float ancho, out float largo, out bool paredVidrioEste)
    {
        float minX = -anchoPabellon / 2f;
        float minZ = -largoPabellon / 2f;
        float maxZ = largoPabellon / 2f;
        float inicioZ = minZ + largoEscalera + anchoPasillos;
        float finZ = maxZ - largoEscalera - anchoPasillos;
        float centroZ = (inicioZ + finZ) / 2f;

        ancho = hayPasilloZ ? anchoPabellon - anchoPasillos : anchoPabellon;
        float centroX = hayPasilloZ ? minX + (anchoPasillos + ancho / 2f) : 0f;
        largo = Mathf.Max(1f, finZ - inicioZ);
        posicion = new Vector3(centroX, 0f, centroZ);
        paredVidrioEste = indicePiso >= 1;
    }

    private void CrearBloqueCentral()
    {
        if (bloqueCentralGenerado)
            return;

        Vector3 posicion;
        float ancho;
        float largo;
        bool paredVidrioEste;
        ObtenerGeometriaCentral(out posicion, out ancho, out largo, out paredVidrioEste);
        CrearBloqueCentral(posicion, ancho, largo, paredVidrioEste, esPrimerPiso);
    }

    private void GenerarTechoPisoIntermedio()
    {
        if (losasGeneradas)
            return;

        bool esTechoPisoEspecial = esPrimerPiso || esUltimoPiso;
        float extX = esTechoPisoEspecial ? grosorParedVidrio : 0f;
        float offsetX = extX > 0f ? extX / 2f : 0f;
        float alturaY = alturaPisos + grosorPisos / 2f;
        GenerarTechoPisoIntermedio(alturaY, largoPabellon, extX, offsetX, cuatroEscaleras, esUltimoPiso);
        losasGeneradas = true;
    }

    private void GenerarTechoPisoIntermedio(float alturaY, float largoLosa, float extX, float offsetX, bool conCuatroEscaleras, bool igualarMats)
    {
        float anchoLosa = anchoPabellon + extX;
        float centroXLosa = offsetX;
        float halfLargo = largoLosa / 2f;
        float losaMinX = centroXLosa - anchoLosa / 2f;

        if (!conCuatroEscaleras)
        {
            float largoMedio = largoLosa - 2f * largoEscalera;
            CrearLosaParte(transform, "TechoPiso_Central", new Vector3(centroXLosa, alturaY, 0f), anchoLosa, largoMedio, igualarMats);

            float anchoLosaInf = anchoLosa - anchoEscalera;
            float centroXInf = losaMinX + anchoEscalera + anchoLosaInf / 2f;
            CrearLosaParte(transform, "TechoPiso_Inferior", new Vector3(centroXInf, alturaY, -halfLargo + largoEscalera / 2f), anchoLosaInf, largoEscalera, igualarMats);
            CrearLosaParte(transform, "TechoPiso_Superior", new Vector3(centroXInf, alturaY, halfLargo - largoEscalera / 2f), anchoLosaInf, largoEscalera, igualarMats);
            return;
        }

        float anchoMitadIzq = anchoPabellon / 2f;
        float largoMedio4 = largoLosa - 2f * largoEscalera;
        CrearLosaParte(transform, "TechoPiso_IzqCentral", new Vector3(losaMinX + anchoMitadIzq / 2f, alturaY, 0f), anchoMitadIzq, largoMedio4, igualarMats);

        float anchoHuecoInfIzq = anchoMitadIzq - anchoEscalera;
        if (anchoHuecoInfIzq > 0.05f)
        {
            float centroX = losaMinX + anchoEscalera + anchoHuecoInfIzq / 2f;
            CrearLosaParte(transform, "TechoPiso_IzqInferior", new Vector3(centroX, alturaY, -halfLargo + largoEscalera / 2f), anchoHuecoInfIzq, largoEscalera, igualarMats);
            CrearLosaParte(transform, "TechoPiso_IzqSuperior", new Vector3(centroX, alturaY, halfLargo - largoEscalera / 2f), anchoHuecoInfIzq, largoEscalera, igualarMats);
        }

        float anchoMitadDer = anchoPabellon / 2f + extX;
        float centroXDer = anchoMitadDer / 2f + offsetX;
        CrearLosaParte(transform, "TechoPiso_DerCentral", new Vector3(centroXDer, alturaY, 0f), anchoMitadDer, largoMedio4, igualarMats);

        float anchoHuecoInfDer = anchoMitadDer - anchoEscalera;
        if (anchoHuecoInfDer > 0.05f)
        {
            float centroX = anchoEscalera + anchoHuecoInfDer / 2f + offsetX;
            CrearLosaParte(transform, "TechoPiso_DerInferior", new Vector3(centroX, alturaY, -halfLargo + largoEscalera / 2f), anchoHuecoInfDer, largoEscalera, igualarMats);
            CrearLosaParte(transform, "TechoPiso_DerSuperior", new Vector3(centroX, alturaY, halfLargo - largoEscalera / 2f), anchoHuecoInfDer, largoEscalera, igualarMats);
        }
    }

    private void CrearLosaParte(Transform padre, string nombre, Vector3 posicion, float ancho, float largo, bool igualarMateriales)
    {
        GameObject losa = techoPisoPrefab != null ? Instantiate(techoPisoPrefab, padre) : new GameObject(nombre);
        losa.name = nombre;
        if (losa.transform.parent != padre)
            losa.transform.SetParent(padre, false);
        losa.transform.localPosition = posicion;
        losa.transform.localRotation = Quaternion.identity;

        TechoPisoConfig configuracion = losa.GetComponent<TechoPisoConfig>();
        if (configuracion != null)
        {
            configuracion.Configurar(ancho, grosorPisos, largo);
            if (igualarMateriales)
                configuracion.IgualarMateriales();
        }
        else
        {
            losa.transform.localScale = new Vector3(ancho, grosorPisos, largo);
        }
    }
}
