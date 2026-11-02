using UnityEngine;

[ExecuteAlways]
public class ParedConfig : MonoBehaviour
{
    [Header("Dimensiones de la Pared")]
    [Tooltip("Largo o ancho horizontal de la pared (eje X local)")]
    [SerializeField] public float largo = 8f;

    [Tooltip("Altura de la pared (eje Y local)")]
    [SerializeField] public float alto = 4.25f;

    [Tooltip("Grosor o profundidad de la pared (eje Z local)")]
    [SerializeField] public float grosor = 0.20f;

    // Propiedad de compatibilidad con prefabs y scripts previos
    public float ancho
    {
        get => grosor;
        set => grosor = value;
    }

    [Header("Configuración de Puertas")]
    [Range(0, 2)]
    public int cantidadPuertas = 0;

    [Tooltip("Activa 1 puerta (compatibilidad)")]
    public bool tienePuerta = false;

    [Tooltip("Activa 2 puertas (compatibilidad)")]
    public bool dosPuertas = false;

    public float anchoPuerta = 1.2f;
    public float altoPuerta = 2.5f;
    public float grosorPuerta = 0.1f;

    [Header("Prefabs")]
    [SerializeField] public GameObject paredBasePrefab;
    [SerializeField] public GameObject paredPrefab; // Alias compatibilidad
    [SerializeField] public GameObject puertaPrefab;

    [Header("Partes de la Pared")]
    [SerializeField] public Transform paredOrigen;
    [SerializeField] public Transform paredIzquierda;
    [SerializeField] public Transform paredDerecha;
    [SerializeField] public Transform paredCentro;

    [Header("Referencias de Puertas")]
    [SerializeField] public PuertaConfig puerta1;
    [SerializeField] public PuertaConfig puerta2;

    // Campos de compatibilidad previa
    public Transform parteIzquierda => paredIzquierda;
    public Transform parteSuperior => paredOrigen;
    public Transform parteDerecha => paredDerecha;
    public PuertaConfig puertaConfig => puerta1;
    public bool tieneVentana = false;

    private void OnValidate()
    {
        SincronizarBools();
        ActualizarPared();
    }

    private void Awake()
    {
        SincronizarBools();
        ActualizarPared();
    }

    private void SincronizarBools()
    {
        if (dosPuertas)
        {
            cantidadPuertas = 2;
            tienePuerta = true;
        }
        else if (tienePuerta && cantidadPuertas == 0)
        {
            cantidadPuertas = 1;
        }
        else if (cantidadPuertas == 2)
        {
            dosPuertas = true;
            tienePuerta = true;
        }
        else if (cantidadPuertas == 1)
        {
            dosPuertas = false;
            tienePuerta = true;
        }
        else
        {
            dosPuertas = false;
            tienePuerta = false;
        }
    }

    public void Configurar(float nuevoLargo, float nuevoAlto, float nuevoGrosor, int numPuertas = 0)
    {
        largo = nuevoLargo;
        alto = nuevoAlto;
        grosor = nuevoGrosor;
        cantidadPuertas = Mathf.Clamp(numPuertas, 0, 2);
        SincronizarBools();
        ActualizarPared();
    }

    public void ConfigurarPuertas(int numPuertas)
    {
        cantidadPuertas = Mathf.Clamp(numPuertas, 0, 2);
        SincronizarBools();
        ActualizarPared();
    }

    [ContextMenu("Actualizar Pared")]
    public void ActualizarPared()
    {
        CargarPrefabsSiEsNecesario();
        AsegurarPartes();

        largo = Mathf.Max(0.5f, largo);
        alto = Mathf.Max(0.5f, alto);
        grosor = Mathf.Max(0.01f, grosor);
        altoPuerta = Mathf.Min(altoPuerta, alto - 0.1f);

        if (cantidadPuertas == 0)
        {
            ActualizarSinPuerta();
        }
        else if (cantidadPuertas == 1)
        {
            ActualizarUnaPuerta();
        }
        else if (cantidadPuertas >= 2)
        {
            ActualizarDosPuertas();
        }
    }

    private void ActualizarSinPuerta()
    {
        // Solo ParedOrigen abarca toda la pared
        if (paredOrigen != null)
        {
            paredOrigen.gameObject.SetActive(true);
            paredOrigen.localScale = new Vector3(largo, alto, grosor);
            paredOrigen.localPosition = new Vector3(0f, alto / 2f, 0f);
        }

        if (paredIzquierda != null) paredIzquierda.gameObject.SetActive(false);
        if (paredDerecha != null) paredDerecha.gameObject.SetActive(false);
        if (paredCentro != null) paredCentro.gameObject.SetActive(false);
        if (puerta1 != null) puerta1.gameObject.SetActive(false);
        if (puerta2 != null) puerta2.gameObject.SetActive(false);
    }

    private void ActualizarUnaPuerta()
    {
        float anchoLateral = Mathf.Max(0.05f, (largo - anchoPuerta) / 2f);
        float altoSuperior = Mathf.Max(0f, alto - altoPuerta);

        // 1. Puerta en el centro
        if (puerta1 != null)
        {
            puerta1.gameObject.SetActive(true);
            puerta1.transform.localPosition = Vector3.zero;
            puerta1.transform.localRotation = Quaternion.identity;
            puerta1.ConfigurarDimensiones(anchoPuerta, altoPuerta, grosorPuerta);
        }
        if (puerta2 != null) puerta2.gameObject.SetActive(false);

        // 2. ParedOrigen se va arriba de la puerta
        if (paredOrigen != null)
        {
            if (altoSuperior > 0.01f)
            {
                paredOrigen.gameObject.SetActive(true);
                paredOrigen.localScale = new Vector3(largo, altoSuperior, grosor);
                paredOrigen.localPosition = new Vector3(0f, altoPuerta + altoSuperior / 2f, 0f);
            }
            else
            {
                paredOrigen.gameObject.SetActive(false);
            }
        }

        // 3. ParedIzquierda
        if (paredIzquierda != null)
        {
            paredIzquierda.gameObject.SetActive(true);
            paredIzquierda.localScale = new Vector3(anchoLateral, altoPuerta, grosor);
            float posX = -largo / 2f + anchoLateral / 2f;
            paredIzquierda.localPosition = new Vector3(posX, altoPuerta / 2f, 0f);
        }

        // 4. ParedDerecha
        if (paredDerecha != null)
        {
            paredDerecha.gameObject.SetActive(true);
            paredDerecha.localScale = new Vector3(anchoLateral, altoPuerta, grosor);
            float posX = largo / 2f - anchoLateral / 2f;
            paredDerecha.localPosition = new Vector3(posX, altoPuerta / 2f, 0f);
        }

        if (paredCentro != null) paredCentro.gameObject.SetActive(false);
    }

    private void ActualizarDosPuertas()
    {
        // "Cuando hay 2, estas están a 2 metros de distancia de su limite respectivo y en medio del grosor de la pared."
        float anchoLateral = 1f;
        float altoSuperior = Mathf.Max(0f, alto - altoPuerta);

        // Si la pared es muy corta para 2 metros a cada lado + puertas, ajustamos proporcionalmente
        if (largo < (anchoLateral * 2f + anchoPuerta * 2f))
        {
            anchoLateral = Mathf.Max(0.2f, (largo - anchoPuerta * 2f) / 3f);
        }

        float posXPuerta1 = -largo / 2f + anchoLateral + anchoPuerta / 2f;
        float posXPuerta2 = largo / 2f - anchoLateral - anchoPuerta / 2f;

        // 1. Puertas
        if (puerta1 != null)
        {
            puerta1.gameObject.SetActive(true);
            puerta1.transform.localPosition = new Vector3(posXPuerta1, 0f, 0f);
            puerta1.transform.localRotation = Quaternion.identity;
            puerta1.ConfigurarDimensiones(anchoPuerta, altoPuerta, grosorPuerta);
        }

        if (puerta2 != null)
        {
            puerta2.gameObject.SetActive(true);
            puerta2.transform.localPosition = new Vector3(posXPuerta2, 0f, 0f);
            puerta2.transform.localRotation = Quaternion.identity;
            puerta2.ConfigurarDimensiones(anchoPuerta, altoPuerta, grosorPuerta);
        }

        // 2. Pared Origen (superior)
        if (paredOrigen != null)
        {
            if (altoSuperior > 0.01f)
            {
                paredOrigen.gameObject.SetActive(true);
                paredOrigen.localScale = new Vector3(largo, altoSuperior, grosor);
                paredOrigen.localPosition = new Vector3(0f, altoPuerta + altoSuperior / 2f, 0f);
            }
            else
            {
                paredOrigen.gameObject.SetActive(false);
            }
        }

        // 3. ParedIzquierda (cubre los 2m desde el borde izquierdo)
        if (paredIzquierda != null)
        {
            paredIzquierda.gameObject.SetActive(true);
            paredIzquierda.localScale = new Vector3(anchoLateral, altoPuerta, grosor);
            float posX = -largo / 2f + anchoLateral / 2f;
            paredIzquierda.localPosition = new Vector3(posX, altoPuerta / 2f, 0f);
        }

        // 4. ParedDerecha (cubre los 2m desde el borde derecho)
        if (paredDerecha != null)
        {
            paredDerecha.gameObject.SetActive(true);
            paredDerecha.localScale = new Vector3(anchoLateral, altoPuerta, grosor);
            float posX = largo / 2f - anchoLateral / 2f;
            paredDerecha.localPosition = new Vector3(posX, altoPuerta / 2f, 0f);
        }

        // 5. ParedCentro (entre ambas puertas)
        float finPuerta1 = posXPuerta1 + anchoPuerta / 2f;
        float inicioPuerta2 = posXPuerta2 - anchoPuerta / 2f;
        float anchoCentro = Mathf.Max(0.01f, inicioPuerta2 - finPuerta1);

        if (paredCentro != null)
        {
            if (anchoCentro > 0.01f)
            {
                paredCentro.gameObject.SetActive(true);
                paredCentro.localScale = new Vector3(anchoCentro, altoPuerta, grosor);
                paredCentro.localPosition = new Vector3(0f, altoPuerta / 2f, 0f);
            }
            else
            {
                paredCentro.gameObject.SetActive(false);
            }
        }
    }

    private void AsegurarPartes()
    {
        GameObject prefabPared = paredBasePrefab != null ? paredBasePrefab : paredPrefab;

        if (paredOrigen == null)
        {
            Transform t = transform.Find("ParedOrigen");
            if (t == null) t = transform.Find("ParedBase");
            if (t == null) t = transform.Find("ParedSuperior");
            if (t != null) paredOrigen = t;
            else if (prefabPared != null)
            {
                GameObject go = Instantiate(prefabPared, transform);
                go.name = "ParedOrigen";
                paredOrigen = go.transform;
            }
        }

        if (paredIzquierda == null)
        {
            Transform t = transform.Find("ParedIzquierda");
            if (t != null) paredIzquierda = t;
            else if (prefabPared != null)
            {
                GameObject go = Instantiate(prefabPared, transform);
                go.name = "ParedIzquierda";
                paredIzquierda = go.transform;
            }
        }

        if (paredDerecha == null)
        {
            Transform t = transform.Find("ParedDerecha");
            if (t != null) paredDerecha = t;
            else if (prefabPared != null)
            {
                GameObject go = Instantiate(prefabPared, transform);
                go.name = "ParedDerecha";
                paredDerecha = go.transform;
            }
        }

        if (paredCentro == null)
        {
            Transform t = transform.Find("ParedCentro");
            if (t == null) t = transform.Find("ParedICentro");
            if (t != null) paredCentro = t;
            else if (prefabPared != null)
            {
                GameObject go = Instantiate(prefabPared, transform);
                go.name = "ParedCentro";
                paredCentro = go.transform;
            }
        }

        if (puerta1 == null)
        {
            PuertaConfig p = transform.Find("Puerta_1")?.GetComponent<PuertaConfig>();
            if (p == null) p = transform.Find("ParedPuerta")?.GetComponent<PuertaConfig>();
            if (p != null) puerta1 = p;
            else if (puertaPrefab != null)
            {
                GameObject go = Instantiate(puertaPrefab, transform);
                go.name = "Puerta_1";
                puerta1 = go.GetComponent<PuertaConfig>();
            }
        }

        if (puerta2 == null)
        {
            PuertaConfig p = transform.Find("Puerta_2")?.GetComponent<PuertaConfig>();
            if (p != null) puerta2 = p;
            else if (puertaPrefab != null)
            {
                GameObject go = Instantiate(puertaPrefab, transform);
                go.name = "Puerta_2";
                puerta2 = go.GetComponent<PuertaConfig>();
            }
        }
    }

    private void CargarPrefabsSiEsNecesario()
    {
#if UNITY_EDITOR
        if (paredBasePrefab == null && paredPrefab == null)
        {
            paredBasePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/BasePrefabs/ParedBase.prefab");
            paredPrefab = paredBasePrefab;
        }
        if (puertaPrefab == null)
        {
            puertaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Advance/Uberto/Prefabs/Puerta.prefab");
        }
#endif
    }
}
