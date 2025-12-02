# Unity Wormhole Era Prototype – Script Architecture

This document describes the main C# scripts in the Unity Wormhole Era prototype and how they fit together after introducing a multi-scene, multi-physics star system architecture.

It is intended as reference for code navigation and as context for AI-assisted tools (e.g. Codex).

---

## High-level Overview

Core ideas:

- A procedurally generated galaxy of star systems, connected by stable wormholes.
- Runtime state that tracks which systems and wormholes have been discovered and which system the player is currently in.
- World objects (wormhole gates) that map the physical in-system scenes into the abstract galaxy graph.
- Ship logic that handles entering/exiting wormholes, moving between scenes, and updating discovery state.
- A 2D galaxy map UI that visualizes the discovered network and allows the player to orient themselves.
- Input using Unity’s new Input System (no direct use of `UnityEngine.Input`).
- A **persistent galaxy root scene** that holds global simulation state and UI (GalaxyGenerator, GameDiscoveryState, galaxy map, etc.).
- One **additive in-system scene per star system**, each with its own physics world, so colliders and rigidbodies never interact across systems.
- A **simulation manager** that advances physics for all active star system scenes, even when they are not currently being rendered.

---

## Script Index

> Note: Names are based on the current prototype and may evolve. Items marked *(NEW)* are architectural additions to support
> the multi-scene approach.

- **Core / Data**
  - `GalaxyGenerator.cs`
- **Game State**
  - `GameDiscoveryState.cs`
  - `GalaxySimulationManager.cs`
  - `StarSystemRuntime.cs`
  - `GameManager.cs`
- **World / Gameplay**
  - `PlayerShipController.cs`
  - `WormholeGate.cs`
  - `ShipWormholeNavigator.cs`
- **UI / Map**
  - `GalaxyMapUIManager.cs`
  - `GalaxyMapToggle.cs`
- **UI / Contextual Prompts**
  - (Logic integrated into `ShipWormholeNavigator` and/or dedicated prompt UI components.)
- **Visuals / Environment**
  - `SpaceBackgroundController.cs`

---

## Folder Structure (proposed)

Actual folder names may differ; this layout reflects conceptual grouping:

```text
Assets/
  Scripts/
    Core/
      GalaxyGenerator.cs
    State/
      GameDiscoveryState.cs
    Simulation/
      GalaxySimulationManager.cs
      StarSystemRuntime.cs
    World/
      WormholeGate.cs
      ShipWormholeNavigator.cs
      PlayerShipController.cs
    UI/
      GalaxyMapUIManager.cs
      GalaxyMapToggle.cs
      // additional UI helper components (prompt labels, system labels, etc.)
    Visual/
      SpaceBackgroundController.cs
```

---

## Multi-Scene / Multi-Physics Architecture

### Scene layout

The game now uses a multi-scene setup:

- **Persistent Galaxy Root Scene**
  - Stays loaded for the entire play session.
  - Contains:
    - `GalaxyGenerator` (galaxy topology and system positions).
    - `GameDiscoveryState` (what the player knows and where they are).
    - `GalaxySimulationManager` (central simulation loop for star system scenes).
    - Global UI such as the galaxy/system map (`GalaxyMapUIManager`, `GalaxyMapToggle`), pause menus, etc.
    - Any cross-system managers (audio, save/load, input).

- **Star System Scenes**
  - One additive scene per star system (or a pool of scenes reused for different systems).
  - Each contains:
    - Local star visuals (primary star, planets/placeholders).
    - Wormhole gates (`WormholeGate` components).
    - Local ships (player and AI) with `PlayerShipController`, AI controllers, and `ShipWormholeNavigator`.
    - Local VFX/SFX and environment dressing.
    - A `StarSystemRuntime` component as the root for per-system simulation and bookkeeping.

  A star system scene is loaded when it becomes relevant (e.g. the player jumps in or a simulation rule requires it) and can be
  kept loaded for continuous simulation or unloaded to save performance.

`GalaxySimulationManager` now instantiates these additive scenes on demand using `LocalPhysicsMode.Physics3D` to enforce physics
isolation. It moves generated star visuals from `GalaxyGenerator` into the proper scene, builds gate prefabs for every neighbor
system, and registers ships as they enter. A lightweight debug label enumerates loaded system IDs and marks the one containing
the player.

### Physics separation

- Each star system scene has its own **physics world** (PhysicsScene):
  - Colliders and rigidbodies in one system cannot collide with or “see” objects in another system.
  - No need to check “are these objects in the same star system?” at collision time; physics isolation guarantees it.

- `GalaxySimulationManager`:
  - Holds references to all active `StarSystemRuntime` instances and their PhysicsScenes.
  - Advances each system’s physics explicitly (e.g. `physicsScene.Simulate(fixedDeltaTime)` for off-camera systems).
  - Ensures the currently viewed system is simulated in lockstep with Unity’s `FixedUpdate`.

This architecture keeps the galaxy logically unified while preserving clean physical separation between systems.

### Wormhole travel and scene transitions

- Wormhole gates are still the bridge between systems at the **data level** (GalaxyGenerator) and **scene level** (Unity scenes).
- When a ship jumps:
  1. `ShipWormholeNavigator` detects entry into a `WormholeGate` and resolves the destination system/gate via `GalaxyGenerator`.
  2. `GalaxySimulationManager`:
     - Ensures the destination star system scene is loaded (load if necessary).
     - Locates or spawns the destination `WormholeGate` in that scene.
  3. The ship GameObject is moved between scenes:
     - Using `SceneManager.MoveGameObjectToScene(ship, destinationScene)`.
     - Its world position/rotation are set to match the destination gate’s exit point.
  4. `GameDiscoveryState` is updated:
     - `SetCurrentSystem(destinationSystemId)`.
     - `DiscoverSystem(destinationSystemId)` and `DiscoverWormhole(wormholeId)` as needed.
  5. Camera and UI are updated to follow the ship in the new system scene.

- AI ships and fleets follow the same pattern:
  - Each ship belongs to exactly one star system scene at a time.
  - When an AI ship uses a wormhole, its GameObject is transferred to the destination scene and its owning `StarSystemRuntime`.

---

## Core: `GalaxyGenerator.cs`

### Responsibility

- Owns the **procedurally generated galaxy graph**:
  - Star systems.
  - Wormhole links between systems.
- Provides fast lookup of systems and wormholes by ID.
- Serves as a read-only data source for other systems after generation.

### Key Concepts

- **Star System**
  - Unique identifier (e.g. `string systemId` or `int id`).
  - Display name.
  - Abstract 3D position in “galaxy space” (used for map layout and high-level navigation).
  - Metadata hooks (e.g. starting system, difficulty, faction control).
  - Configuration for the in-system scene:
    - Name/path of the scene asset to load for this system.
    - Optional system radius used for clamping ships.

- **Wormhole Link**
  - Unique identifier (e.g. `string wormholeId`).
  - `fromSystemId` and `toSystemId` (directed or effectively undirected edge).
  - Optional additional data (travel cost, instability, etc.).

### Important Members (conceptual)

- Collections:
  - `List<StarSystem> Systems`
  - `Dictionary<string, StarSystem> SystemsById`
  - `List<WormholeLink> Wormholes`
  - `Dictionary<string, WormholeLink> WormholesById`

- Accessors / Queries:
  - `bool TryGetSystem(string systemId, out StarSystem system)`
  - `bool TryGetWormhole(string wormholeId, out WormholeLink link)`
  - `IEnumerable<WormholeLink> GetLinksFrom(string systemId)`
  - `IEnumerable<StarSystem> GetNeighborSystems(string systemId)`

- Generation:
  - `void GenerateGalaxy(int seed, GalaxyConfig config)`
    - Uses distance-based placement, connection distribution, Solar System constraints, and map extents output as already implemented.

### Dependencies & Usage

- **Used by:**
  - `GameDiscoveryState` (for validating system and wormhole IDs).
  - `GalaxySimulationManager` (to map system IDs to scenes and to know which systems exist).
  - `WormholeGate` (to know which systems/gates are connected).
  - `ShipWormholeNavigator` (to find destinations).
  - `GalaxyMapUIManager` (to render system positions and connection lines).

- **Lifetime:**
  - Persistent singleton-like MonoBehaviour in the galaxy root scene.
  - Generates the galaxy on startup before other systems request data.

---

## Game State: `GameDiscoveryState.cs`

### Responsibility

- Tracks the **player’s knowledge** of the galaxy:
  - Which systems are discovered.
  - Which wormholes are discovered.
  - Which system the player is currently in.
- Provides a stable source of truth for UI and gameplay to query.

### Core Data

- `string currentSystemId`
- `HashSet<string> discoveredSystemIds`
- `HashSet<string> discoveredWormholeIds`

### Important Methods (conceptual)

- Discovery:
  - `void SetCurrentSystem(string systemId)`
  - `void DiscoverSystem(string systemId)`
  - `void DiscoverWormhole(string wormholeId)`
  - `bool IsSystemDiscovered(string systemId)`
  - `bool IsWormholeDiscovered(string wormholeId)`

- Events:
  - `event Action<string> CurrentSystemChanged`
  - `event Action DiscoveryChanged`

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator` for validation (ensure IDs are valid).

- **Used by:**
  - `GalaxySimulationManager` to know which system the player is in.
  - `ShipWormholeNavigator` (update state after a successful jump).
  - `GalaxyMapUIManager` (query discovered systems/wormholes, highlight current system).
  - Any UI widget showing current system name, exploration progress, etc.

- **Design Note:**
  - Treat `GameDiscoveryState` as the single authority for “what is known,” rather than altering flags directly on `GalaxyGenerator` data.

---

## Simulation / Scenes: `GalaxySimulationManager.cs` (NEW)

### Responsibility

- Orchestrates **multi-scene simulation**:
  - Keeps track of all active `StarSystemRuntime` instances.
  - Ensures each star system physics world is simulated every frame (or fixed timestep).
  - Handles loading/unloading star system scenes on demand.
  - Provides a central API for wormhole jumps and scene transitions.

### Core Data

- `Dictionary<string, StarSystemRuntime> activeSystemsById`
- Optionally:
  - `StarSystemRuntime playerSystemRuntime`
  - Configuration for maximum concurrently loaded systems.

### Important Methods (conceptual)

- Scene management:
  - `Task<StarSystemRuntime> EnsureSystemLoadedAsync(string systemId)`
  - `void UnloadSystem(string systemId)`
  - `IEnumerable<StarSystemRuntime> GetActiveSystems()`

- Simulation:
  - `void FixedUpdate()`
    - Iterate all active `StarSystemRuntime` instances and call `runtime.Simulate(fixedDeltaTime)`.

- Wormhole travel:
  - `void JumpShipThroughWormhole(ShipWormholeNavigator ship, WormholeGate originGate, string wormholeId)`
    - Resolve destination via `GalaxyGenerator`.
    - Ensure destination system is loaded.
    - Move the ship GameObject to the destination scene.
    - Update `GameDiscoveryState` and notify relevant systems.

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator`
  - `GameDiscoveryState`
  - Unity’s scene management and PhysicsScene APIs.

- **Used by:**
  - `ShipWormholeNavigator` (to perform actual jumps).
  - Systems that need to query or manipulate active star system scenes.

---

## Simulation / Scenes: `StarSystemRuntime.cs` (NEW)

### Responsibility

- Represents the **runtime state and entry point** for a single star system scene.

- Handles:
  - References to the local PhysicsScene.
  - Local objects (ships, stations, projectiles).
  - Hooks into `GalaxySimulationManager` for simulation and bookkeeping.

### Core Data

- `string systemId`
- `Scene unityScene`
- `PhysicsScene physicsScene`
- Collections of key local entities (e.g. ships, stations) as needed.

### Important Methods (conceptual)

- `void Initialize(string systemId, GalaxyGenerator generator)`
- `void Simulate(float deltaTime)`
  - Advance physics for this system.
  - Run any per-system logic (AI, spawns, cleanup).

- Helper APIs:
  - `Transform GetGateTransform(string wormholeId, bool isDestination)`
  - `void RegisterShip(ShipWormholeNavigator ship)`
  - `void UnregisterShip(ShipWormholeNavigator ship)`

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator` (for configuration data).
  - Unity scene/physics APIs.

- **Used by:**
  - `GalaxySimulationManager` as the per-system unit of simulation.
  - Wormhole logic when positioning ships in the destination system.

---

## World / Gameplay: `WormholeGate.cs`

### Responsibility

- Represents a **physical wormhole gate** inside a star system scene:
  - Links a scene object (collider, VFX) to an abstract wormhole in `GalaxyGenerator`.
  - Knows which star system scene it belongs to.

### Typical Fields

- `string systemId` – the star system this gate is in.
- `string wormholeId` – the logical wormhole link.
- Reference to `GalaxyGenerator` (assigned via inspector or lookup).
- Optional reference back to `StarSystemRuntime`.

### Behaviour

- Provides navigation info:
  - `GetDestinationSystemId()` looks up the wormhole in `GalaxyGenerator` and returns the opposite end.
- Exposes events such as:
  - `OnGateEntered`, `OnGateExited` for VFX, sounds, or prompts.

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator`
  - Optionally `StarSystemRuntime`

- **Used by:**
  - `ShipWormholeNavigator` during jump initiation.
  - Contextual UI (prompts like “Approaching Wormhole to [System]”).

---

## World / Gameplay: `PlayerShipController.cs`

### Responsibility

- Provides per-ship free-flight controls using the new Input System.
- Enforces **in-system bounds** so the ship cannot drift infinitely far from the local star.

### Key Behaviours

- Integrates velocity with configurable thrust, max speed, and damping.
- Clamps the ship to a **spherical boundary** within the current star system:
  - Radius controlled by a serialized `systemBoundaryRadius` (default e.g. 5000).
  - Center can be provided by `StarSystemRuntime` or a local system anchor.
  - Projects velocity off the outward normal when clamping to reduce edge jitter.

### Dependencies & Usage

- **Depends on:**
  - New Input System.
  - `StarSystemRuntime` or a local system center reference.

- **Used by:**
  - Player ship prefab in each star system scene.
  - Can be reused for AI-controlled ships if needed.

---

## World / Gameplay: `ShipWormholeNavigator.cs`

### Responsibility

- Handles the **ship’s interaction with wormhole gates** and multi-scene jumps:
  - Detecting proximity / entry via colliders.
  - Triggering the wormhole jump animation and teleport.
  - Delegating scene transfer to `GalaxySimulationManager`.
  - Updating game state and notifying UI.

### Key Behaviours

- Proximity & prompting:
  - Uses gate colliders as event horizons.
  - When the ship enters a designated trigger:
    - Shows an on-screen prompt via UI.
  - Jump may be automatic when crossing a threshold or may require input, depending on design.

- Jump execution:
  - Identifies the `WormholeGate` associated with the trigger.
  - Asks `GalaxySimulationManager` to perform the jump:
    - Resolves destination system and gate using `GalaxyGenerator`.
    - Moves the ship GameObject to the destination star system scene.
  - Updates `GameDiscoveryState`:
    - `SetCurrentSystem(destinationSystemId)`
    - Discovery updates for systems and wormholes.

- UI integration:
  - Shows/hides contextual prompts.
  - Raises events for VFX/SFX hooks.

### Dependencies & Usage

- **Depends on:**
  - `GalaxySimulationManager`
  - `GalaxyGenerator`
  - `GameDiscoveryState`
  - `WormholeGate`
  - UI objects (prompt canvas, TextMeshPro labels)

- **Used by:**
  - Player ship prefab.
  - Potentially AI ships if they use the same navigation code.

---

## UI / Map: `GalaxyMapUIManager.cs`

### Responsibility

- Renders and manages the **2D galaxy map** and the **local star-system map**.
- Display behaviour remains the same, but now the map reflects the fact that each star system has its own scene and runtime.

### Core Behaviour (unchanged conceptually)

- Renders systems and wormholes from `GalaxyGenerator`.
- Colours systems based on discovery and ownership.
- Highlights the current system from `GameDiscoveryState`.
- Provides:
  - Galaxy view (full network).
  - System view (local star + gates + player ship icon).
- Switches between views based on zoom or user input.

### Integration With Multi-Scene Architecture

- When the current system changes (`GameDiscoveryState.CurrentSystemChanged`):
  - The map recenters on the new system.
  - Optionally queries `GalaxySimulationManager` or `StarSystemRuntime` to show additional runtime info (e.g. active fleets).
- The system view does not care which Unity scene is currently loaded; it is purely a UI representation driven by data.

---

## UI / Map: `GalaxyMapToggle.cs`

### Responsibility

- Controls **opening and closing the galaxy/system map UI** using Unity’s new Input System.

### Behaviour

- Unchanged conceptually:
  - Toggles the map root canvas via an input action.
  - Optionally triggers `GalaxyMapUIManager` refresh on open.

---

## Visuals / Environment: `SpaceBackgroundController.cs`

### Responsibility

- Provides a reusable space backdrop for in-system scenes via a generated skybox or background.

### Usage

- Add to in-system scenes as needed.
- Can be parameterised per star system via `StarSystemRuntime` (colour, brightness, etc.).

---

## Data Flow Summary (Multi-Scene)

1. **Galaxy Generation**
   - `GalaxyGenerator` builds systems and wormhole links at startup in the galaxy root scene.

2. **Initial State**
   - `GameDiscoveryState` is initialised with the starting system ID.
   - `GalaxySimulationManager` ensures the starting star system scene is loaded and its `StarSystemRuntime` is initialised.
   - Player ship spawns in that system scene near a `WormholeGate`.

3. **Exploration & Jump**
   - Ship enters a gate’s collider in a star system scene.
   - `ShipWormholeNavigator`:
     - Reads gate data (`WormholeGate` → `wormholeId`, `systemId`).
     - Requests a jump via `GalaxySimulationManager`.
   - `GalaxySimulationManager`:
     - Ensures the destination star system scene is loaded.
     - Moves the ship to the destination scene and gate.
     - Updates `GameDiscoveryState` (current system + discovery sets).

4. **Simulation**
   - In `FixedUpdate`, `GalaxySimulationManager` iterates all active `StarSystemRuntime` instances and calls `Simulate`.
   - Each `StarSystemRuntime` advances its own PhysicsScene and local gameplay.

5. **UI Updates**
   - `GameDiscoveryState` raises change events.
   - `GalaxyMapUIManager` updates current-system highlight and any discovery visualisation.
   - Other UI components update labels (current system name, exploration stats).

6. **Map Interaction**
   - Player uses the map toggle action.
   - `GalaxyMapToggle` enables/disables the map canvas and ensures the view is current.

---

## Extension Points

Natural extensions on top of this architecture:

- Use `StarSystemRuntime` to host local systems like AI brains, spawn managers, or local economy simulation.
- Extend `GalaxySimulationManager` with:
  - Priority-based simulation (e.g. simulate nearby systems at higher frequency).
  - Background time scaling for distant systems.
- Add debugging tools to visualise which star system scenes are loaded and how often they update.
