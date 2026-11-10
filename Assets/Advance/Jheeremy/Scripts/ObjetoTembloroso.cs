using System.Collections;
using UnityEngine;

// Objeto que tiembla en el pre-sismo.
// Vibrar: una taza o un celular se sacude.  Balancear: una lámpara colgada se mece.
public class ObjetoTembloroso : MonoBehaviour
{
    public enum Tipo { Vibrar, Balancear }
    public Tipo tipo = Tipo.Vibrar;
    [Tooltip("Vibrar: cuánto se mueve (metros). Balancear: ángulo máximo (grados).")]
    public float intensidad = 0.01f;
    [Tooltip("Balancear: oscilaciones por segundo.")]
    public float frecuencia = 1.2f;

    Vector3 posInicial;
    Quaternion rotInicial;
    bool temblando;

    void Awake()
    {
        posInicial = transform.localPosition;
        rotInicial = transform.localRotation;
    }

    public void Temblar(float duracion)
    {
        if (!temblando) StartCoroutine(Rutina(duracion));
    }

    IEnumerator Rutina(float duracion)
    {
        temblando = true;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float fuerza = Mathf.Sin(t / duracion * Mathf.PI);   // sube y baja suave
            if (tipo == Tipo.Vibrar)
            {
                transform.localPosition = posInicial + Random.insideUnitSphere * intensidad * fuerza;
            }
            else
            {
                float a = t * frecuencia * 2f * Mathf.PI;
                transform.localRotation = rotInicial * Quaternion.Euler(
                    Mathf.Sin(a) * intensidad * fuerza, 0f, Mathf.Cos(a) * intensidad * 0.5f * fuerza);
            }
            yield return null;
        }
        transform.localPosition = posInicial;
        transform.localRotation = rotInicial;
        temblando = false;
    }
}