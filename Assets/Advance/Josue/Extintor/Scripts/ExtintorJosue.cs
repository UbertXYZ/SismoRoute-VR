using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Extintor agarrable (XR Grab Interactable) que apaga los FuegoApagable.
//
// En VR:      agarralo con el boton de agarre y rocia con el GATILLO (Activate).
// En el Editor sin visor (XR Device Simulator): agarralo con el mouse/teclado del simulador y
//             rocia con su boton de "activar", o mantén la tecla F mientras lo sostienes.
//
// Apunta la BOQUILLA (eje azul, Z) al fuego: si esta dentro del alcance y del cono, el fuego
// se va apagando. El chorro gasta carga; al llegar a 0 ya no sale nada.
[RequireComponent(typeof(Rigidbody))]
public class ExtintorJosue : MonoBehaviour
{
    [Header("Partes (las asigna el generador)")]
    public Transform boquilla;
    public ParticleSystem chorro;
    [Tooltip("Aguja/barra del medidor: se encoge al gastarse la carga (opcional).")]
    public Transform medidor;

    [Header("Rociado")]
    [Tooltip("Distancia maxima (m) a la que llega el chorro.")]
    public float alcance = 3.5f;
    [Tooltip("Apertura (grados, a cada lado) del cono del chorro.")]
    public float anguloCono = 28f;
    [Tooltip("Segundos de chorro continuo que dura la carga.")]
    public float segundosDeCarga = 25f;
    [Tooltip("Permite rociar con la tecla F sin visor VR (solo mientras esta agarrado).")]
    public bool teclaDePrueba = true;
    [Tooltip("Para probar rapido: rociar con F aunque no este agarrado.")]
    public bool rociarSinAgarrar = false;

    [Header("Audio")]
    [Tooltip("Clic al agarrar/soltar. El generador pone el click de Jheeremy (Jheeremy/Video).")]
    public AudioClip sonidoClic;
    [Tooltip("Chorro en bucle. Vacio = siseo generado por codigo.")]
    public AudioClip sonidoChorro;
    public AudioClip sonidoVacio;

    public float Carga01 => Mathf.Clamp01(_carga / Mathf.Max(0.1f, segundosDeCarga));
    public bool Rociando => _rociando;

    XRGrabInteractable _agarre;
    AudioSource _audioChorro, _audioEfectos;
    float _carga;
    bool _gatillo, _rociando;
    Vector3 _escalaMedidor = Vector3.one;

    void Awake()
    {
        _carga = segundosDeCarga;
        _agarre = GetComponent<XRGrabInteractable>();
        if (boquilla == null) boquilla = transform;
        if (medidor != null) _escalaMedidor = medidor.localScale;

        _audioChorro = CrearAudio(true);
        _audioChorro.clip = sonidoChorro != null ? sonidoChorro : AudioProceduralJosue.Chorro();
        _audioChorro.volume = 0.7f;
        _audioEfectos = CrearAudio(false);

        PararChorro();
    }

    AudioSource CrearAudio(bool bucle)
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = bucle;
        a.spatialBlend = 1f;
        a.minDistance = 1f;
        a.maxDistance = 15f;
        a.rolloffMode = AudioRolloffMode.Linear;
        return a;
    }

    void OnEnable()
    {
        if (_agarre == null) return;
        _agarre.activated.AddListener(AlActivar);
        _agarre.deactivated.AddListener(AlDesactivar);
        _agarre.selectEntered.AddListener(AlAgarrar);
        _agarre.selectExited.AddListener(AlSoltar);
    }

    void OnDisable()
    {
        if (_agarre != null)
        {
            _agarre.activated.RemoveListener(AlActivar);
            _agarre.deactivated.RemoveListener(AlDesactivar);
            _agarre.selectEntered.RemoveListener(AlAgarrar);
            _agarre.selectExited.RemoveListener(AlSoltar);
        }
        PararChorro();
    }

    // Gatillo (VR / simulador)
    void AlActivar(UnityEngine.XR.Interaction.Toolkit.ActivateEventArgs _) { _gatillo = true; }
    void AlDesactivar(UnityEngine.XR.Interaction.Toolkit.DeactivateEventArgs _) { _gatillo = false; }
    void AlAgarrar(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _) { Clic(); }
    void AlSoltar(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _) { _gatillo = false; Clic(); }

    void Clic()
    {
        if (sonidoClic != null) _audioEfectos.PlayOneShot(sonidoClic);
    }

    void Update()
    {
        bool agarrado = _agarre != null && _agarre.isSelected;
        bool tecla = teclaDePrueba && Keyboard.current != null && Keyboard.current.fKey.isPressed
                     && (agarrado || rociarSinAgarrar);
        bool quiere = (agarrado && _gatillo) || tecla;

        if (quiere && _carga <= 0f)
        {
            if (!_rociando && sonidoVacio != null && !_audioEfectos.isPlaying) _audioEfectos.PlayOneShot(sonidoVacio);
            quiere = false;
        }

        if (quiere) { if (!_rociando) EmpezarChorro(); Rociar(Time.deltaTime); }
        else if (_rociando) PararChorro();

        if (medidor != null)
        {
            var e = _escalaMedidor;
            e.y = Mathf.Max(0.02f, _escalaMedidor.y * Carga01);
            medidor.localScale = e;
        }
    }

    void Rociar(float dt)
    {
        _carga -= dt;

        Vector3 origen = boquilla.position;
        Vector3 dir = boquilla.forward;
        var fuegos = FuegoApagable.Todos;
        for (int i = 0; i < fuegos.Count; i++)
        {
            var f = fuegos[i];
            if (f == null || !f.Encendido) continue;

            Vector3 haciaFuego = f.Centro - origen;
            float dist = haciaFuego.magnitude;
            if (dist > alcance + f.radio) continue;

            // Si el fuego es grande y estas pegado a el, siempre le das; si no, tiene que estar en el cono.
            bool pegado = dist <= f.radio;
            bool enCono = Vector3.Angle(dir, haciaFuego) <= anguloCono + Mathf.Atan2(f.radio, Mathf.Max(0.1f, dist)) * Mathf.Rad2Deg;
            if (pegado || enCono) f.Rociar(dt);
        }
    }

    void EmpezarChorro()
    {
        _rociando = true;
        if (chorro != null) { var em = chorro.emission; em.enabled = true; if (!chorro.isPlaying) chorro.Play(); }
        if (_audioChorro != null && !_audioChorro.isPlaying) _audioChorro.Play();
    }

    void PararChorro()
    {
        _rociando = false;
        if (chorro != null) { var em = chorro.emission; em.enabled = false; }
        if (_audioChorro != null) _audioChorro.Stop();
    }

    [ContextMenu("Recargar")]
    public void Recargar() { _carga = segundosDeCarga; }

    void OnDrawGizmosSelected()
    {
        Transform b = boquilla != null ? boquilla : transform;
        Gizmos.color = new Color(0.3f, 0.8f, 1f);
        Gizmos.DrawLine(b.position, b.position + b.forward * alcance);
        Gizmos.DrawWireSphere(b.position + b.forward * alcance, Mathf.Tan(anguloCono * Mathf.Deg2Rad) * alcance);
    }
}
