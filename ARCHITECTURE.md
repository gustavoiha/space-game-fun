# Unity Wormhole Era Prototype – Script Architecture

This document describes the main C# scripts in the Unity Wormhole Era prototype and how they fit together after introducing:

- A **multi-scene, multi-physics star system architecture**.
- A **galaxy-only strategic map** (no dedicated 2D in-system map).
- A **system intel / manifest layer** with delayed, unreliable information about remote systems.
- A logistics model where **ships and messages must physically travel through wormholes**, and where transports can go missing without the player ever seeing the moment they were lost.

It is intended as reference for code navigation and as context for AI-assisted tools (e.g. Codex).

---

## High-level Overview

Core ideas:

- A procedurally generated **galaxy of star systems**, connected by stable wormholes.
- **Runtime state** that tracks:
  - Which systems and wormholes have been discovered.
  - Which system the player is currently in.
  - The **last-known manifest** of each system (stations, fleets, population, status).
  - How **stale / reliable** that intel is.
- World objects (**wormhole gates**) that map the physical in-system scenes into the abstract galaxy graph.
- Ship logic that handles entering/exiting wormholes, moving between scenes, and updating:
  - Discovery state.
  - System intel manifests when the player (or a courier) arrives.
- A **2D galaxy map UI** that:
  - Visualises the discovered wormhole network.
  - Shows only **last-known intel** per system (no live remote positions).
  - Highlights intel age / uncertainty and warnings such as **missing transports**.
- Only two spatial views for the player:
  - **In-system ship view** (player’s camera flying in the current star system).
  - **Galaxy view** (abstract, informational).
- Input using Unity’s new Input System (no direct use of `UnityEngine.Input`).
- A **persistent galaxy root scene** that holds global simulation state, intel, and UI.
- One **additive in-system scene per star system**, each with its own physics world, so colliders and rigidbodies never interact across systems.
- A **simulation manager** that advances physics for all active star system scenes in real time, even if they are not currently being rendered.

---

## Script Index

> Note: Names are based on the current prototype and may evolve. Items marked *(NEW)* are additions or significantly redefined components to support the intel-based, galaxy-only map design.

- **Core / Data**
  - `GalaxyGenerator.cs`
  - `SystemManifest.cs` *(NEW – data model)*
- **Game State**
  - `GameDiscoveryState.cs`
  - `SystemIntelState.cs` *(NEW – runtime intel/manifests)*
  - `GalaxySimulationManager.cs`
  - `StarSystemRuntime.cs`
  - `GameManager.cs`
- **World / Gameplay**
  - `PlayerShipController.cs`
  - `WormholeGate.cs`
  - `ShipWormholeNavigator.cs`
- **UI / Map & Intel**
  - `GalaxyMapUIManager.cs`
  - `GalaxyMapToggle.cs`
  - `SystemManifestPanel.cs` *(NEW – system info / manifest UI)*
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
      SystemManifest.cs
    State/
      GameDiscoveryState.cs
      SystemIntelState.cs
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
      SystemManifestPanel.cs
      // additional UI helper components (prompt labels, system labels, etc.)
    Visual/
      SpaceBackgroundController.cs
```

---

## Multi-Scene / Multi-Physics Architecture

### Scene layout

The game uses a multi-scene setup similar to the previous design, but the **presentation and intel model have changed**:

- **Persistent Galaxy Root Scene**
  - Stays loaded for the entire play session.
  - Contains:
    - `GalaxyGenerator` (galaxy topology and system positions).
    - `GameDiscoveryState` (what the player has discovered and where they are).
    - `SystemIntelState` (last-known manifests and intel reliability per system).
    - `GalaxySimulationManager` (central simulation loop for star system scenes).
    - Global UI such as:
      - Galaxy map (`GalaxyMapUIManager`, `GalaxyMapToggle`).
      - System manifest/info panel (`SystemManifestPanel`).
      - HUD, pause menus, etc.
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

`GalaxySimulationManager` instantiates these additive scenes on demand using `LocalPhysicsMode.Physics3D` to enforce physics
isolation. It moves generated star visuals from `GalaxyGenerator` into the proper scene, builds gate prefabs for every neighbor
system, and registers ships as they enter.

> Design Note: **There is no dedicated 2D “in-system tactical map” anymore.** All tactical awareness inside the current star system comes from:
> - The 3D in-system camera.
> - Contextual markers / prompts.
> The galaxy map is purely abstract and informational.

### Physics separation

- Each star system scene has its own **physics world** (`PhysicsScene`):
  - Colliders and rigidbodies in one system cannot collide with or “see” objects in another system.
  - No need to check “are these objects in the same star system?” at collision time; physics isolation guarantees it.

- `GalaxySimulationManager`:
  - Holds references to all active `StarSystemRuntime` instances and their PhysicsScenes.
  - Advances each system’s physics explicitly (e.g. `physicsScene.Simulate(fixedDeltaTime)` for off-camera systems).
  - Ensures the currently viewed system is simulated in lockstep with Unity’s `FixedUpdate`.

This architecture keeps the galaxy logically unified while preserving clean physical separation between systems.

### Wormhole travel and scene transitions

- Wormhole gates remain the bridge between systems at the **data level** (`GalaxyGenerator`) and **scene level** (Unity scenes).
- When a ship jumps:
  1. `ShipWormholeNavigator` detects entry into a `WormholeGate` and resolves the destination system/gate via `GalaxyGenerator`.
  2. `GalaxySimulationManager`:
     - Ensures the destination star system scene is loaded (load if necessary).
     - Locates or spawns the destination `WormholeGate` in that scene.
  3. The ship GameObject is moved between scenes using `SceneManager.MoveGameObjectToScene` and placed at the destination gate’s exit point.
  4. `GameDiscoveryState` is updated:
     - `SetCurrentSystem(destinationSystemId)`.
     - `DiscoverSystem(destinationSystemId)` / `DiscoverWormhole(wormholeId)` as needed.
  5. **System intel is updated** via `SystemIntelState`:
     - A fresh manifest snapshot is captured from the `StarSystemRuntime` (stations, fleets, etc.).
     - Timestamp and reliability are reset for that system.
  6. Camera and UI are updated to follow the ship in the new system scene.

- AI ships and fleets follow the same pattern:
  - Each ship belongs to exactly one star system scene at a time.
  - When an AI ship uses a wormhole, its GameObject is transferred to the destination scene and its owning `StarSystemRuntime`.
  - When a “courier” ship designated as a **message carrier** reaches a system, `SystemIntelState` can be updated to reflect newly delivered intel.

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

### Dependencies & Usage

- **Used by:**
  - `GameDiscoveryState` (for validating system and wormhole IDs).
  - `SystemIntelState` (for cross-checking manifest/system IDs).
  - `GalaxySimulationManager` (to map system IDs to scenes).
  - `WormholeGate` (to know which systems/gates are connected).
  - `ShipWormholeNavigator` (to find destinations).
  - `GalaxyMapUIManager` (to render system positions and connection lines).

- **Lifetime:**
  - Persistent singleton-like MonoBehaviour in the galaxy root scene.

---

## Core / Data: `SystemManifest.cs` *(NEW)*

### Responsibility

- Defines a **data-only snapshot** of what is (or was) known to exist in a star system:
  - Stations/colonies and their high-level status.
  - Known fleets and their broad roles (trade, escort, pirate activity, etc.).
  - Population, resource summary, and any notable alerts.

### Example Data Fields

- `string systemId`
- `DateTime lastUpdatedGameTime`
- `float reliability` (0–1; 1 = just updated, 0 = completely unreliable)
- `List<StationEntry> stations`
- `List<FleetEntry> fleets`
- `List<SystemAlert> alerts`  
  e.g. “Transport ship missing”, “High pirate activity”, “Wormhole unstable”.

This is pure data, no MonoBehaviour. Instances are owned by `SystemIntelState`.

---

## Game State: `GameDiscoveryState.cs`

### Responsibility

- Tracks the **player’s knowledge of existence**:
  - Which systems are discovered.
  - Which wormholes are discovered.
  - Which system the player is currently in.

> Design distinction:
> - `GameDiscoveryState` answers “Do we know this exists at all?”.
> - `SystemIntelState` answers “What do we think is happening there, and how out-of-date is that knowledge?”.

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
  - `bool IsWormholeDiscovered(string systemId)`

- Events:
  - `event Action<string> CurrentSystemChanged`
  - `event Action DiscoveryChanged`

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator`.

- **Used by:**
  - `GalaxySimulationManager`
  - `ShipWormholeNavigator`
  - `GalaxyMapUIManager` (discovery highlighting).
  - `SystemManifestPanel` (to decide if a system is even eligible to show intel).

---

## Game State: `SystemIntelState.cs` *(NEW)*

### Responsibility

- Stores **system manifests and intel reliability** for all discovered systems.
- Models **wormhole communication limits**:
  - No true real-time remote data.
  - Intel updates only when:
    - Player ship is physically present in a system.
    - A designated courier/message ship arrives and “syncs” data.
- Detects and records **missing transports** and other long-range issues.

### Core Data

- `Dictionary<string, SystemManifest> manifestsBySystemId`
- `List<MissingShipRecord> missingShips` (for UI warnings)
- Configuration:
  - `float intelDecayPerGameDay` (how fast reliability drops).
  - Thresholds for warning (e.g. `reliability < 0.3` => “stale intel”).

### Important Methods (conceptual)

- Manifest management:
  - `SystemManifest GetOrCreateManifest(string systemId)`
  - `void UpdateManifest(string systemId, SystemManifest newSnapshot, DateTime now)`
  - `SystemManifest? TryGetManifest(string systemId)`
- Intel decay:
  - `void TickIntelDecay(float deltaGameDays)`
- Transport/missing-ship tracking:
  - `void RegisterPlannedTransport(TransportOrder order)`
  - `void MarkTransportArrived(TransportOrder order)`
  - `void CheckForOverdueTransports(DateTime now)` → generates `MissingShipRecord` and adds alerts to manifests.

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator` / `GameDiscoveryState` for valid IDs.

- **Used by:**
  - `GalaxySimulationManager`:
    - To push new snapshots when a system is visited / a courier arrives.
    - To tick intel decay based on game time.
  - `StarSystemRuntime`:
    - When asked, can generate a fresh `SystemManifest` snapshot for its system.
  - `GalaxyMapUIManager` & `SystemManifestPanel`:
    - To visualise intel age, reliability, alerts (e.g. “Transport ship is missing”).

---

## Simulation / Scenes: `GalaxySimulationManager.cs`

### Responsibility

- Orchestrates **multi-scene simulation**:
  - Tracks all active `StarSystemRuntime` instances.
  - Loads/unloads star system scenes.
  - Advances each system’s physics.
- Integrates with **intel and communication constraints**:
  - Updates `SystemIntelState` when the player or couriers arrive.
  - Ticks intel decay and checks for missing transports.

### Core Data

- `Dictionary<string, StarSystemRuntime> activeSystemsById`
- Optional:
  - `StarSystemRuntime playerSystemRuntime`
  - Configuration for maximum concurrently loaded systems.

### Important Methods (conceptual)

- Scene management:
  - `Task<StarSystemRuntime> EnsureSystemLoadedAsync(string systemId)`
  - `void UnloadSystem(string systemId)`
  - `IEnumerable<StarSystemRuntime> GetActiveSystems()`

- Simulation:
  - `void FixedUpdate()`
    - Iterate active `StarSystemRuntime` instances:
      - `runtime.Simulate(fixedDeltaTime)`
    - Call `SystemIntelState.TickIntelDecay()` based on elapsed game time.
    - Periodically call `SystemIntelState.CheckForOverdueTransports()`.

- Wormhole travel:
  - `void JumpShipThroughWormhole(ShipWormholeNavigator ship, WormholeGate originGate, string wormholeId)`
    - Resolve destination via `GalaxyGenerator`.
    - Ensure destination system is loaded.
    - Move the ship GameObject to destination scene.
    - Update `GameDiscoveryState` (current system and discovery).
    - Request `StarSystemRuntime` to produce a manifest snapshot for destination system and push it into `SystemIntelState`.

---

## Simulation / Scenes: `StarSystemRuntime.cs`

### Responsibility

- Represents the **runtime state and entry point** for a single star system scene.
- Knows how to generate a **manifest snapshot** of the system for `SystemIntelState`.

### Core Data

- `string systemId`
- `Scene unityScene`
- `PhysicsScene physicsScene`
- Collections of key local entities (ships, stations, etc.).

### Important Methods (conceptual)

- `void Initialize(string systemId, GalaxyGenerator generator)`
- `void Simulate(float deltaTime)`
- Intel snapshot:
  - `SystemManifest BuildManifestSnapshot(DateTime now)`:
    - Enumerates stations, fleets, alerts.
    - Does not include precise coordinates for remote visibility (we don’t expose real-time positions outside this system).

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator`.

- **Used by:**
  - `GalaxySimulationManager`
  - `SystemIntelState` (indirectly, via snapshot generation).

---

## World / Gameplay: `WormholeGate.cs`

*(Functionally similar to previous design; unchanged aside from feeding intel updates indirectly.)*

- Identifies a wormhole gate within a star system scene.
- Holds a reference to a `wormholeId` or pair of system IDs it connects.
- Triggers `ShipWormholeNavigator` when the player or AI ships enter its volume.
- Visual and audio feedback for entering/exiting.

---

## World / Gameplay: `PlayerShipController.cs`

- Handles free flight within the current star system:
  - Thrust, turning, gravity drive/ion thruster simulation.
  - Collision and clamping to the system’s playable radius.
- Integrates with camera rig:
  - First- or third-person views.
  - HUD overlays for gates, stations, and other key targets.

---

## World / Gameplay: `ShipWormholeNavigator.cs`

- Monitors player or AI ship proximity to `WormholeGate` triggers.
- Handles jump initiation:
  - Can show prompts (e.g., “Press F to enter wormhole to [System Name]”).
- Delegates the actual transfer to `GalaxySimulationManager`.
- On successful arrival:
  - Optionally triggers VFX.
  - Ensures local systems (navigation, HUD) are updated.

---

## UI / Map: `GalaxyMapUIManager.cs`

### Responsibility

- Renders and manages the **2D galaxy map**:
  - No longer provides a 2D in-system tactical view.
  - Shows:
    - Discovered systems and wormhole links.
    - Player’s current system.
    - **System intel status** (manifest highlights, intel age, missing-ship alerts).

### Core Behaviour

- Renders systems and wormholes from `GalaxyGenerator`.
- Colours/overlays systems based on:
  - Discovery (`GameDiscoveryState`).
  - Intel reliability (`SystemIntelState`):
    - e.g. faded color for stale intel.
    - warning icons for missing transports or critical alerts.
- Highlights the current system.
- Supports pan/zoom as before, but **always stays at galaxy scale**:
  - No zoom into a nested “system map” – the deepest view is reading the manifest for a single system.
- Integrates with `SystemManifestPanel`:
  - Clicking a system node opens or updates the manifest panel with that system’s last-known manifest.

### Integration With Multi-Scene Architecture

- The map stays decoupled from Unity scenes:
  - It uses data from `GalaxyGenerator`, `GameDiscoveryState`, and `SystemIntelState`.
  - It **never shows live positions** for objects outside the current system; everything outside is abstract or last-known.
- For the **current system**, the map may show a simple icon or highlight but defers all detailed tactical awareness to the in-system 3D camera and HUD.

---

## UI / Map: `GalaxyMapToggle.cs`

### Responsibility

- Controls **opening and closing the galaxy view** using Unity’s new Input System.

### Behaviour

- Toggles the galaxy map root canvas via an input action.
- Optionally toggles the `SystemManifestPanel` alongside the map.
- Does not switch between “system map vs galaxy map” anymore – only between:
  - “In-system camera / HUD”
  - “Galaxy map + intel UI”

---

## UI / Intel: `SystemManifestPanel.cs` *(NEW)*

### Responsibility

- Displays the **last-known manifest** and intel status for a single star system.

### Core Behaviour

- When a system is selected on the galaxy map:
  - Retrieves its `SystemManifest` (if any) from `SystemIntelState`.
  - Shows:
    - System name and owning faction (if known).
    - Last-updated timestamp and reliability.
    - Summary of known stations, fleets, population, and alerts.
  - If no manifest exists yet:
    - Shows a “No intel yet” state.

- For the **current system**:
  - Optionally indicates that data is “Live” (since the player is physically present and the manifest is being refreshed frequently).

---

## Visuals / Environment: `SpaceBackgroundController.cs`

- Controls backdrop visuals for each star system.
- May pick a skybox, starfield, and color grading preset based on star type or faction control.

---

## Data Flow Summary (Multi-Scene + Intel)

1. **Galaxy Generation**
   - `GalaxyGenerator` builds systems and wormhole links at startup.

2. **Initial State**
   - `GameDiscoveryState` initialises with starting system ID.
   - `GalaxySimulationManager` loads the starting system scene and initialises `StarSystemRuntime`.
   - Player ship spawns in that system.

3. **Exploration & Jump**
   - Ship enters a gate’s collider.
   - `ShipWormholeNavigator` requests a jump via `GalaxySimulationManager`.
   - `GalaxySimulationManager` loads destination scene, moves the ship, updates `GameDiscoveryState`.
   - `StarSystemRuntime` builds a `SystemManifest` snapshot for the destination system.
   - `SystemIntelState` stores this manifest with full reliability (fresh intel).

4. **Simulation & Intel Decay**
   - In `FixedUpdate`, `GalaxySimulationManager`:
     - Calls `Simulate` on each `StarSystemRuntime`.
     - Calls `SystemIntelState.TickIntelDecay()` to reduce reliability over time.
     - Calls `SystemIntelState.CheckForOverdueTransports()` to mark missing transports and add alerts.

5. **UI Updates**
   - `GameDiscoveryState` and `SystemIntelState` raise change events.
   - `GalaxyMapUIManager`:
     - Updates visibility/highlights for discovered systems and wormholes.
     - Updates visual overlays for intel age and alerts.
   - `SystemManifestPanel` updates when the selected system changes or its manifest changes.

6. **Map Interaction**
   - Player toggles the galaxy view via `GalaxyMapToggle`.
   - Player can:
     - Inspect systems and read their manifests.
     - Issue high-level orders (e.g. schedule transports) based on **stale intel**.
   - When returning from the galaxy view, the player is back in the in-system ship camera; there is no separate 2D system map layer.

---

## Extension Points

Natural extensions on top of this architecture:

- Extend `SystemManifest` to hold **economy snapshots** (resources, throughput).
- Extend `SystemIntelState` with:
  - Multiple intel sources (player, couriers, factions).
  - Probabilistic predictions for systems that have not been visited for a long time.
- Add an **orders/logistics layer** that:
  - Issues transport/patrol orders based on last-known data.
  - Feeds into the missing-ship logic and system alerts.
- Add UI filters on the galaxy map to highlight:
  - Systems with very stale intel.
  - Systems with missing transports.
  - High-value routes vs dangerous frontier systems.
