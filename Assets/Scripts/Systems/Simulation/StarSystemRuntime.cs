using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime owner for a single star system scene. Handles:
/// - Tracking the dedicated Unity scene and physics scene for this system.
/// - Spawning and caching wormhole gates local to the system.
/// - Optional simulation of the physics scene when the player is not present.
/// - Lightweight bookkeeping for ships registered within the scene.
/// </summary>
public class StarSystemRuntime : MonoBehaviour
{
    [Header("Runtime Identity")]
    [Tooltip("Stable galaxy identifier for this star system.")]
    [SerializeField] private int systemId = -1;

    [Tooltip("World-space radius used for player boundary clamping inside this system.")]
    [SerializeField] private float systemRadius = 4000f;

    [Header("Gate Placement")]
    [Tooltip("Parent transform used for spawned wormhole gates.")]
    [SerializeField] private Transform gateContainer;

    [Tooltip("Radius from the system center where gates will be positioned.")]
    [SerializeField] private float gateRingRadius = 50f;

    [Tooltip("World-space center used when placing gates. Defaults to the generated system position.")]
    [SerializeField] private Vector3 gateRingCenter = Vector3.zero;

    [Header("Simulation")]
    [Tooltip("Simulate this physics scene explicitly when the player is not present.")]
    [SerializeField] private bool simulateOffscreen = true;

    [Tooltip("Registered gates spawned for this star system scene.")]
    [SerializeField] private List<WormholeGate> gates = new List<WormholeGate>();

    [Tooltip("Ships that are currently registered with this runtime (player and AI).")]
    [SerializeField] private List<GameObject> registeredShips = new List<GameObject>();

    private Scene owningScene;
    private PhysicsScene physicsScene;

    /// <summary>
    /// Identifier of the system represented by this runtime instance.
    /// </summary>
    public int SystemId => systemId;

    /// <summary>
    /// Physics-aware radius for objects within this system.
    /// </summary>
    public float SystemRadius => systemRadius;

    /// <summary>
    /// Unity scene backing this star system runtime.
    /// </summary>
    public Scene OwningScene => owningScene;

    /// <summary>
    /// Physics scene associated with the owning scene.
    /// </summary>
    public PhysicsScene PhysicsScene => physicsScene;

    /// <summary>
    /// World position used for placing gates and default spawn points.
    /// </summary>
    public Vector3 GateRingCenter => gateRingCenter;

    /// <summary>
    /// Collection of gates active within this system.
    /// </summary>
    public IReadOnlyList<WormholeGate> Gates => gates;

    /// <summary>
    /// Collection of ships registered to this runtime.
    /// </summary>
    public IReadOnlyList<GameObject> RegisteredShips => registeredShips;

    /// <summary>
    /// Initialize runtime metadata and hook into the owning scene.
    /// </summary>
    /// <param name="id">System identifier.</param>
    /// <param name="scene">Scene created or loaded for this system.</param>
    /// <param name="radius">Playable radius for boundary clamping.</param>
    public void Initialize(int id, Scene scene, float radius)
    {
        systemId = id;
        owningScene = scene;
        physicsScene = scene.GetPhysicsScene();
        systemRadius = Mathf.Max(0f, radius);

        if (gateContainer == null)
        {
            gateContainer = transform;
        }
    }

    /// <summary>
    /// Configure gate placement defaults supplied by the simulation manager.
    /// </summary>
    /// <param name="ringRadius">Radius from the system center where gates should appear.</param>
    /// <param name="fallbackCenter">Optional explicit center used when galaxy data is missing.</param>
    public void ConfigureGatePlacement(float ringRadius, Vector3? fallbackCenter)
    {
        gateRingRadius = Mathf.Max(1f, ringRadius);

        if (fallbackCenter.HasValue)
        {
            gateRingCenter = fallbackCenter.Value;
        }
    }

    /// <summary>
    /// Configure gate placement and spawn gates for all neighbors of this system.
    /// </summary>
    /// <param name="galaxy">Galaxy data source providing system and wormhole information.</param>
    /// <param name="gatePrefab">Prefab used to instantiate wormhole gates.</param>
    /// <param name="systemPositionScale">Scale factor applied to galaxy coordinates when positioning gates.</param>
    public void BuildWormholeGates(GalaxyGenerator galaxy, WormholeGate gatePrefab, float systemPositionScale)
    {
        ClearGates();

        if (galaxy == null || gatePrefab == null)
            return;

        if (galaxy.TryGetSystem(systemId, out var node))
        {
            systemRadius = Mathf.Max(systemRadius, Mathf.Max(0f, node.systemRadius));
            float gateRadiusFromStar = node.starRadius * 1.5f;
            float gateRadiusFromSystem = Mathf.Max(systemRadius * 0.25f, 10f);
            gateRingRadius = Mathf.Max(gateRingRadius, gateRadiusFromStar, gateRadiusFromSystem);
            gateRingCenter = new Vector3(node.position.x * systemPositionScale, 0f, node.position.y * systemPositionScale);
        }

        IReadOnlyList<int> neighbors = galaxy.GetNeighbors(systemId);
        if (neighbors == null || neighbors.Count == 0)
            return;

        float angleStep = 360f / Mathf.Max(1, neighbors.Count);
        Vector3 center = gateRingCenter;

        for (int i = 0; i < neighbors.Count; i++)
        {
            int neighborId = neighbors[i];
            int wormholeId = FindWormholeIdBetween(galaxy, systemId, neighborId);
            Vector3 gatePos;
            Quaternion gateRot;

            if (TryGetSystemWorldPosition(galaxy, neighborId, systemPositionScale, out var neighborPos))
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

            WormholeGate gateInstance = Instantiate(gatePrefab, gatePos, gateRot);
            gateInstance.SetWormholeId(wormholeId);
            gateInstance.SetExplicitTargetSystemId(neighborId);
            SceneManager.MoveGameObjectToScene(gateInstance.gameObject, owningScene);
            gateInstance.transform.SetParent(gateContainer, true);
            gates.Add(gateInstance);
        }
    }

    /// <summary>
    /// Simulate the physics scene manually when configured to do so.
    /// </summary>
    /// <param name="fixedDeltaTime">Fixed delta time passed from the simulation manager.</param>
    public void Simulate(float fixedDeltaTime)
    {
        if (!simulateOffscreen)
            return;

        if (!physicsScene.IsValid())
            return;

        physicsScene.Simulate(fixedDeltaTime);
    }

    /// <summary>
    /// Register a ship GameObject with this runtime for bookkeeping and AI queries.
    /// </summary>
    /// <param name="ship">Ship instance moved into this star system scene.</param>
    public void RegisterShip(GameObject ship)
    {
        if (ship == null)
            return;

        if (!registeredShips.Contains(ship))
        {
            registeredShips.Add(ship);
        }
    }

    /// <summary>
    /// Remove a ship from the registered list when leaving the system.
    /// </summary>
    /// <param name="ship">Ship instance being removed from this system.</param>
    public void UnregisterShip(GameObject ship)
    {
        if (ship == null)
            return;

        registeredShips.Remove(ship);
    }

    /// <summary>
    /// Attempt to resolve a gate by wormhole identifier within this runtime.
    /// </summary>
    /// <param name="wormholeId">Identifier of the wormhole link.</param>
    /// <param name="gate">Resolved gate if one exists.</param>
    /// <returns>True if a gate with the given wormhole is active in this system.</returns>
    public bool TryGetGateForWormhole(int wormholeId, out WormholeGate gate)
    {
        for (int i = 0; i < gates.Count; i++)
        {
            WormholeGate candidate = gates[i];
            if (candidate != null && candidate.WormholeId == wormholeId)
            {
                gate = candidate;
                return true;
            }
        }

        gate = null;
        return false;
    }

    /// <summary>
    /// Attempt to resolve a gate whose explicit target matches the requested system.
    /// </summary>
    /// <param name="targetSystemId">Destination system identifier.</param>
    /// <param name="gate">Resolved gate instance.</param>
    /// <returns>True if a gate targeting the requested system is active.</returns>
    public bool TryGetGateForTargetSystem(int targetSystemId, out WormholeGate gate)
    {
        for (int i = 0; i < gates.Count; i++)
        {
            WormholeGate candidate = gates[i];
            if (candidate != null && candidate.ExplicitTargetSystemId == targetSystemId)
            {
                gate = candidate;
                return true;
            }
        }

        gate = null;
        return false;
    }

    /// <summary>
    /// Remove all spawned gates before rebuilding the system layout.
    /// </summary>
    public void ClearGates()
    {
        for (int i = 0; i < gates.Count; i++)
        {
            WormholeGate gate = gates[i];
            if (gate != null)
            {
                Destroy(gate.gameObject);
            }
        }

        gates.Clear();
    }

    private int FindWormholeIdBetween(GalaxyGenerator galaxy, int systemAId, int systemBId)
    {
        if (galaxy == null || galaxy.Wormholes == null)
            return -1;

        for (int i = 0; i < galaxy.Wormholes.Count; i++)
        {
            GalaxyGenerator.WormholeLink link = galaxy.Wormholes[i];
            if (link.IsIncidentTo(systemAId) && link.GetOtherSystem(systemAId) == systemBId)
            {
                return link.id;
            }
        }

        return -1;
    }

    private bool TryGetSystemWorldPosition(GalaxyGenerator galaxy, int system, float scale, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (galaxy != null && galaxy.TryGetSystem(system, out var node))
        {
            worldPos = new Vector3(node.position.x * scale, 0f, node.position.y * scale);
            return true;
        }

        return false;
    }
}
