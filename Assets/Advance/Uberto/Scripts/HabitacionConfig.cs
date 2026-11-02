using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HabitacionConfig : MonoBehaviour
{
    [Header("Dimensiones de la Habitación")]
    [Tooltip("Ancho en el eje X")]
    public float ancho = 8f;

    [Tooltip("Largo en el eje Z")]
    public float largo = 10f;

    [Tooltip("Altura en el eje Y")]
    public float alto = 4.25f;

    // Compatibilidad para habitaciones cuadradas previas
    public float tamanoLado
    {
        get => Mathf.Max(ancho, largo);
        set
        {
            ancho = value;
            largo = value;
        }
    }

    [Header("Grosor Configuración")]
    [Min(0.01f)] public float grosorPared = 0.20f;
    [Min(0f)] public float grosorSuelo = 0f;   // Si es 0, no se crea Losa Inferior
    [Min(0f)] public float grosorTecho = 0f;   // Si es 0, no se crea Losa Superior
    public float altoPared = 0f;

    [Header("Habitaciones Partes")]
    public Transform paredFrontal;
    public Transform paredTrasera;
    public Transform paredIzquierda;
    public Transform paredDerecha;

    [Tooltip("Losa Inferior (Suelo). Puede ser None si grosorSuelo = 0.")]
    public Transform losaInferior;   // renombrado de 'suelo'

    [Tooltip("Losa Superior (Techo). Puede ser None si grosorTecho = 0.")]
    public Transform losaSuperior;   // renombrado de 'techo'

    // Alias de compatibilidad
    public Transform suelo  { get => losaInferior;  set => losaInferior  = value; }
    public Transform techo  { get => losaSuperior;  set => losaSuperior  = value; }

    [Header("Control de Puertas por Lado (0, 1 o 2)")]
    [Range(0, 2)] public int puertasFrontal = 0;
    [Range(0, 2)] public int puertasTrasera = 0;
    [Range(0, 2)] public int puertasIzquierda = 0;
    [Range(0, 2)] public int puertasDerecha = 0;

    // Compatibilidad booleana
    public bool tienePuertaFrontal
    {
        get => puertasFrontal > 0;
        set => puertasFrontal = value ? (puertasFrontal > 0 ? puertasFrontal : 1) : 0;
    }
    public bool tienePuertaTrasera
    {
        get => puertasTrasera > 0;
        set => puertasTrasera = value ? (puertasTrasera > 0 ? puertasTrasera : 1) : 0;
    }
    public bool tienePuertaIzquierda
    {
        get => puertasIzquierda > 0;
        set => puertasIzquierda = value ? (puertasIzquierda > 0 ? puertasIzquierda : 1) : 0;
    }
    public bool tienePuertaDerecha
    {
        get => puertasDerecha > 0;
        set => puertasDerecha = value ? (puertasDerecha > 0 ? puertasDerecha : 1) : 0;
    }

    [Header("Referencias de Puertas (Legacy / Opcional)")]
    public PuertaConfig puertaFrontal;
    public PuertaConfig puertaTrasera;
    public PuertaConfig puertaIzquierda;
    public PuertaConfig puertaDerecha;

    public GameObject puertaPrefab;
    public GameObject paredPrefab;
    public Vector3 dimensionesPuerta = new Vector3(1.2f, 2.5f, 0.20f);

    [Header("Lista de Puertas Activas")]
    public List<PuertaConfig> puertas = new List<PuertaConfig>();

    [Header("Luces")]
    [Tooltip("Prefab de foco/spot light para la habitación")]
    public GameObject luzPrefab;
    [Tooltip("Contenedor hijo de las luces generadas")]
    public Transform contenedorLuces;
    public bool tieneLuces = true;

    private void OnValidate()
    {
        ActualizarTodo();
    }

    private void Awake()
    {
        ActualizarTodo();
    }

    public void ActualizarTodo()
    {
        ancho = Mathf.Max(1f, ancho);
        largo = Mathf.Max(1f, largo);
        alto  = Mathf.Max(1f, alto);

        ActualizarLosaInferior();
        ActualizarLosaSuperior();
        altoPared = Mathf.Max(0f, alto - grosorSuelo - grosorTecho);
        ActualizarParedes();
        ActualizarPuertasLegacy();
        ActualizarLuces();
    }

    public void ConfigurarDimensiones(float nuevoAncho, float nuevoLargo, float nuevoAlto, float nuevoGrosorPared, float nuevoGrosorSuelo, float nuevoGrosorTecho)
    {
        ancho       = nuevoAncho;
        largo       = nuevoLargo;
        alto        = nuevoAlto;
        grosorPared = nuevoGrosorPared;
        grosorSuelo = nuevoGrosorSuelo;
        grosorTecho = nuevoGrosorTecho;
        ActualizarTodo();
    }

    public void ConfigurarDimensiones(float nuevoTamano, float nuevoAlto, float nuevoGrosorPared, float nuevoGrosorSuelo, float nuevoGrosorTecho)
    {
        ConfigurarDimensiones(nuevoTamano, nuevoTamano, nuevoAlto, nuevoGrosorPared, nuevoGrosorSuelo, nuevoGrosorTecho);
    }

    public void ConfigurarPuertas(int frontal, int trasera, int izquierda, int derecha)
    {
        puertasFrontal    = frontal;
        puertasTrasera    = trasera;
        puertasIzquierda  = izquierda;
        puertasDerecha    = derecha;
        ActualizarTodo();
    }

    public void ConfigurarPuertas(bool frontal, bool trasera, bool izquierda, bool derecha)
    {
        puertasFrontal   = frontal   ? 1 : 0;
        puertasTrasera   = trasera   ? 1 : 0;
        puertasIzquierda = izquierda ? 1 : 0;
        puertasDerecha   = derecha   ? 1 : 0;
        ActualizarTodo();
    }

    // ──────────────────────────────────────────────────────────
    // LOSA INFERIOR (Suelo)
    // ──────────────────────────────────────────────────────────
    private void ActualizarLosaInferior()
    {
        if (grosorSuelo <= 0f)
        {
            // No se crea / se desactiva si existe
            if (losaInferior != null)
                losaInferior.gameObject.SetActive(false);
            return;
        }

        // Asegurar que exista
        if (losaInferior == null)
        {
            Transform encontrado = transform.Find("LosaInferior");
            if (encontrado == null) encontrado = transform.Find("Suelo");
            if (encontrado != null)
            {
                losaInferior = encontrado;
            }
            // Si sigue sin existir, no crear aquí (el prefab ya lo trae)
        }

        if (losaInferior == null) return;

        losaInferior.gameObject.SetActive(true);
        losaInferior.localScale    = new Vector3(ancho, grosorSuelo, largo);
        losaInferior.localPosition = new Vector3(0f, grosorSuelo / 2f, 0f);
    }

    // ──────────────────────────────────────────────────────────
    // LOSA SUPERIOR (Techo)
    // ──────────────────────────────────────────────────────────
    private void ActualizarLosaSuperior()
    {
        if (grosorTecho <= 0f)
        {
            if (losaSuperior != null)
                losaSuperior.gameObject.SetActive(false);
            return;
        }

        if (losaSuperior == null)
        {
            Transform encontrado = transform.Find("LosaSuperior");
            if (encontrado == null) encontrado = transform.Find("Techo");
            if (encontrado != null)
                losaSuperior = encontrado;
        }

        if (losaSuperior == null) return;

        losaSuperior.gameObject.SetActive(true);
        losaSuperior.localScale    = new Vector3(ancho, grosorTecho, largo);
        losaSuperior.localPosition = new Vector3(0f, alto - grosorTecho / 2f, 0f);
    }

    // ──────────────────────────────────────────────────────────
    // PAREDES
    // ──────────────────────────────────────────────────────────
    private void ActualizarParedes()
    {
        if (altoPared <= 0f)
        {
            if (paredFrontal   != null) paredFrontal.gameObject.SetActive(false);
            if (paredTrasera   != null) paredTrasera.gameObject.SetActive(false);
            if (paredIzquierda != null) paredIzquierda.gameObject.SetActive(false);
            if (paredDerecha   != null) paredDerecha.gameObject.SetActive(false);
            return;
        }

        float centroY    = grosorSuelo;
        float posMitadZ  = largo / 2f - grosorPared / 2f;
        float posMitadX  = ancho / 2f - grosorPared / 2f;

        // Frontal (+Z) - span en X = ancho
        ConfigurarParedTransform(paredFrontal,   new Vector3(0f, centroY, posMitadZ),  Quaternion.identity,             ancho,   altoPared, grosorPared, puertasFrontal);

        // Trasera (-Z) - span en X = ancho
        ConfigurarParedTransform(paredTrasera,   new Vector3(0f, centroY, -posMitadZ), Quaternion.Euler(0f, 180f, 0f),  ancho,   altoPared, grosorPared, puertasTrasera);

        // Izquierda (-X) - span en Z = largo - grosorPared * 2
        float largoParedLat = Mathf.Max(0.1f, largo - grosorPared * 2f);
        ConfigurarParedTransform(paredIzquierda, new Vector3(-posMitadX, centroY, 0f), Quaternion.Euler(0f, 270f, 0f),  largoParedLat, altoPared, grosorPared, puertasIzquierda);

        // Derecha (+X) - span en Z = largo - grosorPared * 2
        ConfigurarParedTransform(paredDerecha,   new Vector3(posMitadX,  centroY, 0f), Quaternion.Euler(0f, 90f, 0f),   largoParedLat, altoPared, grosorPared, puertasDerecha);
    }

    private void ConfigurarParedTransform(Transform tPared, Vector3 localPos, Quaternion localRot, float longitud, float altura, float espesor, int numPuertas)
    {
        if (tPared == null) return;

        tPared.gameObject.SetActive(true);
        tPared.localPosition = localPos;
        tPared.localRotation = localRot;

        ParedConfig pConfig = tPared.GetComponent<ParedConfig>();
        if (pConfig != null)
        {
            pConfig.Configurar(longitud, altura, espesor, numPuertas);
        }
        else
        {
            tPared.localScale    = new Vector3(longitud, altura, espesor);
            tPared.localPosition = new Vector3(localPos.x, localPos.y + altura / 2f, localPos.z);
        }
    }

    private void ActualizarPuertasLegacy()
    {
        puertas.Clear();
        PuertaConfig[] todasPuertas = GetComponentsInChildren<PuertaConfig>(true);
        foreach (var p in todasPuertas)
        {
            if (p.gameObject.activeInHierarchy) puertas.Add(p);
        }
    }

    // ──────────────────────────────────────────────────────────
    // LUCES EN CUADRÍCULA (1 foco por cada 8 m en X y en Z)
    // ──────────────────────────────────────────────────────────
    private void ActualizarLuces()
    {
        // Asegurar contenedor
        if (contenedorLuces == null)
        {
            Transform t = transform.Find("Luces");
            if (t != null)
            {
                contenedorLuces = t;
            }
            else
            {
                GameObject go = new GameObject("Luces");
                go.transform.SetParent(transform, false);
                contenedorLuces = go.transform;
            }
        }

        if (!tieneLuces || luzPrefab == null)
        {
            contenedorLuces.gameObject.SetActive(false);
            return;
        }

        contenedorLuces.gameObject.SetActive(true);

        // Cuántos focos por eje: 1 foco cada 8 m (redondeando: si tamaño < 8 → 1 foco, si 8-15 → 2 focos, etc.)
        int numZ = Mathf.Max(1, Mathf.RoundToInt(largo / 8f));
        int numX = Mathf.Max(1, Mathf.RoundToInt(ancho / 8f));
        int totalFocos = numZ * numX;

        float stepZ = largo / numZ;
        float stepX = ancho / numX;

        // Posición Y de los focos:
        // Si hay Losa Superior → cara inferior de ella; sino → parte más alta disponible
        float posY;
        if (losaSuperior != null && losaSuperior.gameObject.activeSelf && grosorTecho > 0f)
            posY = alto - grosorTecho - 0.05f;
        else
            posY = alto - 0.10f;

        // Ajustar cantidad de hijos
        int hijosExistentes = contenedorLuces.childCount;

        int foco = 0;
        for (int iz = 0; iz < numZ; iz++)
        {
            float posZ = -largo / 2f + (iz + 0.5f) * stepZ;

            for (int ix = 0; ix < numX; ix++)
            {
                float posX = -ancho / 2f + (ix + 0.5f) * stepX;

                Transform luzT;
                if (foco < hijosExistentes)
                {
                    luzT = contenedorLuces.GetChild(foco);
                    luzT.gameObject.SetActive(true);
                }
                else
                {
                    GameObject nueva = Instantiate(luzPrefab, contenedorLuces);
                    nueva.name = $"Spot Light ({foco + 1})";
                    luzT = nueva.transform;
                }

                luzT.localPosition = new Vector3(posX, posY, posZ);
                luzT.localRotation = Quaternion.Euler(90f, 0f, 0f);
                foco++;
            }
        }

        // Desactivar excedentes
        for (int i = totalFocos; i < hijosExistentes; i++)
        {
            contenedorLuces.GetChild(i).gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────────────────
    // CARGA AUTOMÁTICA DE PREFABS
    // ──────────────────────────────────────────────────────────
    private void OnEnable()
    {
        CargarPrefabsSiEsNecesario();
    }

    private void CargarPrefabsSiEsNecesario()
    {
#if UNITY_EDITOR
        if (luzPrefab == null)
            luzPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Spot Light.prefab");
        if (puertaPrefab == null)
            puertaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Puerta.prefab");
        if (paredPrefab == null)
            paredPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/BasePrefabs/ParedBase.prefab");
#endif
    }
}
