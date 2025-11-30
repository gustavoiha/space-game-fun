using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a procedural galaxy graph:
/// - Star systems placed roughly around an average spacing with min/max constraints
/// - Wormhole connections between systems, with a tunable connections-per-system curve
///
/// Each system and each wormhole has a stable integer ID for this galaxy instance.
/// Other systems (map, save, gameplay) should reference systems and wormholes by ID.
/// </summary>
public class GalaxyGenerator : MonoBehaviour
{
    public static GalaxyGenerator Instance { get; private set; }

    [Header("Generation Settings")]
    [SerializeField] private int targetSystemCount = 40;

    [Tooltip("Average distance between systems in 'galaxy units' (map space).")]
    [SerializeField] private float averageSystemSpacing = 30f;

    [SerializeField] private float minSystemSpacing = 10f;
    [SerializeField] private float maxSystemSpacing = 60f;

    [Header("Connections")]
    [Tooltip("Minimum number of wormhole connections each system should have.")]
    [SerializeField] private int minConnectionsPerSystem = 1;

    [Tooltip("Maximum number of wormhole connections each system should have.")]
    [SerializeField] private int maxConnectionsPerSystem = 4;

    [Tooltip("Curve controlling how many connections systems tend to get.\n" +
             "X axis: 0..1 random value, Y axis: 0..1 mapped to [min,max] connections.\n" +
             "Bias curve towards 0.3–0.6 for 'typical' connectivity, with tails for low/high outliers.")]
    [SerializeField] private AnimationCurve connectionsPerSystemCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);

    [Header("Debug / Output")]
    [SerializeField] private bool generateOnAwake = true;

    [SerializeField] private List<SystemNode> systems = new List<SystemNode>();
    [SerializeField] private List<WormholeLink> wormholes = new List<WormholeLink>();

    // ID lookups (built after generation)
    public IReadOnlyList<SystemNode> Systems => systems;
    public IReadOnlyList<WormholeLink> Wormholes => wormholes;

    public Dictionary<int, SystemNode> SystemsById { get; private set; } = new Dictionary<int, SystemNode>();
    public Dictionary<int, WormholeLink> WormholesById { get; private set; } = new Dictionary<int, WormholeLink>();

    /// <summary>
    /// Adjacency list: systemId -> list of neighboring systemIds reachable via wormholes.
    /// </summary>
    public Dictionary<int, List<int>> NeighborsBySystemId { get; private set; } = new Dictionary<int, List<int>>();

    [Serializable]
    public class SystemNode
    {
        [Tooltip("Stable unique ID for this system within the generated galaxy.")]
        public int id;

        public Vector2 position;

        [Tooltip("Optional display name; can be generated or assigned later.")]
        public string displayName;
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

        GenerateSystems();
        GenerateConnections();
        RebuildLookups();
    }

    /// <summary>
    /// Generate star systems with approximate average spacing and min/max constraints.
    /// </summary>
    private void GenerateSystems()
    {
        if (targetSystemCount <= 0)
            return;

        // First system at origin
        AddSystem(Vector2.zero);

        int safety = 0;
        while (systems.Count < targetSystemCount && safety < targetSystemCount * 50)
        {
            safety++;

            SystemNode anchor = PickAnchorSystem();
            Vector2 candidatePosition = GetRandomPositionAround(anchor.position);

            if (IsPositionValid(candidatePosition))
            {
                AddSystem(candidatePosition);
            }
        }
    }

    private void AddSystem(Vector2 position)
    {
        int id = systems.Count; // ID == index for now, stable within this generated galaxy

        var node = new SystemNode
        {
            id = id,
            position = position,
            displayName = $"SYS-{id:D3}"
        };

        systems.Add(node);
    }

    private SystemNode PickAnchorSystem()
    {
        // Mild bias towards "edge" systems by picking one of the furthest few
        if (systems.Count <= 3)
            return systems[UnityEngine.Random.Range(0, systems.Count)];

        // Compute approximate center
        Vector2 sum = Vector2.zero;
        foreach (var s in systems)
            sum += s.position;

        Vector2 center = sum / systems.Count;

        // Sort systems by distance from center and pick from outer half
        systems.Sort((a, b) =>
        {
            float da = (a.position - center).sqrMagnitude;
            float db = (b.position - center).sqrMagnitude;
            return da.CompareTo(db);
        });

        int startIndex = systems.Count / 2;
        int index = UnityEngine.Random.Range(startIndex, systems.Count);
        return systems[index];
    }

    private Vector2 GetRandomPositionAround(Vector2 origin)
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        // Use a non-linear distribution around averageSystemSpacing:
        // Sample t in [0,1], then bias it towards the center with a curve t^2 mirrored.
        float t = UnityEngine.Random.value;
        float centerBias = 1f - Mathf.Pow(1f - t, 2f); // more weight near 1
        float offset = (centerBias - 0.5f) * 2f;       // [-1,1]

        float distance = Mathf.Clamp(
            averageSystemSpacing + offset * (maxSystemSpacing - minSystemSpacing) * 0.5f,
            minSystemSpacing,
            maxSystemSpacing
        );

        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return origin + direction * distance;
    }

    private bool IsPositionValid(Vector2 candidate)
    {
        foreach (var s in systems)
        {
            float d = Vector2.Distance(candidate, s.position);
            if (d < minSystemSpacing)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Generate wormhole connections between systems according to the per-system connection curve.
    /// </summary>
    private void GenerateConnections()
    {
        if (systems.Count <= 1)
            return;

        // Precompute how many desired connections each system wants
        int[] desiredConnections = new int[systems.Count];
        int[] currentConnections = new int[systems.Count];

        for (int i = 0; i < systems.Count; i++)
        {
            float t = Mathf.Clamp01(UnityEngine.Random.value);
            float curveValue = Mathf.Clamp01(connectionsPerSystemCurve.Evaluate(t));
            int targetConnections = Mathf.RoundToInt(Mathf.Lerp(minConnectionsPerSystem, maxConnectionsPerSystem, curveValue));

            desiredConnections[i] = Mathf.Max(minConnectionsPerSystem, targetConnections);
            currentConnections[i] = 0;
        }

        // Simple "connect nearest neighbors" approach while enforcing desiredConnections
        int wormholeId = 0;

        for (int i = 0; i < systems.Count; i++)
        {
            var a = systems[i];
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
    }

    private int FindBestNeighborIndex(int systemIndex, int[] currentConnections, int[] desiredConnections)
    {
        float bestScore = float.MaxValue;
        int bestIndex = -1;

        Vector2 position = systems[systemIndex].position;

        for (int i = 0; i < systems.Count; i++)
        {
            if (i == systemIndex)
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
