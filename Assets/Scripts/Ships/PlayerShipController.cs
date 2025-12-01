using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Free-flight ship controller using the new Input System.
/// - W/S or Up/Down: thrust forward/back
/// - A/D or Left/Right: yaw (turn) left/right
/// - R/F: pitch up/down
/// - Q/E: roll
/// </summary>
public class PlayerShipController : MonoBehaviour
{
    [Header("Movement")]
    public float thrust = 25f;
    public float maxSpeed = 60f;
    public float yawSpeed = 90f;
    public float pitchSpeed = 60f;
    public float rollSpeed = 80f;
    public float damping = 1f;

    [Header("Bounds")]
    [Tooltip("Fallback radius of the playable space if no system data is available.")]
    [FormerlySerializedAs("worldExtent")]
    [SerializeField] private float fallbackSystemBoundaryRadius = 4000f;

    private float systemBoundaryRadius = 0f;

    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        systemBoundaryRadius = Mathf.Max(fallbackSystemBoundaryRadius, 0f);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Forward/backward
        float forwardInput = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) forwardInput += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) forwardInput -= 1f;

        velocity += transform.forward * (forwardInput * thrust * Time.deltaTime);

        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        // Yaw
        float yawInput = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) yawInput += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) yawInput -= 1f;

        // Pitch (R/F)
        float pitchInput = 0f;
        if (kb.rKey.isPressed) pitchInput += 1f;
        if (kb.fKey.isPressed) pitchInput -= 1f;

        // Roll (Q/E)
        float rollInput = 0f;
        if (kb.eKey.isPressed) rollInput += 1f;
        if (kb.qKey.isPressed) rollInput -= 1f;

        transform.Rotate(
            pitchSpeed * pitchInput * Time.deltaTime,
            yawSpeed * yawInput * Time.deltaTime,
            -rollSpeed * rollInput * Time.deltaTime,
            Space.Self
        );

        // Integrate velocity
        transform.position += velocity * Time.deltaTime;
        velocity = Vector3.Lerp(velocity, Vector3.zero, damping * Time.deltaTime);

        ConstrainToSystemBounds();
    }

    /// <summary>
    /// Set the radius used to constrain the ship to the active star system.
    /// </summary>
    /// <param name="radius">Radius in world units.</param>
    public void SetSystemBoundaryRadius(float radius)
    {
        systemBoundaryRadius = Mathf.Max(radius, 0f);
    }

    /// <summary>
    /// Keep the ship inside a spherical boundary around the current system center.
    /// </summary>
    private void ConstrainToSystemBounds()
    {
        if (systemBoundaryRadius <= 0f)
            return;

        Vector3 center = Vector3.zero;
        var gm = GameManager.Instance;
        if (gm != null)
            center = gm.CurrentSystemWorldPosition;

        Vector3 offsetFromCenter = transform.position - center;
        float radius = systemBoundaryRadius;

        if (offsetFromCenter.sqrMagnitude > radius * radius)
        {
            // Clamp position to the sphere surface and remove outward velocity to reduce jitter.
            Vector3 clampedOffset = offsetFromCenter.normalized * radius;
            transform.position = center + clampedOffset;
            velocity = Vector3.ProjectOnPlane(velocity, offsetFromCenter.normalized);
        }
    }
}
