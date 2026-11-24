using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

// Bloquea una escalera después de detectar un NPC o al jugador y reproducir
// las animaciones de derrumbe importadas desde EscaleraAnim.fbx.
public class BloqueoEscalera : BloqueoBase
{
    private bool decisionTomada;
    private bool activacionPermitidaPorSismo;

    [Header("Animación de derrumbe")]
    [Tooltip("Clips de EscaleraAnim.fbx, uno por cada parte animada.")]
    [SerializeField] private AnimationClip[] animacionesBloqueo;
    [Tooltip("Raíz del modelo EscaleraAnim. Si queda vacío, se busca automáticamente.")]
    [SerializeField] private Transform raizAnimada;

    [Header("Sonido (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoDerrumbe;

    protected override void Start()
    {
        if (raizAnimada == null)
        {
            Transform raizPrefab = transform.parent;
            raizAnimada = raizPrefab != null ? raizPrefab.Find("EscaleraAnim") : null;
            if (raizAnimada == null)
                raizAnimada = transform.parent;
        }

        // La clase base inicia la secuencia desde Start si esta opción está activa.
        // Marca también la decisión para que el detector no intente activarla de nuevo.
        if (activarAlIniciar)
            decisionTomada = true;

        base.Start();
    }

    /// <summary>Activa el bloqueo una sola vez al entrar un actor válido en ColliderDeteccion.</summary>
    public void IntentarActivar(Collider actor)
    {
        // El controlador solo abre esta puerta lógica al comenzar el sismo.
        // El derrumbe se inicia cuando el actor entra en ColliderDeteccion.
        if (!enabled || !gameObject.activeInHierarchy || !activacionPermitidaPorSismo
            || decisionTomada || actor == null)
            return;

        bool esJugador = actor.GetComponentInParent<XROrigin>() != null;
        if (!esJugador)
            return;

        decisionTomada = true;
        activacionPermitidaPorSismo = false;
        Activar();
    }

    /// <summary>El controlador habilita la activación por contacto cuando inicia el sismo.</summary>
    public void PermitirActivacionPorSismo()
    {
        if (!decisionTomada)
            activacionPermitidaPorSismo = true;
    }

    /// <summary>Revoca el permiso antes del sismo o para las escaleras no seleccionadas.</summary>
    public void RevocarActivacionPorSismo()
    {
        activacionPermitidaPorSismo = false;
    }

    protected override IEnumerator Ejecutar()
    {
        Sonar(sonidoDerrumbe);
        BloquearRuta();
        yield return StartCoroutine(ReproducirAnimaciones());
    }

    private IEnumerator ReproducirAnimaciones()
    {
        if (raizAnimada == null || animacionesBloqueo == null || animacionesBloqueo.Length == 0)
            yield break;

        float duracion = 0f;
        foreach (AnimationClip clip in animacionesBloqueo)
        {
            if (clip != null)
                duracion = Mathf.Max(duracion, clip.length);
        }

        if (duracion <= 0f)
            yield break;

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            AplicarAnimaciones(tiempo);
            tiempo += Time.deltaTime;
            yield return null;
        }

        AplicarAnimaciones(duracion);
    }

    private void AplicarAnimaciones(float tiempo)
    {
        foreach (AnimationClip clip in animacionesBloqueo)
        {
            if (clip != null)
                clip.SampleAnimation(raizAnimada.gameObject, Mathf.Min(tiempo, clip.length));
        }
    }

    private void Sonar(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
