using UnityEngine;

[ExecuteAlways]
public class TechoPisoConfig : MonoBehaviour
{
    [Header("Dimensiones Techo / Piso")]
    [Tooltip("Ancho en el eje X")]
    public float ancho = 1f;

    [Tooltip("Espesor o altura en el eje Y")]
    public float alto = 0.25f;

    [Tooltip("Largo en el eje Z")]
    public float largo = 1f;

    [Header("Referencias Opcionales")]
    [Tooltip("Si el modelo visual es un hijo, se asigna aquí. Si no, escala el objeto raíz.")]
    public Transform modeloVisual;

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
        Vector3 escala = new Vector3(Mathf.Max(0.01f, ancho), Mathf.Max(0.01f, alto), Mathf.Max(0.01f, largo));

        if (modeloVisual != null)
        {
            modeloVisual.localScale    = escala;
            modeloVisual.localPosition = Vector3.zero;
        }
        else
        {
            transform.localScale = escala;
        }
    }

    public void Configurar(float nuevoAncho, float nuevoAlto, float nuevoLargo)
    {
        ancho = nuevoAncho;
        alto  = nuevoAlto;
        largo = nuevoLargo;
        ActualizarVisuales();
    }

    /// <summary>
    /// Iguala los materiales 0 y 2 al material 1 en todos los MeshRenderer del objeto y sus hijos.
    /// Útil para el techo final donde la cara superior e inferior deben verse igual que los lados.
    /// </summary>
    public void IgualarMateriales()
    {
        IgualarEnRenderer(GetComponent<MeshRenderer>());
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            IgualarEnRenderer(mr);
    }

    private static void IgualarEnRenderer(MeshRenderer mr)
    {
        if (mr == null) return;
        Material[] mats = mr.sharedMaterials;
        if (mats == null || mats.Length < 2) return;

        Material matRef = mats[1];
        for (int i = 0; i < mats.Length; i++)
            if (i != 1) mats[i] = matRef;

        mr.sharedMaterials = mats;
    }
}
