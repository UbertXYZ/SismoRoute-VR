using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hace que el fuego de Jheeremy (FuegoAlto) se pueda APAGAR con el extintor.
// No modifica el script de Jheeremy: solo usa sus campos publicos (llamas, humo, luz,
// colliderBloqueo, obstaculoNavMesh, audioSource, sonidoExplosion, sonidoFuego).
//
// Como se usa: agregalo al MISMO objeto que tiene FuegoAlto (el prefab fuego.prefab).
// El GeneradorBloqueosAleatorios lo agrega solo a los fuegos que crea.
//
// Al apagarse el fuego se libera el paso: se desactivan el collider que bloquea al jugador
// y el NavMeshObstacle (los NPC vuelven a poder pasar).
[RequireComponent(typeof(FuegoAlto))]
public class FuegoApagable : MonoBehaviour
{
    public static readonly List<FuegoApagable> Todos = new List<FuegoApagable>();
    public static event Action<FuegoApagable> Apagado;

    [Header("Apagado")]
    [Tooltip("Segundos de rociado continuo (con el extintor apuntando) para apagarlo del todo.")]
    public float segundosParaApagar = 3f;
    [Tooltip("Cuanto se reaviva por segundo si dejas de rociar (0 = no se reaviva).")]
    public float reavivar = 0.25f;
    [Tooltip("Radio (m) del fuego, para saber si el chorro le esta dando.")]
    public float radio = 1.8f;
    [Tooltip("Segundos que sigue saliendo humo despues de apagarse.")]
    public float humoResidual = 4f;

    [Header("Audio (se completa solo si Jheeremy dejo los clips vacios)")]
    public AudioClip sonidoApagado;

    FuegoAlto _fuego;
    float _vida = 1f;           // 1 = encendido a tope, 0 = apagado
    float _tasaLlamas, _intensidadLuz;
    bool _apagado;

    public bool EstaApagado => _apagado;
    public float Vida => _vida;

    // Solo se puede apagar cuando YA se encendio (las llamas estan reproduciendose).
    public bool Encendido => !_apagado && _fuego != null && _fuego.llamas != null && _fuego.llamas.isPlaying;

    // Centro del fuego (a la altura del pecho) para apuntar.
    public Vector3 Centro
    {
        get
        {
            if (_fuego != null && _fuego.colliderBloqueo != null)
                return _fuego.colliderBloqueo.transform.TransformPoint(_fuego.colliderBloqueo.center);
            return transform.position + Vector3.up * 1f;
        }
    }

    void Awake()
    {
        _fuego = GetComponent<FuegoAlto>();
        if (_fuego.llamas != null) _tasaLlamas = _fuego.llamas.emission.rateOverTimeMultiplier;
        _intensidadLuz = _fuego.intensidadFuego;

        // Jheeremy dejo vacios los slots de audio del fuego; se rellenan aqui para que suene
        // (FuegoAlto los reproduce en su propia secuencia: explosion y fuego en bucle).
        if (_fuego.audioSource == null)
        {
            _fuego.audioSource = gameObject.AddComponent<AudioSource>();
            _fuego.audioSource.spatialBlend = 1f;
            _fuego.audioSource.maxDistance = 25f;
            _fuego.audioSource.rolloffMode = AudioRolloffMode.Linear;
            _fuego.audioSource.playOnAwake = false;
        }
        if (_fuego.sonidoExplosion == null) _fuego.sonidoExplosion = AudioProceduralJosue.Explosion();
        if (_fuego.sonidoFuego == null) _fuego.sonidoFuego = AudioProceduralJosue.Crepitar();
    }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Update()
    {
        if (_apagado || reavivar <= 0f || _vida >= 1f || !Encendido) return;
        _vida = Mathf.Min(1f, _vida + reavivar * Time.deltaTime);
        Escalar();
    }

    // El extintor llama a esto cada frame que el chorro le da al fuego.
    public void Rociar(float dt)
    {
        if (!Encendido) return;
        _vida -= dt / Mathf.Max(0.1f, segundosParaApagar);
        if (_vida <= 0f) { Apagar(); return; }
        Escalar();
    }

    // Las llamas y la luz se hacen mas chicas a medida que se apaga.
    void Escalar()
    {
        if (_fuego.llamas != null)
        {
            var em = _fuego.llamas.emission;
            em.rateOverTimeMultiplier = _tasaLlamas * Mathf.Clamp01(_vida);
            _fuego.llamas.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, _vida);
        }
        _fuego.intensidadFuego = _intensidadLuz * Mathf.Clamp01(_vida); // FuegoAlto la usa en su parpadeo
        if (_fuego.audioSource != null) _fuego.audioSource.volume = Mathf.Lerp(0.3f, 1f, _vida);
    }

    [ContextMenu("Apagar ahora")]
    public void Apagar()
    {
        if (_apagado) return;
        _apagado = true;
        _vida = 0f;

        if (_fuego.llamas != null) _fuego.llamas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (_fuego.explosion != null) _fuego.explosion.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (_fuego.luz != null) { _fuego.luz.enabled = false; _fuego.luz.intensity = 0f; }

        // Se libera el paso (jugador y NPC).
        if (_fuego.colliderBloqueo != null) _fuego.colliderBloqueo.enabled = false;
        if (_fuego.obstaculoNavMesh != null) _fuego.obstaculoNavMesh.enabled = false;

        if (_fuego.audioSource != null)
        {
            _fuego.audioSource.Stop();
            _fuego.audioSource.loop = false;
            _fuego.audioSource.volume = 1f;
            if (sonidoApagado != null) _fuego.audioSource.PlayOneShot(sonidoApagado);
        }

        StartCoroutine(ApagarHumo());
        Apagado?.Invoke(this);
    }

    IEnumerator ApagarHumo()
    {
        yield return new WaitForSeconds(humoResidual);
        if (_fuego.humo != null) _fuego.humo.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
