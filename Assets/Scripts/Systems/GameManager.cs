using UnityEngine;
using TMPro;

/// <summary>
/// High-level game coordinator:
/// - Keeps references to GalaxyGenerator, GameDiscoveryState, and the map
/// - Bridges current-system changes into simple UI (current system label)
/// - Spawns the player ship prefab once if needed
/// - Coordinates with GalaxySimulationManager to keep the player's system scene active
///
/// Starting system is defined only in GameDiscoveryState (no duplication here).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private GalaxyGenerator galaxy;
    [SerializeField] private GameDiscoveryState discoveryState;
    [SerializeField] private GalaxyMapUIManager galaxyMapUI;
    [SerializeField] private GalaxySimulationManager simulationManager;

    [Header("Simulation Defaults")]
    [Tooltip("Fallback gate ring radius used when no simulation manager is available.")]
    [SerializeField] private float fallbackGateRingRadius = 50f;

    [Header("World Mapping")]
    [Tooltip("Scale factor applied when mapping 2D galaxy coordinates into 3D world space (x, z).")]
    [SerializeField] private float systemPositionScale = 1f;

    [Tooltip("World-space center used for placing gate rings or as a fallback position when a system cannot be resolved.")]
    [SerializeField] private Transform gateRingCenter;

    [Header("UI")]
    [SerializeField] private TMP_Text currentSystemLabel;

    [Header("Player Ship")]
    [Tooltip("Prefab of the player ship to spawn at game start if no existing ship is found.")]
    [SerializeField] private GameObject playerShipPrefab;

    [Tooltip("Optional spawn point for the player ship. If null, the ship spawns at world origin.")]
    [SerializeField] private Transform playerSpawnPoint;

    [Tooltip("Tag used to find an existing player ship in the scene before spawning a new one.")]
    [SerializeField] private string playerShipTag = "PlayerShip";

    private GameObject playerShipInstance;

    private bool hasAlignedInitialShipPosition;
    private Vector3 currentSystemWorldPosition = Vector3.zero;
    private float currentSystemRadius;

    public int CurrentSystemId => discoveryState != null ? discoveryState.CurrentSystemId : -1;
    public GameObject PlayerShip => playerShipInstance;
    public Vector3 CurrentSystemWorldPosition => currentSystemWorldPosition;
    public float GateRingRadius => simulationManager != null ? simulationManager.GateRingRadius : fallbackGateRingRadius;
    public float CurrentSystemRadius => currentSystemRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (galaxy == null)
            galaxy = GalaxyGenerator.Instance;

        if (discoveryState == null)
            discoveryState = GameDiscoveryState.Instance;

        if (galaxyMapUI == null)
            galaxyMapUI = GalaxyMapUIManager.Instance;

        if (simulationManager == null)
            simulationManager = GalaxySimulationManager.Instance;

        if (discoveryState != null)
        {
            discoveryState.CurrentSystemChanged += OnCurrentSystemChanged;
        }

        // Ensure we have a ship in the scene
        SpawnPlayerShipIfNeeded();

        if (simulationManager != null && discoveryState != null && discoveryState.CurrentSystemId >= 0)
        {
            simulationManager.EnsureSystemLoaded(discoveryState.CurrentSystemId);
        }

        // Initial UI / gates. If discovery state hasn't initialized yet,
        // this may be -1 and will be corrected when CurrentSystemChanged fires.
        int systemId = discoveryState != null ? discoveryState.CurrentSystemId : -1;
        OnCurrentSystemChanged(systemId);
    }

    private void OnDestroy()
    {
        if (discoveryState != null)
        {
            discoveryState.CurrentSystemChanged -= OnCurrentSystemChanged;
        }
    }

    /// <summary>
    /// Spawn the player ship once if it doesn't already exist.
    /// </summary>
    private void SpawnPlayerShipIfNeeded()
    {
        if (playerShipInstance != null)
            return;

        GameObject existing = null;

        if (!string.IsNullOrEmpty(playerShipTag))
        {
            try
            {
                existing = GameObject.FindGameObjectWithTag(playerShipTag);
            }
            catch
            {
                // Tag might not exist; ignore and fall through to prefab spawn.
            }
        }

        if (existing != null)
        {
            playerShipInstance = existing;
            return;
        }

        if (playerShipPrefab == null)
            return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (playerSpawnPoint != null)
        {
            spawnPos = playerSpawnPoint.position;
            spawnRot = playerSpawnPoint.rotation;
        }

        playerShipInstance = Instantiate(playerShipPrefab, spawnPos, spawnRot);
    }

    /// <summary>
    /// Called whenever the authoritative current system changes in GameDiscoveryState.
    /// Updates the UI label and rebuilds wormhole gates for that system.
    /// </summary>
    private void OnCurrentSystemChanged(int systemId)
    {
        UpdateCurrentSystemLabel(systemId);
        UpdateCurrentSystemRadius(systemId);
        EnableActiveSystemStar(systemId);
        currentSystemWorldPosition = GetSystemWorldPosition(systemId);

        if (simulationManager != null && systemId >= 0)
        {
            simulationManager.EnsureSystemLoaded(systemId);

            if (playerShipInstance != null && !simulationManager.IsShipInSystem(playerShipInstance, systemId))
            {
                simulationManager.MoveShipToSystem(playerShipInstance, systemId, null, null);
            }
        }

        if (!hasAlignedInitialShipPosition && playerShipInstance != null && systemId >= 0)
        {
            playerShipInstance.transform.position = GetSystemWorldPosition(systemId);
            hasAlignedInitialShipPosition = true;
        }

        UpdatePlayerBoundaryRadius();
    }

    private void UpdateCurrentSystemLabel(int systemId)
    {
        if (currentSystemLabel == null)
            return;

        if (systemId < 0 || galaxy == null)
        {
            currentSystemLabel.text = string.Empty;
            return;
        }

        if (galaxy.TryGetSystem(systemId, out var node) && !string.IsNullOrEmpty(node.displayName))
        {
            currentSystemLabel.text = node.displayName;
        }
        else
        {
            currentSystemLabel.text = $"System {systemId}";
        }
    }

    /// <summary>
    /// Ensures only the current system's primary star visual is active in the scene.
    /// </summary>
    /// <param name="systemId">System identifier that should be visible; negative values hide all.</param>
    private void EnableActiveSystemStar(int systemId)
    {
        if (galaxy == null)
            return;

        galaxy.SetActiveSystemVisual(systemId);
    }

    /// <summary>
    /// Cache the current system radius so other systems (map, ship bounds) can react.
    /// </summary>
    /// <param name="systemId">Active system identifier.</param>
    private void UpdateCurrentSystemRadius(int systemId)
    {
        currentSystemRadius = 0f;

        if (galaxy != null && galaxy.TryGetSystem(systemId, out var node))
        {
            currentSystemRadius = Mathf.Max(node.systemRadius, 0f);
        }
    }

    /// <summary>
    /// Push the active system radius into the player ship controller to keep the ship within bounds.
    /// </summary>
    private void UpdatePlayerBoundaryRadius()
    {
        if (playerShipInstance == null)
            return;

        var shipController = playerShipInstance.GetComponent<PlayerShipController>();
        if (shipController == null)
            return;

        float radiusToApply = currentSystemRadius;
        shipController.SetSystemBoundaryRadius(radiusToApply);
    }

    /// <summary>
    /// Convert a generated 2D galaxy position into world space (x -> x, y -> z).
    /// Falls back to the gate ring center or Vector3.zero if the system cannot be resolved.
    /// </summary>
    public Vector3 GetSystemWorldPosition(int systemId)
    {
        if (TryGetSystemWorldPosition(systemId, out var pos))
            return pos;

        if (gateRingCenter != null)
            return gateRingCenter.position;

        return Vector3.zero;
    }

    private bool TryGetSystemWorldPosition(int systemId, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (galaxy != null && galaxy.TryGetSystem(systemId, out var node))
        {
            worldPos = new Vector3(node.position.x * systemPositionScale, 0f, node.position.y * systemPositionScale);
            return true;
        }

        return false;
    }

    public bool TryGetActiveGateForWormhole(int wormholeId, out WormholeGate gate)
    {
        gate = null;

        if (simulationManager != null && CurrentSystemId >= 0)
        {
            return simulationManager.TryGetGateForWormhole(CurrentSystemId, wormholeId, out gate);
        }

        return false;
    }

    public bool TryGetActiveGateForTargetSystem(int targetSystemId, out WormholeGate gate)
    {
        gate = null;

        if (simulationManager != null && CurrentSystemId >= 0)
        {
            return simulationManager.TryGetGateForTargetSystem(CurrentSystemId, targetSystemId, out gate);
        }

        return false;
    }

    #region Public helpers for other scripts

    /// <summary>
    /// Set the player's current system. This will:
    /// - Update GameDiscoveryState (system + connected wormholes)
    /// - Notify listeners (map, UI, gates, etc.)
    /// </summary>
    public void SetCurrentSystem(int systemId)
    {
        if (discoveryState != null)
        {
            discoveryState.SetCurrentSystem(systemId);
        }
    }

    /// <summary>
    /// Mark a system as discovered without moving the player there.
    /// </summary>
    public void DiscoverSystem(int systemId)
    {
        if (discoveryState != null)
        {
            discoveryState.DiscoverSystem(systemId);
        }
    }

    /// <summary>
    /// Mark a wormhole and both of its endpoint systems as discovered.
    /// Use this when a wormhole is scanned or traversed.
    /// </summary>
    public void DiscoverWormholeAndEndpoints(int wormholeId)
    {
        if (discoveryState != null)
        {
            discoveryState.DiscoverWormholeAndEndpoints(wormholeId);
        }
    }

    #endregion
}
