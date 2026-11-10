using UnityEngine;

public class EscaleraConfig : MonoBehaviour
{
    [Header("Escalera Configuracion")]
    public GameObject bloqueo;
    public bool bloqueado = false;
    public void Configurar(bool bloquearEscalera)
    {
        bloqueado = bloquearEscalera;
        bloqueo.SetActive(bloqueado);
    }
}
