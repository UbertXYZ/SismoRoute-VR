using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

// Televisor de pared: al tocarlo (XR Simple Interactable → Select Entered → Encender)
// reproduce la noticia. Cuando termina, avisa al ControladorInicio.
// Tecla N para probar sin lentes.
[RequireComponent(typeof(VideoPlayer))]
public class TelevisorNoticia : MonoBehaviour
{
    VideoPlayer video;
    bool encendido;

    void Awake()
    {
        video = GetComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = false;
        video.loopPointReached += AlTerminar;

        // Pantalla negra mientras está apagado
        if (video.targetTexture != null)
        {
            var anterior = RenderTexture.active;
            RenderTexture.active = video.targetTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = anterior;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            Encender();
    }

    public void Encender()
    {
        if (encendido) return;
        encendido = true;
        video.Play();
        if (ControladorInicio.Instancia != null) ControladorInicio.Instancia.TelevisorEncendido();
    }

    void AlTerminar(VideoPlayer vp)
    {
        if (ControladorInicio.Instancia != null) ControladorInicio.Instancia.NoticiaTerminada();
    }
}