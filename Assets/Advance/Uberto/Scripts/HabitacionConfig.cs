using System.Collections.Generic;
using UnityEngine;

public class HabitacionConfig : MonoBehaviour
{
    [Header("Habitaciones Configuración (Siempre Cuadrada)")]
    [Tooltip("Largo y ancho de la habitación (siempre son iguales al ser cuadrada)")]
    public float tamanoLado = 6f;
    public float alto = 4.5f;

    // Campos sincronizados para compatibilidad con código o prefabs previos
    [HideInInspector] public float largo = 6f;
    [HideInInspector] public float ancho = 6f;

    [Header("Grosor Configuración")]
    [Min(0.01f)] public float grosorPared = 0.15f;
    [Min(0f)] public float grosorSuelo = 0.05f;
    [Min(0f)] public float grosorTecho = 0.05f;
    public float altoPared = 0f;

    [Header("Habitaciones Partes")]
    public Transform paredFrontal;
    public Transform paredTrasera;
    public Transform paredIzquierda;
    public Transform paredDerecha;
    public Transform techo;
    public Transform suelo;

    [Header("Control de Puertas (Hasta 4 - Una por lado)")]
    public bool tienePuertaFrontal = false;
    public bool tienePuertaTrasera = false;
    public bool tienePuertaIzquierda = false;
    public bool tienePuertaDerecha = false;

    [Header("Referencias de Puertas")]
    public PuertaConfig puertaFrontal;
    public PuertaConfig puertaTrasera;
    public PuertaConfig puertaIzquierda;
    public PuertaConfig puertaDerecha;

    [Tooltip("Prefab opcional para instanciar puertas automáticamente si no están asignadas")]
    public GameObject puertaPrefab;
    [Tooltip("Dimensiones por defecto de las puertas")]
    public Vector3 dimensionesPuerta = new Vector3(1.2f, 2.5f, 0.15f);

    [Header("Lista de Puertas Activas")]
    public List<PuertaConfig> puertas = new List<PuertaConfig>();

    private void OnValidate()
    {
        // Asegurar que la habitación sea cuadrada
        if (tamanoLado <= 0f)
        {
            tamanoLado = Mathf.Max(largo, ancho, 6f);
        }
        largo = tamanoLado;
        ancho = tamanoLado;

        ActualizarTodo();
    }

    private void Awake()
    {
        ActualizarTodo();
    }

    public void ActualizarTodo()
    {
        ActualizarSuelo();
        ActualizarTecho();
        altoPared = Mathf.Max(0f, alto - grosorSuelo - grosorTecho);
        ActualizarParedes();
        ActualizarPuertas();
    }

    public void ConfigurarDimensiones(float nuevoTamano, float nuevoAlto, float nuevoGrosorPared, float nuevoGrosorSuelo, float nuevoGrosorTecho)
    {
        tamanoLado = nuevoTamano;
        largo = tamanoLado;
        ancho = tamanoLado;
        alto = nuevoAlto;
        grosorPared = nuevoGrosorPared;
        grosorSuelo = nuevoGrosorSuelo;
        grosorTecho = nuevoGrosorTecho;
        ActualizarTodo();
    }

    public void ConfigurarPuertas(bool frontal, bool trasera, bool izquierda, bool derecha)
    {
        tienePuertaFrontal = frontal;
        tienePuertaTrasera = trasera;
        tienePuertaIzquierda = izquierda;
        tienePuertaDerecha = derecha;
        ActualizarPuertas();
    }

    private void ActualizarSuelo()
    {
        if (suelo == null)
            return;

        if (grosorSuelo <= 0f)
        {
            suelo.gameObject.SetActive(false);
            return;
        }

        suelo.gameObject.SetActive(true);
        suelo.localScale = new Vector3(tamanoLado, grosorSuelo, tamanoLado);
        suelo.localPosition = new Vector3(0f, grosorSuelo / 2f, 0f);
    }

    private void ActualizarTecho()
    {
        if (techo == null)
            return;

        if (grosorTecho <= 0f)
        {
            techo.gameObject.SetActive(false);
            return;
        }

        techo.gameObject.SetActive(true);
        techo.localScale = new Vector3(tamanoLado, grosorTecho, tamanoLado);
        techo.localPosition = new Vector3(0f, alto - grosorTecho / 2f, 0f);
    }

    private void ActualizarParedes()
    {
        if (altoPared <= 0f)
        {
            if (paredFrontal != null) paredFrontal.gameObject.SetActive(false);
            if (paredTrasera != null) paredTrasera.gameObject.SetActive(false);
            if (paredIzquierda != null) paredIzquierda.gameObject.SetActive(false);
            if (paredDerecha != null) paredDerecha.gameObject.SetActive(false);
            return;
        }

        float centroY = grosorSuelo + altoPared / 2f;
        float posMitad = tamanoLado / 2f - grosorPared / 2f;

        if (paredFrontal != null)
        {
            paredFrontal.gameObject.SetActive(true);
            paredFrontal.localPosition = new Vector3(0f, centroY, posMitad);
            paredFrontal.localScale = new Vector3(tamanoLado, altoPared, grosorPared);
        }

        if (paredTrasera != null)
        {
            paredTrasera.gameObject.SetActive(true);
            paredTrasera.localPosition = new Vector3(0f, centroY, -posMitad);
            paredTrasera.localScale = new Vector3(tamanoLado, altoPared, grosorPared);
        }

        if (paredIzquierda != null)
        {
            paredIzquierda.gameObject.SetActive(true);
            paredIzquierda.localPosition = new Vector3(-posMitad, centroY, 0f);
            paredIzquierda.localScale = new Vector3(grosorPared, altoPared, tamanoLado - grosorPared * 2f);
        }

        if (paredDerecha != null)
        {
            paredDerecha.gameObject.SetActive(true);
            paredDerecha.localPosition = new Vector3(posMitad, centroY, 0f);
            paredDerecha.localScale = new Vector3(grosorPared, altoPared, tamanoLado - grosorPared * 2f);
        }
    }

    private void ActualizarPuertas()
    {
        float posMitad = tamanoLado / 2f - grosorPared / 2f;

        // Frontal (+Z)
        ActualizarPuertaIndividual(ref puertaFrontal, tienePuertaFrontal, "Puerta_Frontal",
            new Vector3(0f, grosorSuelo, posMitad),
            Quaternion.Euler(0f, 0f, 0f));

        // Trasera (-Z)
        ActualizarPuertaIndividual(ref puertaTrasera, tienePuertaTrasera, "Puerta_Trasera",
            new Vector3(0f, grosorSuelo, -posMitad),
            Quaternion.Euler(0f, 180f, 0f));

        // Izquierda (-X)
        ActualizarPuertaIndividual(ref puertaIzquierda, tienePuertaIzquierda, "Puerta_Izquierda",
            new Vector3(-posMitad, grosorSuelo, 0f),
            Quaternion.Euler(0f, 270f, 0f));

        // Derecha (+X)
        ActualizarPuertaIndividual(ref puertaDerecha, tienePuertaDerecha, "Puerta_Derecha",
            new Vector3(posMitad, grosorSuelo, 0f),
            Quaternion.Euler(0f, 90f, 0f));

        // Mantener la lista sincronizada
        puertas.Clear();
        if (tienePuertaFrontal && puertaFrontal != null) puertas.Add(puertaFrontal);
        if (tienePuertaTrasera && puertaTrasera != null) puertas.Add(puertaTrasera);
        if (tienePuertaIzquierda && puertaIzquierda != null) puertas.Add(puertaIzquierda);
        if (tienePuertaDerecha && puertaDerecha != null) puertas.Add(puertaDerecha);
    }

    private void ActualizarPuertaIndividual(ref PuertaConfig puerta, bool activa, string nombreFallback, Vector3 localPos, Quaternion localRot)
    {
        if (activa)
        {
            if (puerta == null && puertaPrefab != null)
            {
                // Instanciar si no existe referencia pero hay prefab
                GameObject nuevaPuerta = Instantiate(puertaPrefab, transform);
                nuevaPuerta.name = nombreFallback;
                puerta = nuevaPuerta.GetComponent<PuertaConfig>();
            }

            if (puerta != null)
            {
                puerta.gameObject.SetActive(true);
                puerta.transform.localPosition = localPos;
                puerta.transform.localRotation = localRot;
                puerta.dimensiones = new Vector3(dimensionesPuerta.x, dimensionesPuerta.y, grosorPared);
                puerta.ActualizarVisuales();
            }
        }
        else
        {
            if (puerta != null)
            {
                puerta.gameObject.SetActive(false);
            }
        }
    }
}