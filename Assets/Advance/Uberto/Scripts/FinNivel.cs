using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

[AddComponentMenu("SismoRoute/Fin Nivel")]
[DisallowMultipleComponent]
public sealed class FinNivel : MonoBehaviour
{
    [SerializeField] private ControladorNivel controladorNivel;
    [SerializeField] private MenuPrincipal menuPrincipal;
    [SerializeField] private GameObject canvas;
    [SerializeField] private Vector3 tamanoTrigger = new Vector3(5f, 2f, 5f);
    [SerializeField] private Vector3 centroTrigger = new Vector3(0f, 1f, 0f);

    private BoxCollider triggerZona;
    private bool resultadoMostrado;

    private void Awake()
    {
        triggerZona = GetComponent<BoxCollider>();
        triggerZona.isTrigger = true;
        triggerZona.size = tamanoTrigger;
        triggerZona.center = centroTrigger;

        if (controladorNivel == null)
            controladorNivel = GetComponent<ControladorNivel>();

        if (canvas == null)
        {
            GameObject encontrado = GameObject.Find("Canvas");
            if (encontrado != null)
                canvas = encontrado;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (resultadoMostrado || !TieneCharacterController(other))
            return;

        if (GameObject.Find("MenuFinNivel") != null)
        {
            resultadoMostrado = true;
            return;
        }

        resultadoMostrado = true;
        MostrarResultado();
    }

    private static bool TieneCharacterController(Collider other)
    {
        return other.GetComponent<CharacterController>() != null ||
               other.GetComponentInParent<CharacterController>() != null;
    }

    private void MostrarResultado()
    {
        float tiempoFinal = controladorNivel != null ? controladorNivel.TiempoSismo : 0f;
        int segundos = Mathf.Max(0, Mathf.FloorToInt(tiempoFinal));
        int puntos = Mathf.Max(0, 200 - segundos);

        if (controladorNivel != null)
            controladorNivel.SismoActivo = false;

        if (menuPrincipal != null)
        {
            menuPrincipal.CerrarMenu();
            menuPrincipal.enabled = false;
        }

        if (canvas == null)
        {
            Debug.LogError("FinNivel requiere un Canvas para mostrar el resultado.", this);
            return;
        }

        PrepararEntradaUI();

        Canvas canvasComponent = canvas.GetComponent<Canvas>();
        if (canvasComponent != null)
            canvasComponent.sortingOrder = Mathf.Max(canvasComponent.sortingOrder, 100);

        GameObject menu = new GameObject("MenuFinNivel", typeof(RectTransform), typeof(Image));
        menu.transform.SetParent(canvas.transform, false);
        menu.transform.SetAsLastSibling();

        RectTransform menuRect = menu.GetComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        Image fondo = menu.GetComponent<Image>();
        fondo.color = new Color(0.015f, 0.025f, 0.045f, 0.82f);

        GameObject panel = new GameObject("PanelResultado", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(menu.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600f, 570f);

        Image panelFondo = panel.GetComponent<Image>();
        panelFondo.color = new Color(0.075f, 0.105f, 0.15f, 0.98f);

        CrearTexto("Titulo", "Evacuación completada", panel.transform, 34,
            new Vector2(0f, 205f), new Vector2(520f, 70f), FontStyle.Bold);

        string resumen = string.Format(
            "Tiempo de finalización: {0:00}:{1:00}\nPuntos: {2}",
            segundos / 60,
            segundos % 60,
            puntos);
        CrearTexto("Resumen", resumen, panel.transform, 25,
            new Vector2(0f, 95f), new Vector2(520f, 100f), FontStyle.Normal);

        CrearBoton("BotonReiniciar", "Reiniciar", panel.transform, -20f, ReiniciarNivel);
        CrearBoton("BotonSalir", "Salir", panel.transform, -105f, SalirDelJuego, true);
        CrearBoton("BotonNuevoEdificio", "Reiniciar con nuevo edificio", panel.transform, -190f, null);
    }

    private void PrepararEntradaUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        foreach (BaseInputModule inputModule in eventSystem.GetComponents<BaseInputModule>())
        {
            if (!(inputModule is XRUIInputModule))
                inputModule.enabled = false;
        }

        XRUIInputModule xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
        if (xrInputModule == null)
            xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();

        xrInputModule.enableXRInput = true;

        if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvas.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    private static void CrearBoton(string nombre, string etiqueta, Transform padre, float posicionY,
        UnityEngine.Events.UnityAction accion, bool botonSalir = false)
    {
        GameObject botonObject = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        botonObject.transform.SetParent(padre, false);

        RectTransform botonRect = botonObject.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(0.5f, 0.5f);
        botonRect.anchorMax = new Vector2(0.5f, 0.5f);
        botonRect.pivot = new Vector2(0.5f, 0.5f);
        botonRect.sizeDelta = new Vector2(430f, 68f);
        botonRect.anchoredPosition = new Vector2(0f, posicionY);

        Image imagen = botonObject.GetComponent<Image>();
        imagen.color = botonSalir ? new Color(0.58f, 0.16f, 0.18f, 1f) : new Color(0.12f, 0.36f, 0.58f, 1f);

        Button boton = botonObject.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.transition = Selectable.Transition.ColorTint;
        boton.colors = new ColorBlock
        {
            normalColor = imagen.color,
            highlightedColor = botonSalir ? new Color(0.75f, 0.22f, 0.23f, 1f) : new Color(0.2f, 0.5f, 0.75f, 1f),
            pressedColor = new Color(0.08f, 0.22f, 0.35f, 1f),
            selectedColor = imagen.color,
            disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f,
        };

        if (accion != null)
            boton.onClick.AddListener(accion);

        CrearTexto("Etiqueta", etiqueta, botonObject.transform, 23,
            Vector2.zero, new Vector2(410f, 62f), FontStyle.Bold);
    }

    private static Text CrearTexto(string nombre, string contenido, Transform padre, int tamano,
        Vector2 posicion, Vector2 dimensiones, FontStyle estilo)
    {
        GameObject textoObject = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        textoObject.transform.SetParent(padre, false);

        RectTransform textoRect = textoObject.GetComponent<RectTransform>();
        textoRect.anchorMin = new Vector2(0.5f, 0.5f);
        textoRect.anchorMax = new Vector2(0.5f, 0.5f);
        textoRect.pivot = new Vector2(0.5f, 0.5f);
        textoRect.sizeDelta = dimensiones;
        textoRect.anchoredPosition = posicion;

        Text texto = textoObject.GetComponent<Text>();
        texto.text = contenido;
        texto.font = ObtenerFuenteUI();
        texto.fontSize = tamano;
        texto.fontStyle = estilo;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Truncate;
        return texto;
    }

    private static Font ObtenerFuenteUI()
    {
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return fuente != null ? fuente : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void ReiniciarNivel()
    {
        Scene escenaActual = SceneManager.GetActiveScene();
        if (escenaActual.buildIndex >= 0)
            SceneManager.LoadScene(escenaActual.buildIndex);
        else
            SceneManager.LoadScene(escenaActual.name);
    }

    private static void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnValidate()
    {
        tamanoTrigger.x = Mathf.Max(0.1f, tamanoTrigger.x);
        tamanoTrigger.y = Mathf.Max(0.1f, tamanoTrigger.y);
        tamanoTrigger.z = Mathf.Max(0.1f, tamanoTrigger.z);

        if (triggerZona == null)
            triggerZona = GetComponent<BoxCollider>();

        if (triggerZona != null)
        {
            triggerZona.isTrigger = true;
            triggerZona.size = tamanoTrigger;
            triggerZona.center = centroTrigger;
        }
    }
}
