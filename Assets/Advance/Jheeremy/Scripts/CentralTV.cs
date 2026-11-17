using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

// Controla TODAS las TVs del edificio con un solo Video Player (todas muestran lo mismo).
// 1) Programación normal en bucle mientras el jugador explora.
// 2) Cuando el jugador mira una TV unos segundos (o pasa el tiempo máximo): pre-sismo y noticia.
// 3) Al terminar la noticia avisa al ControladorInicio (las alarmas empiezan a parpadear).
[RequireComponent(typeof(VideoPlayer))]
public class CentralTV : MonoBehaviour
{
    public static event Action AlEmpezarNoticia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { AlEmpezarNoticia = null; }

    [Header("Videos (el audio va aparte)")]
    public VideoClip videoPrograma;
    public VideoClip videoNoticia;

    [Header("Audios (suenan en todas las TVs a la vez)")]
    public AudioClip audioPrograma;
    public AudioClip audioNoticia;

    [Header("Cuándo empieza la noticia")]
    [Tooltip("Segundos mínimos explorando antes de que mirar una TV dispare la noticia.")]
    public float tiempoMinimoExplorando = 20f;
    [Tooltip("Segundos seguidos mirando una TV para que empiece.")]
    public float segundosMirando = 3f;
    [Tooltip("Si no mira ninguna TV, empieza sola después de estos segundos.")]
    public float tiempoMaximoExplorando = 120f;
    [Tooltip("Ángulo máximo entre la mirada y la TV para contar como 'mirando'.")]
    public float anguloMirada = 15f;
    public float distanciaMaximaMirada = 12f;

    [Header("Pre-sismo (opcional)")]
    public EfectoPreSismo preSismo;
    [Tooltip("Segundos de temblor leve antes de que corte la transmisión.")]
    public float duracionPreSismo = 4f;

    VideoPlayer video;
    PantallaTV[] pantallas;
    float inicio;
    float tiempoMirando;
    bool noticiaIniciada;

    void Awake()
    {
        video = GetComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.audioOutputMode = VideoAudioOutputMode.None;   // el audio lo ponen las PantallaTV
        video.loopPointReached += AlTerminarVideo;
    }

    IEnumerator Start()
    {
        pantallas = FindObjectsByType<PantallaTV>(FindObjectsSortMode.None);
        inicio = Time.time;
        yield return Reproducir(videoPrograma, audioPrograma, true);
    }

    void Update()
    {
        if (noticiaIniciada) return;

        // Tecla N para probar sin lentes
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) { EmpezarNoticia(); return; }

        float explorando = Time.time - inicio;
        if (explorando >= tiempoMaximoExplorando) { EmpezarNoticia(); return; }
        if (explorando < tiempoMinimoExplorando) return;

        if (JugadorMirandoUnaTV()) tiempoMirando += Time.deltaTime;
        else tiempoMirando = 0f;

        if (tiempoMirando >= segundosMirando) EmpezarNoticia();
    }

    bool JugadorMirandoUnaTV()
    {
        Camera cam = Camera.main;
        if (cam == null || pantallas == null) return false;
        foreach (var p in pantallas)
        {
            Vector3 hacia = p.transform.position - cam.transform.position;
            if (hacia.magnitude > distanciaMaximaMirada) continue;
            if (Vector3.Angle(cam.transform.forward, hacia) > anguloMirada) continue;
            // Que no haya una pared en medio
            if (Physics.Linecast(cam.transform.position, p.transform.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(p.transform)) continue;
            return true;
        }
        return false;
    }

    public void EmpezarNoticia()
    {
        if (noticiaIniciada) return;
        noticiaIniciada = true;
        StartCoroutine(SecuenciaNoticia());
    }

    IEnumerator SecuenciaNoticia()
    {
        if (preSismo != null)
        {
            preSismo.Iniciar(duracionPreSismo);
            yield return new WaitForSeconds(duracionPreSismo);
        }
        AlEmpezarNoticia?.Invoke();
        if (ControladorInicio.Instancia != null) ControladorInicio.Instancia.TelevisorEncendido();
        yield return Reproducir(videoNoticia, audioNoticia, false);
    }

    // Prepara el video y arranca video + audio de todas las TVs al mismo tiempo
    IEnumerator Reproducir(VideoClip clip, AudioClip audio, bool enBucle)
    {
        video.Stop();
        video.clip = clip;
        video.isLooping = enBucle;
        if (clip != null)
        {
            video.Prepare();
            while (!video.isPrepared) yield return null;
        }

        double cuando = AudioSettings.dspTime + 0.2;
        foreach (var p in pantallas) p.Reproducir(audio, enBucle, cuando);
        while (AudioSettings.dspTime < cuando) yield return null;
        if (clip != null) video.Play();
    }

    void AlTerminarVideo(VideoPlayer vp)
    {
        if (vp.clip == videoNoticia && ControladorInicio.Instancia != null)
            ControladorInicio.Instancia.NoticiaTerminada();
    }
}