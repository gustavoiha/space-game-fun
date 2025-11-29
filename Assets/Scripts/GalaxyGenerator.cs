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
/// No visual objects are spawned here; this is pure data.
/// </summary>
public class GalaxyGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("How many procedural (unnamed) systems to add on top of the hand-authored ones.")]
    public int proceduralStarCount = 15;

    [Tooltip("Extent of the galaxy in world units (systems are inside ±size/2 on X and Z).")]
    public float galaxySize = 400f;

    [Tooltip("How many nearest neighbours to connect each system to via wormholes.")]
    public int neighboursPerSystem = 2;

    [Tooltip("Optional random seed. 0 = use time-based seed.")]
    public int randomSeed = 0;

    [Tooltip("If true, all systems start as discovered (debug).")]
    public bool revealAllAtStart = true;

    private readonly List<StarSystemData> systems = new List<StarSystemData>();
    public IReadOnlyList<StarSystemData> Systems => systems;

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

        // --- Hand-authored key systems ---

        // 0: Solar System (Helios-controlled, not starting position)
        systems.Add(new StarSystemData
        {
            id = 0,
            displayName = "Sol",
            role = SystemRole.Solar,
            faction = Faction.HeliosProtectorate,
            position = Vector3.zero,
            discovered = true,  // known as a legend / political centre
            visited = false
        });

        // 1: Horizon start refuge
        systems.Add(new StarSystemData
        {
            id = 1,
            displayName = "Horizon Refuge",
            role = SystemRole.Start,
            faction = Faction.HorizonInitiative,
            position = new Vector3(-150f, 0f, 80f),
            discovered = true,
            visited = false
        });

        // --- Procedural extra systems ---

        for (int i = 0; i < proceduralStarCount; i++)
        {
            int id = systems.Count;
            var pos = new Vector3(
                Random.Range(-galaxySize * 0.5f, galaxySize * 0.5f),
                0f,
                Random.Range(-galaxySize * 0.5f, galaxySize * 0.5f)
            );

            var sys = new StarSystemData
            {
                id = id,
                displayName = $"System {id}",
                role = SystemRole.Normal,
                faction = Faction.None,
                position = pos,
                discovered = false,
                visited = false
            };

            systems.Add(sys);
        }

        // --- Wormhole network ---

        // Ensure each system connects to some neighbours by distance.
        for (int i = 0; i < systems.Count; i++)
        {
            var a = systems[i];

            var indices = new List<int>();
            for (int j = 0; j < systems.Count; j++)
            {
                if (j == i) continue;
                indices.Add(j);
            }

            indices.Sort((i1, i2) =>
                Vector3.SqrMagnitude(systems[i1].position - a.position)
                    .CompareTo(Vector3.SqrMagnitude(systems[i2].position - a.position)));

            int count = Mathf.Min(neighboursPerSystem, indices.Count);
            for (int k = 0; k < count; k++)
            {
                AddWormhole(i, indices[k]);
            }
        }

        // Ensure full connectivity (single component graph).
        var visited = new HashSet<int>();
        if (systems.Count > 0)
        {
            DepthFirstSearch(0, visited);
            if (visited.Count < systems.Count)
            {
                var visitedList = new List<int>(visited);
                for (int i = 0; i < systems.Count; i++)
                {
                    if (visited.Contains(i)) continue;
                    int existingIndex = visitedList[Random.Range(0, visitedList.Count)];
                    AddWormhole(i, existingIndex);
                    DepthFirstSearch(i, visited);
                    visitedList = new List<int>(visited);
                }
            }
        }

        // Discovery defaults
        if (revealAllAtStart)
        {
            foreach (var sys in systems)
                sys.discovered = true;
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
