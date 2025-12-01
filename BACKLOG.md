# Space Game Development Backlog

Backlog for the Wormhole Era space exploration and reconstruction game.

Each task has an ID (e.g. `M1.1`) so tools and humans can reference them unambiguously.

---

## Bugs or gameplay issues

### B1 - Map UI
  -  When map is opened, first you see the star system map. But as soon as you zoom in or out, the galaxy map appears. When map is opened, it should open in the star system view, with the player's ship in the center, and close to it. There should be a variable similar to SystemMapWorldRadius that defines the size of the initial system area rendered in the map when first opened.

---

## Milestone 0 – Stabilize Current Prototype (Gates, Discovery, Map)

**Goal:** Reliable travel between systems via wormholes, with correct discovery state and a working map that updates without reopening.

### M0 – Data & Architecture

- **M0.1 – System & Wormhole IDs (implemented)**
  - Ensure each star system has a unique, persistent ID (e.g. ScriptableObject or data class).
  - Ensure each wormhole/gate pair has unique IDs and references their source/target systems.
  - Centralize this in a `GalaxyState` (or equivalent) manager so UI and gameplay share the same data source.

- **M0.2 – Discovery State (implemented)**
  - Add data structures to track:
    - Discovered systems.
    - Visited systems (subset of discovered).
    - Discovered wormholes.
  - Hook discovery updates into:
    - Initial starting system.
    - Successful wormhole jumps.

### M0 – Wormhole Travel & Prompts

- **M0.3 – Refactor Wormhole Jump Flow (implemented)**
  - Update `WormholeGate` so it exposes a “can jump / jump target” API.
  - Move the actual jump decision to the ship/controller:
    - Ship requests jump through a gate.
    - Gate validates and triggers scene/system transition.
  - Keep jump logic reusable for player and AI ships.

- **M0.4 – Screen-Space Wormhole Prompt (implemented)**
  - Create a `GatePromptLabel` prefab using TextMeshPro (screen-space UI).
  - Hook it into the ship’s targeting/proximity logic so:
    - When close to a gate, show "Approaching black hole".
    - When out of range or target changes, hide/refresh the prompt.
  - Ensure this works with the ship as a prefab (no direct scene references; prefer events or a shared manager).

### M0 – Map Refresh Issues

- **M0.5 – Event-Based Map Refresh (implemented)**
  - Create events in `GalaxyState`, e.g.:
    - `OnSystemDiscovered`.
    - `OnCurrentSystemChanged`.
  - Make `GalaxyMapUIManager` subscribe to these events to:
    - Update current-system highlight.
    - Add newly discovered systems and wormholes.
  - Fix the issue where the player must close/reopen the map to see changes.

- **M0.6 – In-System Boundary Rework (implemented)**
  - Replace the global cube clamp with a per-system spherical boundary in `PlayerShipController`.
  - Boundary is centered on `GameManager.CurrentSystemWorldPosition`; falls back to origin if unavailable.
  - Radius is configurable via `systemBoundaryRadius` (default 5000); outward velocity is projected to reduce jitter when clamped.

---

## Milestone 0A – Basic Playability Visuals (In-System)

**Goal:** Establish minimal in-system visuals so players can orient themselves: readable stars, backgrounds, and major points of interest.

- **M0A.1 – Space Background (skybox/parallax) [new]**
  - Add a simple starfield skybox or parallax background usable across systems.
  - Ensure performance-friendly settings and document any required materials/textures.

- **M0A.2 – Primary Star Visual [new]**
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

## Milestone 1 – Galaxy Generation & Topology

**Goal:** Generate a believable Wormhole Era network according to new rules (min/max spacing, randomized spread, connection counts, Solar System constraints).

### M1 – GalaxyGenerator Overhaul

- **M1.1 – Distance-Based Placement (implemented)**
  - Remove `galaxySize` dependency.
  - Add parameters:
    - `minSystemDistance`.
    - `maxSystemDistance`.
  - Place systems around the origin using Poisson-disk style sampling with jitter to stay uniform while keeping patterns organic.

- **M1.2 – Connection Distribution (implemented)**
  - Add parameters:
    - `minConnectionsPerSystem`.
    - `maxConnectionsPerSystem`.
  - Use this to create wormhole links:
    - Ensure overall connectivity (ideally a single connected graph, or very few components).
    - Sample connection counts from configurable weights (1–5) so high-degree hubs (4–5) stay rare.

- **M1.5 – Solar System Wormhole Cap (implemented)**
  - Ensure Earth's Solar System (system ID 0) always has exactly one wormhole connection.

- **M1.3 – Edge Placement Fallback (implemented)**
  - When a new star placement is too close to existing stars:
    - Try a limited number of retries with different offsets.
    - If all retries fail, place the star further out toward “edge” regions (e.g. using current max radius).
  - Avoid infinite loops and ensure predictable generation time.

- **M1.4 – Map Extents Output (implemented)**
  - From generated systems, compute spatial extents:
    - `minX`, `maxX`, `minY`, `maxY`.
  - Expose these values to `GalaxyMapUIManager` so it can:
    - Auto-fit the map camera.
    - Normalize coordinates into map space.

---

## Milestone 2 – Galaxy Map UX (2D Star Map with Lines, Zoom, Pan)

**Goal:** A 2D star map that visualizes the Wormhole Era network, supports zoom/pan, and reflects the player’s current position and discoveries.

### M2 – Rendering & Navigation

- **M2.1 – Coordinate Normalization (implemented)**
  - Decide a mapping from galaxy world coordinates to 2D map space (e.g. 0–1 or UI pixels).
  - Use the extents from `M1.4` to normalize positions:
    - Maintain aspect ratio.
    - Keep systems inside map bounds.

- **M2.2 – Pannable / Zoomable Map (implemented)**
  - Implement pan and zoom controls:
    - Pan via mouse drag and/or WASD/arrow keys.
    - Zoom via mouse scroll.
  - Implementation options:
    - Dedicated camera rendering a world-space map.
    - UI-based map using RectTransforms with scale/position changes (e.g. within a ScrollRect).

- **M2.3 – Draw Systems & Wormhole Lines (implemented)**
  - Render system nodes:
    - Distinct visuals for:
      - Current system.
      - Discovered systems.
      - Undiscovered systems, shown as unknown (only for systems adjacent to discovered systems).
  - Draw lines for wormholes between discovered systems, and from discovered systems to adjacent frontier systems when the wormhole is known.
  - Ensure this updates when:
    - New systems are discovered.
    - Wormholes become known.
    - The current system changes.
  - Drive these updates via `GalaxyState` events from `M0.5`.

- **M2.4 - Zooming on map (implemented)**
  - Pivot center of zooming on the map should be the mouse pointer.

- **M2.5 - Star system map (implemented)**
  - Map UI will allow user to see both the galaxy view and a star system view.
  - By default, the map is opened in the local star system view.
  - A star system map consists of:
    - A circle defining the delimited star system area.
    - A star in the middle.
    - Icons indicating the position of wormholes.
    - Icons indicating position of player's ship.
  - When zooming out to a custommizable threshold:
    - Map UI will switch from star system map to galaxy map.
    - Galaxy map should appear centered in star system that was zoomed in.
  - When zooming in close to a star, in the galaxy mode:
    - Map UI will switch to star system map.
    - The system selected for the zoom-in transition is the one closest to the current zoom pivot (mouse pointer), falling back to the view center if needed.
  - Clicking on a star will also open its star system map.

### M2 – Interaction

- **M2.6 – System Selection**
  - Enable clicking on a system node to:
    - Select/highlight it.
    - Open a small info panel showing:
      - System name.
      - Faction.
      - Basic properties (e.g. hazard level, known stations).
  - Clearly indicate the current system (e.g. halo, size difference, color).

- **M2.7 – Wormhole Selection**
  - Enable selecting wormhole edges:
    - Click on line or a gate icon on the map.
    - Show the two connected systems.
    - Provide a quick button to focus the map camera on the adjacent system.
  - Support the UX pattern: “Click wormhole, see the adjacent star system immediately.”

- **M2.8 – Jump / Navigation from Map**
  - From the current system, allow:
    - Selecting an adjacent system on the map and issuing a “jump” or “set course” command.
  - Internally:
    - Map selection to an actual gate or route for the player ship.
    - Reuse wormhole jump logic from `M0.3`.

---

## Milestone 3 – System View and In-System Control

**Goal:** From the galaxy map, zoom into a system, see ships, and issue basic orders.

### M3 – System View

- **M3.1 – System View Scene/Layout**
  - Implement a 2D or light 3D view for a single star system:
    - Display gates (wormholes).
    - Display stations/colonies (when implemented).
    - Display ships (player and AI).
  - Keep the layout readable and consistent with the galaxy map.

- **M3.2 – Transition Map ↔ System View**
  - From the galaxy map:
    - Allow zoom-in or a dedicated button to enter the current system view.
  - From the system view:
    - Allow returning to the galaxy map without full scene reload if possible (UI-driven layer).
  - Ensure current system context is preserved between views.

- **M3.3 – Ship Selection & Basic Orders**
  - Implement selection mechanics:
    - Click-to-select single ship.
    - Optional drag-select for multiple ships (later).
  - Provide basic orders for selected ships:
    - Move to point/waypoint.
    - Dock/undock when stations are present (future integration).
    - Jump via gate (integrating with `WormholeGate` logic and `M0.3`).

---

## Milestone 4 – Economy, Population, and Logistics Helpers

**Goal:** Introduce core mechanics for “population as a resource” and “logistics via fleets,” including feedback on whether assigned ships are sufficient for an order.

### M4 – Economy Foundation

- **M4.1 – Resource Definitions**
  - Define resource types (examples):
    - Raw ore, refined metals, fuel, food, tech parts, etc.
  - Define “Population” as a special resource type impacting:
    - Production capacity.
    - Growth.
    - Potentially crew requirements.

- **M4.2 – Colony/Station Model**
  - Create a data model for colonies/stations:
    - Population count.
    - Storage capacity per resource type.
    - Production/consumption rates.
    - Optional attributes like stability or habitability.
  - Integrate colony/station data with system view and galaxy state.

- **M4.3 – Fleet Cargo & Capabilities**
  - Extend ship/fleet data:
    - Cargo capacity (per resource or generic).
    - Speed and travel time modifiers.
    - Fuel usage if modeled.
    - Combat rating (for risk/evaluation).
  - Ensure fleets can be referenced and manipulated from UI and order systems.

### M4 – Orders & Evaluation

- **M4.4 – Order Types**
  - Implement basic order types:
    - Transport cargo (A → B).
    - Supply or “maintain threshold” orders.
    - Patrol/escort orders (for later risk modeling).
  - Define how orders are stored and executed over time.

- **M4.5 – “Is This Enough Ships?” Evaluation**
  - For each order, compute:
    - Required cargo throughput.
    - Expected travel time.
    - Optional: required combat rating for safety.
  - Compare with assigned fleet capabilities to determine:
    - Sufficient / borderline / insufficient status.
  - Integrate into order-creation UI:
    - Show a simple indicator with a short explanation (e.g. “Expected trips per hour: X, required: Y”).

---

## Milestone 5 – Factions, Territory, and Wormhole Era Politics

**Goal:** Make the galaxy feel like the Wormhole Era: Helios Protectorate vs Horizon Initiative vs alien polities, with territory and attitudes.

### M5 – Faction Data & Ownership

- **M5.1 – Faction ScriptableObjects**
  - Create faction definitions for:
    - Helios Protectorate.
    - Horizon Initiative.
    - Myriad Combine.
    - Verdure Hegemony.
    - Aelari Concord.
    - Karthan Assemblies.
    - Serathi Enclave.
  - Include attributes:
    - Name and display name.
    - Emblem/icon.
    - Primary color(s).
    - Behavioral tags (authoritarian, exploratory, isolationist, etc.).

- **M5.2 – System Ownership & Control**
  - Assign each system an owning faction (or “unclaimed”).
  - Optionally track:
    - Control level or stability.
    - Hostility/risk level for travel.

- **M5.3 – Map Overlays**
  - Add a faction overlay mode to the galaxy map:
    - Color systems based on owning faction.
    - Provide a legend for faction colors.
  - Optionally show contested or neutral zones with distinct styling.

### M5 – Reputation & Interaction

- **M5.4 – Reputation System**
  - Track player standing per faction:
    - Example states: Allied, Friendly, Neutral, Wary, Hostile.
  - Define simple rules for reputation changes:
    - Missions completed.
    - Trade.
    - Attacking ships or violating territory.
  - Hook reputation into:
    - Access to certain systems.
    - Prices and trade terms.
    - AI behavior (pirates, patrols, etc.).

---

## Milestone 6 – Narrative Scaffolding (Shrouding, Sol, Long-Term Goal)

**Goal:** Connect systems and mechanics to the lore: Earth as ruined but hopeful project, Helios-controlled Sol, and the long-term reconstruction theme.

- **M6.1 – Special Systems**
  - Define the Sol system as a special case:
    - Owned by Helios Protectorate.
    - Heavily fortified, restricted, or high-risk.
  - Define Earth as a toxic graveyard world:
    - Special environmental rules (e.g. uninhabitable surface, orbital infrastructure remnants).
    - Unique visuals or markers in system and map views.
  - Ensure these systems are non-random and always present.

- **M6.2 – Progression Locks**
  - Gate access to key systems/wormholes via:
    - Reputation thresholds.
    - Technology or research.
    - Story milestones (e.g. specific missions completed).
  - Implement a simple mechanism for “locked” vs “unlocked” wormholes and systems on the map.

- **M6.3 – Event System**
  - Implement an event layer capable of:
    - Triggering story beats (e.g. Helios crackdowns, Horizon discoveries).
    - Generating unique missions tied to:
      - Reconstruction.
      - Exploration of ancient wormhole tech.
      - Political tensions in the network.
  - Integrate events with:
    - Factions.
    - Systems (local incidents).
    - Player fleets and colonies.

This document should be updated as tasks are completed, and new mechanics are introduced.
