using System.Collections.Generic;
using UnityEngine;

// Objeto padre cuyos HIJOS (en orden) son los puntos de la ruta.
public class RutaEvacuacion : MonoBehaviour
{
    public Color colorGizmo = Color.cyan;

    public List<Vector3> ObtenerPuntos()
    {
        var puntos = new List<Vector3>();
        foreach (Transform hijo in transform) puntos.Add(hijo.position);
        return puntos;
    }

    // Dibuja la ruta en la vista Scene (esferas y lineas).
    void OnDrawGizmos()
    {
        Gizmos.color = colorGizmo;
        bool hayAnterior = false;
        Vector3 anterior = Vector3.zero;
        foreach (Transform hijo in transform)
        {
            Gizmos.DrawSphere(hijo.position, 0.2f);
            if (hayAnterior) Gizmos.DrawLine(anterior, hijo.position);
            anterior = hijo.position;
            hayAnterior = true;
        }
    }
}