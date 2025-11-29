using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles wormhole interaction for a ship.
/// Ship decides when to interact; gates know how to perform the jump.
/// </summary>
public class ShipWormholeNavigator : MonoBehaviour
{
    [Header("Control")]
    public bool isPlayerControlled = true;

    [Tooltip("Maximum distance to search for a gate to interact with.")]
    public float interactionRadius = 40f;

    [Tooltip("Key used by the player to trigger a jump.")]
    public Key interactKey = Key.F;

    private WormholeGate currentGateInRange;

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Find nearest gate within interactionRadius.
        WormholeGate nearest = gm.FindNearestGate(transform.position, interactionRadius);

        // Update prompt visibility if gate changed.
        if (nearest != currentGateInRange)
        {
            if (currentGateInRange != null)
                currentGateInRange.SetPromptVisible(false);

            currentGateInRange = nearest;
        }

        if (currentGateInRange == null)
            return;

        // Show prompt for the gate currently in range.
        currentGateInRange.SetPromptVisible(true);

        // Player-controlled: key press triggers TryActivate.
        if (isPlayerControlled)
        {
            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame)
            {
                bool jumped = currentGateInRange.TryActivate(this);
                if (jumped)
                {
                    // After jump, gates are respawned in new system.
                    currentGateInRange = null;
                }
            }
        }
        else
        {
            // NPC behaviour can decide when to call currentGateInRange.TryActivate(this).
        }
    }
}
