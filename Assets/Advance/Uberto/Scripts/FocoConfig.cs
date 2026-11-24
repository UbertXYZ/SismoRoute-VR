using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FocoConfig : MonoBehaviour
{
    public enum EstadoFoco
    {
        Normal,
        Averiado
    }

    private sealed class ObjetivoEmision
    {
        public Renderer renderer;
        public int indiceMaterial;
        public MaterialPropertyBlock propiedades;
    }

    [Header("Luz de techo")]
    [SerializeField] private Light luzPrincipal;
    [SerializeField, Min(0f)] private float intensidadNormal = 10f;
    [SerializeField, Min(0.1f)] private float alcanceNormal = 10f;
    [SerializeField, Range(20f, 150f)] private float anguloFoco = 105f;
    [SerializeField] private Color colorNormal = new Color(1f, 0.96f, 0.88f, 1f);
    [SerializeField] private Color emisionNormal = new Color(0.72f, 0.66f, 0.52f, 1f);

    [Header("Avería")]
    [SerializeField, Min(0.01f)] private float duracionPulsoMinima = 0.025f;
    [SerializeField, Min(0.01f)] private float duracionPulsoMaxima = 0.12f;
    [SerializeField, Min(0.01f)] private float intervaloPulsoMinimo = 0.04f;
    [SerializeField, Min(0.01f)] private float intervaloPulsoMaximo = 0.24f;
    [SerializeField, Min(0.1f)] private float pausaOscuraMinima = 1.1f;
    [SerializeField, Min(0.1f)] private float pausaOscuraMaxima = 4.8f;
    [SerializeField] private ParticleSystem chispas;

    private readonly List<ObjetivoEmision> objetivosEmision = new List<ObjetivoEmision>();
    private Coroutine rutinaAveria;

    public EstadoFoco EstadoActual { get; private set; } = EstadoFoco.Normal;
    public bool EstaAveriado => EstadoActual == EstadoFoco.Averiado;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        ResolverReferencias();
        ConfigurarParticulas();
        CachearMaterialesEmisivos();
        EstablecerNormal();
    }

    private void Start()
    {
        // Garantiza el estado seguro inicial aunque otro componente haya cambiado el foco.
        EstablecerNormal();
    }

    public void EstablecerNormal()
    {
        DetenerRutinaAveria();
        EstadoActual = EstadoFoco.Normal;
        AplicarSalidaLuminosa(true, intensidadNormal, emisionNormal);

        if (chispas != null)
            chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void EstablecerAveriado()
    {
        if (EstaAveriado)
            return;

        EstadoActual = EstadoFoco.Averiado;
        AplicarSalidaLuminosa(false, 0f, Color.black);
        DetenerRutinaAveria();
        rutinaAveria = StartCoroutine(RutinaAveria());
    }

    private IEnumerator RutinaAveria()
    {
        while (EstaAveriado)
        {
            int cantidadPulsos = Random.Range(1, 5);
            for (int i = 0; i < cantidadPulsos && EstaAveriado; i++)
            {
                float factor = Random.Range(0.35f, 1.25f);
                Color emision = emisionNormal * factor;
                AplicarSalidaLuminosa(true, intensidadNormal * factor, emision);

                if (Random.value < 0.42f)
                    EmitirChispas();

                yield return new WaitForSecondsRealtime(
                    Random.Range(duracionPulsoMinima, duracionPulsoMaxima));

                AplicarSalidaLuminosa(false, 0f, Color.black);
                yield return new WaitForSecondsRealtime(
                    Random.Range(intervaloPulsoMinimo, intervaloPulsoMaximo));
            }

            AplicarSalidaLuminosa(false, 0f, Color.black);
            if (Random.value < 0.55f)
                EmitirChispas();

            // Las pausas oscuras largas rompen el ritmo y hacen que la avería se sienta irregular.
            yield return new WaitForSecondsRealtime(
                Random.Range(pausaOscuraMinima, pausaOscuraMaxima));
        }
    }

    private void EmitirChispas()
    {
        if (chispas == null)
            return;

        if (!chispas.gameObject.activeSelf)
            chispas.gameObject.SetActive(true);

        if (!chispas.isPlaying)
            chispas.Play(true);

        chispas.Emit(Random.Range(3, 8));
    }

    private void AplicarSalidaLuminosa(bool encendida, float intensidad, Color emision)
    {
        if (luzPrincipal != null)
        {
            luzPrincipal.enabled = encendida;
            luzPrincipal.color = colorNormal;
            luzPrincipal.intensity = Mathf.Max(0f, intensidad);
            luzPrincipal.range = alcanceNormal;
            luzPrincipal.spotAngle = anguloFoco;
            luzPrincipal.innerSpotAngle = Mathf.Min(anguloFoco, 78f);
            luzPrincipal.useColorTemperature = false;
        }

        AplicarEmision(encendida ? emision : Color.black);
    }

    private void ResolverReferencias()
    {
        if (luzPrincipal == null)
            luzPrincipal = GetComponentInChildren<Light>(true);

        if (chispas == null)
            chispas = GetComponentInChildren<ParticleSystem>(true);
    }

    private void ConfigurarParticulas()
    {
        if (chispas == null)
            return;

        ParticleSystem.MainModule main = chispas.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.96f, 0.72f, 1f), new Color(1f, 0.28f, 0.035f, 1f));
        main.gravityModifier = 1.6f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = chispas.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;

        ParticleSystem.ShapeModule shape = chispas.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.025f;
        shape.radiusThickness = 1f;
        shape.randomDirectionAmount = 0.22f;
        shape.sphericalDirectionAmount = 0.12f;

        ParticleSystem.ColorOverLifetimeModule color = chispas.colorOverLifetime;
        color.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.96f, 0.7f), 0f),
                new GradientColorKey(new Color(1f, 0.38f, 0.06f), 0.55f),
                new GradientColorKey(new Color(0.24f, 0.08f, 0.025f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.18f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(gradiente);

        ParticleSystem.SizeOverLifetimeModule size = chispas.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer particleRenderer = chispas.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.lengthScale = 3f;
            particleRenderer.velocityScale = 0.12f;
        }

        chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void CachearMaterialesEmisivos()
    {
        objetivosEmision.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materiales = renderer.sharedMaterials;
            for (int i = 0; i < materiales.Length; i++)
            {
                Material material = materiales[i];
                if (material == null || !material.name.StartsWith("Foco02"))
                    continue;

                objetivosEmision.Add(new ObjetivoEmision
                {
                    renderer = renderer,
                    indiceMaterial = i,
                    propiedades = new MaterialPropertyBlock()
                });
            }
        }
    }

    private void AplicarEmision(Color color)
    {
        foreach (ObjetivoEmision objetivo in objetivosEmision)
        {
            if (objetivo.renderer == null)
                continue;

            objetivo.renderer.GetPropertyBlock(objetivo.propiedades, objetivo.indiceMaterial);
            objetivo.propiedades.SetColor(EmissionColorId, color);
            objetivo.renderer.SetPropertyBlock(objetivo.propiedades, objetivo.indiceMaterial);
        }
    }

    private void DetenerRutinaAveria()
    {
        if (rutinaAveria == null)
            return;

        StopCoroutine(rutinaAveria);
        rutinaAveria = null;
    }
}
