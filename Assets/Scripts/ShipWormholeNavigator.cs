using UnityEngine;
using TMPro;

/// <summary>
/// Handles interaction with wormholes:
/// - Each frame, finds the nearest WormholeGate and shows a proximity prompt when close.
/// - When the ship ENTERS a wormhole's trigger collider, the gate's collider is treated
///   as the event horizon and the jump sequence is started.
/// - Notifies GameManager / GameDiscoveryState to update current system & discovery.
///
/// No player input is required; jump is proximity + collider based.
/// Requires a Rigidbody so physics triggers always work.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipWormholeNavigator : MonoBehaviour
{
    [Header("Proximity Settings")]
    [Tooltip("World distance from a wormhole within which the ship is considered 'close' and a prompt is shown.")]
    [SerializeField] private float proximityDistance = 80f;

    [Tooltip("Cooldown between automatic wormhole jumps, in seconds.")]
    [SerializeField] private float jumpCooldown = 2f;

    [Tooltip("Optional delay before teleporting, to allow for VFX/animation.")]
    [SerializeField] private float jumpDelaySeconds = 0.5f;

    [Header("UI References (optional)")]
    [Tooltip("If assigned, this canvas will be used directly for the wormhole proximity/jump prompt.")]
    [SerializeField] private Canvas promptCanvas;

    [Tooltip("If assigned, this label will be used directly for the wormhole prompt text.")]
    [SerializeField] private TMP_Text gatePromptLabel;

    [Header("UI Lookup (Tags, fallback)")]
    [Tooltip("Tag on the Canvas used to show the wormhole prompt.")]
    [SerializeField] private string promptCanvasTag = "GatePromptCanvas";

    [Tooltip("Tag on the TextMeshPro label used for the wormhole prompt text.")]
    [SerializeField] private string gatePromptLabelTag = "GatePromptLabel";

    [Header("Gate Scanning")]
    [Tooltip("How often (in seconds) the navigator refreshes the list of wormhole gates in the scene.")]
    [SerializeField] private float gatesRefreshInterval = 1f;

    private WormholeGate[] cachedGates = new WormholeGate[0];
    private float gatesRefreshTimer;

    private WormholeGate nearestGateForPrompt;
    private WormholeGate activeGateForJump;

    private float lastJumpTime = -999f;
    private bool isJumping;

    private void Awake()
    {
        ResolvePromptUI();
        RefreshGatesImmediate();
    }

    private void Start()
    {
        // In case UI is instantiated later or scene changes, try resolving again.
        if (promptCanvas == null || gatePromptLabel == null)
        {
            ResolvePromptUI();
        }

        HidePrompt();
    }

    private void Update()
    {
        // Regularly refresh the list of gates, since GameManager may respawn them when systems change.
        gatesRefreshTimer -= Time.deltaTime;
        if (gatesRefreshTimer <= 0f)
        {
            RefreshGatesImmediate();
        }

        if (isJumping)
            return;

        UpdateProximityPrompt();
    }

    /// <summary>
    /// Searches for wormhole gates in the scene and caches them.
    /// Uses FindObjectsByType on newer Unity versions, with a fallback for older ones.
    /// </summary>
    private void RefreshGatesImmediate()
    {
#if UNITY_2023_1_OR_NEWER
        cachedGates = Object.FindObjectsByType<WormholeGate>(FindObjectsSortMode.None);
#else
        cachedGates = Object.FindObjectsOfType<WormholeGate>();
#endif
        gatesRefreshTimer = Mathf.Max(0.1f, gatesRefreshInterval);
    }

    /// <summary>
    /// Resolve Canvas and TMP_Text via direct references or tags.
    /// Direct references win; tags are only used if references are null.
    /// </summary>
    private void ResolvePromptUI()
    {
        if (promptCanvas == null && !string.IsNullOrEmpty(promptCanvasTag))
        {
            GameObject canvasGO = null;
            try
            {
                canvasGO = GameObject.FindGameObjectWithTag(promptCanvasTag);
            }
            catch
            {
                // Tag might not exist; ignore.
            }

            if (canvasGO != null)
            {
                promptCanvas = canvasGO.GetComponent<Canvas>();
                if (promptCanvas == null)
                {
                    promptCanvas = canvasGO.GetComponentInChildren<Canvas>(true);
                }
            }
        }

        if (gatePromptLabel == null && !string.IsNullOrEmpty(gatePromptLabelTag))
        {
            GameObject labelGO = null;
            try
            {
                labelGO = GameObject.FindGameObjectWithTag(gatePromptLabelTag);
            }
            catch
            {
                // Tag might not exist; ignore.
            }

            if (labelGO != null)
            {
                gatePromptLabel = labelGO.GetComponent<TMP_Text>();
            }
        }

#if UNITY_EDITOR
        if (promptCanvas == null)
        {
            Debug.LogWarning($"ShipWormholeNavigator: No prompt Canvas assigned and no object found with tag '{promptCanvasTag}'. Prompt UI will be hidden.");
        }

        if (gatePromptLabel == null)
        {
            Debug.LogWarning($"ShipWormholeNavigator: No prompt TMP_Text assigned and no object found with tag '{gatePromptLabelTag}'. Prompt text will be empty.");
        }
#endif
    }

    /// <summary>
    /// Finds the nearest gate and shows/hides the proximity prompt.
    /// </summary>
    private void UpdateProximityPrompt()
    {
        nearestGateForPrompt = null;
        float nearestDist = float.MaxValue;

        Vector3 shipPos = transform.position;

        for (int i = 0; i < cachedGates.Length; i++)
        {
            var gate = cachedGates[i];
            if (gate == null)
                continue;

            float d = Vector3.Distance(shipPos, gate.transform.position);
            if (d < proximityDistance && d < nearestDist)
            {
                nearestDist = d;
                nearestGateForPrompt = gate;
            }
        }

        if (nearestGateForPrompt != null)
        {
            ShowProximityPrompt(nearestGateForPrompt);
        }
        else
        {
            HidePrompt();
        }
    }

    private void ShowProximityPrompt(WormholeGate gate)
    {
        if (promptCanvas == null || gatePromptLabel == null)
            return;

        string destinationName = gate != null
            ? gate.GetTargetSystemDisplayName()
            : "unknown destination";

        gatePromptLabel.text = $"You are close to a wormhole leading to {destinationName}";
        promptCanvas.enabled = true;
    }

    private void ShowJumpingPrompt(WormholeGate gate)
    {
        if (promptCanvas == null || gatePromptLabel == null)
            return;

        string destinationName = gate != null
            ? gate.GetTargetSystemDisplayName()
            : "unknown destination";

        gatePromptLabel.text = $"Entering wormhole to {destinationName}...";
        promptCanvas.enabled = true;
    }

    private void HidePrompt()
    {
        if (promptCanvas != null)
            promptCanvas.enabled = false;

        if (gatePromptLabel != null)
            gatePromptLabel.text = string.Empty;
    }

    /// <summary>
    /// Called by WormholeGate when the ship's collider ENTERS the wormhole's trigger collider.
    /// This collider is treated as the event horizon and starts the jump sequence.
    /// </summary>
    public void OnEventHorizonEntered(WormholeGate gate)
    {
        if (gate == null)
            return;

        if (isJumping)
            return;

        if (Time.time < lastJumpTime + jumpCooldown)
            return;

        activeGateForJump = gate;
        StartJumpSequence();
    }

    private void StartJumpSequence()
    {
        if (activeGateForJump == null)
            return;

        isJumping = true;
        lastJumpTime = Time.time;

        ShowJumpingPrompt(activeGateForJump);

        if (jumpDelaySeconds > 0f)
        {
            Invoke(nameof(PerformJump), jumpDelaySeconds);
        }
        else
        {
            PerformJump();
        }
    }

    private void PerformJump()
    {
        if (activeGateForJump == null)
        {
            isJumping = false;
            HidePrompt();
            return;
        }

        int targetSystemId = activeGateForJump.GetTargetSystemId();
        if (targetSystemId < 0)
        {
            Debug.LogWarning("ShipWormholeNavigator: Active gate has no valid target system.");
            isJumping = false;
            HidePrompt();
            return;
        }

        int wormholeId = activeGateForJump.WormholeId;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (wormholeId >= 0)
            {
                gm.DiscoverWormholeAndEndpoints(wormholeId);
            }
            else
            {
                gm.DiscoverSystem(targetSystemId);
            }

            gm.SetCurrentSystem(targetSystemId);
        }

        var exit = activeGateForJump.ExitPoint;
        if (exit != null)
        {
            transform.position = exit.position;
            transform.rotation = exit.rotation;
        }

        HidePrompt();
        isJumping = false;
        activeGateForJump = null;
    }
}
