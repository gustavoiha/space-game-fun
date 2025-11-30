using System.Collections.Generic;
using UnityEngine;

/// High-level role of a star system in the setting.
public enum SystemRole
{
    Normal,
    Start,      // Horizon Initiative starting refuge
    Solar,      // Sol / Helios-controlled Solar System
    Core,
    Frontier,
    AlienHub
}

/// Which polity currently controls a system.
public enum Faction
{
    None,
    HorizonInitiative,
    HeliosProtectorate,
    MyriadCombine,
    VerdureHegemony,
    AelariConcord,
    KarthanAssemblies,
    SerathiEnclave
}

/// Logical description of a single star system (no visuals here).
[System.Serializable]
public class StarSystemData
{
    public int id;
    public string displayName;
    public SystemRole role;
    public Faction faction;

    public Vector3 position;

    public bool discovered;   // visible on galaxy map
    public bool visited;      // player has physically been here at least once

    /// Indices of other systems this system is connected to via wormholes.
    public List<int> wormholeLinks = new List<int>();
}

/// <summary>
/// Generates the logical galaxy graph: star systems + wormhole links.
/// This is pure data; visuals and gates are spawned elsewhere.
/// </summary>
public class GalaxyGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("How many procedural (unnamed) systems to add on top of the hand-authored ones.")]
    public int proceduralStarCount = 15;

    [Tooltip("Optional random seed. 0 = use time-based seed.")]
    public int randomSeed = 0;

    [Tooltip("If true, all systems start as discovered (debug).")]
    public bool revealAllAtStart = false;

    [Header("Spatial Layout")]
    [Tooltip("Target average spacing between neighbouring systems.")]
    public float averageSystemSpacing = 120f;

    [Range(0f, 1f)]
    [Tooltip("How much random variation to apply around the average spacing. 0 = perfectly uniform, 1 = strong variation.")]
    public float spacingRandomness = 0.4f;

    [Tooltip("Minimum allowed distance between any two star systems; 0 disables this constraint.")]
    public float minSystemSeparation = 60f;

    [Tooltip("Maximum suggested distance between neighbouring systems when sampling positions; 0 = no upper limit.")]
    public float maxSystemSeparation = 240f;

    [Tooltip("Maximum random placement attempts around existing systems before falling back to extending the outer edge.")]
    public int maxPlacementAttemptsPerStar = 16;

    [Header("Wormhole Connectivity")]
    [Tooltip("Minimum number of wormhole links each system aims to have before connectivity fixes.")]
    public int minNeighboursPerSystem = 1;

    [Tooltip("Maximum number of wormhole links each system aims to have before connectivity fixes.")]
    public int maxNeighboursPerSystem = 4;

    [Tooltip("Curve controlling how link counts vary by radial distance from the origin (usually Sol).\nX = normalized radius [0,1], Y = 0–1 mapped to [min,max] links.")]
    public AnimationCurve neighbourCountByRadius = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private readonly List<StarSystemData> systems = new List<StarSystemData>();
    public IReadOnlyList<StarSystemData> Systems => systems;

    // Galaxy origin used for "core vs edge" logic (Sol at 0,0,0 by convention).
    private static readonly Vector3 GalaxyOrigin = Vector3.zero;

    private void Awake()
    {
        GenerateGalaxy();
    }

    /// <summary>
    /// Generates systems and wormhole links.
    /// </summary>
    public void GenerateGalaxy()
    {
        systems.Clear();

        int seed = randomSeed != 0 ? randomSeed : (int)System.DateTime.Now.Ticks;
        Random.InitState(seed);

        // Ensure connectivity parameters are sane.
        minNeighboursPerSystem = Mathf.Max(0, minNeighboursPerSystem);
        maxNeighboursPerSystem = Mathf.Max(minNeighboursPerSystem, maxNeighboursPerSystem);

        // --- Hand-authored key systems ---

        // 0: Solar System (Helios-controlled, not starting position)
        systems.Add(new StarSystemData
        {
            id = 0,
            displayName = "Sol",
            role = SystemRole.Solar,
            faction = Faction.HeliosProtectorate,
            position = GalaxyOrigin,
            discovered = true,  // known as a legend / political centre
            visited = false
        });

        // 1: Horizon start refuge (kept at a fixed authorial position)
        systems.Add(new StarSystemData
        {
            id = 1,
            displayName = "Horizon Refuge",
            role = SystemRole.Start,
            faction = Faction.HorizonInitiative,
            position = new Vector3(-150f, 0f, 80f),
            discovered = true,   // visible at start
            visited = false
        });

        // --- Procedural extra systems ---

        for (int i = 0; i < proceduralStarCount; i++)
        {
            int id = systems.Count;

            Vector3 pos = GetNewSystemPosition();
            var sys = new StarSystemData
            {
                id = id,
                displayName = $"System {id}",
                role = SystemRole.Normal,
                faction = Faction.None,
                position = pos,
                discovered = false,   // hidden until discovered
                visited = false
            };

            systems.Add(sys);
        }

        // --- Wormhole network ---

        BuildWormholeNetwork();

        // Optional debug: reveal everything.
        if (revealAllAtStart)
        {
            foreach (var sys in systems)
                sys.discovered = true;
        }
    }

    /// <summary>
    /// Picks a new system position relative to existing systems, using
    /// averageSystemSpacing with non-linear random variation and enforcing
    /// minSystemSeparation. If random placement fails, extends the galaxy
    /// outward from an edge system.
    /// </summary>
    private Vector3 GetNewSystemPosition()
    {
        if (systems.Count == 0)
            return GalaxyOrigin;

        int attempts = Mathf.Max(1, maxPlacementAttemptsPerStar);
        Vector3 pos = GalaxyOrigin;
        bool placed = false;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            StarSystemData anchor = systems[Random.Range(0, systems.Count)];

            float distance = SampleNeighbourSpacing();
            Vector3 dir = Random.onUnitSphere;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;
            dir.Normalize();

            Vector3 candidate = anchor.position + dir * distance;

            if (IsPositionValid(candidate))
            {
                pos = candidate;
                placed = true;
                break;
            }
        }

        if (!placed)
        {
            // Fallback: extend from an outer edge system away from the galaxy origin.
            StarSystemData edge = GetFurthestSystemFromOrigin();
            if (edge == null)
                return GalaxyOrigin;

            Vector3 flatPos = new Vector3(edge.position.x, 0f, edge.position.z);
            Vector3 outward = flatPos.sqrMagnitude > 0.0001f
                ? flatPos.normalized
                : Vector3.right;

            float distance = SampleNeighbourSpacing();
            Vector3 candidate = edge.position + outward * distance;

            int safety = 0;
            while (!IsPositionValid(candidate) && safety < 8)
            {
                float push = minSystemSeparation > 0f ? minSystemSeparation : averageSystemSpacing;
                distance += push;
                candidate = edge.position + outward * distance;
                safety++;
            }

            pos = candidate;
        }

        return pos;
    }

    /// <summary>
    /// Samples a neighbour spacing distance around averageSystemSpacing,
    /// with a non-linear distribution that clusters values near the average.
    /// </summary>
    private float SampleNeighbourSpacing()
    {
        float baseDist = Mathf.Max(1f, averageSystemSpacing);

        // Symmetric random in [-1,1].
        float r = Random.Range(-1f, 1f);

        // Non-linear shaping: squaring the magnitude biases values toward 0,
        // so most distances are close to the average and fewer are far off.
        float shaped = Mathf.Sign(r) * r * r; // still in [-1,1], but peaked near 0

        float offsetFactor = shaped * spacingRandomness;
        float d = baseDist * (1f + offsetFactor);

        if (minSystemSeparation > 0f)
            d = Mathf.Max(d, minSystemSeparation);
        if (maxSystemSeparation > 0f)
            d = Mathf.Min(d, maxSystemSeparation);

        return d;
    }

    /// <summary>
    /// Checks whether a candidate position is at least minSystemSeparation away
    /// from all already-placed systems.
    /// </summary>
    private bool IsPositionValid(Vector3 candidatePos)
    {
        if (minSystemSeparation <= 0f || systems.Count == 0)
            return true;

        float minSqr = minSystemSeparation * minSystemSeparation;

        foreach (var existing in systems)
        {
            Vector3 delta = existing.position - candidatePos;
            delta.y = 0f;
            if (delta.sqrMagnitude < minSqr)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the system furthest from the galaxy origin on the XZ plane.
    /// </summary>
    private StarSystemData GetFurthestSystemFromOrigin()
    {
        if (systems.Count == 0)
            return null;

        StarSystemData best = systems[0];
        float bestSqr = new Vector2(best.position.x - GalaxyOrigin.x, best.position.z - GalaxyOrigin.z).sqrMagnitude;

        for (int i = 1; i < systems.Count; i++)
        {
            var sys = systems[i];
            float dSqr = new Vector2(sys.position.x - GalaxyOrigin.x, sys.position.z - GalaxyOrigin.z).sqrMagnitude;
            if (dSqr > bestSqr)
            {
                best = sys;
                bestSqr = dSqr;
            }
        }

        return best;
    }

    /// <summary>
    /// Builds the wormhole network using a per-system neighbour count
    /// derived from neighbourCountByRadius and then enforces full connectivity.
    /// </summary>
    private void BuildWormholeNetwork()
    {
        int count = systems.Count;
        if (count == 0)
            return;

        // Clear any existing links.
        for (int i = 0; i < count; i++)
        {
            systems[i].wormholeLinks.Clear();
        }

        // Compute max radius from origin for normalization.
        float maxRadius = 0f;
        for (int i = 0; i < count; i++)
        {
            var pos = systems[i].position;
            float radius = new Vector2(pos.x - GalaxyOrigin.x, pos.z - GalaxyOrigin.z).magnitude;
            if (radius > maxRadius)
                maxRadius = radius;
        }

        // Primary neighbour pass.
        for (int i = 0; i < count; i++)
        {
            var a = systems[i];

            // Determine how many neighbours this system should try to have.
            float radius = new Vector2(a.position.x - GalaxyOrigin.x, a.position.z - GalaxyOrigin.z).magnitude;
            float t = (maxRadius > 0f) ? Mathf.Clamp01(radius / maxRadius) : 0f;

            float curveValue = neighbourCountByRadius != null
                ? Mathf.Clamp01(neighbourCountByRadius.Evaluate(t))
                : 1f;

            float desiredFloat = Mathf.Lerp(minNeighboursPerSystem, maxNeighboursPerSystem, curveValue);
            int desiredNeighbours = Mathf.Clamp(Mathf.RoundToInt(desiredFloat), minNeighboursPerSystem, maxNeighboursPerSystem);

            // Build and sort list of other systems by distance.
            var indices = new List<int>();
            for (int j = 0; j < count; j++)
            {
                if (j == i) continue;
                indices.Add(j);
            }

            indices.Sort((i1, i2) =>
                Vector3.SqrMagnitude(systems[i1].position - a.position)
                    .CompareTo(Vector3.SqrMagnitude(systems[i2].position - a.position)));

            int limit = Mathf.Min(desiredNeighbours, indices.Count);
            for (int k = 0; k < limit; k++)
            {
                AddWormhole(i, indices[k]);
            }
        }

        // Ensure full connectivity (single component graph).
        var visited = new HashSet<int>();
        DepthFirstSearch(0, visited);

        if (visited.Count < count)
        {
            var visitedList = new List<int>(visited);
            for (int i = 0; i < count; i++)
            {
                if (visited.Contains(i)) continue;

                int existingIndex = visitedList[Random.Range(0, visitedList.Count)];
                AddWormhole(i, existingIndex);

                DepthFirstSearch(i, visited);
                visitedList = new List<int>(visited);
            }
        }
    }

    private void AddWormhole(int aIndex, int bIndex)
    {
        if (aIndex == bIndex) return;

        var a = systems[aIndex];
        var b = systems[bIndex];

        if (!a.wormholeLinks.Contains(bIndex)) a.wormholeLinks.Add(bIndex);
        if (!b.wormholeLinks.Contains(aIndex)) b.wormholeLinks.Add(aIndex);
    }

    private void DepthFirstSearch(int index, HashSet<int> visited)
    {
        if (visited.Contains(index)) return;
        visited.Add(index);

        var sys = systems[index];
        foreach (int neighbourIndex in sys.wormholeLinks)
        {
            DepthFirstSearch(neighbourIndex, visited);
        }
    }

    public StarSystemData GetStartSystem()
    {
        foreach (var sys in systems)
        {
            if (sys.role == SystemRole.Start)
                return sys;
        }
        // Fallback: any system
        return systems.Count > 0 ? systems[0] : null;
    }

    public StarSystemData GetSystem(int id)
    {
        if (id < 0 || id >= systems.Count) return null;
        return systems[id];
    }

    public void DiscoverSystem(int id)
    {
        var sys = GetSystem(id);
        if (sys != null)
            sys.discovered = true;
    }

#if UNITY_EDITOR
    // Optional: draw gizmos in the Scene view to visualise the graph.
    private void OnDrawGizmosSelected()
    {
        if (systems == null || systems.Count == 0) return;

        foreach (var sys in systems)
        {
            Gizmos.color = sys.role == SystemRole.Start ? Color.cyan :
                           sys.role == SystemRole.Solar ? Color.yellow :
                           Color.white;
            Gizmos.DrawSphere(sys.position, 4f);

            Gizmos.color = Color.magenta;
            foreach (int n in sys.wormholeLinks)
            {
                if (n < 0 || n >= systems.Count) continue;
                Gizmos.DrawLine(sys.position, systems[n].position);
            }
        }
    }
#endif
}
