using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bloqueo repentino: cae polvo del techo como aviso, luego caen rocas
// y un collider invisible impide el paso.
public class BloqueoRepentino : BloqueoBase
{
    [Header("Rocas")]
    [Tooltip("Objeto padre que contiene todas las rocas (con Rigidbody kinematic).")]
    public GameObject rocas;

    [Tooltip("Segundos que las rocas tienen física antes de congelarse (ahorra rendimiento).")]
    public float tiempoAsentamiento = 3f;

    [Header("Aviso")]
    public ParticleSystem polvoAviso;
    [Tooltip("Segundos de polvo cayendo antes del derrumbe.")]
    public float tiempoAviso = 1f;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip sonidoCrujido;
    public AudioClip sonidoDerrumbe;

    readonly List<Rigidbody> cuerposRocas = new List<Rigidbody>();

    protected override void Start()
    {
        // Guardar las rocas y esconderlas hasta que se active
        cuerposRocas.AddRange(rocas.GetComponentsInChildren<Rigidbody>(true));
        foreach (Rigidbody rb in cuerposRocas)
            rb.isKinematic = true;

        rocas.SetActive(false);

        base.Start();
    }

    protected override IEnumerator Ejecutar()
    {
        // 1) Aviso: crujido + polvo cayendo
        Sonar(sonidoCrujido);
        if (polvoAviso != null) polvoAviso.Play();
        yield return new WaitForSeconds(tiempoAviso);

        // 2) Derrumbe: las rocas aparecen y caen con física
        Sonar(sonidoDerrumbe);
        rocas.SetActive(true);
        foreach (Rigidbody rb in cuerposRocas)
        {
            rb.isKinematic = false;
            rb.angularVelocity = Random.insideUnitSphere * 3f; // que giren al caer
        }

        // 3) Bloquear el paso cuando ya cayeron
        yield return new WaitForSeconds(0.6f);
        BloquearRuta();

        // Las rocas no deben chocar con la pared invisible
        foreach (Rigidbody rb in cuerposRocas)
        {
            Collider col = rb.GetComponent<Collider>();
            if (col != null && colliderBloqueo != null)
                Physics.IgnoreCollision(col, colliderBloqueo, true);
        }

        if (polvoAviso != null) polvoAviso.Stop();

        // 4) Congelar las rocas cuando se asientan
        yield return new WaitForSeconds(tiempoAsentamiento);
        foreach (Rigidbody rb in cuerposRocas)
            rb.isKinematic = true;
    }

    void Sonar(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}