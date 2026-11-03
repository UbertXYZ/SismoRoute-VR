using System.Collections;
using UnityEngine;
using TMPro;

// Controla el inicio de la simulación:
// 1) Esperando: el jugador recorre el edificio libremente, nada pasa.
// 2) Noticia: encendió el televisor y se está viendo la noticia.
// 3) CuentaRegresiva: terminó la noticia; tiene 'tiempoLimite' segundos para activar una alarma.
// 4) Iniciado: alarma activada (por el jugador o sola) -> los NPC reaccionan.
public class ControladorInicio : MonoBehaviour
{
    public static ControladorInicio Instancia { get; private set; }

    public enum Estado { EsperandoTV, Noticia, CuentaRegresiva, Iniciado }
    public Estado EstadoActual { get; private set; } = Estado.EsperandoTV;

    [Tooltip("Segundos después de la noticia para que la alarma se active sola.")]
    public float tiempoLimite = 60f;
    [Tooltip("Permitir activar la alarma mientras todavía se ve la noticia.")]
    public bool permitirAlarmaDuranteNoticia = true;

    [Header("Opcionales")]
    [Tooltip("Texto 3D (TextMeshPro) para mostrar la cuenta regresiva, por ejemplo debajo del televisor.")]
    public TMP_Text textoCuenta;
    [Tooltip("Sonido de alarma (marcar Loop en el AudioSource).")]
    public AudioSource sonidoAlarma;

    [Header("Resultados (solo lectura)")]
    public bool alarmaActivadaPorJugador;
    [Tooltip("Segundos desde que terminó la noticia hasta que el jugador activó la alarma.")]
    public float tiempoReaccion;

    float finNoticia;

    void Awake()
    {
        Instancia = this;
        if (textoCuenta != null) textoCuenta.text = "";
    }

    // Lo llama el televisor al encenderse
    public void TelevisorEncendido()
    {
        if (EstadoActual == Estado.EsperandoTV) EstadoActual = Estado.Noticia;
    }

    // Lo llama el televisor cuando el video termina
    public void NoticiaTerminada()
    {
        if (EstadoActual != Estado.Noticia) return;
        EstadoActual = Estado.CuentaRegresiva;
        finNoticia = Time.time;
        StartCoroutine(CuentaRegresiva());
    }

    IEnumerator CuentaRegresiva()
    {
        float restante = tiempoLimite;
        while (restante > 0f && EstadoActual == Estado.CuentaRegresiva)
        {
            if (textoCuenta != null)
                textoCuenta.text = $"¡Active la alarma más cercana!\nSe activará sola en {Mathf.CeilToInt(restante)} s";
            restante -= Time.deltaTime;
            yield return null;
        }
        if (EstadoActual == Estado.CuentaRegresiva) Iniciar(false);
    }

    // Llamar desde cada botón/palanca de alarma (XR Simple Interactable → Select Entered)
    public void ActivarAlarma()
    {
        bool puede = EstadoActual == Estado.CuentaRegresiva
                  || (EstadoActual == Estado.Noticia && permitirAlarmaDuranteNoticia);
        if (puede) Iniciar(true);
        else if (EstadoActual == Estado.EsperandoTV) Debug.Log("Todavía no pasa nada: primero hay que encender el televisor.");
    }

    void Iniciar(bool porJugador)
    {
        EstadoActual = Estado.Iniciado;
        alarmaActivadaPorJugador = porJugador;
        tiempoReaccion = (finNoticia > 0f) ? Time.time - finNoticia : 0f;

        if (textoCuenta != null)
            textoCuenta.text = porJugador ? "¡Alarma activada!\nEvacúe el edificio" : "La alarma se activó sola\nEvacúe el edificio";
        if (sonidoAlarma != null) sonidoAlarma.Play();

        Debug.Log(porJugador
            ? $"Alarma activada por el jugador ({tiempoReaccion:0.0} s después de la noticia)"
            : "Alarma activada automáticamente (se acabó el tiempo)");

        EventosSismo.Disparar(transform.position);
    }
}