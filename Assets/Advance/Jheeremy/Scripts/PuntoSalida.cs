using UnityEngine;

// Marca un punto de salida (puerta, escalera, punto de encuentro).
// Los NPC que huyen buscan el PuntoSalida mas cercano.
public class PuntoSalida : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
