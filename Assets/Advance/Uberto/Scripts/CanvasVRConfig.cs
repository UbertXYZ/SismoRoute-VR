using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("SismoRoute/Canvas VR Config")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class CanvasVRConfig : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform camaraXR;
    [SerializeField, Min(0.25f)] private float distanciaCamara = 1.5f;
    [SerializeField, Min(0.0001f)] private float escalaCanvas = 0.001f;
    [SerializeField] private Vector2 tamanoReferencia = new Vector2(800f, 600f);

    private RectTransform rectTransformCanvas;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (camaraXR == null && Camera.main != null)
            camaraXR = Camera.main.transform;

        if (canvas == null || camaraXR == null)
        {
            Debug.LogError("CanvasVRConfig requiere un Canvas y la cámara XR.", this);
            enabled = false;
            return;
        }

        ConfigurarCanvas();
    }

    private void LateUpdate()
    {
        if (canvas == null || camaraXR == null)
            return;

        if (rectTransformCanvas.parent != camaraXR)
            rectTransformCanvas.SetParent(camaraXR, false);

        AplicarPoseFrontal();
    }

    private void ConfigurarCanvas()
    {
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camaraXR.GetComponent<Camera>();
        if (canvas.worldCamera == null)
            canvas.worldCamera = camaraXR.GetComponentInChildren<Camera>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);

        rectTransformCanvas = canvas.GetComponent<RectTransform>();
        if (rectTransformCanvas == null)
        {
            Debug.LogError("El Canvas necesita un RectTransform.", this);
            enabled = false;
            return;
        }

        if (rectTransformCanvas.sizeDelta.x <= 0f || rectTransformCanvas.sizeDelta.y <= 0f)
        {
            CanvasScaler escalador = canvas.GetComponent<CanvasScaler>();
            if (escalador != null && escalador.referenceResolution.x > 0f && escalador.referenceResolution.y > 0f)
                rectTransformCanvas.sizeDelta = escalador.referenceResolution;
            else
                rectTransformCanvas.sizeDelta = tamanoReferencia;
        }

        rectTransformCanvas.SetParent(camaraXR, false);
        rectTransformCanvas.localScale = Vector3.one * escalaCanvas;
        AplicarPoseFrontal();
    }

    private void AplicarPoseFrontal()
    {
        rectTransformCanvas.localPosition = new Vector3(0f, 0f, distanciaCamara);
        rectTransformCanvas.localRotation = Quaternion.identity;
    }

    private void OnValidate()
    {
        distanciaCamara = Mathf.Max(0.25f, distanciaCamara);
        escalaCanvas = Mathf.Max(0.0001f, escalaCanvas);
        tamanoReferencia.x = Mathf.Max(1f, tamanoReferencia.x);
        tamanoReferencia.y = Mathf.Max(1f, tamanoReferencia.y);
    }
}
