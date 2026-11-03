using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class ControladorNivel : MonoBehaviour
{
    [Header("Pantalla de carga")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject imagenCargando;
    [SerializeField, Min(0f)] private float esperaAntesDeDesvanecer = 3f;
    [SerializeField, Min(0.01f)] private float duracionDesvanecimiento = 1f;

    [Header("Sismo")]
    [SerializeField] private ControladorInicio controladorInicio;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform destinoSismo;
    [SerializeField] private Text cronometro;
    [SerializeField] private bool sismoActivo;
    [SerializeField, Min(0f)] private float intensidadTraslacion = 0.035f;
    [SerializeField, Min(0f)] private float intensidadRotacion = 0.8f;
    [SerializeField, Min(0.1f)] private float frecuenciaSismo = 8f;

    private CanvasGroup grupoImagenCargando;
    private Vector3 desplazamientoSismoAplicado;
    private Quaternion rotacionSismoAplicada = Quaternion.identity;
    private bool temblorAplicado;
    private bool sismoIniciado;
    private float tiempoSismo;

    public bool SismoActivo
    {
        get => sismoActivo;
        set => sismoActivo = value;
    }

    public bool SismoIniciado => sismoIniciado;
    public float TiempoSismo => tiempoSismo;

    private void Awake()
    {
        if (canvas == null)
        {
            GameObject encontrado = GameObject.Find("Canvas");
            if (encontrado != null)
                canvas = encontrado;
        }

        if (imagenCargando == null && canvas != null)
        {
            Transform encontrado = canvas.transform.Find("ImagenCargando");
            if (encontrado != null)
                imagenCargando = encontrado.gameObject;
        }

        if (xrOrigin == null)
        {
            GameObject encontrado = GameObject.Find("XR Origin (XR Rig)");
            if (encontrado != null)
                xrOrigin = encontrado.transform;
        }

        if (destinoSismo == null)
        {
            Transform encontrado = transform.Find("DestinoSismo");
            if (encontrado != null)
                destinoSismo = encontrado;
        }

        if (cronometro == null && canvas != null)
        {
            Transform encontrado = canvas.transform.Find("Cronometro");
            if (encontrado != null)
                cronometro = encontrado.GetComponent<Text>();
        }

        if (canvas != null)
            canvas.SetActive(true);

        if (imagenCargando != null)
        {
            imagenCargando.SetActive(true);
            grupoImagenCargando = imagenCargando.GetComponent<CanvasGroup>();
            if (grupoImagenCargando == null)
                grupoImagenCargando = imagenCargando.AddComponent<CanvasGroup>();

            grupoImagenCargando.alpha = 1f;
        }

        if (cronometro != null)
        {
            cronometro.text = "00:00";
            cronometro.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        if (imagenCargando != null && grupoImagenCargando != null)
            StartCoroutine(DesvanecerImagenCargando());
    }

    private void Update()
    {
        if((int)controladorInicio.EstadoActual == 3)
        {
            IniciarSismo();
        }
        if (sismoActivo)
        {
            tiempoSismo += Time.deltaTime;
            ActualizarCronometro();
        }

        if (xrOrigin == null)
            return;

        RetirarTemblorAnterior();
        if (!sismoActivo)
            return;

        float tiempo = Time.time * frecuenciaSismo;
        Vector3 desplazamiento = new Vector3(
            RuidoCentrado(tiempo, 0.17f),
            RuidoCentrado(0.43f, tiempo),
            RuidoCentrado(tiempo, 7.31f)) * intensidadTraslacion;

        Vector3 angulos = new Vector3(
            RuidoCentrado(tiempo + 13.2f, 0.29f),
            RuidoCentrado(0.71f, tiempo + 4.8f),
            RuidoCentrado(tiempo + 27.4f, 0.53f)) * intensidadRotacion;

        desplazamientoSismoAplicado = desplazamiento;
        rotacionSismoAplicada = Quaternion.Euler(angulos);
        xrOrigin.localPosition += desplazamientoSismoAplicado;
        xrOrigin.localRotation = rotacionSismoAplicada * xrOrigin.localRotation;
        temblorAplicado = true;
    }

    public void IniciarSismo()
    {
        if (sismoIniciado)
            return;

        sismoIniciado = true;
        if (xrOrigin != null && destinoSismo != null)
        {
            RetirarTemblorAnterior();
            xrOrigin.SetPositionAndRotation(destinoSismo.position, destinoSismo.rotation);
            Physics.SyncTransforms();
        }
        else if (destinoSismo == null)
        {
            Debug.LogWarning("Asigna un DestinoSismo para teletransportar el XR Origin.", this);
        }

        tiempoSismo = 0f;
        if (cronometro != null)
            cronometro.text = "00:00";

        sismoActivo = true;
    }

    private void ActualizarCronometro()
    {
        if (cronometro == null)
            return;

        int segundosTotales = Mathf.FloorToInt(tiempoSismo);
        cronometro.text = string.Format("{0:00}:{1:00}", segundosTotales / 60, segundosTotales % 60);
    }

    private IEnumerator DesvanecerImagenCargando()
    {
        yield return new WaitForSecondsRealtime(esperaAntesDeDesvanecer);

        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionDesvanecimiento)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime;
            grupoImagenCargando.alpha = Mathf.Lerp(1f, 0f, tiempoTranscurrido / duracionDesvanecimiento);
            yield return null;
        }

        grupoImagenCargando.alpha = 0f;
        imagenCargando.SetActive(false);
    }

    private void RetirarTemblorAnterior()
    {
        if (!temblorAplicado)
            return;

        xrOrigin.localPosition -= desplazamientoSismoAplicado;
        xrOrigin.localRotation = Quaternion.Inverse(rotacionSismoAplicada) * xrOrigin.localRotation;
        desplazamientoSismoAplicado = Vector3.zero;
        rotacionSismoAplicada = Quaternion.identity;
        temblorAplicado = false;
    }

    private static float RuidoCentrado(float x, float y)
    {
        return (Mathf.PerlinNoise(x, y) - 0.5f) * 2f;
    }

    private void OnDisable()
    {
        if (xrOrigin != null)
            RetirarTemblorAnterior();
    }

    private void OnValidate()
    {
        esperaAntesDeDesvanecer = Mathf.Max(0f, esperaAntesDeDesvanecer);
        duracionDesvanecimiento = Mathf.Max(0.01f, duracionDesvanecimiento);
        intensidadTraslacion = Mathf.Max(0f, intensidadTraslacion);
        intensidadRotacion = Mathf.Max(0f, intensidadRotacion);
        frecuenciaSismo = Mathf.Max(0.1f, frecuenciaSismo);
    }
}
