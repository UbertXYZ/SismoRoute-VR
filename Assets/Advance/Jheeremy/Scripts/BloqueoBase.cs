using System.Collections;
using UnityEngine;

// Clase base para los 3 bloqueos (aglomeración, bloqueo repentino, fuego alto).
// Es abstracta: no se agrega directo a un objeto, los otros scripts heredan de ella.
public abstract class BloqueoBase : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Se activa apenas empieza la escena (para probar).")]
    public bool activarAlIniciar = false;

    [Tooltip("Segundos de espera antes de que empiece el bloqueo.")]
    public float retardo = 0f;

    [Header("Bloqueo")]
    [Tooltip("BoxCollider que impide el paso. Hazlo de piso a techo.")]
    public BoxCollider colliderBloqueo;

    // true cuando ya se activó (para que no se active dos veces)
    protected bool activo = false;

    protected virtual void Start()
    {
        // Al inicio la ruta está libre
        if (colliderBloqueo != null)
            colliderBloqueo.enabled = false;

        if (activarAlIniciar)
            Activar();
    }

    // Esto es lo que llamará el generador aleatorio de Uberto
    [ContextMenu("Activar")]
    public void Activar()
    {
        if (activo || !Application.isPlaying) return;
        activo = true;
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        if (retardo > 0f)
            yield return new WaitForSeconds(retardo);

        yield return Ejecutar();
    }

    // Cada bloqueo escribe aquí su propia secuencia (animación, partículas, etc.)
    protected abstract IEnumerator Ejecutar();

    // Prende el collider para que no se pueda pasar
    protected void BloquearRuta()
    {
        if (colliderBloqueo != null)
            colliderBloqueo.enabled = true;
    }
}