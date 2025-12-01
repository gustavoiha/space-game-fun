using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a procedural galaxy graph:
/// - Star systems placed using random distances constrained by min/max spacing
/// - Wormhole connections between systems, with randomized connection counts per system
///
/// Each system and each wormhole has a stable integer ID for this galaxy instance.
/// Other systems (map, save, gameplay) should reference systems and wormholes by ID.
/// </summary>
public class GalaxyGenerator : MonoBehaviour
{
    public static GalaxyGenerator Instance { get; private set; }

    [Header("Generation Settings")]
    [SerializeField] private int targetSystemCount = 40;

    [Tooltip("Prefab used to represent an individual generated star system.")]
    [SerializeField] private StarSystem starSystemPrefab;

    [Tooltip("Minimum allowed distance between two systems.")]
    [SerializeField] private float minSystemDistance = 10f;

    [Tooltip("Maximum allowed distance between two systems.")]
    [SerializeField] private float maxSystemDistance = 60f;

    [Tooltip("Minimum playable radius assigned to generated star systems.")]
    [SerializeField] private float minSystemRadius = 3000f;

    [Tooltip("Maximum playable radius assigned to generated star systems.")]
    [SerializeField] private float maxSystemRadius = 5000f;

    [Tooltip("How many candidate points to try per active site in Poisson sampling.")]
    [SerializeField] private int poissonCandidatesPerPoint = 12;

    [Tooltip("Small random offset applied after a valid sample to avoid overly regular layouts.")]
    [SerializeField] private float placementJitter = 0.5f;

    [Tooltip("How many attempts to make for a valid placement before falling back to edge placement.")]
    [SerializeField] private int maxPlacementRetries = 8;

    [Header("Connections")]
    [Tooltip("Minimum number of wormhole connections each system should have.")]
    [SerializeField] private int minConnectionsPerSystem = 1;

    [Tooltip("Maximum number of wormhole connections each system should have.")]
    [SerializeField] private int maxConnectionsPerSystem = 5;

    [Tooltip("Relative weights for systems having 1–5 wormholes (index 0 => 1 wormhole, etc.).")]
    [SerializeField] private List<float> connectionWeights = new List<float> { 0.3f, 0.35f, 0.2f, 0.08f, 0.02f };

    [Tooltip("Earth's Solar System (system ID 0) is forced to have exactly one wormhole connection.")]
    [SerializeField] private int earthSystemId = 0;

    [Header("Debug / Output")]
    [SerializeField] private bool generateOnAwake = true;

    [SerializeField] private List<SystemNode> systems = new List<SystemNode>();
    [SerializeField] private List<WormholeLink> wormholes = new List<WormholeLink>();

    /// <summary>
    /// Faction names sampled for generated systems.
    /// </summary>
    private static readonly string[] FactionNames =
    {
        "Horizon Initiative",
        "Helios Protectorate",
        "Independent Freehold",
        "Uncharted Space"
    };

    /// <summary>
    /// Hazard descriptors available to generated systems.
    /// </summary>
    private static readonly string[] HazardLevels =
    {
        "Low",
        "Moderate",
        "High"
    };

    /// <summary>
    /// Station name options assigned to systems for display purposes.
    /// </summary>
    private static readonly string[] StationNames =
    {
        "Listening Post",
        "Relay Hub",
        "Research Outpost",
        "Refuelling Depot",
        "Salvage Yard"
    };

    // ID lookups (built after generation)
    public IReadOnlyList<SystemNode> Systems => systems;
    public IReadOnlyList<WormholeLink> Wormholes => wormholes;

    public Dictionary<int, SystemNode> SystemsById { get; private set; } = new Dictionary<int, SystemNode>();
    public Dictionary<int, WormholeLink> WormholesById { get; private set; } = new Dictionary<int, WormholeLink>();

    /// <summary>
    /// Adjacency list: systemId -> list of neighboring systemIds reachable via wormholes.
    /// </summary>
    public Dictionary<int, List<int>> NeighborsBySystemId { get; private set; } = new Dictionary<int, List<int>>();

    /// <summary>
    /// Minimum/maximum extents of generated systems, useful for map fitting.
    /// </summary>
    public Vector2 MinPosition { get; private set; } = Vector2.zero;
    public Vector2 MaxPosition { get; private set; } = Vector2.zero;

    [Serializable]
    public class SystemNode
    {
        [Tooltip("Stable unique ID for this system within the generated galaxy.")]
        public int id;

        public Vector2 position;

        [Tooltip("Optional display name; can be generated or assigned later.")]
        public string displayName;

        [Tooltip("Faction primarily associated with this system.")]
        public string faction;

        [Tooltip("High-level hazard rating used for navigation guidance.")]
        public string hazardLevel;

        [Tooltip("List of notable or known stations present in the system.")]
        public List<string> knownStations = new List<string>();

        [Tooltip("World-space radius that bounds gameplay within this system.")]
        public float systemRadius = 4000f;

        [Tooltip("Runtime instance created from the configured star system prefab.")]
        public StarSystem starSystemInstance;
    }

    [Serializable]
    public class WormholeLink
    {
        [Tooltip("Stable unique ID for this wormhole within the generated galaxy.")]
        public int id;

        [Tooltip("ID of the 'A' system.")]
        public int fromSystemId;

        [Tooltip("ID of the 'B' system.")]
        public int toSystemId;

        public bool IsIncidentTo(int systemId)
        {
            return fromSystemId == systemId || toSystemId == systemId;
        }

        public int GetOtherSystem(int systemId)
        {
            if (fromSystemId == systemId) return toSystemId;
            if (toSystemId == systemId) return fromSystemId;
            return systemId;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (generateOnAwake)
        {
            GenerateGalaxy();
        }
        else
        {
            RebuildLookups();
        }
    }

    /// <summary>
    /// Public entry point for (re)generating the galaxy graph at runtime or in editor.
    /// </summary>
    public void GenerateGalaxy()
    {
        systems.Clear();
        wormholes.Clear();
        SystemsById.Clear();
        WormholesById.Clear();
        NeighborsBySystemId.Clear();
        MinPosition = new Vector2(float.MaxValue, float.MaxValue);
        MaxPosition = new Vector2(float.MinValue, float.MinValue);

        GenerateSystems();
        GenerateConnections();
        RebuildLookups();
    }

    /// <summary>
    /// Generate star systems with randomized spacing between min/max constraints.
    /// </summary>
    private void GenerateSystems()
    {
        if (targetSystemCount <= 0)
            return;

        // First system at origin
        AddSystem(Vector2.zero);

        List<Vector2> activeSites = new List<Vector2> { systems[0].position };
        int safety = 0;
        int maxIterations = Mathf.Max(targetSystemCount * maxPlacementRetries * 4, targetSystemCount * 8);

        while (systems.Count < targetSystemCount && safety < maxIterations)
        {
            safety++;

            // If we run out of active sites, reseed from the current edge to keep expanding outward.
            if (activeSites.Count == 0)
            {
                Vector2 reseed = SampleEdgePlacement();
                if (IsPositionValid(reseed))
                {
                    AddSystem(reseed);
                    activeSites.Add(reseed);
                }

                continue;
            }

            int activeIndex = UnityEngine.Random.Range(0, activeSites.Count);
            Vector2 center = activeSites[activeIndex];
            bool placed = false;

            for (int attempt = 0; attempt < poissonCandidatesPerPoint; attempt++)
            {
                Vector2 candidate = SamplePoissonCandidate(center);
                if (IsPositionValid(candidate))
                {
                    AddSystem(candidate);
                    activeSites.Add(candidate);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                activeSites.RemoveAt(activeIndex);
            }
        }
    }

    private void AddSystem(Vector2 position)
    {
        int id = systems.Count; // ID == index for now, stable within this generated galaxy

        float clampedMinRadius = Mathf.Min(minSystemRadius, maxSystemRadius);
        float clampedMaxRadius = Mathf.Max(minSystemRadius, maxSystemRadius);
        float radius = UnityEngine.Random.Range(clampedMinRadius, clampedMaxRadius);

        var node = new SystemNode
        {
            id = id,
            position = position,
            displayName = $"SYS-{id:D3}",
            faction = SampleFaction(id),
            hazardLevel = SampleHazardLevel(id),
            knownStations = SampleStations(id),
            systemRadius = radius
        };

        node.starSystemInstance = SpawnStarSystemInstance(node);

        systems.Add(node);

        MinPosition = Vector2.Min(MinPosition, position);
        MaxPosition = Vector2.Max(MaxPosition, position);
    }

    /// <summary>
    /// Selects a faction name for a system using its identifier for deterministic variety.
    /// </summary>
    /// <param name="systemId">Identifier of the system being generated.</param>
    /// <returns>Faction name assigned to the system.</returns>
    private string SampleFaction(int systemId)
    {
        if (FactionNames == null || FactionNames.Length == 0)
            return string.Empty;

        int index = Mathf.Abs(systemId) % FactionNames.Length;
        return FactionNames[index];
    }

    /// <summary>
    /// Chooses a hazard level for a system using its identifier for consistent sampling.
    /// </summary>
    /// <param name="systemId">Identifier of the system being generated.</param>
    /// <returns>Hazard level descriptor.</returns>
    private string SampleHazardLevel(int systemId)
    {
        if (HazardLevels == null || HazardLevels.Length == 0)
            return string.Empty;

        int index = Mathf.Abs(systemId * 3 + 1) % HazardLevels.Length;
        return HazardLevels[index];
    }

    /// <summary>
    /// Builds a list of known stations in a system based on deterministic sampling.
    /// </summary>
    /// <param name="systemId">Identifier of the system being generated.</param>
    /// <returns>List of station names assigned to the system.</returns>
    private List<string> SampleStations(int systemId)
    {
        List<string> stations = new List<string>();

        if (StationNames == null || StationNames.Length == 0)
            return stations;

        int seed = Mathf.Abs(systemId * 11 + 5);
        int stationCount = Mathf.Clamp(seed % 3, 0, 2);

        for (int i = 0; i < stationCount; i++)
        {
            int index = (seed + i * 7) % StationNames.Length;
            string station = StationNames[index];
            if (!stations.Contains(station))
            {
                stations.Add(station);
            }
        }

        return stations;
    }

    /// <summary>
    /// Spawn a star system prefab instance using generated data.
    /// </summary>
    /// <param name="node">System metadata used for initialization.</param>
    /// <returns>Instantiated <see cref="StarSystem"/> or null if no prefab is configured.</returns>
    private StarSystem SpawnStarSystemInstance(SystemNode node)
    {
        if (starSystemPrefab == null)
            return null;

        Vector3 position = new Vector3(node.position.x, 0f, node.position.y);
        StarSystem instance = Instantiate(starSystemPrefab, position, Quaternion.identity, transform);
        instance.Initialize(node.id, node.displayName, node.systemRadius);
        return instance;
    }

    private Vector2 SamplePoissonCandidate(Vector2 center)
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float spanMin = Mathf.Max(minSystemDistance, 0.001f);
        float spanMax = Mathf.Max(maxSystemDistance, spanMin + 0.001f);
        float distance = UnityEngine.Random.Range(spanMin, spanMax);

        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 candidate = center + direction * distance;

        if (placementJitter > 0f)
            candidate += UnityEngine.Random.insideUnitCircle * placementJitter;

        return candidate;
    }

    private Vector2 SampleEdgePlacement()
    {
        float currentMaxRadius = 0f;
        foreach (var system in systems)
        {
            currentMaxRadius = Mathf.Max(currentMaxRadius, system.position.magnitude);
        }

        float fallbackDistance = Mathf.Max(currentMaxRadius + maxSystemDistance * 0.5f, minSystemDistance);
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * fallbackDistance;
    }

    private bool IsPositionValid(Vector2 candidate)
    {
        foreach (var s in systems)
        {
            float d = Vector2.Distance(candidate, s.position);
            if (d < minSystemDistance)
                return false;
        }

        return true;
    }

    private void ConnectSpanningTree(ref int wormholeId, int[] currentConnections, int[] desiredConnections)
    {
        if (systems.Count <= 1)
            return;

        HashSet<int> connected = new HashSet<int> { 0 };
        bool earthIsValid = earthSystemId >= 0 && earthSystemId < systems.Count;

        while (connected.Count < systems.Count)
        {
            float bestDistance = float.MaxValue;
            int bestA = -1;
            int bestB = -1;

            foreach (int aIndex in connected)
            {
                if (earthIsValid && aIndex == earthSystemId && currentConnections[earthSystemId] >= 1)
                    continue;

                Vector2 aPos = systems[aIndex].position;

                for (int bIndex = 0; bIndex < systems.Count; bIndex++)
                {
                    if (connected.Contains(bIndex))
                        continue;

                    float distance = Vector2.Distance(aPos, systems[bIndex].position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestA = aIndex;
                        bestB = bIndex;
                    }
                }
            }

            if (bestA < 0 || bestB < 0)
                break;

            desiredConnections[bestA] = Mathf.Max(desiredConnections[bestA], currentConnections[bestA] + 1);
            desiredConnections[bestB] = Mathf.Max(desiredConnections[bestB], currentConnections[bestB] + 1);

            if (CreateConnectionIfNotExists(ref wormholeId, bestA, bestB))
            {
                currentConnections[bestA]++;
                currentConnections[bestB]++;
            }

            connected.Add(bestB);
        }
    }

    /// <summary>
    /// Generate wormhole connections between systems using randomized per-system connection counts.
    /// </summary>
    private void GenerateConnections()
    {
        if (systems.Count <= 1)
            return;

        bool earthIsValid = earthSystemId >= 0 && earthSystemId < systems.Count;

        // Precompute how many desired connections each system wants
        int[] desiredConnections = new int[systems.Count];
        int[] currentConnections = new int[systems.Count];

        for (int i = 0; i < systems.Count; i++)
        {
            if (earthIsValid && i == earthSystemId)
            {
                desiredConnections[i] = 1;
                currentConnections[i] = 0;
                continue;
            }

            int targetConnections = SampleDesiredConnectionCount();
            desiredConnections[i] = Mathf.Clamp(targetConnections, minConnectionsPerSystem, maxConnectionsPerSystem);
            currentConnections[i] = 0;
        }

        // Ensure the graph is connected by first linking all systems via a spanning tree
        int wormholeId = 0;
        ConnectSpanningTree(ref wormholeId, currentConnections, desiredConnections);

        // Then add additional connections according to the randomized targets

        for (int i = 0; i < systems.Count; i++)
        {
            var a = systems[i];

            if (earthIsValid && i == earthSystemId && currentConnections[i] >= desiredConnections[i])
                continue;

            while (currentConnections[i] < desiredConnections[i])
            {
                int bIndex = FindBestNeighborIndex(i, currentConnections, desiredConnections);
                if (bIndex < 0)
                    break;

                if (CreateConnectionIfNotExists(ref wormholeId, i, bIndex))
                {
                    currentConnections[i]++;
                    currentConnections[bIndex]++;
                }
                else
                {
                    // If the connection already exists, adjust desiredConnections to avoid infinite loops
                    desiredConnections[i] = currentConnections[i];
                }
            }
        }

        EnsureEarthHasSingleWormhole(ref wormholeId);
    }

    private int SampleDesiredConnectionCount()
    {
        if (connectionWeights == null || connectionWeights.Count == 0)
            return UnityEngine.Random.Range(minConnectionsPerSystem, maxConnectionsPerSystem + 1);

        float totalWeight = 0f;
        for (int i = 0; i < connectionWeights.Count; i++)
        {
            totalWeight += Mathf.Max(0f, connectionWeights[i]);
        }

        if (totalWeight <= 0f)
            return UnityEngine.Random.Range(minConnectionsPerSystem, maxConnectionsPerSystem + 1);

        float pick = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < connectionWeights.Count; i++)
        {
            float w = Mathf.Max(0f, connectionWeights[i]);
            if (w <= 0f)
                continue;

            cumulative += w;
            if (pick <= cumulative)
                return Mathf.Min(i + 1, maxConnectionsPerSystem); // index 0 => 1 connection
        }

        return Mathf.Min(connectionWeights.Count, maxConnectionsPerSystem);
    }

    private int FindBestNeighborIndex(int systemIndex, int[] currentConnections, int[] desiredConnections)
    {
        float bestScore = float.MaxValue;
        int bestIndex = -1;

        bool earthAtLimit = earthSystemId >= 0 && earthSystemId < systems.Count && currentConnections[earthSystemId] >= 1;

        if (systemIndex == earthSystemId && earthAtLimit)
            return -1;

        Vector2 position = systems[systemIndex].position;

        for (int i = 0; i < systems.Count; i++)
        {
            if (i == systemIndex)
                continue;

            if (earthAtLimit && i == earthSystemId)
                continue;

            // Already saturated?
            if (currentConnections[i] >= desiredConnections[i])
                continue;

            float distance = Vector2.Distance(position, systems[i].position);
            float score = distance; // could add penalties / heuristics here

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void EnsureEarthHasSingleWormhole(ref int wormholeId)
    {
        if (earthSystemId < 0 || earthSystemId >= systems.Count)
            return;

        int connectionsFound = 0;

        for (int i = wormholes.Count - 1; i >= 0; i--)
        {
            if (!wormholes[i].IsIncidentTo(earthSystemId))
                continue;

            connectionsFound++;
            if (connectionsFound > 1)
            {
                wormholes.RemoveAt(i);
            }
        }

        if (systems.Count <= 1)
            return;

        if (connectionsFound == 0)
        {
            int targetIndex = FindClosestSystemIndex(earthSystemId);
            if (targetIndex >= 0)
            {
                CreateConnectionIfNotExists(ref wormholeId, earthSystemId, targetIndex);
            }
        }
    }

    private int FindClosestSystemIndex(int sourceIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= systems.Count)
            return -1;

        float bestDistance = float.MaxValue;
        int bestIndex = -1;
        Vector2 sourcePos = systems[sourceIndex].position;

        for (int i = 0; i < systems.Count; i++)
        {
            if (i == sourceIndex)
                continue;

            float distance = Vector2.Distance(sourcePos, systems[i].position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool CreateConnectionIfNotExists(ref int wormholeId, int indexA, int indexB)
    {
        int idA = systems[indexA].id;
        int idB = systems[indexB].id;

        // Ensure consistent ordering: fromSystemId < toSystemId
        if (idB < idA)
        {
            int tmp = idA;
            idA = idB;
            idB = tmp;
        }

        // Check if a wormhole already exists between these IDs
        for (int i = 0; i < wormholes.Count; i++)
        {
            var w = wormholes[i];
            if (w.fromSystemId == idA && w.toSystemId == idB)
                return false;
        }

        var link = new WormholeLink
        {
            id = wormholeId++,
            fromSystemId = idA,
            toSystemId = idB
        };

        wormholes.Add(link);
        return true;
    }

    /// <summary>
    /// Builds all ID-based dictionaries and adjacency lists from the current systems/wormholes lists.
    /// Call this after loading or manually editing the lists in the inspector.
    /// </summary>
    public void RebuildLookups()
    {
        SystemsById.Clear();
        WormholesById.Clear();
        NeighborsBySystemId.Clear();

        RecalculateExtents();

        foreach (var system in systems)
        {
            SystemsById[system.id] = system;
            if (!NeighborsBySystemId.ContainsKey(system.id))
                NeighborsBySystemId[system.id] = new List<int>();
        }

        foreach (var wormhole in wormholes)
        {
            WormholesById[wormhole.id] = wormhole;

            if (!NeighborsBySystemId.ContainsKey(wormhole.fromSystemId))
                NeighborsBySystemId[wormhole.fromSystemId] = new List<int>();

            if (!NeighborsBySystemId.ContainsKey(wormhole.toSystemId))
                NeighborsBySystemId[wormhole.toSystemId] = new List<int>();

            if (!NeighborsBySystemId[wormhole.fromSystemId].Contains(wormhole.toSystemId))
                NeighborsBySystemId[wormhole.fromSystemId].Add(wormhole.toSystemId);

            if (!NeighborsBySystemId[wormhole.toSystemId].Contains(wormhole.fromSystemId))
                NeighborsBySystemId[wormhole.toSystemId].Add(wormhole.fromSystemId);
        }
    }

    private void RecalculateExtents()
    {
        MinPosition = new Vector2(float.MaxValue, float.MaxValue);
        MaxPosition = new Vector2(float.MinValue, float.MinValue);

        foreach (var system in systems)
        {
            MinPosition = Vector2.Min(MinPosition, system.position);
            MaxPosition = Vector2.Max(MaxPosition, system.position);
        }

        if (systems.Count == 0)
        {
            MinPosition = Vector2.zero;
            MaxPosition = Vector2.one;
        }
    }

    #region Public ID helpers

    public bool TryGetSystem(int systemId, out SystemNode node)
    {
        return SystemsById.TryGetValue(systemId, out node);
    }

    public bool TryGetWormhole(int wormholeId, out WormholeLink link)
    {
        return WormholesById.TryGetValue(wormholeId, out link);
    }

    /// <summary>
    /// Returns read-only neighbors (by system ID) of the given system ID.
    /// Returns an empty list if none exist.
    /// </summary>
    public IReadOnlyList<int> GetNeighbors(int systemId)
    {
        if (NeighborsBySystemId.TryGetValue(systemId, out var list))
            return list;

        return Array.Empty<int>();
    }

    #endregion
}
