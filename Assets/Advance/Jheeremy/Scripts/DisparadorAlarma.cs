using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Canal de eventos: cualquier NPC se suscribe aqui para reaccionar.
public static class EventosSismo
{
    public static event Action<Vector3> Alarma;
    public static void Disparar(Vector3 origen) => Alarma?.Invoke(origen);
}

// Pon este componente en un objeto con un Box Collider (Is Trigger marcado).
// Dispara la alarma cuando el jugador entra, o con la tecla P para probar.
public class DisparadorAlarma : MonoBehaviour
{
    public bool dispararAlEntrarElJugador = true;
    public string tagJugador = "Player";
    bool yaDisparado;

    void OnTriggerEnter(Collider other)
    {
        if (dispararAlEntrarElJugador && !yaDisparado && other.CompareTag(tagJugador))
            Disparar();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            Disparar();
    }

    [ContextMenu("Disparar ahora")]
    public void Disparar()
    {
        yaDisparado = true;
        EventosSismo.Disparar(transform.position);
    }
}
