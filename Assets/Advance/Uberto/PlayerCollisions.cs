using UnityEngine;

public class PlayerCollisions : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private LayerMask collisionLayers;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        collisionLayers |= LayerMask.GetMask("Default");
    }

    public bool IsGrounded()
    {
        return characterController != null &&
               characterController.isGrounded;
    }
}