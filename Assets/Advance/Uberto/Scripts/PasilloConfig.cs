using UnityEngine;

public class PasilloConfig : MonoBehaviour
{
    [Header("Dimensiones del Pasillo")]
    [Tooltip("Largo a lo largo del eje X")]
    public float largo = 24f;
    [Tooltip("Ancho a lo largo del eje Z")]
    public float ancho = 4f;
    [Tooltip("Altura o espesor en el eje Y")]
    public float alto = 0.25f;

    [Header("Referencias")]
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
        // Si no está asignado, busca el primer hijo (por ejemplo 'Cuerpo')
        if (modelo == null && transform.childCount > 0)
        {
            modelo = transform.GetChild(0);
        }

        if (modelo != null)
        {
            modelo.localScale = new Vector3(largo, alto, ancho);
            // Centrado con la base en Y = 0
            modelo.localPosition = new Vector3(0f, alto / 2f, 0f);
        }
        else
        {
            transform.localScale = new Vector3(largo, alto, ancho);
        }
    }

    public void Configurar(float nuevoLargo, float nuevoAncho, float nuevoAlto)
    {
        largo = nuevoLargo;
        ancho = nuevoAncho;
        alto = nuevoAlto;
        ActualizarVisuales();
    }
}