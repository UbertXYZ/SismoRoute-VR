using UnityEngine;

// Va en cada televisor. Hace sonar el audio desde esa TV (3D)
// y sirve para detectar si el jugador la está mirando.
[RequireComponent(typeof(AudioSource))]
public class PantallaTV : MonoBehaviour
{
    AudioSource fuente;

    void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
    }

    // Todas las TVs llaman esto con el mismo 'cuando' para sonar sincronizadas
    public void Reproducir(AudioClip clip, bool enBucle, double cuando)
    {
        fuente.Stop();
        if (clip == null) return;
        fuente.clip = clip;
        fuente.loop = enBucle;
        fuente.PlayScheduled(cuando);
    }
}