using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Coordinates multi-scene, multi-physics simulation across all star systems.
/// - Creates dedicated scenes with isolated physics for each star system.
/// - Maintains StarSystemRuntime instances and keeps their physics updated.
/// - Moves ships between scenes when travelling through wormholes.
/// - Provides a lightweight debug UI to show loaded systems.
/// </summary>
public class GalaxySimulationManager : MonoBehaviour
{
    public static GalaxySimulationManager Instance { get; private set; }

    [Header("Core References")]
    [Tooltip("Galaxy generator providing system metadata and wormhole connectivity.")]
    [SerializeField] private GalaxyGenerator galaxy;

    [Tooltip("Authoritative discovery state for the player.")]
    [SerializeField] private GameDiscoveryState discoveryState;

    [Header("Scene Settings")]
    [Tooltip("Gate prefab spawned inside each star system scene.")]
    [SerializeField] private WormholeGate wormholeGatePrefab;

    [Tooltip("Scale applied to galaxy coordinates when mapping to world positions.")]
    [SerializeField] private float systemPositionScale = 1f;

    [Tooltip("Radius around the system center where wormhole gates are placed.")]
    [SerializeField] private float gateRingRadius = 50f;

    [Tooltip("Optional transform used as a baseline for gate ring centers if galaxy data is unavailable.")]
    [SerializeField] private Transform defaultGateRingCenter;

    [Header("Debug UI")]
    [Tooltip("Label used to list currently loaded star system scenes.")]
    [SerializeField] private TMP_Text loadedSystemsLabel;

    [Header("Behaviour")]
    [Tooltip("Load the current system from discovery state on Start.")]
    [SerializeField] private bool autoLoadCurrentSystem = true;

    [Tooltip("Simulate all loaded physics scenes every FixedUpdate.")]
    [SerializeField] private bool simulateAllSystems = true;

    private readonly Dictionary<int, StarSystemRuntime> runtimesBySystemId = new Dictionary<int, StarSystemRuntime>();
    private readonly Dictionary<GameObject, int> shipToSystemMap = new Dictionary<GameObject, int>();
    private readonly List<StarSystemRuntime> simulationBuffer = new List<StarSystemRuntime>();

    /// <summary>
    /// Collection of active star system runtimes keyed by system ID.
    /// </summary>
    public IReadOnlyDictionary<int, StarSystemRuntime> RuntimesBySystemId => runtimesBySystemId;

    /// <summary>
    /// Gate ring radius used for runtime gate placement.
    /// </summary>
    public float GateRingRadius => gateRingRadius;

    /// <summary>
    /// Check whether a given ship is already registered inside the specified system runtime.
    /// </summary>
    /// <param name="ship">Ship instance to evaluate.</param>
    /// <param name="systemId">System identifier to verify.</param>
    /// <returns>True if the ship is tracked inside the requested system.</returns>
    public bool IsShipInSystem(GameObject ship, int systemId)
    {
        if (ship == null)
            return false;

        return shipToSystemMap.TryGetValue(ship, out var registeredSystem) && registeredSystem == systemId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveDependencies();
    }

    private void Start()
    {
        ResolveDependencies();

        if (discoveryState != null)
        {
            discoveryState.CurrentSystemChanged += OnCurrentSystemChanged;
        }

        if (autoLoadCurrentSystem && discoveryState != null && discoveryState.CurrentSystemId >= 0)
        {
            EnsureSystemLoaded(discoveryState.CurrentSystemId);
        }

        RefreshLoadedSystemsLabel();
    }

    private void OnDestroy()
    {
        if (discoveryState != null)
        {
            discoveryState.CurrentSystemChanged -= OnCurrentSystemChanged;
        }
    }

    private void FixedUpdate()
    {
        if (!simulateAllSystems)
            return;

        float dt = Time.fixedDeltaTime;
        simulationBuffer.Clear();

        foreach (var pair in runtimesBySystemId)
        {
            if (pair.Value != null)
            {
                simulationBuffer.Add(pair.Value);
            }
        }

        foreach (var runtime in simulationBuffer)
        {
            runtime.Simulate(dt);
        }
    }

    /// <summary>
    /// Ensure that a runtime scene exists for the requested system ID.
    /// </summary>
    /// <param name="systemId">Target system identifier.</param>
    /// <returns>Runtime instance created or located for the system.</returns>
    public StarSystemRuntime EnsureSystemLoaded(int systemId)
    {
        if (systemId < 0)
            return null;

        if (runtimesBySystemId.TryGetValue(systemId, out var existing))
            return existing;

        ResolveDependencies();

        string sceneName = $"StarSystem_{systemId}";
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid())
        {
            scene = SceneManager.CreateScene(sceneName, new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        }

        StarSystemRuntime runtime = FindOrCreateRuntime(systemId, scene);

        float systemRadius = ResolveSystemRadius(systemId);
        runtime.Initialize(systemId, scene, systemRadius);
        runtime.ConfigureGatePlacement(gateRingRadius, defaultGateRingCenter != null ? (Vector3?)defaultGateRingCenter.position : null);

        MoveStarVisualToScene(systemId, scene);

        if (wormholeGatePrefab == null)
        {
            Debug.LogWarning("GalaxySimulationManager: Wormhole gate prefab is not assigned. Gates will not be spawned.");
        }

        runtime.BuildWormholeGates(galaxy, wormholeGatePrefab, systemPositionScale);

        runtimesBySystemId[systemId] = runtime;
        RefreshLoadedSystemsLabel();

        return runtime;
    }

    private StarSystemRuntime FindOrCreateRuntime(int systemId, Scene scene)
    {
        StarSystemRuntime runtime = FindRuntimeInScene(systemId, scene);
        if (runtime != null)
            return runtime;

        GameObject runtimeRoot = new GameObject($"StarSystemRuntime_{systemId}");
        SceneManager.MoveGameObjectToScene(runtimeRoot, scene);
        return runtimeRoot.AddComponent<StarSystemRuntime>();
    }

    private StarSystemRuntime FindRuntimeInScene(int systemId, Scene scene)
    {
        if (!scene.IsValid())
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            StarSystemRuntime runtime = root.GetComponent<StarSystemRuntime>();
            if (runtime != null && runtime.SystemId == systemId)
            {
                return runtime;
            }
        }

        return null;
    }

    /// <summary>
    /// Move the star system's visual instance into the runtime scene if one exists.
    /// </summary>
    /// <param name="systemId">Identifier of the system being loaded.</param>
    /// <param name="scene">Scene that should own the visual objects.</param>
    private void MoveStarVisualToScene(int systemId, Scene scene)
    {
        if (galaxy == null)
            return;

        if (!galaxy.TryGetSystem(systemId, out var node))
            return;

        if (node.starSystemInstance == null)
            return;

        Transform starTransform = node.starSystemInstance.transform;
        if (starTransform.parent != null)
        {
            starTransform.SetParent(null);
        }

        SceneManager.MoveGameObjectToScene(starTransform.gameObject, scene);
        starTransform.gameObject.SetActive(true);
    }

    /// <summary>
    /// Move a ship GameObject into the requested system scene, aligning it to an exit if provided.
    /// </summary>
    /// <param name="ship">Ship instance being transferred.</param>
    /// <param name="targetSystemId">Destination system identifier.</param>
    /// <param name="exitTransform">Optional transform used to place the ship.</param>
    /// <param name="fallbackGate">Optional gate used to derive a position if no exit transform is present.</param>
    public void MoveShipToSystem(GameObject ship, int targetSystemId, Transform exitTransform, WormholeGate fallbackGate)
    {
        if (ship == null)
            return;

        StarSystemRuntime runtime = EnsureSystemLoaded(targetSystemId);
        if (runtime == null)
            return;

        if (shipToSystemMap.TryGetValue(ship, out var currentSystem) && runtimesBySystemId.TryGetValue(currentSystem, out var currentRuntime))
        {
            currentRuntime.UnregisterShip(ship);
        }

        SceneManager.MoveGameObjectToScene(ship, runtime.OwningScene);
        runtime.RegisterShip(ship);
        shipToSystemMap[ship] = targetSystemId;

        Vector3 targetPosition = runtime.GateRingCenter;
        Quaternion targetRotation = Quaternion.identity;

        if (exitTransform != null)
        {
            targetPosition = exitTransform.position;
            targetRotation = exitTransform.rotation;
        }
        else if (fallbackGate != null)
        {
            targetPosition = fallbackGate.transform.position + fallbackGate.transform.forward * 5f;
            targetRotation = fallbackGate.transform.rotation;
        }
        else if (defaultGateRingCenter != null)
        {
            targetPosition = defaultGateRingCenter.position;
            targetRotation = defaultGateRingCenter.rotation;
        }

        ship.transform.position = targetPosition;
        ship.transform.rotation = targetRotation;

        Rigidbody rb = ship.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayerShipController playerController = ship.GetComponent<PlayerShipController>();
        if (playerController != null)
        {
            playerController.SetSystemBoundaryRadius(runtime.SystemRadius);
        }

        RefreshLoadedSystemsLabel();
    }

    /// <summary>
    /// Resolve a gate for a given system and wormhole identifier.
    /// </summary>
    /// <param name="systemId">System to search.</param>
    /// <param name="wormholeId">Wormhole identifier.</param>
    /// <param name="gate">Resolved gate instance.</param>
    /// <returns>True if a matching gate exists in the requested system.</returns>
    public bool TryGetGateForWormhole(int systemId, int wormholeId, out WormholeGate gate)
    {
        gate = null;
        if (runtimesBySystemId.TryGetValue(systemId, out var runtime))
        {
            return runtime.TryGetGateForWormhole(wormholeId, out gate);
        }

        return false;
    }

    /// <summary>
    /// Resolve a gate for a given system targeting the requested destination.
    /// </summary>
    /// <param name="systemId">System to search.</param>
    /// <param name="targetSystemId">Destination system identifier.</param>
    /// <param name="gate">Resolved gate if found.</param>
    /// <returns>True if a gate targeting the destination exists in the system.</returns>
    public bool TryGetGateForTargetSystem(int systemId, int targetSystemId, out WormholeGate gate)
    {
        gate = null;
        if (runtimesBySystemId.TryGetValue(systemId, out var runtime))
        {
            return runtime.TryGetGateForTargetSystem(targetSystemId, out gate);
        }

        return false;
    }

    /// <summary>
    /// Compute a system radius from galaxy data, falling back to the gate ring radius.
    /// </summary>
    /// <param name="systemId">System identifier being resolved.</param>
    /// <returns>Configured playable radius for the system.</returns>
    private float ResolveSystemRadius(int systemId)
    {
        if (galaxy != null && galaxy.TryGetSystem(systemId, out var node))
        {
            return Mathf.Max(node.systemRadius, gateRingRadius);
        }

        return gateRingRadius;
    }

    private void ResolveDependencies()
    {
        if (galaxy == null)
            galaxy = GalaxyGenerator.Instance;

        if (discoveryState == null)
            discoveryState = GameDiscoveryState.Instance;
    }

    private void OnCurrentSystemChanged(int systemId)
    {
        if (autoLoadCurrentSystem)
        {
            EnsureSystemLoaded(systemId);
        }

        RefreshLoadedSystemsLabel();
    }

    private void RefreshLoadedSystemsLabel()
    {
        if (loadedSystemsLabel == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.Append("Loaded Systems: ");

        bool first = true;
        foreach (var pair in runtimesBySystemId)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            int id = pair.Key;
            sb.Append(id);

            if (discoveryState != null && discoveryState.CurrentSystemId == id)
            {
                sb.Append(" (player)");
            }
        }

        loadedSystemsLabel.text = sb.ToString();
    }
}
