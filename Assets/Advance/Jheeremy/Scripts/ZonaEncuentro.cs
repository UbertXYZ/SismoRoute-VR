using UnityEngine;
using UnityEngine.AI;

// Zona segura / punto de encuentro. Las personas evacuadas caminan hasta aquí
// y se ubican en un lugar al azar dentro del radio (para no amontonarse).
public class ZonaEncuentro : MonoBehaviour
{
    [Tooltip("Radio del área donde se reparten las personas.")]
    public float radio = 4f;

    public Vector3 PuntoLibre()
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 r = Random.insideUnitCircle * radio;
            Vector3 p = transform.position + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                return hit.position;
        }
        return transform.position;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}