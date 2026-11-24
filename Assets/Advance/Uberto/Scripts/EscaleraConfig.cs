using UnityEngine;

public class EscaleraConfig : MonoBehaviour
{
    [Header("Escalera Configuracion")]
    public GameObject paredBajada;
    public GameObject bloqueoBajada;
    public BloqueoEscalera escaleraBloqueo;
    public bool conPared = false;

    public void Configurar(bool conPared)
    {
        this.conPared = conPared;

        if (paredBajada != null)
            paredBajada.SetActive(conPared);

        if (bloqueoBajada != null)
            bloqueoBajada.SetActive(!conPared);
    }
}
