using System.Collections;
using UnityEngine;

// Botón de alarma de pared: al tocarlo se hunde, cambia de color y activa la alarma.
// Si todavía no se encendió el televisor, se hunde y vuelve a salir (no pasa nada).
public class BotonAlarma : MonoBehaviour
{
    [Tooltip("Cuánto se hunde el botón (metros).")]
    public float profundidad = 0.04f;
    [Tooltip("Duración de la animación de hundirse (segundos).")]
    public float duracion = 0.12f;
    public Color colorPresionado = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Opcional: sonido de 'clic' al presionar.")]
    public AudioSource sonidoClic;

    Vector3 posicionInicial;
    Renderer rend;
    bool presionado;
    bool animando;

    void Awake()
    {
        posicionInicial = transform.position;
        rend = GetComponent<Renderer>();
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