# Space Game Development Backlog

Backlog for the Wormhole Era space exploration and reconstruction game.

Each task has an ID (e.g. `M1.1`) so tools and humans can reference them unambiguously.

The backlog has been updated to reflect a **multi-scene, multi-physics star system architecture**, where:
- A persistent galaxy root scene holds global data and UI.
- Each star system has its own additive scene and physics world.
- A simulation manager advances all active star system scenes in real time.

---

## Bugs or gameplay issues

### B1 - Map UI
  -  When map is opened, first you see the star system map. But as soon as you zoom in or out, the galaxy map appears. When map is opened, it should open in the star system view, with the player's ship in the center, and close to it. There should be a variable similar to SystemMapWorldRadius that defines the size of the initial system area rendered in the map when first opened.
  -  Fixed: system map now opens centered on the player with a configurable initial system view radius to prevent unintended galaxy view switching when zooming.

### B2 - Map UI player ship movement
  - Player ship icon in the star system map does not track player ship and looks still.

### B3 - In-system star renders as sphere with no texture or materials
---

## Milestone 0 – Stabilize Current Prototype (Gates, Discovery, Map)

**Goal:** Reliable travel between systems via wormholes, with correct discovery state and a working map that updates without reopening.

### M0 – Data & Architecture

- **M0.1 – System & Wormhole IDs (IMPLEMENTED)**
  - Ensure each star system has a unique, persistent ID (e.g. ScriptableObject or data class).
  - Ensure each wormhole/gate pair has unique IDs and references their source/target systems.
  - Centralize this in a `GalaxyState`/`GalaxyGenerator` manager so UI and gameplay share the same data source.

- **M0.2 – Discovery State (IMPLEMENTED)**
  - Add data structures to track:
    - Discovered systems.
    - Visited systems (subset of discovered).
    - Discovered wormholes.
  - Hook discovery updates into:
    - Initial starting system.
    - Successful wormhole jumps.

### M0 – Wormhole Travel & Prompts

- **M0.3 – Refactor Wormhole Jump Flow (IMPLEMENTED)**
  - Update `WormholeGate` so it exposes a “can jump / jump target” API.
  - Move the actual jump decision to the ship/controller:
    - Ship requests jump through a gate.
    - Gate validates and triggers scene/system transition.
  - Keep jump logic reusable for player and AI ships.

- **M0.4 – Screen-Space Wormhole Prompt (IMPLEMENTED)**
  - Create a `GatePromptLabel` prefab using TextMeshPro (screen-space UI).
  - Hook it into the ship’s targeting/proximity logic so:
    - When close to a gate, show "Approaching black hole".
    - When out of range or target changes, hide/refresh the prompt.
  - Ensure this works with the ship as a prefab (no direct scene references; prefer events or a shared manager).

### M0 – Map Refresh Issues

- **M0.5 – Event-Based Map Refresh (IMPLEMENTED)**
  - Create events in `GalaxyState`, e.g.:
    - `OnSystemDiscovered`.
    - `OnCurrentSystemChanged`.
  - Make `GalaxyMapUIManager` subscribe to these events to:
    - Update current-system highlight.
    - Add newly discovered systems and wormholes.
  - Fix the issue where the player must close/reopen the map to see changes.

- **M0.6 – In-System Boundary Rework [new]**
  - Replace the global cube clamp with a per-system spherical boundary in `PlayerShipController`.
  - Boundary is centered on the current star system’s anchor (via a local system manager or runtime).
  - Radius is configurable via `systemBoundaryRadius` (default 5000); outward velocity is projected to reduce jitter when clamped.

---

## Milestone 0A – Basic Playability Visuals (In-System)

**Goal:** Establish minimal in-system visuals so players can orient themselves: readable stars, backgrounds, and major points of interest.

- **M0A.1 – Space Background (static starfield) (IMPLEMENTED)**
  - Add a single static starfield background usable across systems.
  - Ensure performance-friendly settings and document any required materials/textures.

- **M0A.2 – Primary Star Visual (IMPLEMENTED)**
  - Create a basic 3D star using a mesh plus sprite/texture overlay for glow.
  - Configure lighting/intensity so nearby ships remain visible without overexposure.
  - Expose color/size parameters so different system stars can be represented.

- **M0A.3 – Black Hole / Gate Anchor Visual [new]**
  - Add a readable black hole visual for wormhole anchors (e.g., distorted sphere, particle swirl, rim glow).
  - Keep scale adjustable per gate to match gameplay boundaries and prompts.

- **M0A.4 – Planet/Body Placeholders [new]**
  - Provide simple planet or moon stand-ins (spheres with basic materials) for system dressing.
  - Include a lightweight rotation script or animation hook for optional slow spins.

- **M0A.5 – Ambient Lighting & Exposure Baseline [new]**
  - Set default ambient light and exposure values so ships and UI remain legible in dark space.
  - Document any post-processing profiles needed for the baseline look.

- **M0A.6 – Navigation Readability Touches [new]**
  - Add unobtrusive distance/selection markers for key objects (stars, black holes, planets) visible against the background.
  - Verify markers work with existing targeting/prompt systems without scene-specific references.

---

## Milestone 0B – Multi-Scene Star System Architecture (NEW)

**Goal:** Move from “all systems share one physical space” to a **multi-scene, multi-physics** setup where each star system has its own scene and physics world, but all are simulated in real time.

- **M0B.1 – Persistent Galaxy Root Scene (IN PROGRESS)**
  - Create a dedicated “GalaxyRoot” scene that stays loaded:
    - Hosts `GalaxyGenerator`, `GameDiscoveryState`, `GalaxySimulationManager`, and global UI (map, HUD, menus).
  - Ensure this scene can bootstrap the initial star system.
  - **Notes:** `GalaxySimulationManager` now assumes the persistent scene owns the global managers and will auto-load the current
    system when discovery state is initialised. Scene authoring steps added to `UNITY_CONFIGURATION.md`.

- **M0B.2 – Star System Scene Template (IN PROGRESS)**
  - Define a reusable star system scene layout:
    - Contains a `StarSystemRuntime` root object.
    - Includes slots for star, background, wormhole gates, and local ships.
  - Configure one or more example system scenes and link them to system IDs in `GalaxyGenerator`.
  - **Notes:** Star system runtimes are now created procedurally per system using `LocalPhysicsMode.Physics3D` and the generated
    star visuals. Authored template scenes can still be added later for richer dressing.

- **M0B.3 – Per-System Physics Worlds (IMPLEMENTED)**
  - Ensure each star system scene has its own physics world (PhysicsScene):
    - Set up creation using `LocalPhysicsMode.Physics3D` or equivalent.
  - Confirm that colliders and rigidbodies from different systems never interact.
  - **Notes:** `GalaxySimulationManager` creates per-system scenes with isolated physics and drives them in `FixedUpdate`.

- **M0B.4 – GalaxySimulationManager Implementation (IN PROGRESS)**
  - Implement `GalaxySimulationManager`:
    - Tracks active `StarSystemRuntime` instances by system ID.
    - Loads/unloads star system scenes as needed.
    - Advances each system’s physics and gameplay in `FixedUpdate`.
  - Provide a simple debug UI to show which systems are currently loaded.
  - **Notes:** Manager now creates runtime scenes, spawns gates, moves ships across scenes, and updates a debug label. Unloading
    and persistent AI hooks remain to be fleshed out.

- **M0B.5 – Ship Transfer Between Scenes (TODO)**
  - Implement the logic to move ships between system scenes on wormhole jumps:
    - Use `SceneManager.MoveGameObjectToScene` or equivalent.
    - Position ships at the destination gate’s exit point.
  - Ensure components such as `PlayerShipController` and `ShipWormholeNavigator` remain functional after scene moves.

- **M0B.6 – AI Ships in Multiple Systems (TODO)**
  - Define how AI ships are registered with `StarSystemRuntime`.
  - Keep AI ships simulated in their local star system scenes even when the player is not present.
  - Provide hooks for later economy/logistics systems to query ships per system.

---

## Milestone 1 – Galaxy Generation & Topology

**Goal:** Generate a believable Wormhole Era network according to new rules (min/max spacing, randomized spread, connection counts, Solar System constraints).

### M1 – GalaxyGenerator Overhaul

- **M1.1 – Distance-Based Placement (IMPLEMENTED)**
  - Remove `galaxySize` dependency.
  - Add parameters:
    - `minSystemDistance`.
    - `maxSystemDistance`.
  - Place systems around the origin using Poisson-disk style sampling with jitter.

- **M1.2 – Connection Distribution (IMPLEMENTED)**
  - Add parameters:
    - `minConnectionsPerSystem`.
    - `maxConnectionsPerSystem`.
  - Use this to create wormhole links with configurable hub distribution.

- **M1.5 – Solar System Wormhole Cap (IMPLEMENTED)**
  - Ensure Earth's Solar System (system ID 0) always has exactly one wormhole connection.

- **M1.3 – Edge Placement Fallback (IMPLEMENTED)**
  - Implement a robust fallback for star placement when min-distance constraints fail.

- **M1.4 – Map Extents Output (IMPLEMENTED)**
  - From generated systems, compute spatial extents for use by `GalaxyMapUIManager`.

---

## Milestone 2 – Galaxy Map UX (2D Star Map with Lines, Zoom, Pan)

**Goal:** A 2D star map that visualizes the Wormhole Era network, supports zoom/pan, and reflects the player’s current position and discoveries.

### M2 – Rendering & Navigation

- **M2.1 – Coordinate Normalization (IMPLEMENTED)**
  - Map galaxy-space coordinates into 2D map space using extents from `M1.4`.

- **M2.2 – Pannable / Zoomable Map (IMPLEMENTED)**
  - Implement pan and zoom controls using a dedicated map camera or UI transforms.

- **M2.3 – Draw Systems & Wormhole Lines (IMPLEMENTED)**
  - Render system nodes and wormhole links.
  - Visually distinguish current, discovered, and frontier systems.

- **M2.4 – Zoom Pivot on Mouse (IMPLEMENTED)**
  - Pivot zooming on the mouse pointer.

- **M2.5 – Star System Map (IMPLEMENTED)**
  - Map UI allows user to see both galaxy view and star system view.
  - By default, the map opens in the local star system view.
  - Handles smooth switching between system view and galaxy view based on zoom.

- **M2.10 – Galaxy Map State Container (TODO)**
  - Extract galaxy map data (systems, factions, hazards, ownership flags) into a dedicated `GalaxyState.cs` script separate from UI.
  - Keep `GalaxyMapUIManager` reading from this state container (backed by `GalaxyGenerator`/discovery data) rather than storing map state internally.

### M2 – Interaction

- **M2.6 – System Selection (IMPLEMENTED)**
  - Enable clicking on a system node to select/highlight it and open an info panel.

- **M2.7 – Wormhole Selection (TODO)**
  - Enable selecting wormhole edges in the map and system view for adjacency preview.

- **M2.8 – Jump / Navigation from Map (TODO)**
  - From the current system, allow selecting an adjacent system and issuing a “jump” or “set course” command.
  - Internally, map this to actual gate usage in the relevant system scene via `GalaxySimulationManager`.

- **M2.9 – System Mouse Hovering (TODO)**
  - Enable hover tooltips for systems showing their information using the existing info panel.

---

## Milestone 3 – System View and In-System Control

**Goal:** From the galaxy map, zoom into a system, see ships, and issue basic orders, using the new multi-scene star system architecture.

### M3 – System View

- **M3.1 – System View Scene/Layout (TODO)**
  - Finalise the layout for a single star system **scene**:
    - Display gates (wormholes).
    - Display stations/colonies (when implemented).
    - Display ships (player and AI).
  - Reuse the `StarSystemRuntime` and in-system scene template from `M0B.2`.

- **M3.2 – Transition Map ↔ System View (TODO)**
  - From the galaxy map:
    - Allow zoom-in or a dedicated button to focus on the current system (camera framing into the active in-system scene).
  - From the system view:
    - Allow returning to the galaxy map without full scene reload (UI-driven layer; the in-system scene stays loaded).
  - Ensure current system context is preserved between views via `GameDiscoveryState`.

- **M3.3 – Ship Selection & Basic Orders (TODO)**
  - Implement selection mechanics in the in-system scene:
    - Click-to-select single ship.
    - Optional drag-select for multiple ships.
  - Provide basic orders for selected ships:
    - Move to point/waypoint.
    - Dock/undock when stations are present.
    - Jump via gate:
      - Integrate with `ShipWormholeNavigator` and `GalaxySimulationManager` for cross-scene transfers.

---

## Milestone 4 – Economy, Population, and Logistics Helpers

**Goal:** Introduce core mechanics for “population as a resource” and “logistics via fleets,” including feedback on whether assigned ships are sufficient for an order.

### M4 – Economy Foundation

- **M4.1 – Resource Definitions (TODO)**
  - Define resource types (ore, metals, fuel, food, tech parts, etc.).
  - Define “Population” as a special resource type.

- **M4.2 – Colony/Station Model (TODO)**
  - Create a data model for colonies/stations linked to star systems.
  - Integrate with `StarSystemRuntime` so colonies/stations exist in specific system scenes.

- **M4.3 – Fleet Cargo & Capabilities (TODO)**
  - Extend ship/fleet data with cargo, speed, fuel, and combat rating.
  - Ensure fleets can be referenced from both galaxy-level UI and in-system scenes.

### M4 – Orders & Evaluation

- **M4.4 – Order Types (TODO)**
  - Implement basic order types like transport, supply, and patrol/escort.

- **M4.5 – “Is This Enough Ships?” Evaluation (TODO)**
  - For each order, compute requirements vs assigned fleet capabilities.
  - Integrate into order-creation UI with simple indicators.

---

## Milestone 5 – Factions, Territory, and Wormhole Era Politics

**Goal:** Make the galaxy feel like the Wormhole Era: Helios Protectorate vs Horizon Initiative vs alien polities, with territory and attitudes.

### M5 – Faction Data & Ownership

- **M5.1 – Faction ScriptableObjects (TODO)**
  - Create faction definitions (Helios Protectorate, Horizon Initiative, Myriad Combine, etc.).

- **M5.2 – System Ownership & Control (TODO)**
  - Assign each system an owning faction (or “unclaimed”).
  - Link ownership to system IDs used by `StarSystemRuntime`.

- **M5.3 – Map Overlays (TODO)**
  - Add a faction overlay mode to the galaxy map.

### M5 – Reputation & Interaction

- **M5.4 – Reputation System (TODO)**
  - Track player standing per faction and hook into trade, access, and AI behaviour.

---

## Milestone 6 – Narrative Scaffolding (Shrouding, Sol, Long-Term Goal)

**Goal:** Connect systems and mechanics to the lore: Earth as ruined but hopeful project, Helios-controlled Sol, and the long-term reconstruction theme.

- **M6.1 – Special Systems (TODO)**
  - Define Sol/Earth as special systems with fixed system IDs and dedicated star system scenes.

- **M6.2 – Progression Locks (TODO)**
  - Gate access to key systems/wormholes via reputation, tech, or story milestones.

- **M6.3 – Event System (TODO)**
  - Implement an event system for story beats, missions, and dynamic incidents.
  - Integrate events with systems, factions, and fleets using the new multi-scene architecture.
