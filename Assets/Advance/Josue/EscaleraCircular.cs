using UnityEngine;

public class EscaleraCircular : MonoBehaviour
{
    [Header("Peldaños")]
    public int cantidadPeldanos = 22;
    public float alturaPeldano = 0.18f;     // subida entre peldaños (metros)
    public float gradosPorPeldano = 16f;    // 22 x 16° = ~352° (una vuelta)
    public float espesorPeldano = 0.08f;

    [Header("Radios")]
    public float radioExterior = 1.2f;
    public float radioEje = 0.2f;

    [Header("Barandilla")]
    public bool crearBarandilla = true;
    public float alturaBarandilla = 1.0f;

    [ContextMenu("Generar escalera")]
    public void Generar()
    {
        // Borra lo generado antes
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        float radioMedio = (radioExterior + radioEje) / 2f;
        float ancho = radioExterior - radioEje;
        float profundidad = 2f * radioMedio * Mathf.Sin(gradosPorPeldano * Mathf.Deg2Rad / 2f) * 1.15f;

        // Eje central
        float alturaTotal = cantidadPeldanos * alturaPeldano + alturaBarandilla;
        var eje = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        eje.name = "Eje";
        eje.transform.SetParent(transform, false);
        eje.transform.localScale = new Vector3(radioEje * 2f, alturaTotal / 2f, radioEje * 2f);
        eje.transform.localPosition = new Vector3(0, alturaTotal / 2f, 0);

        // Peldaños
        Vector3 anterior = Vector3.zero;
        for (int i = 0; i < cantidadPeldanos; i++)
        {
            float ang = i * gradosPorPeldano;
            float rad = ang * Mathf.Deg2Rad;
            float y = (i + 1) * alturaPeldano;

            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Peldano_" + (i + 1);
            p.transform.SetParent(transform, false);
            p.transform.localScale = new Vector3(ancho, espesorPeldano, profundidad);
            p.transform.localPosition = new Vector3(Mathf.Cos(rad) * radioMedio, y - espesorPeldano / 2f, Mathf.Sin(rad) * radioMedio);
            p.transform.localRotation = Quaternion.Euler(0, -ang, 0);

            if (crearBarandilla)
            {
                Vector3 puntoRiel = new Vector3(Mathf.Cos(rad) * (radioExterior - 0.05f), y + alturaBarandilla, Mathf.Sin(rad) * (radioExterior - 0.05f));

                var poste = GameObject.CreatePrimitive(PrimitiveType.Cube);
                poste.name = "Poste_" + (i + 1);
                poste.transform.SetParent(transform, false);
                poste.transform.localScale = new Vector3(0.04f, alturaBarandilla, 0.04f);
                poste.transform.localPosition = new Vector3(puntoRiel.x, y + alturaBarandilla / 2f, puntoRiel.z);

                if (i > 0)
                {
                    Vector3 dir = puntoRiel - anterior;
                    var riel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    riel.name = "Pasamanos_" + i;
                    riel.transform.SetParent(transform, false);
                    riel.transform.localPosition = (puntoRiel + anterior) / 2f;
                    riel.transform.localRotation = Quaternion.LookRotation(dir);
                    riel.transform.localScale = new Vector3(0.06f, 0.05f, dir.magnitude);
                }
                anterior = puntoRiel;
            }
        }
    }
}
