using UnityEngine;

/// <summary>
/// Represents a wormhole gate in space. Knows which system it leads to and
/// the distance at which ships can use it. Ship-side logic calls TryActivate(...)
/// to perform a jump via GameManager.
/// </summary>
public class WormholeGate : MonoBehaviour
{
    [Tooltip("System id this gate leads to (GalaxyGenerator index).")]
    public int targetSystemId = -1;

    [Tooltip("Distance at which ships are considered 'in range' to use this gate.")]
    public float activationDistance = 30f;

    /// <summary>
    /// Called by a ship when it wants to use this gate.
    /// Returns true if a jump was performed.
    /// </summary>
    public bool TryActivate(ShipWormholeNavigator ship)
    {
        if (ship == null || targetSystemId < 0 || GameManager.Instance == null)
            return false;

        float dist = Vector3.Distance(transform.position, ship.transform.position);
        if (dist > activationDistance)
            return false;

        GameManager.Instance.JumpShipToSystem(ship, targetSystemId);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
