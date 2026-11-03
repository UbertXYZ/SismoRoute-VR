using UnityEngine;
using UnityEngine.AI;

// Puerta que se abre sola cuando se acerca un NPC o el jugador, y se cierra cuando se van.
// Gira el modelo de la puerta alrededor de una "bisagra" en uno de sus costados.
public class PuertaAutomatica : MonoBehaviour
{
    [Tooltip("Modelo de la puerta que gira. Si está vacío, lo busca solo.")]
    public Transform modelo;

    [Header("Apertura")]
    public float anguloAbierta = 95f;
    public float velocidad = 4f;
    [Tooltip("Bisagra en el lado izquierdo (-X). Desmárcalo para usar el lado derecho.")]
    public bool bisagraIzquierda = true;
    [Tooltip("Segundos que espera sin nadie cerca antes de cerrarse.")]
    public float tiempoCierre = 1.5f;

    [Header("Detección")]
    [Tooltip("Zona alrededor de la puerta donde se detecta a las personas.")]
    public Vector3 tamanoZona = new Vector3(2f, 2f, 3f);

    Vector3 posCerrada;
    Quaternion rotCerrada;
    Vector3 bisagra;
    float anguloActual;
    float ultimaDeteccion = -99f;
    bool listo = false;
    bool avisado = false;
    readonly Collider[] encontrados = new Collider[16];

    void Start()
    {
        Inicializar();
        // Revisar 5 veces por segundo (más barato que cada frame)
        InvokeRepeating(nameof(Detectar), Random.value * 0.2f, 0.2f);
    }

    // Busca el modelo y calcula la bisagra. Devuelve true cuando ya está listo.
    bool Inicializar()
    {
        if (listo) return true;

        // 1) Lo que use el PuertaConfig de Uberto
        if (modelo == null)
        {
            PuertaConfig config = GetComponent<PuertaConfig>();
            if (config != null && config.modelo != null)
                modelo = config.modelo;
        }

        // 2) Si no, el primer hijo
        if (modelo == null && transform.childCount > 0)
            modelo = transform.GetChild(0);

        if (modelo == null)
        {
            if (!avisado)
            {
                avisado = true;
                Debug.LogWarning($"PuertaAutomatica: '{name}' (padre: '{(transform.parent ? transform.parent.name : "ninguno")}') no tiene modelo de puerta. Haz clic en este aviso para ver cuál es.", this);
            }
            return false;
        }

        // Posición cerrada y punto de la bisagra (borde de la puerta)
        posCerrada = modelo.localPosition;
        rotCerrada = modelo.localRotation;
        float mitadAncho = modelo.localScale.x / 2f;
        bisagra = posCerrada + new Vector3(bisagraIzquierda ? -mitadAncho : mitadAncho, 0f, 0f);
        listo = true;
        return true;
    }

    void Detectar()
    {
        // 1) Jugador: revisar la posición de la cámara (sirve con visor real y con el XR Device Simulator)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 local = transform.InverseTransformPoint(cam.transform.position);
            if (Mathf.Abs(local.x) <= tamanoZona.x / 2f &&
                Mathf.Abs(local.z) <= tamanoZona.z / 2f &&
                local.y > -0.5f && local.y < 2.5f)
            {
                ultimaDeteccion = Time.time;
                return;
            }
        }

        // 2) NPC y jugador por sus colliders
        Vector3 centro = transform.TransformPoint(new Vector3(0f, tamanoZona.y / 2f, 0f));
        int n = Physics.OverlapBoxNonAlloc(centro, tamanoZona / 2f, encontrados,
                                           transform.rotation, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            Collider c = encontrados[i];
            if (c.GetComponentInParent<NavMeshAgent>() != null ||
                c.GetComponentInParent<CharacterController>() != null)
            {
                ultimaDeteccion = Time.time;
                return;
            }
        }
    }

    void Update()
    {
        if (!Inicializar()) return;

        bool abrir = Time.time - ultimaDeteccion < tiempoCierre;
        float objetivo = abrir ? anguloAbierta : 0f;
        anguloActual = Mathf.Lerp(anguloActual, objetivo, Time.deltaTime * velocidad);

        // Girar el modelo alrededor de la bisagra
        float signo = bisagraIzquierda ? 1f : -1f;
        Quaternion giro = Quaternion.Euler(0f, anguloActual * signo, 0f);
        modelo.localRotation = giro * rotCerrada;
        modelo.localPosition = bisagra + giro * (posCerrada - bisagra);
    }

    // Para probar: en Play, clic derecho en el componente > "Probar abrir"
    [ContextMenu("Probar abrir")]
    void ProbarAbrir()
    {
        ultimaDeteccion = Time.time + 5f; // la mantiene abierta unos 6 segundos
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(new Vector3(0f, tamanoZona.y / 2f, 0f), tamanoZona);
    }
}