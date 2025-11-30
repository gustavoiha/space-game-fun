using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative discovery state for the game:
/// - Which systems are known
/// - Which wormholes are known
/// - Which system the player is currently in
///
/// Other systems (map, gates, UI) should talk to this instead of
/// maintaining their own discovery flags.
/// </summary>
public class GameDiscoveryState : MonoBehaviour
{
    public static GameDiscoveryState Instance { get; private set; }

    [Header("Startup")]
    [Tooltip("System ID where the player starts. If invalid, the first generated system is used.")]
    [SerializeField] private int startingSystemId = 0;

    /// <summary>
    /// Fired whenever any discovery state changes (systems or wormholes).
    /// </summary>
    public event Action DiscoveryChanged;

    /// <summary>
    /// Fired whenever the current system changes. Argument is the new system ID.
    /// </summary>
    public event Action<int> CurrentSystemChanged;

    private readonly HashSet<int> discoveredSystems = new HashSet<int>();
    private readonly HashSet<int> discoveredWormholes = new HashSet<int>();

    /// <summary>
    /// Systems the player has actually entered. This is a subset of discovered systems.
    /// </summary>
    private readonly HashSet<int> visitedSystems = new HashSet<int>();

    private int currentSystemId = -1;
    private GalaxyGenerator galaxy;

    public int CurrentSystemId => currentSystemId;

    public bool IsSystemDiscovered(int systemId)
    {
        return discoveredSystems.Contains(systemId);
    }

    public bool IsWormholeDiscovered(int wormholeId)
    {
        return discoveredWormholes.Contains(wormholeId);
    }

    public IReadOnlyCollection<int> GetDiscoveredSystems()
    {
        return discoveredSystems;
    }

    public IReadOnlyCollection<int> GetDiscoveredWormholes()
    {
        return discoveredWormholes;
    }

    public IReadOnlyCollection<int> GetVisitedSystems()
    {
        return visitedSystems;
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
    }

    private void Start()
    {
        galaxy = GalaxyGenerator.Instance;

        if (galaxy == null || galaxy.Systems == null || galaxy.Systems.Count == 0)
            return;

        // If nothing has initialized us yet, auto-init
        if (currentSystemId < 0)
        {
            int initialId = startingSystemId;

            if (!galaxy.SystemsById.ContainsKey(initialId))
            {
                // Fallback: use first generated system
                initialId = galaxy.Systems[0].id;
            }

            Initialize(initialId);
        }
    }

    /// <summary>
    /// Clear and rebuild discovery state with the given starting system.
    /// </summary>
    public void Initialize(int systemId)
    {
        discoveredSystems.Clear();
        discoveredWormholes.Clear();
        visitedSystems.Clear();

        currentSystemId = systemId;

        // Starting system is discovered, visited, and its local wormholes are known
        DiscoverSystemInternal(systemId, raiseEvents: false);
        MarkSystemVisited(systemId);

        RaiseDiscoveryChanged();
        RaiseCurrentSystemChanged();
    }

    /// <summary>
    /// Mark a system as discovered. Does not move the player.
    /// Also reveals any wormholes directly connected to that system.
    /// </summary>
    public void DiscoverSystem(int systemId)
    {
        DiscoverSystemInternal(systemId, raiseEvents: true);
    }

    /// <summary>
    /// Mark a wormhole itself as discovered and ensure both endpoints are discovered.
    /// Use this when the player scans or jumps through a specific wormhole.
    /// </summary>
    public void DiscoverWormholeAndEndpoints(int wormholeId)
    {
        EnsureGalaxy();

        if (galaxy == null)
            return;

        if (!galaxy.TryGetWormhole(wormholeId, out var link))
            return;

        bool changed = false;

        if (discoveredWormholes.Add(wormholeId))
            changed = true;

        if (discoveredSystems.Add(link.fromSystemId))
            changed = true;

        if (discoveredSystems.Add(link.toSystemId))
            changed = true;

        if (changed)
            RaiseDiscoveryChanged();
    }

    /// <summary>
    /// Set the player's current system. Also marks that system as discovered and
    /// reveals all wormholes attached to it.
    /// </summary>
    public void SetCurrentSystem(int systemId)
    {
        if (systemId < 0 || systemId == currentSystemId)
            return;

        EnsureGalaxy();

        currentSystemId = systemId;
        bool discoveryChanged = MarkSystemVisited(systemId);

        // Ensure the system is discovered
        // (MarkSystemVisited already adds to discoveredSystems; this call preserves previous behavior
        //  in case another caller cleared visited while keeping discovered.)
        if (discoveredSystems.Add(systemId))
            discoveryChanged = true;

        // Reveal all wormholes attached to this system
        if (DiscoverAllWormholesFrom(systemId, raiseEvents: false))
        {
            discoveryChanged = true;
        }

        if (discoveryChanged)
            RaiseDiscoveryChanged();

        RaiseCurrentSystemChanged();
    }

    /// <summary>
    /// Adds the system to the visited set, ensuring the subset relationship is maintained.
    /// </summary>
    private bool MarkSystemVisited(int systemId)
    {
        if (systemId < 0)
            return false;

        bool newlyVisited = visitedSystems.Add(systemId);
        bool newlyDiscovered = discoveredSystems.Add(systemId);

        return newlyVisited || newlyDiscovered;
    }

    /// <summary>
    /// Internal helper for discovering a system (and its outgoing wormholes).
    /// </summary>
    private void DiscoverSystemInternal(int systemId, bool raiseEvents)
    {
        if (systemId < 0)
            return;

        EnsureGalaxy();

        bool changed = false;

        if (discoveredSystems.Add(systemId))
        {
            changed = true;
        }

        if (DiscoverAllWormholesFrom(systemId, raiseEvents: false))
        {
            changed = true;
        }

        if (changed && raiseEvents)
            RaiseDiscoveryChanged();
    }

    /// <summary>
    /// Reveal all wormholes that originate or terminate at systemId.
    /// Returns true if any new wormholes were revealed.
    /// </summary>
    private bool DiscoverAllWormholesFrom(int systemId, bool raiseEvents)
    {
        EnsureGalaxy();

        if (galaxy == null)
            return false;

        bool anyNew = false;

        foreach (var wormhole in galaxy.Wormholes)
        {
            if (wormhole.fromSystemId == systemId || wormhole.toSystemId == systemId)
            {
                if (discoveredWormholes.Add(wormhole.id))
                {
                    anyNew = true;
                }
            }
        }

        if (anyNew && raiseEvents)
            RaiseDiscoveryChanged();

        return anyNew;
    }

    private void EnsureGalaxy()
    {
        if (galaxy == null)
        {
            galaxy = GalaxyGenerator.Instance;
        }
    }

    private void RaiseDiscoveryChanged()
    {
        DiscoveryChanged?.Invoke();
    }

    private void RaiseCurrentSystemChanged()
    {
        CurrentSystemChanged?.Invoke(currentSystemId);
    }
}
