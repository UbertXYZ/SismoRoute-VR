using UnityEngine;

/// <summary>Reenvía las entradas del collider hijo al bloqueo de la escalera.</summary>
[RequireComponent(typeof(Collider))]
public class DetectorBloqueoEscalera : MonoBehaviour
{
    private BloqueoEscalera bloqueo;

    private void Awake()
    {
        bloqueo = GetComponentInParent<BloqueoEscalera>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (bloqueo == null)
            bloqueo = GetComponentInParent<BloqueoEscalera>();

        if (bloqueo != null)
            bloqueo.IntentarActivar(other);
    }
}
