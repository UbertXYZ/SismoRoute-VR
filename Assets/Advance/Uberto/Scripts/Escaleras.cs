using UnityEngine;

[ExecuteAlways]
public class Escaleras : MonoBehaviour
{
    [Header("Dimensiones de la Escalera")]
    [Tooltip("Ancho en el eje X")]
    public float ancho = 7f;

    [Tooltip("Largo o profundidad en el eje Z")]
    public float largo = 5f;

    [Tooltip("Altura vertical en el eje Y")]
    public float alto = 4.25f;

    [Header("Detalle Visual")]
    public int numeroEscalones = 16;
    public Transform modeloVisual;

    private void OnValidate()
    {
        ActualizarVisuales();
    }

    private void Awake()
    {
        ActualizarVisuales();
    }

    public void Configurar(float nuevoAncho, float nuevoLargo, float nuevoAlto)
    {
        ancho = Mathf.Max(1f, nuevoAncho);
        largo = Mathf.Max(1f, nuevoLargo);
        alto = Mathf.Max(1f, nuevoAlto);
        ActualizarVisuales();
    }

    [ContextMenu("Actualizar Visuales")]
    public void ActualizarVisuales()
    {
        if (modeloVisual != null)
        {
            modeloVisual.localScale = new Vector3(ancho, alto, largo);
            modeloVisual.localPosition = Vector3.zero;
            return;
        }

        // Si no hay modelo hijo asignado, verificamos si ya se crearon escalones procedimentales
        Transform contEscalones = transform.Find("EscalonesVisuales");
        if (contEscalones == null)
        {
            GameObject go = new GameObject("EscalonesVisuales");
            go.transform.SetParent(transform, false);
            contEscalones = go.transform;
            GenerarEscalonesProcedurales(contEscalones);
        }
        else
        {
            // Ajustar escala global del contenedor o regenerar si cambió
            GenerarEscalonesProcedurales(contEscalones);
        }
    }

    private void GenerarEscalonesProcedurales(Transform contenedor)
    {
        // Limpiar escalones viejos
        while (contenedor.childCount > 0)
        {
            Transform c = contenedor.GetChild(0);
            c.SetParent(null);
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }

        int steps = Mathf.Max(4, numeroEscalones);
        float stepHeight = alto / steps;
        float stepDepth = largo / steps;

        for (int i = 0; i < steps; i++)
        {
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Paso_{i + 1}";
            step.transform.SetParent(contenedor, false);

            float currentY = (i + 1) * stepHeight;
            step.transform.localScale = new Vector3(ancho, currentY, stepDepth);

            float posX = 0f;
            float posY = currentY / 2f;
            float posZ = -largo / 2f + (i + 0.5f) * stepDepth;

            step.transform.localPosition = new Vector3(posX, posY, posZ);
        }
    }
}
