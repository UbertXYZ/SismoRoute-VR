using UnityEngine;
using UnityEngine.AI;

// Puerta que se abre sola cuando se acerca un NPC o el jugador, y se cierra cuando se van.
// Gira el modelo de la puerta alrededor de una "bisagra" en uno de sus costados.
public class PuertaAutomatica : MonoBehaviour
{
    [Tooltip("Modelo de la puerta que gira. Si está vacío, usa el primer hijo.")]
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
    readonly Collider[] encontrados = new Collider[16];

    void Start()
    {
        if (modelo == null && transform.childCount > 0)
            modelo = transform.GetChild(0);

        // Posición cerrada y punto de la bisagra (borde de la puerta)
        posCerrada = modelo.localPosition;
        rotCerrada = modelo.localRotation;
        float mitadAncho = modelo.localScale.x / 2f;
        bisagra = posCerrada + new Vector3(bisagraIzquierda ? -mitadAncho : mitadAncho, 0f, 0f);

        // Revisar 5 veces por segundo (más barato que cada frame)
        InvokeRepeating(nameof(Detectar), Random.value * 0.2f, 0.2f);
    }

    void Detectar()
    {
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
        bool abrir = Time.time - ultimaDeteccion < tiempoCierre;
        float objetivo = abrir ? anguloAbierta : 0f;
        anguloActual = Mathf.Lerp(anguloActual, objetivo, Time.deltaTime * velocidad);

        // Girar el modelo alrededor de la bisagra
        float signo = bisagraIzquierda ? 1f : -1f;
        Quaternion giro = Quaternion.Euler(0f, anguloActual * signo, 0f);
        modelo.localRotation = giro * rotCerrada;
        modelo.localPosition = bisagra + giro * (posCerrada - bisagra);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(new Vector3(0f, tamanoZona.y / 2f, 0f), tamanoZona);
    }
}