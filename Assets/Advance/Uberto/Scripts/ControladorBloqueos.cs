using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>Decide una sola vez si cada bloqueo de BloqueoBase se activa.</summary>
public class ControladorBloqueos : MonoBehaviour
{
    [System.Serializable]
    private class EstadoBloqueo
    {
        public BloqueoBase bloqueo;
        public bool decisionTomada;
        public bool activado;
        [System.NonSerialized] public bool dentroDelRango;
    }

    [Header("Bloqueos de la escena")]
    [Tooltip("Asigna aquí las instancias en la escena de aglomeracion, bloqueo y fuego.")]
    [SerializeField] private List<EstadoBloqueo> bloqueos = new List<EstadoBloqueo>();

    [Header("Detección del XR Origin")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField, Min(0.1f)] private float radioDeteccion = 3f;

    [Header("Probabilidad")]
    [SerializeField, Range(0f, 1f)] private float probabilidadActivacion = 0.5f;

    private void Awake()
    {
        bloqueos.RemoveAll(estado => estado == null || estado.bloqueo == null);

        if (bloqueos.Count == 0)
        {
            foreach (BloqueoBase encontrado in FindObjectsByType<BloqueoBase>(FindObjectsSortMode.None))
                bloqueos.Add(new EstadoBloqueo { bloqueo = encontrado });
        }

        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();

        // BloqueoBase ya activa estos elementos por su cuenta al iniciar.
        foreach (EstadoBloqueo estado in bloqueos)
        {
            if (estado.bloqueo.activarAlIniciar)
            {
                estado.decisionTomada = true;
                estado.activado = true;
            }
        }
    }

    private void Update()
    {
        if (xrOrigin == null)
            return;

        Vector3 posicionJugador = xrOrigin.transform.position;
        float radioCuadrado = radioDeteccion * radioDeteccion;

        foreach (EstadoBloqueo estado in bloqueos)
        {
            if (estado == null || estado.bloqueo == null)
                continue;

            bool dentro = (posicionJugador - estado.bloqueo.transform.position).sqrMagnitude <= radioCuadrado;
            if (dentro && !estado.dentroDelRango)
                NotificarEntrada(estado.bloqueo);

            estado.dentroDelRango = dentro;
        }
    }

    /// <summary>Procesa una entrada al rango; también puede llamarse desde un detector de trigger.</summary>
    public void NotificarEntrada(BloqueoBase bloqueo)
    {
        EstadoBloqueo estado = BuscarEstado(bloqueo);
        if (estado == null || estado.decisionTomada)
            return;

        // Incluso si el azar da "no", la decisión queda guardada y no se vuelve a sortear.
        // Se permite decidir si hay al menos dos sin activar, para conservar uno como mínimo.
        if (ContarSinActivar() <= 1)
            return;

        estado.decisionTomada = true;
        if (Random.value < probabilidadActivacion)
        {
            estado.activado = true;
            bloqueo.Activar();
        }
    }

    private EstadoBloqueo BuscarEstado(BloqueoBase bloqueo)
    {
        if (bloqueo == null)
            return null;

        foreach (EstadoBloqueo estado in bloqueos)
        {
            if (estado != null && estado.bloqueo == bloqueo)
                return estado;
        }

        return null;
    }

    private int ContarSinActivar()
    {
        int cantidad = 0;
        foreach (EstadoBloqueo estado in bloqueos)
        {
            if (estado != null && estado.bloqueo != null && !estado.activado)
                cantidad++;
        }

        return cantidad;
    }

    private void OnValidate()
    {
        radioDeteccion = Mathf.Max(0.1f, radioDeteccion);
        probabilidadActivacion = Mathf.Clamp01(probabilidadActivacion);
    }
}
