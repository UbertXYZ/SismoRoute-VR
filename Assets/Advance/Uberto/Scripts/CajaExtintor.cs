using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CajaExtintor : MonoBehaviour
{
    [Header("Puerta del gabinete")]
    [SerializeField] private Transform puerta;
    [SerializeField] private float anguloApertura = -100f;
    [SerializeField, Min(1f)] private float velocidadApertura = 180f;

    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private Quaternion rotacionObjetivo;
    private bool puertaAbierta;

    public bool PuertaAbierta => puertaAbierta;

    private void Awake()
    {
        if (puerta == null)
        {
            Transform[] hijos = GetComponentsInChildren<Transform>(true);
            foreach (Transform hijo in hijos)
            {
                if (hijo != transform && hijo.name == "Puerta")
                {
                    puerta = hijo;
                    break;
                }
            }
        }

        if (puerta == null)
        {
            Debug.LogWarning("CajaExtintor no encontró un hijo llamado 'Puerta'.", this);
            return;
        }

        rotacionCerrada = puerta.localRotation;
        rotacionAbierta = rotacionCerrada * Quaternion.Euler(0f, anguloApertura, 0f);
        rotacionObjetivo = rotacionCerrada;

        // El prefab ya incluye un BoxCollider en la puerta. Este interactuable
        // permite abrirla con los rayos de los mandos VR.
        XRSimpleInteractable interactuable = puerta.GetComponent<XRSimpleInteractable>();
        if (interactuable == null)
            interactuable = puerta.gameObject.AddComponent<XRSimpleInteractable>();

        interactuable.selectEntered.AddListener(_ => AlternarPuerta());
    }

    private void Update()
    {
        if (puerta == null)
            return;

        puerta.localRotation = Quaternion.RotateTowards(
            puerta.localRotation,
            rotacionObjetivo,
            velocidadApertura * Time.deltaTime);
    }

    /// <summary>Invierte el estado de la puerta. Puede enlazarse también desde un botón UnityEvent.</summary>
    public void AlternarPuerta()
    {
        if (puerta == null)
            return;

        puertaAbierta = !puertaAbierta;
        rotacionObjetivo = puertaAbierta ? rotacionAbierta : rotacionCerrada;
    }

    public void AbrirPuerta()
    {
        if (puerta == null)
            return;

        puertaAbierta = true;
        rotacionObjetivo = rotacionAbierta;
    }

    public void CerrarPuerta()
    {
        if (puerta == null)
            return;

        puertaAbierta = false;
        rotacionObjetivo = rotacionCerrada;
    }

}
