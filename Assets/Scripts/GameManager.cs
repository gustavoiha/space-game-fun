using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boots the game and manages system-level state:
/// - Chooses a Horizon start system
/// - Spawns the player near a wormhole gate in that system
/// - Spawns wormhole gates for the current system, oriented toward their target systems
/// - Handles jumps between systems
/// - Notifies listeners when galaxy state changes (for UI refresh)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Raised whenever the player's galaxy-related state changes in a way
    /// that should be reflected in UI (e.g. current system, discovery).
    /// </summary>
    public static event Action OnGalaxyStateChanged;

    [Header("References")]
    public GalaxyGenerator galaxy;
    public GameObject playerShipPrefab;
    public GameObject wormholeGatePrefab;

    [Header("Spawn Settings")]
    [Tooltip("Default distance from a system centre where the player spawns if no gate is available.")]
    public float startDistanceFromStar = 60f;

    [Tooltip("Distance from a wormhole gate where the player appears when spawning near it.")]
    public float playerSpawnDistanceFromGate = 40f;

    [Header("Wormhole Gate Placement")]
    [Tooltip("Radius around a system where wormhole gates are placed.")]
    public float gateRingRadius = 200f;

    [Header("Runtime")]
    [Tooltip("ID of the star system the player is currently in.")]
    public int currentSystemId = -1;

    private GameObject playerInstance;
    private readonly List<WormholeGate> activeGates = new List<WormholeGate>();

    public Transform PlayerTransform => playerInstance != null ? playerInstance.transform : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally persist across scenes:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (galaxy == null)
        {
            Debug.LogError("GameManager: Galaxy reference not assigned.");
            return;
        }
        if (playerShipPrefab == null)
        {
            Debug.LogError("GameManager: Player ship prefab not assigned.");
            return;
        }
        if (wormholeGatePrefab == null)
        {
            Debug.LogError("GameManager: Wormhole gate prefab not assigned.");
            return;
        }

        var startSystem = galaxy.GetStartSystem();
        if (startSystem == null)
        {
            Debug.LogError("GameManager: No start system found.");
            return;
        }

        currentSystemId = startSystem.id;

        // Mark the starting system discovered/visited.
        galaxy.DiscoverSystem(currentSystemId);
        startSystem.visited = true;

        // Spawn gates first so the player can be placed relative to a gate.
        SpawnWormholeGates();

        // Spawn player close to a wormhole gate if possible.
        SpawnPlayerAtSystemStart(currentSystemId);

        RaiseGalaxyStateChanged();
    }

    /// <summary>
    /// Spawns the player when the game first starts.
    /// Prefers spawning near one of the system's wormhole gates.
    /// </summary>
    private void SpawnPlayerAtSystemStart(int systemId)
    {
        var sys = galaxy.GetSystem(systemId);
        if (sys == null) return;

        WormholeGate gateForSpawn = activeGates.Count > 0 ? activeGates[0] : null;

        if (gateForSpawn != null)
        {
            SpawnPlayerNearGate(gateForSpawn, sys);
        }
        else
        {
            SpawnPlayerNearSystemCentre(sys);
        }
    }

    /// <summary>
    /// Helper to create or move the player ship instance.
    /// </summary>
    private void SpawnOrMovePlayer(Vector3 position, Quaternion rotation)
    {
        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerShipPrefab, position, rotation);
        }
        else
        {
            playerInstance.transform.SetPositionAndRotation(position, rotation);
        }
    }

    /// <summary>
    /// Places the player a short distance "inside" the system from a given gate.
    /// </summary>
    private void SpawnPlayerNearGate(WormholeGate gate, StarSystemData sys)
    {
        if (gate == null) return;

        Vector3 inwardDir;

        if (sys != null)
        {
            // Direction from gate back toward the system centre.
            inwardDir = sys.position - gate.transform.position;
        }
        else
        {
            inwardDir = -gate.transform.forward;
        }

        inwardDir.y = 0f;
        if (inwardDir.sqrMagnitude < 0.0001f)
            inwardDir = -gate.transform.forward;

        inwardDir.Normalize();

        Vector3 spawnPos = gate.transform.position + inwardDir * playerSpawnDistanceFromGate;
        Quaternion rot = Quaternion.LookRotation(inwardDir, Vector3.up);

        SpawnOrMovePlayer(spawnPos, rot);
    }

    /// <summary>
    /// Fallback spawn near the system centre in a random horizontal direction.
    /// </summary>
    private void SpawnPlayerNearSystemCentre(StarSystemData sys)
    {
        if (sys == null) return;

        // Random horizontal direction from system centre.
        Vector3 dir = UnityEngine.Random.onUnitSphere;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 spawnPos = sys.position + dir * startDistanceFromStar;
        Quaternion rot = Quaternion.LookRotation(sys.position - spawnPos, Vector3.up);

        SpawnOrMovePlayer(spawnPos, rot);
    }

    /// <summary>
    /// Spawns wormhole gates around the current system.
    /// Each gate is placed in the direction of the system it connects to.
    /// </summary>
    private void SpawnWormholeGates()
    {
        ClearWormholeGates();

        var sys = galaxy.GetSystem(currentSystemId);
        if (sys == null || wormholeGatePrefab == null) return;

        int linkCount = sys.wormholeLinks.Count;
        if (linkCount == 0) return;

        Vector3 sysPos = sys.position;

        for (int i = 0; i < linkCount; i++)
        {
            int targetId = sys.wormholeLinks[i];
            var targetSys = galaxy.GetSystem(targetId);
            if (targetSys == null) continue;

            // Direction on the galaxy map from this system to the target system.
            Vector3 toTarget = targetSys.position - sysPos;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                toTarget = Vector3.forward;

            Vector3 dir = toTarget.normalized;

            Vector3 gatePos = sysPos + dir * gateRingRadius;

            // Gate faces roughly outward toward the target system.
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            GameObject gateObj = Instantiate(wormholeGatePrefab, gatePos, rot);
            var gate = gateObj.GetComponent<WormholeGate>();
            if (gate != null)
            {
                gate.targetSystemId = targetId;
                activeGates.Add(gate);
            }
        }
    }

    private void ClearWormholeGates()
    {
        foreach (var gate in activeGates)
        {
            if (gate != null)
                Destroy(gate.gameObject);
        }
        activeGates.Clear();
    }

    /// <summary>
    /// Returns the nearest active gate to the given position, within maxDistance.
    /// Returns null if none are in range.
    /// </summary>
    public WormholeGate FindNearestGate(Vector3 position, float maxDistance)
    {
        WormholeGate best = null;
        float maxSqr = maxDistance * maxDistance;

        foreach (var gate in activeGates)
        {
            if (gate == null) continue;

            float sqr = (gate.transform.position - position).sqrMagnitude;
            if (sqr <= maxSqr)
            {
                maxSqr = sqr;
                best = gate;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns a gate in the current system that leads to the given system id, if any.
    /// </summary>
    public WormholeGate FindGateLeadingToSystem(int systemId)
    {
        foreach (var gate in activeGates)
        {
            if (gate != null && gate.targetSystemId == systemId)
                return gate;
        }
        return null;
    }

    /// <summary>
    /// Performs a jump for a given ship through a specific gate.
    /// Player ships spawn near the gate in the destination system that leads back
    /// to the system they came from (if such a gate exists).
    /// </summary>
    public void JumpShipToSystem(ShipWormholeNavigator ship, WormholeGate sourceGate)
    {
        if (ship == null || galaxy == null || sourceGate == null)
            return;

        int targetSystemId = sourceGate.targetSystemId;
        var targetSys = galaxy.GetSystem(targetSystemId);
        if (targetSys == null)
            return;

        if (ship.isPlayerControlled)
        {
            int fromSystemId = currentSystemId;

            currentSystemId = targetSystemId;

            galaxy.DiscoverSystem(targetSystemId);
            targetSys.visited = true;

            // Build gates for the target system first so we can find the one pointing back.
            SpawnWormholeGates();

            // Try to position the player near the gate that leads back to the previous system.
            WormholeGate exitGate = FindGateLeadingToSystem(fromSystemId);

            if (exitGate != null)
            {
                SpawnPlayerNearGate(exitGate, targetSys);
            }
            else
            {
                SpawnPlayerNearSystemCentre(targetSys);
            }

            RaiseGalaxyStateChanged();
        }
        else
        {
            // NPC: simple behaviour, just move the ship near the target system's centre.
            Vector3 dir = UnityEngine.Random.onUnitSphere;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 spawnPos = targetSys.position + dir * startDistanceFromStar;
            ship.transform.position = spawnPos;
            ship.transform.LookAt(targetSys.position);
        }
    }

    private void RaiseGalaxyStateChanged()
    {
        OnGalaxyStateChanged?.Invoke();
    }
}
