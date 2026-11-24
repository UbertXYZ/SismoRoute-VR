using UnityEngine;

// Tipo de bloqueo que puede aparecer en un punto.
public enum TipoBloqueoJosue { Aleatorio, Fuego, Rocas }

// Marcador OPCIONAL: ponlo en un objeto vacio dentro de un pasillo para decirle al
// GeneradorBloqueosAleatorios "aqui PUEDE aparecer un bloqueo".
// Convencion: el eje Z local (flecha azul) debe CRUZAR el pasillo, de pared a pared.
// No es obligatorio: el generador tambien encuentra puntos solo (pasillos del PCG
// de Uberto y bloqueos ya colocados por Jheeremy).
public class PuntoBloqueo : MonoBehaviour
{
    [Tooltip("Aleatorio = el generador decide si es fuego o rocas.")]
    public TipoBloqueoJosue tipo = TipoBloqueoJosue.Aleatorio;

    [Tooltip("Probabilidad de que este punto entre al sorteo en cada partida.")]
    [Range(0f, 1f)] public float probabilidad = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = tipo == TipoBloqueoJosue.Fuego ? new Color(1f, 0.4f, 0f) :
                       tipo == TipoBloqueoJosue.Rocas ? new Color(0.6f, 0.45f, 0.3f) : Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(new Vector3(0f, 1.5f, 0f), new Vector3(3f, 3f, 4f));
        Gizmos.DrawLine(new Vector3(0f, 0.1f, -2f), new Vector3(0f, 0.1f, 2f)); // linea que cruza el pasillo
        Gizmos.matrix = Matrix4x4.identity;
    }
}
