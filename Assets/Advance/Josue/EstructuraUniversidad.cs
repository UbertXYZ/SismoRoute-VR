using UnityEngine;

public class EstructuraUniversidad : MonoBehaviour
{
    public enum Tipo { EscaleraRecta, Rampa, Pasillo, PuertaSalida, Aula, FlechaSuelo }
    public Tipo tipo = Tipo.Pasillo;

    [Header("Escalera recta con descanso")]
    public int peldanosPorTramo = 10;
    public float alturaPeldano = 0.17f;
    public float profundidadPeldano = 0.28f;
    public float anchoEscalera = 1.5f;

    [Header("Rampa")]
    public float largoRampa = 6f;
    public float alturaRampa = 0.5f;
    public float anchoRampa = 1.5f;

    [Header("Pasillo")]
    public float largoPasillo = 12f;
    public float anchoPasillo = 2.5f;
    public float altoPasillo = 3f;
    public float separacionColumnas = 4f;

    [Header("Puerta de salida")]
    public float anchoPuerta = 1.2f;
    public float altoPuerta = 2.1f;
    public float anchoMuro = 4f;
    public float altoMuro = 3f;

    [Header("Aula")]
    public int filas = 4;
    public int columnasPupitres = 5;
    public float separacionPupitres = 1.3f;

    GameObject Pieza(string nombre, Vector3 pos, Vector3 escala, Quaternion? rot = null, PrimitiveType prim = PrimitiveType.Cube)
    {
        var g = GameObject.CreatePrimitive(prim);
        g.name = nombre;
        g.transform.SetParent(transform, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        if (rot.HasValue) g.transform.localRotation = rot.Value;
        return g;
    }

    [ContextMenu("Generar estructura")]
    public void Generar()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        switch (tipo)
        {
            case Tipo.EscaleraRecta: GenerarEscaleraRecta(); break;
            case Tipo.Rampa: GenerarRampa(); break;
            case Tipo.Pasillo: GenerarPasillo(); break;
            case Tipo.PuertaSalida: GenerarPuerta(); break;
            case Tipo.Aula: GenerarAula(); break;
            case Tipo.FlechaSuelo: GenerarFlecha(); break;
        }
    }

    void GenerarEscaleraRecta()
    {
        int n = peldanosPorTramo;
        float xA = -anchoEscalera / 2f - 0.05f;
        float xB = anchoEscalera / 2f + 0.05f;
        float alturaDescanso = n * alturaPeldano;

        for (int i = 0; i < n; i++)
        {
            float h = (i + 1) * alturaPeldano;
            Pieza("Peldano_A_" + (i + 1), new Vector3(xA, h / 2f, (i + 0.5f) * profundidadPeldano),
                  new Vector3(anchoEscalera, h, profundidadPeldano));
        }

        Pieza("Descanso", new Vector3(0, alturaDescanso / 2f, n * profundidadPeldano + anchoEscalera / 2f),
              new Vector3(anchoEscalera * 2f + 0.1f, alturaDescanso, anchoEscalera));

        for (int j = 0; j < n; j++)
        {
            float h = alturaDescanso + (j + 1) * alturaPeldano;
            Pieza("Peldano_B_" + (j + 1), new Vector3(xB, h / 2f, n * profundidadPeldano - (j + 0.5f) * profundidadPeldano),
                  new Vector3(anchoEscalera, h, profundidadPeldano));
        }
    }

    void GenerarRampa()
    {
        float angulo = Mathf.Atan2(alturaRampa, largoRampa) * Mathf.Rad2Deg;
        float largoReal = Mathf.Sqrt(largoRampa * largoRampa + alturaRampa * alturaRampa);
        Pieza("Rampa", new Vector3(0, alturaRampa / 2f, largoRampa / 2f),
              new Vector3(anchoRampa, 0.1f, largoReal), Quaternion.Euler(-angulo, 0, 0));
    }

    void GenerarPasillo()
    {
        Pieza("Piso", new Vector3(0, -0.05f, largoPasillo / 2f), new Vector3(anchoPasillo, 0.1f, largoPasillo));
        Pieza("Techo", new Vector3(0, altoPasillo + 0.05f, largoPasillo / 2f), new Vector3(anchoPasillo, 0.1f, largoPasillo));
        float xPared = anchoPasillo / 2f + 0.05f;
        Pieza("Pared_Izq", new Vector3(-xPared, altoPasillo / 2f, largoPasillo / 2f), new Vector3(0.1f, altoPasillo, largoPasillo));
        Pieza("Pared_Der", new Vector3(xPared, altoPasillo / 2f, largoPasillo / 2f), new Vector3(0.1f, altoPasillo, largoPasillo));

        int k = 1;
        for (float z = separacionColumnas; z < largoPasillo; z += separacionColumnas)
        {
            float xCol = anchoPasillo / 2f - 0.2f;
            Pieza("Columna_Izq_" + k, new Vector3(-xCol, altoPasillo / 2f, z), new Vector3(0.3f, altoPasillo / 2f, 0.3f), null, PrimitiveType.Cylinder);
            Pieza("Columna_Der_" + k, new Vector3(xCol, altoPasillo / 2f, z), new Vector3(0.3f, altoPasillo / 2f, 0.3f), null, PrimitiveType.Cylinder);
            k++;
        }
    }

    void GenerarPuerta()
    {
        float lado = (anchoMuro - anchoPuerta) / 2f;
        float xLado = anchoPuerta / 2f + lado / 2f;
        float altoSuperior = altoMuro - altoPuerta;

        Pieza("Muro_Izq", new Vector3(-xLado, altoMuro / 2f, 0), new Vector3(lado, altoMuro, 0.2f));
        Pieza("Muro_Der", new Vector3(xLado, altoMuro / 2f, 0), new Vector3(lado, altoMuro, 0.2f));
        Pieza("Muro_Superior", new Vector3(0, altoPuerta + altoSuperior / 2f, 0), new Vector3(anchoPuerta, altoSuperior, 0.2f));
        Pieza("Cartel_Salida", new Vector3(0, altoPuerta + altoSuperior / 2f, -0.11f), new Vector3(0.6f, 0.2f, 0.02f));
    }

    void GenerarAula()
    {
        float ancho = columnasPupitres * separacionPupitres + 2f;
        float largo = filas * separacionPupitres + 2f;
        Pieza("Piso", new Vector3(0, -0.05f, largo / 2f), new Vector3(ancho, 0.1f, largo));

        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < columnasPupitres; c++)
            {
                float x = (c - (columnasPupitres - 1) / 2f) * separacionPupitres;
                float z = 1f + f * separacionPupitres;
                Pieza("Mesa_" + f + "_" + c, new Vector3(x, 0.75f, z), new Vector3(0.7f, 0.05f, 0.5f));
                Pieza("Silla_" + f + "_" + c, new Vector3(x, 0.45f, z - 0.5f), new Vector3(0.4f, 0.05f, 0.4f));
            }
        }
    }

    void GenerarFlecha()
    {
        var partes = new GameObject[3];
        partes[0] = Pieza("Eje", new Vector3(0, 0.02f, 0.5f), new Vector3(0.2f, 0.02f, 1f));
        partes[1] = Pieza("Punta_Izq", new Vector3(-0.177f, 0.02f, 0.823f), new Vector3(0.2f, 0.02f, 0.5f), Quaternion.Euler(0, 45, 0));
        partes[2] = Pieza("Punta_Der", new Vector3(0.177f, 0.02f, 0.823f), new Vector3(0.2f, 0.02f, 0.5f), Quaternion.Euler(0, -45, 0));
        // Sin colisión: la flecha es solo visual
        foreach (var p in partes) DestroyImmediate(p.GetComponent<Collider>());
    }
}
