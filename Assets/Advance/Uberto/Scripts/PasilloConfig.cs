using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PasilloConfig : MonoBehaviour
{
    [Header("Dimensiones del Pasillo")]
    [Tooltip("Extensión o largo a lo largo del eje Z local")]
    public float largo = 24f;

    [Tooltip("Ancho a lo largo del eje X local")]
    public float ancho = 4f;

    [Tooltip("Grosor o espesor de las losas en el eje Y")]
    public float alto = 0.25f;

    [Tooltip("Altura libre del pasillo entre piso y techo (espacio vertical)")]
    public float altura = 4.25f;

    [Header("Componentes Activos")]
    public bool tieneSuelo = false;
    public bool tieneTecho = false;
    public bool tieneLuces = false;

    [Header("Prefabs")]
    public GameObject luzPrefab;
    public GameObject losaPrefab;

    [Header("Estructura de Partes")]
    [Tooltip("Losa Inferior (Suelo). Puede ser None; si es None y tieneSuelo=true se crea dinámicamente.")]
    public Transform losasInferiores;
    [Tooltip("Losa Superior (Techo). Puede ser None; si es None y tieneTecho=true se crea dinámicamente.")]
    public Transform losasSuperiores;
    public Transform contenedorLuces;

    // Referencia de compatibilidad previa
    public Transform modelo => losasInferiores;

    private void OnValidate()
    {
        ActualizarVisuales();
    }

    private void Awake()
    {
        ActualizarVisuales();
    }

    public void Configurar(float nuevoLargoZ, float nuevoAnchoX, float nuevoGrosorY, float nuevaAlturaY, bool conTecho = true, bool conSuelo = true)
    {
        largo   = Mathf.Max(0.5f, nuevoLargoZ);
        ancho   = Mathf.Max(0.5f, nuevoAnchoX);
        alto    = Mathf.Max(0.01f, nuevoGrosorY);
        altura  = Mathf.Max(0.5f, nuevaAlturaY);
        tieneTecho  = conTecho;
        tieneSuelo  = conSuelo;
        ActualizarVisuales();
    }

    // Sobrecarga de compatibilidad
    public void Configurar(float nuevoLargo, float nuevoAncho, float nuevoAlto)
    {
        Configurar(nuevoLargo, nuevoAncho, nuevoAlto, 4.25f, true, true);
    }

    [ContextMenu("Actualizar Visuales")]
    public void ActualizarVisuales()
    {
        CargarPrefabsSiEsNecesario();

        largo  = Mathf.Max(0.5f, largo);
        ancho  = Mathf.Max(0.5f, ancho);
        alto   = Mathf.Max(0.01f, alto);
        altura = Mathf.Max(0.5f, altura);

        Vector3 escalaLosa = new Vector3(ancho, alto, largo);

        // 1. LosasInferiores (Suelo)
        //    Si tieneSuelo=true pero el campo es null, se crea dinámicamente.
        //    Si tieneSuelo=false, se desactiva si existe.
        if (tieneSuelo)
        {
            if (losasInferiores == null)
                losasInferiores = CrearObjetoLosa("LosasInferiores");

            if (losasInferiores != null)
            {
                losasInferiores.gameObject.SetActive(true);
                losasInferiores.localScale    = escalaLosa;
                losasInferiores.localPosition = new Vector3(0f, alto / 2f, 0f);
                losasInferiores.localRotation = Quaternion.identity;
            }
        }
        else
        {
            if (losasInferiores != null)
                losasInferiores.gameObject.SetActive(false);
            // Si era None y tieneSuelo=false, no hacemos nada (correcto).
        }

        // 2. LosasSuperiores (Techo)
        //    Crece hacia abajo desde el nivel de techo (altura).
        if (tieneTecho)
        {
            if (losasSuperiores == null)
                losasSuperiores = CrearObjetoLosa("LosasSuperiores");

            if (losasSuperiores != null)
            {
                losasSuperiores.gameObject.SetActive(true);
                losasSuperiores.localScale    = escalaLosa;
                losasSuperiores.localPosition = new Vector3(0f, altura - alto / 2f, 0f);
                losasSuperiores.localRotation = Quaternion.identity;
            }
        }
        else
        {
            if (losasSuperiores != null)
                losasSuperiores.gameObject.SetActive(false);
        }

        // 3. Luces
        ActualizarLuces();
    }

    private void ActualizarLuces()
    {
        // Asegurar contenedor de luces
        if (contenedorLuces == null)
        {
            Transform t = transform.Find("Luces");
            if (t != null)
                contenedorLuces = t;
            else
            {
                GameObject goLuces = new GameObject("Luces");
                goLuces.transform.SetParent(transform, false);
                contenedorLuces = goLuces.transform;
            }
        }

        if (!tieneLuces || luzPrefab == null)
        {
            contenedorLuces.gameObject.SetActive(false);
            return;
        }

        contenedorLuces.gameObject.SetActive(true);
        contenedorLuces.localPosition = Vector3.zero;
        contenedorLuces.localRotation = Quaternion.identity;

        // "Cada 8 de extension en Z/-Z(local), creará un Prefab de Luz"
        int numLuces = Mathf.Max(1, Mathf.RoundToInt(largo / 8f));
        float stepZ  = largo / numLuces;

        // "las luces siempre deben de estar en la parte inferior de la losa superior si existe.
        //  Sino, en la parte más alta del rango disponible."
        float posY = tieneTecho ? (altura - alto - 0.05f) : (altura - 0.10f);

        int hijosExistentes = contenedorLuces.childCount;

        for (int i = 0; i < numLuces; i++)
        {
            Transform luzTransform;
            if (i < hijosExistentes)
            {
                luzTransform = contenedorLuces.GetChild(i);
                luzTransform.gameObject.SetActive(true);
            }
            else
            {
                GameObject nuevaLuz = Instantiate(luzPrefab, contenedorLuces);
                nuevaLuz.name  = $"Spot Light ({i + 1})";
                luzTransform   = nuevaLuz.transform;
            }

            float posZ = -largo / 2f + (i + 0.5f) * stepZ;
            luzTransform.localPosition = new Vector3(0f, posY, posZ);
            luzTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // Desactivar excedentes
        for (int i = numLuces; i < hijosExistentes; i++)
        {
            contenedorLuces.GetChild(i).gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Busca la losa existente por nombre o crea un primitivo
    // ─────────────────────────────────────────────────────────
    private Transform CrearObjetoLosa(string nombre)
    {
        // Primero buscar si ya existe con ese nombre
        Transform existente = transform.Find(nombre);
        if (existente != null) return existente;

        // Buscar nombres legados
        if (nombre == "LosasInferiores")
        {
            existente = transform.Find("Suelo");
            if (existente != null) { existente.name = nombre; return existente; }
        }
        else if (nombre == "LosasSuperiores")
        {
            existente = transform.Find("Techo");
            if (existente != null) { existente.name = nombre; return existente; }
        }

        if (losaPrefab != null)
        {
            GameObject nueva = Instantiate(losaPrefab, transform);
            nueva.name = nombre;
            return nueva.transform;
        }

        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.SetParent(transform, false);
        return cubo.transform;
    }

    private void CargarPrefabsSiEsNecesario()
    {
#if UNITY_EDITOR
        if (luzPrefab == null)
            luzPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Spot Light.prefab");
        if (losaPrefab == null)
            losaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Losa.prefab");
#endif
    }
}
