using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
    private const float FixedControllerHeight = 1.70f;

    [Header("XR Origin")]
    [Tooltip("Transform raíz del XR Origin que debe desplazarse con el CharacterController.")]
    [SerializeField] private Transform xrOriginTransform;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform forwardSource;
    [Tooltip("Tag que identifica las paredes que deben bloquear y corregir penetraciones.")]
    [SerializeField] private string wallTag = "Pared";
    [SerializeField, Min(0f)] private float overlapRecoveryMargin = 0.002f;
    [SerializeField, Range(0f, 89f)] private float slopeLimit = 50f;
    [SerializeField, Min(0f)] private float stepOffset = 0.3f;
    [SerializeField, Min(0.01f)] private float skinWidth = 0.01f;
    [SerializeField, Min(0f)] private float minMoveDistance = 0f;

    [Header("Movimiento")]
    [Tooltip("Velocidad de trote controlado en unidades por segundo.")]
    [SerializeField, Min(0f), FormerlySerializedAs("moveSpeed")] private float velocidad = 2f;
    [SerializeField, Range(0f, 1f)] private float inAirControlModifier = 0.5f;
    [SerializeField] private bool enableStrafe = true;
    [SerializeField, Min(0f)] private float gravityScale = 1f;
    [SerializeField, Min(0.1f)] private float terminalVelocity = 50f;

    [Header("Detección de suelo")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;
    [SerializeField, Range(0.5f, 1f)] private float groundProbeRadiusFactor = 0.8f;

    private ContinuousMoveProvider moveProvider;
    private float verticalVelocity;
    private int lastSideCollisionFrame = -1;
    private Collider lastSideCollider;
    private Vector3 lastSideCollisionNormal;
    private Collider[] wallOverlapBuffer = new Collider[16];

    /// <summary>Collider lateral que tocó el XR Origin durante el frame actual.</summary>
    public Collider SideColliderThisFrame => lastSideCollisionFrame == Time.frameCount ? lastSideCollider : null;

    private void Reset()
    {
        xrOriginTransform = transform;
        characterController = GetComponent<CharacterController>();
        collisionLayers = Physics.DefaultRaycastLayers;
        ConfigureCharacterController();
    }

    private void Awake()
    {
        ResolveXROriginTransform();
        ResolveCharacterController();
        ResolveHeadTransform();
        ResolveMoveProvider();

        if (collisionLayers.value == 0)
            collisionLayers = Physics.DefaultRaycastLayers;

        ConfigureCharacterController();
        DisableXriBodyMovement();
        collisionLayers |= LayerMask.GetMask("Default");
        UpdateCharacterControllerCenter();
        ResolveTaggedWallOverlaps();
    }

    private void OnEnable()
    {
        // El proveedor queda desactivado, pero sus lectores se reutilizan para conservar
        // exactamente las acciones de movimiento configuradas para los mandos.
        if (moveProvider == null)
            return;

        moveProvider.leftHandMoveInput?.EnableDirectActionIfModeUsed();
        moveProvider.rightHandMoveInput?.EnableDirectActionIfModeUsed();
    }

    private void OnDisable()
    {
        if (moveProvider == null)
            return;

        moveProvider.leftHandMoveInput?.DisableDirectActionIfModeUsed();
        moveProvider.rightHandMoveInput?.DisableDirectActionIfModeUsed();
    }

    private void Update()
    {
        if (characterController == null || !characterController.enabled)
            return;

        ApplyFixedControllerDimensions();
        UpdateCharacterControllerCenter();
        ResolveTaggedWallOverlaps();

        float deltaTime = Time.deltaTime;
        bool grounded = IsGrounded();
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * gravityScale * deltaTime, -terminalVelocity);

        Vector2 input = Vector2.zero;
        if (moveProvider != null)
        {
            if (moveProvider.leftHandMoveInput != null)
                input += moveProvider.leftHandMoveInput.ReadValue();
            if (moveProvider.rightHandMoveInput != null)
                input += moveProvider.rightHandMoveInput.ReadValue();

            if (!enableStrafe)
                input.x = 0f;
        }

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 forward = GetMoveForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 planarDirection = right * input.x + forward * input.y;

        // Mantiene bloqueado el avance hacia la pared mientras la cápsula siga junto a ella.
        // No invierte el desplazamiento ni impide deslizarse en paralelo o alejarse.
        if (HasSideContact())
        {
            float movementIntoWall = Vector3.Dot(planarDirection, lastSideCollisionNormal);
            if (movementIntoWall < 0f)
                planarDirection -= lastSideCollisionNormal * movementIntoWall;
        }
        else
        {
            lastSideCollider = null;
            lastSideCollisionNormal = Vector3.zero;
        }

        float airControl = grounded ? 1f : inAirControlModifier;
        Vector3 motion = planarDirection * (velocidad * airControl * deltaTime);
        motion.y = verticalVelocity * deltaTime;

        CollisionFlags collisionFlags = Move(motion);
        ResolveTaggedWallOverlaps();

        if ((collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
        else if ((collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = -2f;
    }

    private void OnValidate()
    {
        overlapRecoveryMargin = Mathf.Max(0f, overlapRecoveryMargin);
        slopeLimit = Mathf.Clamp(slopeLimit, 0f, 89f);
        stepOffset = Mathf.Max(0f, stepOffset);
        skinWidth = Mathf.Max(0.01f, skinWidth);
        minMoveDistance = Mathf.Max(0f, minMoveDistance);
        velocidad = Mathf.Max(0f, velocidad);
        inAirControlModifier = Mathf.Clamp01(inAirControlModifier);
        gravityScale = Mathf.Max(0f, gravityScale);
        terminalVelocity = Mathf.Max(0.1f, terminalVelocity);
        groundProbeDistance = Mathf.Max(0.01f, groundProbeDistance);
        groundProbeRadiusFactor = Mathf.Clamp(groundProbeRadiusFactor, 0.5f, 1f);

        if (xrOriginTransform == null)
            xrOriginTransform = transform;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        ApplyFixedControllerDimensions();
    }

    /// <summary>
    /// Mueve el XR Origin usando el desplazamiento que el CharacterController acepta.
    /// La rotación del XR Origin no se modifica.
    /// </summary>
    public CollisionFlags Move(Vector3 motion)
    {
        if (characterController == null || !characterController.enabled)
            return CollisionFlags.None;

        Transform controllerTransform = characterController.transform;
        Vector3 previousPosition = controllerTransform.position;
        if (xrOriginTransform == null)
            xrOriginTransform = transform;

        Vector3 previousOriginPosition = xrOriginTransform.position;
        CollisionFlags flags = characterController.Move(motion);

        if (xrOriginTransform == controllerTransform)
            return flags;

        Vector3 actualDelta = controllerTransform.position - previousPosition;
        if (actualDelta.sqrMagnitude > 0f)
        {
            // El CharacterController calcula el desplazamiento permitido. Se revierte
            // su movimiento directo y se aplica ese mismo delta solo al XR Origin.
            controllerTransform.position = previousPosition;
            xrOriginTransform.position = previousOriginPosition + actualDelta;
            Physics.SyncTransforms();
        }
        return flags;
    }

    private void ResolveXROriginTransform()
    {
        if (xrOriginTransform == null)
            xrOriginTransform = transform;
    }

    private void ResolveCharacterController()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (characterController == null)
            characterController = GetComponentInChildren<CharacterController>();
    }

    private void ResolveHeadTransform()
    {
        if (headTransform == null)
        {
            Camera headCamera = GetComponentInChildren<Camera>();
            if (headCamera != null)
                headTransform = headCamera.transform;
        }
    }

    private void ResolveMoveProvider()
    {
        if (moveProvider == null)
            moveProvider = GetComponentInChildren<ContinuousMoveProvider>(true);

        if (moveProvider == null)
            return;

        if (forwardSource == null)
            forwardSource = moveProvider.forwardSource;
    }

    private void DisableXriBodyMovement()
    {
        if (moveProvider != null)
            moveProvider.enabled = false;

        GravityProvider gravityProvider = GetComponentInChildren<GravityProvider>(true);
        if (gravityProvider != null)
            gravityProvider.enabled = false;

        // XRI providers usan este componente para mover el origen. Se desactiva para que
        // el único movimiento del rig pase por el CharacterController de este script.
        XRBodyTransformer bodyTransformer = GetComponentInChildren<XRBodyTransformer>(true);
        if (bodyTransformer != null)
            bodyTransformer.enabled = false;
    }

    private Vector3 GetMoveForward()
    {
        Transform source = forwardSource != null ? forwardSource : headTransform;
        if (source != null)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(source.forward, Vector3.up);
            if (projectedForward.sqrMagnitude > 0.0001f)
                return projectedForward.normalized;
        }

        Vector3 originForward = xrOriginTransform != null ? xrOriginTransform.forward : transform.forward;
        Vector3 fallbackForward = Vector3.ProjectOnPlane(originForward, Vector3.up);
        return fallbackForward.sqrMagnitude > 0.0001f ? fallbackForward.normalized : Vector3.forward;
    }

    private void ApplyFixedControllerDimensions()
    {
        if (characterController == null)
            return;

        characterController.height = FixedControllerHeight;
        float maximumStepOffset = Mathf.Max(0f, FixedControllerHeight - characterController.radius * 2f);
        characterController.stepOffset = Mathf.Min(stepOffset, maximumStepOffset);
    }

    private void UpdateCharacterControllerCenter()
    {
        if (characterController == null || !characterController.enabled)
            return;

        ResolveHeadTransform();
        if (headTransform == null)
            return;

        Vector3 headPosition = characterController.transform.InverseTransformPoint(headTransform.position);
        characterController.center = new Vector3(
            headPosition.x,
            FixedControllerHeight * 0.5f + characterController.skinWidth,
            headPosition.z);
    }

    private void ConfigureCharacterController()
    {
        if (characterController == null)
            return;

        characterController.enabled = true;
        ApplyFixedControllerDimensions();
        characterController.detectCollisions = true;
        characterController.enableOverlapRecovery = true;

        // Conserva el CharacterController en Ignore Raycast para que las consultas
        // de suelo no se detecten a sí mismas; habilita colisión con paredes Default.
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        int defaultLayer = LayerMask.NameToLayer("Default");
        if (ignoreRaycastLayer >= 0 && defaultLayer >= 0)
        {
            if (characterController.gameObject.layer == defaultLayer)
                characterController.gameObject.layer = ignoreRaycastLayer;

            Physics.IgnoreLayerCollision(ignoreRaycastLayer, defaultLayer, false);
        }

        characterController.slopeLimit = slopeLimit;
        characterController.minMoveDistance = minMoveDistance;

        float maximumSkinWidth = Mathf.Max(0.01f, characterController.radius * 0.1f);
        characterController.skinWidth = Mathf.Min(skinWidth, maximumSkinWidth);

        float maximumStepOffset = Mathf.Max(0f, FixedControllerHeight - characterController.radius * 2f);
        characterController.stepOffset = Mathf.Min(stepOffset, maximumStepOffset);
    }

    /// <summary>Indica si el controlador está apoyado en una superficie caminable.</summary>
    public bool IsGrounded()
    {
        if (characterController == null || !characterController.enabled)
            return false;

        if (characterController.isGrounded)
            return true;

        return TryGetGroundHit(out _);
    }

    /// <summary>Busca una superficie caminable justo debajo de la cápsula.</summary>
    public bool TryGetGroundHit(out RaycastHit hit)
    {
        hit = default;
        if (characterController == null || !characterController.enabled)
            return false;

        float scaleX = Mathf.Abs(characterController.transform.lossyScale.x);
        float scaleY = Mathf.Abs(characterController.transform.lossyScale.y);
        float scaleZ = Mathf.Abs(characterController.transform.lossyScale.z);
        float worldRadius = characterController.radius * Mathf.Max(scaleX, scaleZ);
        float worldHeight = characterController.height * scaleY;
        float probeRadius = Mathf.Max(0.02f, worldRadius * groundProbeRadiusFactor);
        probeRadius = Mathf.Min(probeRadius, worldRadius);

        Vector3 up = characterController.transform.up;
        Vector3 worldCenter = characterController.transform.TransformPoint(characterController.center);
        Vector3 lowerSphereCenter = worldCenter - up * (worldHeight * 0.5f - worldRadius);
        Vector3 castOrigin = lowerSphereCenter + up * (worldRadius - probeRadius + 0.02f);
        float castDistance = groundProbeDistance + 0.02f;

        if (!Physics.SphereCast(castOrigin, probeRadius, -up, out hit, castDistance,
                collisionLayers, QueryTriggerInteraction.Ignore))
            return false;

        float allowedGroundAngle = Mathf.Min(89f, characterController.slopeLimit + 1f);
        return Vector3.Angle(hit.normal, up) <= allowedGroundAngle;
    }

    private bool HasSideContact()
    {
        if (lastSideCollider == null || !lastSideCollider.enabled || characterController == null)
            return false;

        Vector3 pointOnWall = lastSideCollider.ClosestPoint(characterController.bounds.center);
        Vector3 pointOnCapsule = characterController.ClosestPoint(pointOnWall);
        float scale = Mathf.Max(
            Mathf.Abs(characterController.transform.lossyScale.x),
            Mathf.Abs(characterController.transform.lossyScale.y),
            Mathf.Abs(characterController.transform.lossyScale.z));
        float contactDistance = characterController.skinWidth * scale + 0.05f;

        return (pointOnWall - pointOnCapsule).sqrMagnitude <= contactDistance * contactDistance;
    }

    private bool IsTaggedWall(Collider collider)
    {
        if (collider == null || string.IsNullOrWhiteSpace(wallTag))
            return false;

        for (Transform current = collider.transform; current != null; current = current.parent)
        {
            if (current.gameObject.tag == wallTag)
                return true;
        }

        return false;
    }

    private void ResolveTaggedWallOverlaps()
    {
        if (characterController == null || !characterController.enabled || string.IsNullOrWhiteSpace(wallTag))
            return;

        Physics.SyncTransforms();
        for (int pass = 0; pass < 5; pass++)
        {
            GetControllerCapsule(out Vector3 point1, out Vector3 point2, out float radius);
            int overlapCount = GetOverlappingColliders(point1, point2, radius);
            bool movedOut = false;

            for (int i = 0; i < overlapCount; i++)
            {
                Collider candidate = wallOverlapBuffer[i];
                if (candidate == null || IsPartOfPlayer(candidate) || !IsTaggedWall(candidate))
                    continue;

                if (TryDepenetrateFromWall(candidate))
                {
                    movedOut = true;
                    break;
                }
            }

            if (!movedOut)
                break;
        }
    }

    private void GetControllerCapsule(out Vector3 point1, out Vector3 point2, out float radius)
    {
        Transform controllerTransform = characterController.transform;
        float scaleX = Mathf.Abs(controllerTransform.lossyScale.x);
        float scaleY = Mathf.Abs(controllerTransform.lossyScale.y);
        float scaleZ = Mathf.Abs(controllerTransform.lossyScale.z);
        radius = characterController.radius * Mathf.Max(scaleX, scaleZ);
        float height = Mathf.Max(characterController.height * scaleY, radius * 2f);
        Vector3 center = controllerTransform.TransformPoint(characterController.center);
        Vector3 up = controllerTransform.up;
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        point1 = center + up * halfSegment;
        point2 = center - up * halfSegment;
    }

    private int GetOverlappingColliders(Vector3 point1, Vector3 point2, float radius)
    {
        int overlapCount;
        bool bufferWasFull;
        do
        {
            overlapCount = Physics.OverlapCapsuleNonAlloc(
                point1,
                point2,
                radius,
                wallOverlapBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            bufferWasFull = overlapCount == wallOverlapBuffer.Length;
            if (bufferWasFull)
                System.Array.Resize(ref wallOverlapBuffer, wallOverlapBuffer.Length * 2);
        }
        while (bufferWasFull);

        return overlapCount;
    }

    private bool IsPartOfPlayer(Collider candidate)
    {
        Transform candidateTransform = candidate.transform;
        Transform controllerTransform = characterController.transform;
        if (candidate == characterController || candidateTransform == controllerTransform || candidateTransform.IsChildOf(controllerTransform))
            return true;

        return xrOriginTransform != null &&
               (candidateTransform == xrOriginTransform || candidateTransform.IsChildOf(xrOriginTransform));
    }

    private bool TryDepenetrateFromWall(Collider wallCollider)
    {
        if (wallCollider == null || !characterController.enabled)
            return false;

        if (!Physics.ComputePenetration(
                characterController,
                characterController.transform.position,
                characterController.transform.rotation,
                wallCollider,
                wallCollider.transform.position,
                wallCollider.transform.rotation,
                out Vector3 direction,
                out float distance))
            return false;

        if (distance <= 0f || direction.sqrMagnitude <= 0.000001f)
            return false;

        if (xrOriginTransform == null)
            xrOriginTransform = transform;

        xrOriginTransform.position += direction.normalized * (distance + overlapRecoveryMargin);
        Physics.SyncTransforms();
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider wallCollider = collision.collider;
        if (wallCollider == null && collision.gameObject != null)
            wallCollider = collision.gameObject.GetComponent<Collider>();

        if (!IsTaggedWall(wallCollider))
            return;

        TryDepenetrateFromWall(wallCollider);
        ResolveTaggedWallOverlaps();
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (IsTaggedWall(hit.collider) && Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.5f)
        {
            Vector3 sideNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
            if (sideNormal.sqrMagnitude > 0.0001f)
            {
                lastSideCollisionFrame = Time.frameCount;
                lastSideCollider = hit.collider;
                lastSideCollisionNormal = sideNormal.normalized;
            }
        }
    }
}
