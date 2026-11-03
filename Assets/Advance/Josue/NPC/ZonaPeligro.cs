using System.Collections.Generic;
using UnityEngine;

public enum TipoZona { Bloqueo, Fuego, Aglomeracion }

// Se agrega a cualquier objeto (rocas, fuego, multitud) para que los NPC
// sepan que ahi hay un problema, SIN necesitar conocer el codigo de ese objeto.
public class ZonaPeligro : MonoBehaviour
{
    public TipoZona tipo = TipoZona.Bloqueo;
    public float radio = 2f;   // radio de la zona, en metros

    // Lista compartida con todas las zonas ACTIVAS. OnEnable/OnDisable se llaman
    // solos cuando el objeto se activa/desactiva, asi la lista siempre esta al dia.
    public static readonly List<ZonaPeligro> Activas = new List<ZonaPeligro>();
    void OnEnable()  { Activas.Add(this); }
    void OnDisable() { Activas.Remove(this); }

    // ¿El punto p esta dentro de la zona? Solo miramos X y Z (vista desde arriba).
    public bool Contiene(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        return d.magnitude <= radio;
    }

    // Dibuja un circulo en la vista Scene para que veas el radio.
    void OnDrawGizmos()
    {
        Gizmos.color = tipo == TipoZona.Fuego ? Color.red :
                       tipo == TipoZona.Bloqueo ? Color.yellow : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}