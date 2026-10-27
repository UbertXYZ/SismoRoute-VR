using UnityEngine;

public class PuertaConfig : MonoBehaviour
{
    [Header("Configuración de la Puerta")]
    public Vector3 dimensiones = new Vector3(1.2f, 2.5f, 0.15f);
    public Transform modelo;

    private void OnValidate()
    {
        ActualizarVisuales();
    }

    private void Awake()
    {
        ActualizarVisuales();
    }

    public void ActualizarVisuales()
    {
        // Si no está asignado, busca el primer hijo
        if (modelo == null && transform.childCount > 0)
        {
            modelo = transform.GetChild(0);
        }

        if (modelo != null)
        {
            modelo.localScale = dimensiones;
            // El pivote se apoya en el suelo (base en Y = 0)
            modelo.localPosition = new Vector3(0f, dimensiones.y / 2f, 0f);
        }
        else
        {
            // Si el objeto actual contiene la malla directamente
            transform.localScale = dimensiones;
        }
    }

    public void ConfigurarDimensiones(float ancho, float alto, float grosor)
    {
        dimensiones = new Vector3(ancho, alto, grosor);
        ActualizarVisuales();
    }
}