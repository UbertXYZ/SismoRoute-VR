using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Aglomeración enorme: mucha gente atrapada (nerviosa o llorando)
// sin poder avanzar, y un collider invisible que impide el paso.
public class AglomeracionEnorme : BloqueoBase
{
    [Header("Multitud")]
    [Tooltip("Objeto padre que contiene a todas las personas.")]
    public GameObject personas;

    [Tooltip("Porcentaje de personas que miran nerviosas; el resto llora (0 a 1).")]
    [Range(0f, 1f)] public float porcentajeNerviosos = 0.75f;

    [Header("Empujones")]
    [Tooltip("Cuánto avanzan y retroceden al empujar (metros).")]
    public float amplitudEmpuje = 0.1f;
    public float velocidadEmpuje = 2f;

    [Header("Sonido (opcional)")]
    public AudioSource audioMultitud;

    // Datos de cada persona para el empujón
    readonly List<Transform> cuerpos = new List<Transform>();
    readonly List<Vector3> posicionesIniciales = new List<Vector3>();
    readonly List<float> fases = new List<float>();
    bool empujando = false;

    protected override void Start()
    {
        // Guardar a cada persona y su posición original
        foreach (Animator anim in personas.GetComponentsInChildren<Animator>(true))
        {
            cuerpos.Add(anim.transform);
            posicionesIniciales.Add(anim.transform.localPosition);
            fases.Add(Random.Range(0f, Mathf.PI * 2f));
        }

        // Al inicio la gente no está
        personas.SetActive(false);

        base.Start(); // apaga el collider y revisa activarAlIniciar
    }

    protected override IEnumerator Ejecutar()
    {
        personas.SetActive(true);
        BloquearRuta();

        // Cada persona empieza su animación en un punto distinto
        // para que no se muevan todos sincronizados
        foreach (Transform t in cuerpos)
        {
            Animator anim = t.GetComponent<Animator>();
            string estado = Random.value < porcentajeNerviosos ? "Nervously Look Around" : "Crying";
            anim.Play(estado, 0, Random.value);
            anim.speed = Random.Range(0.9f, 1.1f);
        }

        if (audioMultitud != null)
        {
            audioMultitud.loop = true;
            audioMultitud.Play();
        }

        empujando = true;
        yield break;
    }

    void Update()
    {
        if (!empujando) return;

        // Se mueven un poco adelante y atrás, como una multitud apretada
        for (int i = 0; i < cuerpos.Count; i++)
        {
            float onda = Mathf.Sin(Time.time * velocidadEmpuje + fases[i]);
            Vector3 adelante = cuerpos[i].parent.InverseTransformDirection(cuerpos[i].forward);
            cuerpos[i].localPosition = posicionesIniciales[i] + adelante * onda * amplitudEmpuje;
        }
    }
}