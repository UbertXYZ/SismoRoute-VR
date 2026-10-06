using System.Collections.Generic;
using UnityEngine;

public class HabitacionConfig : MonoBehaviour
{
    [Header("Habitaciones Configuración")]
    public float largo = 6f;
    public float ancho = 8f;
    public float alto = 4.5f;
    [Header("Habitaciones Partes")]
    public Transform paredFrontal;
    public Transform paredTrasera;
    public Transform paredIzquierda;
    public Transform paredDerecha;
    public Transform techo;
    public Transform suelo;
    public List<PuertaConfig> puertas;
    [Header("Grosor Configuración")]
    [Min(0.10f)] public float grosorPared = 0.15f;
    [Min(0f)] public float grosorSuelo = 0.05f;
    [Min(0f)] public float grosorTecho = 0.05f;
    public float altoPared = 0f;
    private void OnValidate()
    {
        ActualizarSuelo();
        ActualizarTecho();
        altoPared = Mathf.Max(0f, alto - grosorSuelo - grosorTecho);
        ActualizarParedes();
    }
    private void ActualizarParedes()
    {
        if (altoPared <= 0f)
        {
            paredFrontal.gameObject.SetActive(false);
            paredTrasera.gameObject.SetActive(false);
            paredIzquierda.gameObject.SetActive(false);
            paredDerecha.gameObject.SetActive(false);
            return;
        }
        float centroY = grosorSuelo + altoPared / 2f;
        if (paredFrontal != null)
        {
            paredFrontal.gameObject.SetActive(true);
            paredFrontal.localPosition = new Vector3(
                0f,
                centroY,
                ancho / 2f - grosorPared / 2f
            );
            paredFrontal.localScale = new Vector3(
                largo,
                altoPared,
                grosorPared
            );
        }
        if (paredTrasera != null)
        {
            paredTrasera.gameObject.SetActive(true);
            paredTrasera.localPosition = new Vector3(
                0f,
                centroY,
                -ancho / 2f + grosorPared / 2f
            );
            paredTrasera.localScale = new Vector3(
                largo,
                altoPared,
                grosorPared
            );
        }
        if (paredIzquierda != null)
        {
            paredIzquierda.gameObject.SetActive(true);
            paredIzquierda.localPosition = new Vector3(
                -largo / 2f + grosorPared / 2f,
                centroY,
                0f
            );
            paredIzquierda.localScale = new Vector3(
                grosorPared,
                altoPared,
                ancho - grosorPared * 2f
            );
        }
        if (paredDerecha != null)
        {
            paredDerecha.gameObject.SetActive(true);
            paredDerecha.localPosition = new Vector3(
                largo / 2f - grosorPared / 2f,
                centroY,
                0f
            );
            paredDerecha.localScale = new Vector3(
                grosorPared,
                altoPared,
                ancho - grosorPared * 2f
            );
        }
    }
    private void ActualizarSuelo()
    {
        if (suelo == null)
            return;
        if (grosorSuelo <= 0f)
        {
            suelo.gameObject.SetActive(false);
            return;
        }
        suelo.gameObject.SetActive(true);
        suelo.localScale = new Vector3(largo, grosorSuelo, ancho);
        suelo.localPosition = new Vector3(0f, grosorSuelo / 2f, 0f);
    }

    private void ActualizarTecho()
    {
        if (techo == null)
            return;
        if (grosorTecho <= 0f)
        {
            techo.gameObject.SetActive(false);
            return;
        }
        techo.gameObject.SetActive(true);
        techo.localScale = new Vector3(largo, grosorTecho, ancho);
        techo.localPosition = new Vector3(0f, alto - grosorTecho / 2f, 0f);
    }
}
