using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Handles wormhole interaction for a ship.
/// - Finds nearest gate within a radius
/// - Shows a screen-space prompt when a gate is in range
/// - For player-controlled ships, listens for input to jump
/// </summary>
public class ShipWormholeNavigator : MonoBehaviour
{
    [Header("Control")]
    public bool isPlayerControlled = true;

    [Tooltip("Maximum distance to search for a gate to interact with.")]
    public float interactionRadius = 40f;

    [Tooltip("Key used by the player to trigger a jump.")]
    public Key interactKey = Key.F;

    [Header("Screen Prompt UI")]
    [Tooltip("Screen-space TextMeshProUGUI used to show the wormhole prompt. " +
             "If not assigned, will try to find by tag.")]
    public TextMeshProUGUI promptLabel;

    [Tooltip("Tag used to auto-find the prompt label UI if 'promptLabel' is not assigned.")]
    public string promptLabelTag = "GatePromptLabel";

    private WormholeGate currentGateInRange;

    private void Awake()
    {
        // Auto-find the prompt label in the scene if not wired in the prefab.
        if (promptLabel == null && !string.IsNullOrEmpty(promptLabelTag))
        {
            GameObject go = GameObject.FindWithTag(promptLabelTag);
            if (go != null)
            {
                promptLabel = go.GetComponent<TextMeshProUGUI>();
            }
        }

        // Ensure it starts hidden
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Find nearest gate within interactionRadius.
        WormholeGate nearest = gm.FindNearestGate(transform.position, interactionRadius);

        // If nearest gate changed, update the prompt.
        if (nearest != currentGateInRange)
        {
            currentGateInRange = nearest;
            UpdatePrompt();
        }

        if (currentGateInRange == null)
            return;

        // Player-controlled: key press triggers TryActivate.
        if (isPlayerControlled)
        {
            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame)
            {
                bool jumped = currentGateInRange.TryActivate(this);
                if (jumped)
                {
                    currentGateInRange = null;
                    UpdatePrompt();
                }
            }
        }
        else
        {
            // NPC behaviour can decide when to call currentGateInRange.TryActivate(this).
        }
    }

    private void UpdatePrompt()
    {
        if (promptLabel == null)
            return;

        if (currentGateInRange == null)
        {
            promptLabel.gameObject.SetActive(false);
        }
        else
        {
            string targetName = "Unknown Destination";
            var gm = GameManager.Instance;
            if (gm != null && gm.galaxy != null)
            {
                var sys = gm.galaxy.GetSystem(currentGateInRange.targetSystemId);
                if (sys != null)
                    targetName = sys.displayName;
            }

            promptLabel.text = $"Press G to enter wormhole to {targetName}";
            promptLabel.gameObject.SetActive(true);
        }
    }
}
