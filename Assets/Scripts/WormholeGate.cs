using UnityEngine;

/// <summary>
/// A physical wormhole gate in the scene.
///
/// Responsibilities:
/// - Its trigger collider represents the wormhole's event horizon.
/// - When the ship enters this trigger, ShipWormholeNavigator is notified to start the jump.
/// - Provides navigation data: which system this gate leads to, and an exit point.
/// - Optionally binds to a wormholeId from GalaxyGenerator for proper discovery.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WormholeGate : MonoBehaviour
{
    [Header("Wormhole IDs")]
    [Tooltip("If you are using GalaxyGenerator's wormhole graph, assign the wormhole link ID here.\n" +
             "If negative, the gate will fall back to Explicit Target System Id.")]
    [SerializeField] private int wormholeId = -1;

    [Tooltip("Fallback target system ID when wormholeId is not used or cannot be resolved.\n" +
             "This should be a valid systemId from GalaxyGenerator.")]
    [SerializeField] private int explicitTargetSystemId = -1;

    [Header("Navigation")]
    [Tooltip("Where the ship should appear after jumping through this gate.\n" +
             "If null, the ship will not be moved (only the current system will change).")]
    [SerializeField] private Transform exitPoint;

    [Header("Debug / Info")]
    [Tooltip("Optional custom label for the gate, purely for debugging/inspector.")]
    [SerializeField] private string gateLabel;

    /// <summary>
    /// Public accessors used by ShipWormholeNavigator and other systems.
    /// </summary>
    public int WormholeId => wormholeId;
    public Transform ExitPoint => exitPoint;

    private void Awake()
    {
        // Make sure the collider is configured as a trigger.
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var navigator = other.GetComponentInParent<ShipWormholeNavigator>();
        if (navigator == null)
            return;

        // This collider is treated as the event horizon.
        navigator.OnEventHorizonEntered(this);
    }

    /// <summary>
    /// Resolve which system this gate leads to:
    /// - If wormholeId is valid and GalaxyGenerator + GameDiscoveryState are available,
    ///   use the wormhole link and current system to get the "other" endpoint.
    /// - Otherwise, fall back to explicitTargetSystemId.
    /// </summary>
    public int GetTargetSystemId()
    {
        var galaxy = GalaxyGenerator.Instance;
        var discovery = GameDiscoveryState.Instance;

        if (wormholeId >= 0 && galaxy != null && discovery != null)
        {
            if (galaxy.TryGetWormhole(wormholeId, out var link))
            {
                int currentSystemId = discovery.CurrentSystemId;
                if (currentSystemId >= 0)
                {
                    return link.GetOtherSystem(currentSystemId);
                }
            }
        }

        // Fallback mode (e.g., for hand-authored prototypes)
        return explicitTargetSystemId;
    }

    /// <summary>
    /// Returns a user-facing name for the target system, using GalaxyGenerator if possible.
    /// </summary>
    public string GetTargetSystemDisplayName()
    {
        int targetId = GetTargetSystemId();
        var galaxy = GalaxyGenerator.Instance;

        if (targetId >= 0 && galaxy != null && galaxy.TryGetSystem(targetId, out var node))
        {
            if (!string.IsNullOrEmpty(node.displayName))
                return node.displayName;

            return $"System {targetId}";
        }

        if (targetId >= 0)
            return $"System {targetId}";

        return "Unknown system";
    }

    /// <summary>
    /// Allow runtime configuration of the wormhole link ID.
    /// </summary>
    public void SetWormholeId(int id)
    {
        wormholeId = id;
    }

    /// <summary>
    /// Allow runtime configuration of the explicit target system ID.
    /// </summary>
    public void SetExplicitTargetSystemId(int systemId)
    {
        explicitTargetSystemId = systemId;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);

        if (exitPoint != null)
        {
            Gizmos.DrawLine(transform.position, exitPoint.position);
            Gizmos.DrawWireSphere(exitPoint.position, 0.5f);
        }
    }
#endif
}
