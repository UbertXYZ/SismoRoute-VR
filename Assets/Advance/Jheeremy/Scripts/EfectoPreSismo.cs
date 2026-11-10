using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Temblor leve antes de la noticia: tiemblan los ObjetoTembloroso,
// suena un retumbe, vibran los controles y suena la alerta del celular.
public class EfectoPreSismo : MonoBehaviour
{
    [Tooltip("Ruido sordo de temblor (Audio Source 2D).")]
    public AudioSource sonidoRetumbe;
    [Tooltip("Alerta tipo SISMATE (Audio Source en el celular).")]
    public AudioSource alertaCelular;
    [Range(0f, 1f)] public float fuerzaVibracion = 0.4f;

    public void Iniciar(float duracion)
    {
        StartCoroutine(Temblar(duracion));
    }

    IEnumerator Temblar(float duracion)
    {
        if (sonidoRetumbe != null) sonidoRetumbe.Play();
        foreach (var o in FindObjectsByType<ObjetoTembloroso>(FindObjectsSortMode.None))
            o.Temblar(duracion);
        Vibrar(fuerzaVibracion, duracion);

        yield return new WaitForSeconds(duracion * 0.6f);
        if (alertaCelular != null) alertaCelular.Play();
    }

    // Vibración de los controles de VR
    static void Vibrar(float fuerza, float duracion)
    {
        var mandos = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, mandos);
        foreach (var m in mandos)
            if (m.TryGetHapticCapabilities(out HapticCapabilities cap) && cap.supportsImpulse)
                m.SendHapticImpulse(0, fuerza, duracion);
    }
}