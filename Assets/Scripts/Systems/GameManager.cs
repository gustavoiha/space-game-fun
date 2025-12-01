using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// High-level game coordinator:
/// - Keeps references to GalaxyGenerator, GameDiscoveryState, and the map
/// - Bridges current-system changes into simple UI (current system label)
/// - Spawns the player ship prefab once if needed
/// - Spawns wormhole gate instances for the current system
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

    [Header("UI")]
    [SerializeField] private TMP_Text currentSystemLabel;

    [Header("Player Ship")]
    [Tooltip("Prefab of the player ship to spawn at game start if no existing ship is found.")]
    [SerializeField] private GameObject playerShipPrefab;

    [Tooltip("Optional spawn point for the player ship. If null, the ship spawns at world origin.")]
    [SerializeField] private Transform playerSpawnPoint;

    [Tooltip("Tag used to find an existing player ship in the scene before spawning a new one.")]
    [SerializeField] private string playerShipTag = "PlayerShip";

    [Header("Wormhole Gates")]
    [Tooltip("Prefab for wormhole gates that will be spawned for each connection from the current system.")]
    [SerializeField] private WormholeGate wormholeGatePrefab;

    [Tooltip("Optional parent transform for spawned wormhole gates.")]
    [SerializeField] private Transform wormholeGatesParent;

    [Tooltip("Scale factor applied when mapping 2D galaxy coordinates into 3D world space (x, z).")]
    [SerializeField] private float systemPositionScale = 1f;

    [Tooltip("Radius around the origin at which to place wormhole gates for the current system.")]
    [SerializeField] private float gateRingRadius = 50f;

    [Tooltip("World-space center used for placing gate ring. If null, Vector3.zero is used.")]
    [SerializeField] private Transform gateRingCenter;

    private GameObject playerShipInstance;
    private readonly List<WormholeGate> activeGates = new List<WormholeGate>();

    private bool hasAlignedInitialShipPosition;
    private Vector3 currentSystemWorldPosition = Vector3.zero;
    private float currentSystemRadius;

    public int CurrentSystemId => discoveryState != null ? discoveryState.CurrentSystemId : -1;
    public GameObject PlayerShip => playerShipInstance;
    public Vector3 CurrentSystemWorldPosition => currentSystemWorldPosition;
    public float GateRingRadius => gateRingRadius;
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

        if (discoveryState != null)
        {
            discoveryState.CurrentSystemChanged += OnCurrentSystemChanged;
        }

        // Ensure we have a ship in the scene
        SpawnPlayerShipIfNeeded();

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
        BuildWormholeGatesForSystem(systemId);

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

    private void ClearWormholeGates()
    {
        for (int i = 0; i < activeGates.Count; i++)
        {
            if (activeGates[i] != null)
            {
                Destroy(activeGates[i].gameObject);
            }
        }

        activeGates.Clear();
    }

    /// <summary>
    /// Spawn wormhole gates for every neighbor of the given system ID.
    /// Each gate is tied to the wormhole link ID that connects to that neighbor.
    /// </summary>
    private void BuildWormholeGatesForSystem(int systemId)
    {
        ClearWormholeGates();

        if (galaxy == null || systemId < 0)
            return;

        var neighbors = galaxy.GetNeighbors(systemId);
        if (neighbors == null || neighbors.Count == 0)
            return;

        if (wormholeGatePrefab == null)
            return;

        currentSystemWorldPosition = GetSystemWorldPosition(systemId);

        Vector3 center = currentSystemWorldPosition;
        int count = neighbors.Count;
        float angleStep = 360f / Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            int neighborId = neighbors[i];

            // Find the wormhole that connects systemId <-> neighborId
            int wormholeId = FindWormholeIdBetween(systemId, neighborId);

            Vector3 gatePos;
            Quaternion gateRot;

            if (TryGetSystemWorldPosition(neighborId, out var neighborPos))
            {
                Vector3 dir = neighborPos - center;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                {
                    dir.Normalize();
                    gatePos = center + dir * gateRingRadius;
                    gateRot = Quaternion.LookRotation((center - gatePos).normalized, Vector3.up);
                }
                else
                {
                    // Neighbor sits on top of us; fall back to even spacing.
                    float angleDeg = i * angleStep;
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * gateRingRadius;
                    gatePos = center + offset;
                    gateRot = Quaternion.LookRotation((center - gatePos).normalized, Vector3.up);
                }
            }
            else
            {
                float angleDeg = i * angleStep;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * gateRingRadius;
                gatePos = center + offset;
                gateRot = Quaternion.LookRotation((center - gatePos).normalized, Vector3.up);
            }

            WormholeGate gateInstance = Instantiate(wormholeGatePrefab, gatePos, gateRot, wormholeGatesParent);
            gateInstance.SetWormholeId(wormholeId);
            gateInstance.SetExplicitTargetSystemId(neighborId);

            activeGates.Add(gateInstance);
        }
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

        float radiusToApply = currentSystemRadius > 0f ? currentSystemRadius : gateRingRadius;
        shipController.SetSystemBoundaryRadius(radiusToApply);
    }

    /// <summary>
    /// Brute-force search for the wormhole link ID connecting two systems.
    /// Returns -1 if no link is found.
    /// </summary>
    private int FindWormholeIdBetween(int systemAId, int systemBId)
    {
        if (galaxy == null || galaxy.Wormholes == null)
            return -1;

        for (int i = 0; i < galaxy.Wormholes.Count; i++)
        {
            var w = galaxy.Wormholes[i];
            if (w.IsIncidentTo(systemAId) && w.GetOtherSystem(systemAId) == systemBId)
            {
                return w.id;
            }
        }

        return -1;
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
        for (int i = 0; i < activeGates.Count; i++)
        {
            var candidate = activeGates[i];
            if (candidate != null && candidate.WormholeId == wormholeId)
            {
                gate = candidate;
                return true;
            }
        }

        gate = null;
        return false;
    }

    public bool TryGetActiveGateForTargetSystem(int targetSystemId, out WormholeGate gate)
    {
        for (int i = 0; i < activeGates.Count; i++)
        {
            var candidate = activeGates[i];
            if (candidate != null && candidate.ExplicitTargetSystemId == targetSystemId)
            {
                gate = candidate;
                return true;
            }
        }

        gate = null;
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
