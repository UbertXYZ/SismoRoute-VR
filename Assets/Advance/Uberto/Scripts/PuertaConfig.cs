using UnityEngine;

public class PuertaConfig : MonoBehaviour
{
    [Header("Habitaciones Configuración")]
    public Vector3 dimensiones = new Vector3(1f, 2.5f, 0.1f);
    public Transform modelo;
    private void OnValidate()
    {
        modelo.localScale = dimensiones;
        modelo.localPosition = new Vector3 (0f, dimensiones.y/2, 0f);
    }
}
