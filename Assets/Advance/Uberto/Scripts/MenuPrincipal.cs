using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

[AddComponentMenu("SismoRoute/Menu Principal")]
[DisallowMultipleComponent]
public sealed class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private ControladorNivel controladorNivel;
    [SerializeField] private GameObject canvas;

    private InputAction alternarMenuAction;
    private GameObject menuRoot;
    private Button botonIniciarSismo;

    private void Awake()
    {
        if (controladorNivel == null)
            controladorNivel = GetComponent<ControladorNivel>();

        if (canvas == null)
        {
            GameObject encontrado = GameObject.Find("Canvas");
            if (encontrado != null)
                canvas = encontrado;
        }

        alternarMenuAction = new InputAction(
            "Alternar Menu Principal",
            InputActionType.Button,
            "<XRController>{RightHand}/secondaryButton");

        PrepararEntradaUI();
        CrearMenu();
    }

    private void OnEnable()
    {
        alternarMenuAction?.Enable();
    }

    private void OnDisable()
    {
        alternarMenuAction?.Disable();
    }

    private void Update()
    {
        if (alternarMenuAction != null && alternarMenuAction.WasPressedThisFrame())
            AlternarMenu();
    }

    private void OnDestroy()
    {
        if (alternarMenuAction == null)
            return;

        alternarMenuAction.Disable();
        alternarMenuAction.Dispose();
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

        if (canvas != null && canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvas.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    private void CrearMenu()
    {
        if (canvas == null)
        {
            Debug.LogError("MenuPrincipal requiere un Canvas asignado.", this);
            return;
        }

        menuRoot = new GameObject("Menu Principal", typeof(RectTransform), typeof(Image));
        menuRoot.transform.SetParent(canvas.transform, false);
        menuRoot.transform.SetAsLastSibling();

        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image fondo = menuRoot.GetComponent<Image>();
        fondo.color = new Color(0.015f, 0.025f, 0.045f, 0.78f);
        fondo.raycastTarget = true;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(menuRoot.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 460f);

        Image panelFondo = panel.GetComponent<Image>();
        panelFondo.color = new Color(0.075f, 0.105f, 0.15f, 0.98f);
        panelFondo.raycastTarget = true;

        CrearTexto("Titulo", "MENÚ PRINCIPAL", panel.transform, 30, new Vector2(0f, 165f), new Vector2(420f, 60f), FontStyle.Bold);

        botonIniciarSismo = CrearBoton("BotonIniciarSismo", "Iniciar Sismo", panel.transform, 70f, IniciarSismo);
        CrearBoton("BotonCerrarMenu", "Cerrar Menú", panel.transform, -15f, CerrarMenu);
        CrearBoton("BotonSalir", "Salir", panel.transform, -100f, SalirDelJuego, true);

        menuRoot.SetActive(false);
    }

    private Button CrearBoton(string nombre, string etiqueta, Transform padre, float posicionY, UnityEngine.Events.UnityAction accion, bool botonSalir = false)
    {
        GameObject botonObject = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        botonObject.transform.SetParent(padre, false);

        RectTransform botonRect = botonObject.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(0.5f, 0.5f);
        botonRect.anchorMax = new Vector2(0.5f, 0.5f);
        botonRect.pivot = new Vector2(0.5f, 0.5f);
        botonRect.sizeDelta = new Vector2(360f, 64f);
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
        boton.onClick.AddListener(accion);

        CrearTexto("Etiqueta", etiqueta, botonObject.transform, 24, Vector2.zero, new Vector2(340f, 58f), FontStyle.Bold);
        return boton;
    }

    private static Text CrearTexto(string nombre, string contenido, Transform padre, int tamano, Vector2 posicion, Vector2 dimensiones, FontStyle estilo)
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

    private void AlternarMenu()
    {
        if (menuRoot == null)
            return;

        bool mostrar = !menuRoot.activeSelf;
        menuRoot.SetActive(mostrar);
        if (mostrar)
        {
            menuRoot.transform.SetAsLastSibling();
            if (botonIniciarSismo != null && controladorNivel != null)
                botonIniciarSismo.interactable = !controladorNivel.SismoIniciado;
        }
    }

    private void IniciarSismo()
    {
        if (controladorNivel != null)
            controladorNivel.IniciarSismo();

        CerrarMenu();
    }

    public void CerrarMenu()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    private static void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
