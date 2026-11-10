using System.Collections;
using UnityEngine;

// Botón de alarma de pared.
// - Cuando ya se puede activar (terminó la noticia): parpadea en rojo y pita.
// - Al tocarlo: se hunde, cambia de color y activa la alarma.
// - Si todavía no se puede: se hunde y vuelve a salir (no pasa nada).
public class BotonAlarma : MonoBehaviour
{
    [Tooltip("Cuánto se hunde el botón (metros).")]
    public float profundidad = 0.04f;
    [Tooltip("Duración de la animación de hundirse (segundos).")]
    public float duracion = 0.12f;
    public Color colorPresionado = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Opcional: sonido de 'clic' al presionar.")]
    public AudioSource sonidoClic;

    [Header("Llamar la atención (cuando ya se puede activar)")]
    public Color colorParpadeo = new Color(1f, 0.45f, 0.45f);
    public float velocidadParpadeo = 2f;
    [Tooltip("Opcional: una Point Light roja que parpadea junto al botón.")]
    public Light luz;
    [Tooltip("Opcional: pitido suave en bucle mientras parpadea.")]
    public AudioSource pitido;

    Vector3 posicionInicial;
    Renderer rend;
    Color colorInicial;
    bool presionado;
    bool animando;
    bool parpadeando;

    void Awake()
    {
        posicionInicial = transform.position;
        rend = GetComponent<Renderer>();
        if (rend != null) colorInicial = rend.material.color;
        if (luz != null) luz.enabled = false;
    }

    void Update()
    {
        var ctrl = ControladorInicio.Instancia;
        bool llamar = !presionado && ctrl != null && ctrl.EstadoActual == ControladorInicio.Estado.CuentaRegresiva;

        if (llamar)
        {
            bool prendido = Mathf.Repeat(Time.time * velocidadParpadeo, 1f) < 0.5f;
            if (rend != null) rend.material.color = prendido ? colorParpadeo : colorInicial;
            if (luz != null) luz.enabled = prendido;
            if (pitido != null && !pitido.isPlaying) pitido.Play();
        }
        else if (parpadeando)
        {
            if (rend != null && !presionado) rend.material.color = colorInicial;
            if (luz != null) luz.enabled = false;
            if (pitido != null) pitido.Stop();
        }
        parpadeando = llamar;
    }

    // Llamar desde XR Simple Interactable → Select Entered
    public void Presionar()
    {
        if (presionado || animando) return;
        if (sonidoClic != null) sonidoClic.Play();

        var ctrl = ControladorInicio.Instancia;
        bool puedeActivar = ctrl != null &&
            (ctrl.EstadoActual == ControladorInicio.Estado.CuentaRegresiva ||
            (ctrl.EstadoActual == ControladorInicio.Estado.Noticia && ctrl.permitirAlarmaDuranteNoticia));

        if (puedeActivar)
        {
            presionado = true;
            ctrl.ActivarAlarma();
            StartCoroutine(Mover(posicionInicial + transform.forward * profundidad));
            if (rend != null) rend.material.color = colorPresionado;
        }
        else
        {
            StartCoroutine(HundirYVolver());   // aún no se puede: solo da el "clic"
        }
    }

    IEnumerator HundirYVolver()
    {
        yield return Mover(posicionInicial + transform.forward * profundidad);
        yield return new WaitForSeconds(0.1f);
        yield return Mover(posicionInicial);
    }

    IEnumerator Mover(Vector3 destino)
    {
        animando = true;
        Vector3 desde = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duracion)
        {
            transform.position = Vector3.Lerp(desde, destino, t);
            yield return null;
        }
        transform.position = destino;
        animando = false;
    }
}