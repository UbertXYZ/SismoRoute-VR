using System.Collections;
using UnityEngine;

// Fuego alto: explosión repentina y luego un muro de llamas con humo
// que impide el paso.
public class FuegoAlto : BloqueoBase
{
    [Header("Partículas")]
    public ParticleSystem explosion;
    public ParticleSystem llamas;
    public ParticleSystem humo;

    [Header("Luz")]
    public Light luz;
    public float intensidadDestello = 25f;
    public float intensidadFuego = 4f;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip sonidoExplosion;
    public AudioClip sonidoFuego; // en bucle

    bool parpadear = false;

    protected override void Start()
    {
        // Todo apagado al inicio
        if (luz != null)
        {
            luz.enabled = false;
            luz.intensity = 0f;
        }
        base.Start();
    }

    protected override IEnumerator Ejecutar()
    {
        // 1) Explosión + destello fuerte
        if (explosion != null) explosion.Play();
        if (audioSource != null && sonidoExplosion != null)
            audioSource.PlayOneShot(sonidoExplosion);
        BloquearRuta();

        if (luz != null)
        {
            luz.enabled = true;
            float t = 0f, duracion = 0.35f;
            while (t < duracion)
            {
                t += Time.deltaTime;
                luz.intensity = Mathf.Lerp(intensidadDestello, intensidadFuego, t / duracion);
                yield return null;
            }
        }

        // 2) Llamas y humo permanentes
        if (llamas != null) llamas.Play();
        if (humo != null) humo.Play();

        if (audioSource != null && sonidoFuego != null)
        {
            audioSource.clip = sonidoFuego;
            audioSource.loop = true;
            audioSource.Play();
        }

        parpadear = true;
    }

    void Update()
    {
        if (!parpadear || luz == null) return;

        // Parpadeo natural del fuego
        float ruido = Mathf.PerlinNoise(0f, Time.time * 6f);
        luz.intensity = intensidadFuego * Mathf.Lerp(0.6f, 1.25f, ruido);
    }
}