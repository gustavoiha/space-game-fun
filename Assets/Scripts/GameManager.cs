using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boots the game and manages system-level state:
/// - Chooses a Horizon start system
/// - Spawns the player near that system (in empty space)
/// - Spawns wormhole gates for the current system
/// - Handles jumps between systems
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GalaxyGenerator galaxy;
    public GameObject playerShipPrefab;
    public GameObject wormholeGatePrefab;

    [Header("Spawn Settings")]
    [Tooltip("Distance from a system centre where the player spawns.")]
    public float startDistanceFromStar = 60f;

    [Tooltip("Radius around a system where wormhole gates are placed.")]
    public float gateRingRadius = 200f;

    [Header("Runtime")]
    [Tooltip("ID of the star system the player is currently in.")]
    public int currentSystemId = -1;

    private GameObject playerInstance;
    private readonly List<GameObject> activeGates = new List<GameObject>();

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

        SpawnPlayerAtSystem(currentSystemId);
        SpawnWormholeGates();
    }

    private void SpawnPlayerAtSystem(int systemId)
    {
        var sys = galaxy.GetSystem(systemId);
        if (sys == null) return;

        // Random horizontal direction from system centre.
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 spawnPos = sys.position + dir * startDistanceFromStar;

        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerShipPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            playerInstance.transform.position = spawnPos;
            playerInstance.transform.rotation = Quaternion.identity;
        }

        // Face toward system centre (optional)
        playerInstance.transform.LookAt(sys.position);
    }

    private void SpawnWormholeGates()
    {
        ClearWormholeGates();

        var sys = galaxy.GetSystem(currentSystemId);
        if (sys == null || wormholeGatePrefab == null) return;

        int linkCount = sys.wormholeLinks.Count;
        if (linkCount == 0) return;

        for (int i = 0; i < linkCount; i++)
        {
            int targetId = sys.wormholeLinks[i];
            var targetSys = galaxy.GetSystem(targetId);
            if (targetSys == null) continue;

            float angleDeg = (360f / linkCount) * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * gateRingRadius;
            Vector3 gatePos = sys.position + offset;

            Quaternion rot = Quaternion.LookRotation(sys.position - gatePos, Vector3.up);

            GameObject gateObj = Instantiate(wormholeGatePrefab, gatePos, rot);
            var gate = gateObj.GetComponent<WormholeGate>();
            if (gate != null)
            {
                gate.targetSystemId = targetId;
            }

            activeGates.Add(gateObj);
        }
    }

    private void ClearWormholeGates()
    {
        foreach (var go in activeGates)
        {
            if (go != null)
                Destroy(go);
        }
        activeGates.Clear();
    }

    /// <summary>
    /// Called by WormholeGate when the player activates a gate.
    /// </summary>
    public void JumpToSystem(int targetSystemId)
    {
        var targetSys = galaxy.GetSystem(targetSystemId);
        if (targetSys == null) return;

        currentSystemId = targetSystemId;

        galaxy.DiscoverSystem(targetSystemId);
        targetSys.visited = true;

        SpawnPlayerAtSystem(currentSystemId);
        SpawnWormholeGates();
    }
}
