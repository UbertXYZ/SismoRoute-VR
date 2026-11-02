using System.Collections.Generic;
using UnityEngine;

public class BloqueHabitacion : MonoBehaviour
{
    [Header("Configuración del Bloque (n x 2)")]
    [Tooltip("Largo del bloque en número de habitaciones por fila (total: n x 2 habitaciones)")]
    [Min(1)] public int n = 3;

    [Tooltip("Indica si el bloque cuenta con un pasillo central intermedio entre las dos filas de habitaciones")]
    public bool caminoIntermedio = true;

    [Header("Dimensiones Compartidas de Habitaciones")]
    [Tooltip("Largo y ancho de cada habitación (al ser cuadradas)")]
    public float tamanoHabitacion = 8f;
    public float altoHabitacion = 4.5f;
    [Min(0.01f)] public float grosorPared = 0.15f;
    [Min(0f)] public float grosorSuelo = 0.05f;
    [Min(0f)] public float grosorTecho = 0.05f;

    [Header("Configuración del Pasillo Central")]
    public float anchoPasillo = 4f;
    public float altoPasillo = 0.25f;
    public PasilloConfig pasilloCentral;

    [Header("Reglas de Puertas")]
    [Tooltip("Generar puertas orientadas hacia el pasillo central")]
    public bool puertasHaciaPasillo = true;
    [Tooltip("Generar puertas hacia la fachada exterior (muros externos del bloque)")]
    public bool puertasHaciaExterior = false;
    [Tooltip("Generar puertas en los extremos del bloque (primera y última habitación hacia afuera)")]
    public bool puertasExtremosBloque = false;

    [Header("Prefabs para Generador PCG")]
    public GameObject habitacionPrefab;
    public GameObject pasilloPrefab;

    [Header("Habitaciones del Bloque")]
    [Tooltip("Fila superior de habitaciones (+Z)")]
    public List<HabitacionConfig> HabitacionesSuperiores = new List<HabitacionConfig>();
    [Tooltip("Fila inferior de habitaciones (-Z)")]
    public List<HabitacionConfig> HabitacionesInferiores = new List<HabitacionConfig>();

    private void OnValidate()
    {
        ActualizarBloque();
    }

    private void Start()
    {
        ActualizarBloque();
    }

    /// <summary>
    /// Actualiza dimensiones, posiciones y reglas de puertas de todas las habitaciones y pasillo existentes.
    /// </summary>
    [ContextMenu("Actualizar Bloque")]
    public void ActualizarBloque()
    {
        if (n < 1) n = 1;

        // 1. Configurar y posicionar el pasillo central
        ActualizarPasilloCentral();

        // 2. Posición Z de cada fila:
        // Si hay pasillo intermedio: se separan a la mitad del ancho del pasillo + mitad del tamaño de la habitación
        // Si NO hay pasillo: se tocan en Z = 0
        float separacionZ = caminoIntermedio
            ? (anchoPasillo / 2f + tamanoHabitacion / 2f)
            : (tamanoHabitacion / 2f);

        // Regla: Las puertas hacia el centro solo pueden existir si hay un pasillo intermedio.
        // Si no hay pasillo, el centro es la habitación de enfrente (NUNCA puerta hacia otra habitación).
        bool permitirPuertaHaciaCentro = caminoIntermedio && puertasHaciaPasillo;

        // 3. Actualizar fila superior (+Z)
        ActualizarFila(HabitacionesSuperiores, separacionZ, esFilaSuperior: true, permitirPuertaHaciaCentro);

        // 4. Actualizar fila inferior (-Z)
        ActualizarFila(HabitacionesInferiores, -separacionZ, esFilaSuperior: false, permitirPuertaHaciaCentro);
    }

    private void ActualizarPasilloCentral()
    {
        if (pasilloCentral == null)
        {
            pasilloCentral = GetComponentInChildren<PasilloConfig>();
        }

        if (pasilloCentral != null)
        {
            if (caminoIntermedio)
            {
                pasilloCentral.gameObject.SetActive(true);
                pasilloCentral.transform.localPosition = Vector3.zero;
                pasilloCentral.transform.localRotation = Quaternion.identity;

                float largoTotalPasillo = n * tamanoHabitacion;
                pasilloCentral.Configurar(largoTotalPasillo, anchoPasillo, altoPasillo);
            }
            else
            {
                pasilloCentral.gameObject.SetActive(false);
            }
        }
    }

    private void ActualizarFila(List<HabitacionConfig> fila, float posZ, bool esFilaSuperior, bool permitirPuertaHaciaCentro)
    {
        if (fila == null) return;

        int cantidad = fila.Count;
        for (int i = 0; i < cantidad; i++)
        {
            HabitacionConfig hab = fila[i];
            if (hab == null) continue;

            // Centrado a lo largo del eje X
            float posX = (i - (cantidad - 1) / 2f) * tamanoHabitacion;
            hab.transform.localPosition = new Vector3(posX, 0f, posZ);
            hab.transform.localRotation = Quaternion.identity;

            // Sincronizar dimensiones compartidas
            hab.ConfigurarDimensiones(tamanoHabitacion, altoHabitacion, grosorPared, grosorSuelo, grosorTecho);

            // Reglas de Puertas:
            // - NUNCA hacia otra habitación (lados entre cuartos adyacentes desactivados)
            // - Extremos (i == 0 e i == n-1): pueden apuntar hacia afuera si puertasExtremosBloque está activo
            bool puertaIzquierda = (i == 0) && puertasExtremosBloque;
            bool puertaDerecha = (i == cantidad - 1) && puertasExtremosBloque;

            bool puertaFrontal;
            bool puertaTrasera;

            if (esFilaSuperior)
            {
                // Fila Superior (+Z):
                // Frontal (+Z) apunta hacia el exterior del bloque
                // Trasera (-Z) apunta hacia el centro (pasillo o habitación opuesta)
                puertaFrontal = puertasHaciaExterior;
                puertaTrasera = permitirPuertaHaciaCentro;
            }
            else
            {
                // Fila Inferior (-Z):
                // Frontal (+Z) apunta hacia el centro (pasillo o habitación opuesta)
                // Trasera (-Z) apunta hacia el exterior del bloque
                puertaFrontal = permitirPuertaHaciaCentro;
                puertaTrasera = puertasHaciaExterior;
            }

            hab.ConfigurarPuertas(puertaFrontal, puertaTrasera, puertaIzquierda, puertaDerecha);
        }
    }

    /// <summary>
    /// Genera proceduralmente las n x 2 habitaciones y el pasillo a partir de prefabs.
    /// </summary>
    [ContextMenu("Generar / Regenerar Bloque (PCG)")]
    public void GenerarBloque()
    {
        if (habitacionPrefab == null)
        {
            Debug.LogWarning("BloqueHabitacion: Asigna el prefab de habitación para generar el bloque.");
            return;
        }

        // Limpiar habitaciones anteriores
        LimpiarHabitaciones();

        // 1. Instanciar o configurar Pasillo Central
        if (pasilloCentral == null && pasilloPrefab != null)
        {
            GameObject nuevoPasillo = Instantiate(pasilloPrefab, transform);
            nuevoPasillo.name = "PasilloCentral";
            pasilloCentral = nuevoPasillo.GetComponent<PasilloConfig>();
        }

        // 2. Instanciar Fila Superior (n habitaciones)
        for (int i = 0; i < n; i++)
        {
            GameObject go = Instantiate(habitacionPrefab, transform);
            go.name = "Habitacion_Superior_" + i;
            HabitacionConfig config = go.GetComponent<HabitacionConfig>();
            if (config != null)
            {
                HabitacionesSuperiores.Add(config);
            }
        }

        // 3. Instanciar Fila Inferior (n habitaciones)
        for (int i = 0; i < n; i++)
        {
            GameObject go = Instantiate(habitacionPrefab, transform);
            go.name = "Habitacion_Inferior_" + i;
            HabitacionConfig config = go.GetComponent<HabitacionConfig>();
            if (config != null)
            {
                HabitacionesInferiores.Add(config);
            }
        }

        // 4. Aplicar dimensiones, posiciones y puertas reglamentarias
        ActualizarBloque();
    }

    /// <summary>
    /// Limpia las listas y destruye los GameObjects de las habitaciones generadas.
    /// </summary>
    [ContextMenu("Limpiar Bloque")]
    public void LimpiarHabitaciones()
    {
        foreach (var hab in HabitacionesSuperiores)
        {
            if (hab != null)
            {
                if (Application.isPlaying) Destroy(hab.gameObject);
                else DestroyImmediate(hab.gameObject);
            }
        }
        HabitacionesSuperiores.Clear();

        foreach (var hab in HabitacionesInferiores)
        {
            if (hab != null)
            {
                if (Application.isPlaying) Destroy(hab.gameObject);
                else DestroyImmediate(hab.gameObject);
            }
        }
        HabitacionesInferiores.Clear();
    }

    /// <summary>
    /// Busca y clasifica automáticamente habitaciones existentes bajo este GameObject.
    /// </summary>
    [ContextMenu("Buscar Habitaciones en Hijos")]
    public void AutoDetectarHabitaciones()
    {
        HabitacionesSuperiores.Clear();
        HabitacionesInferiores.Clear();

        HabitacionConfig[] todas = GetComponentsInChildren<HabitacionConfig>();
        foreach (var hab in todas)
        {
            if (hab.transform.localPosition.z >= 0f)
            {
                HabitacionesSuperiores.Add(hab);
            }
            else
            {
                HabitacionesInferiores.Add(hab);
            }
        }

        // Ordenar por posición X
        HabitacionesSuperiores.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
        HabitacionesInferiores.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

        n = Mathf.Max(HabitacionesSuperiores.Count, HabitacionesInferiores.Count);
        ActualizarBloque();
    }
}