# Unity Wormhole Era Prototype – Script Architecture

This document describes the main C# scripts in the Unity Wormhole Era prototype and how they fit together. It is intended as reference for code navigation and as context for AI-assisted tools (e.g. Codex).

---

## High-level Overview

Core ideas:

- A procedurally generated galaxy of star systems, connected by stable wormholes.
- Runtime state that tracks which systems and wormholes have been discovered and which system the player is currently in.
- World objects (wormhole gates) that map the physical scene into the abstract galaxy graph.
- Ship logic that handles entering/exiting wormholes and updating discovery state.
- A 2D galaxy map UI that visualizes the discovered network and allows the player to orient themselves.
- Input using Unity’s new Input System (no direct use of `UnityEngine.Input`).

---

## Script Index

> Note: Names are based on the current prototype and may evolve.

- **Core / Data**
  - `GalaxyGenerator.cs`
- **Game State**
  - `GameDiscoveryState.cs`
- **World / Gameplay**
  - `WormholeGate.cs`
  - `ShipWormholeNavigator.cs`
- **UI / Map**
  - `GalaxyMapUIManager.cs`
  - `GalaxyMapToggle.cs`
- **UI / Contextual Prompts**
  - (Logic integrated into `ShipWormholeNavigator` and/or dedicated prompt UI components, depending on the current iteration.)

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
    World/
      WormholeGate.cs
      ShipWormholeNavigator.cs
    UI/
      GalaxyMapUIManager.cs
      GalaxyMapToggle.cs
      // additional UI helper components (prompt labels, system labels, etc.)
```

---

## Core: `GalaxyGenerator.cs`

### Responsibility

- Owns the **procedurally generated galaxy graph**:
  - Star systems
  - Wormhole links between systems
- Provides fast lookup of systems and wormholes by ID.
- Serves as a read-only data source for other systems after generation.

### Key Concepts

- **Star System**
  - Unique identifier (e.g. `string systemId` or `int id`).
  - Display name.
  - 3D position in world/galaxy space (used both for scene layout and 2D map projection).
  - Metadata hooks (e.g. starting system flag, difficulty, faction control) for later expansion.

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
    - Uses:
      - Min/max distance constraints with random sampling between them (no distribution curve bias).
      - Randomized connection counts within min/max instead of a distribution curve.
      - A Solar System constraint that forces Earth (system ID 0) to expose exactly one wormhole.

### Dependencies & Usage

- **Used by:**
  - `GameDiscoveryState` (for validating system and wormhole IDs).
  - `WormholeGate` (to know which systems/gates are connected).
  - `ShipWormholeNavigator` (to find destinations).
  - `GalaxyMapUIManager` (to render system positions and connection lines).

- **Lifetime:**
  - Typically a singleton-like MonoBehaviour in a bootstrap scene or a persistent manager object.
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
    - Raised when systems/wormholes enter the discovered sets.

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator` for validation (ensure IDs are valid).

- **Used by:**
  - `ShipWormholeNavigator` (update state after a successful jump).
  - `GalaxyMapUIManager` (query discovered systems/wormholes, highlight current system).
  - Any UI widget showing current system name, exploration progress, etc.

- **Design Note:**
  - Other systems should treat `GameDiscoveryState` as the single authority for “what is known,” rather than altering flags directly on `GalaxyGenerator` data.

---

## World / Gameplay: `WormholeGate.cs`

### Responsibility

- Represents a **physical wormhole gate** in a scene:
  - Links a scene object (collider, VFX) to an abstract wormhole in `GalaxyGenerator`.
- Exposes data necessary for navigation:
  - Which system this gate belongs to.
  - Which wormhole (and thus which destination system) it corresponds to.

### Typical Fields

- `string systemId` – the star system this gate is in.
- `string wormholeId` – the logical wormhole link.
- Reference to `GalaxyGenerator` (assigned via inspector or lookup).

### Behaviour

- Provides information for the ship when it collides with, or enters, the gate:
  - E.g. `GetDestinationSystemId()` looks up the wormhole in `GalaxyGenerator` and returns the opposite end.
- Optionally exposes events:
  - `OnGateEntered`, `OnGateExited` for hooks such as VFX, sounds, or prompts.

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator` (for wormhole lookup via `TryGetWormhole` and system queries).

- **Used by:**
  - `ShipWormholeNavigator` during jump initiation.
  - Contextual UI (to show prompts like “Approaching Wormhole to [System]”).

---

## World / Gameplay: `ShipWormholeNavigator.cs`

### Responsibility

- Handles the **ship’s interaction with wormhole gates**:
  - Detecting proximity / entry via colliders.
  - Triggering the wormhole jump animation and teleport.
  - Updating game state and notifying UI.

### Key Behaviours

- **Proximity & Prompting**
  - Uses the wormhole’s collider as an “event horizon.”
  - When the ship enters a designated trigger:
    - Shows an on-screen prompt (via a UI canvas / TextMeshPro label) indicating the gate and destination.
  - The design was updated to avoid explicit key presses for jumping:
    - Jump begins automatically when the ship reaches the event horizon (e.g., inner trigger boundary).

- **Jump Execution**
  - Identifies the `WormholeGate` associated with the trigger.
  - Uses `GalaxyGenerator` to find the linked destination system and gate.
  - Moves the ship to the destination gate’s position.
  - Updates `GameDiscoveryState`:
    - `SetCurrentSystem(destinationSystemId)`
    - `DiscoverSystem(destinationSystemId)`
    - `DiscoverWormhole(wormholeId)`

- **UI Integrations**
  - Shows/hides contextual prompts (through serialized references to a prompt canvas and TextMeshPro text fields).
  - May notify other systems of a successful jump (events or direct calls).

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator`
  - `GameDiscoveryState`
  - `WormholeGate` components
  - UI objects (prompt canvas, TextMeshPro labels)

- **Used by:**
  - Player ship prefab (attached as a component).
  - VFX / SFX systems can subscribe to its events.

---

## UI / Map: `GalaxyMapUIManager.cs`

### Responsibility

- Renders and manages the **2D galaxy map**:
  - Displays discovered systems as points / icons.
  - Draws wormhole connections as lines between systems.
  - Highlights the current system.
- Provides hooks for zooming, panning, and potentially selecting systems.

### Core Behaviour

- **Rendering Systems**
  - Queries `GalaxyGenerator.Systems` and `GameDiscoveryState.discoveredSystemIds`.
  - Projects 3D positions into map space (e.g., 2D coordinates in a RectTransform).
  - Instantiates UI elements (icons, labels) for each discovered system.

- **Rendering Wormholes**
  - Queries discovered wormhole links.
  - Draws lines between system icons using:
    - UI line rendering (e.g., `LineRenderer` in world-space canvas), or
    - Procedural generation of `Image`/mesh segments.

- **Current System Highlight**
  - Highlights the icon of `GameDiscoveryState.currentSystemId`.
  - May update a “Current System” label using TextMeshPro.

- **Map Extents**
  - No longer depends on a single `galaxySize` field.
  - Either:
    - Infers map bounds from system positions (min/max of projected coordinates), or
    - Uses a fixed view area and allows zoom/pan to explore the network.
  - Designed to support scroll-wheel zoom and drag/pan navigation.

- **Discovery Updates**
  - Subscribes to `GameDiscoveryState` events (e.g., `DiscoveryChanged`, `CurrentSystemChanged`).
  - Rebuilds or incrementally updates UI when:
    - New systems/wormholes are discovered.
    - Current system changes after a wormhole jump.

### Dependencies & Usage

- **Depends on:**
  - `GalaxyGenerator`
  - `GameDiscoveryState`

- **Used by:**
  - `GalaxyMapToggle` (to show/hide the map canvas).
  - Any UI that needs to center on a specific system or wormhole.

---

## UI / Map: `GalaxyMapToggle.cs`

### Responsibility

- Controls **opening and closing the galaxy map UI** using Unity’s new Input System.

### Behaviour

- Holds serialized references:
  - `InputActionReference mapToggleAction` (or equivalent).
  - `GameObject mapRootCanvas` (the root UI object for the map).
- Subscribes to the input action’s performed callback:
  - Toggles `mapRootCanvas` active state.
  - Avoids direct use of `Input.GetKeyDown(KeyCode.M)`; input is configured in the project’s Input Actions asset.
- Optionally coordinates with `GalaxyMapUIManager` on open:
  - Ensures the map is refreshed when opened (e.g., current system highlight up to date).

### Dependencies & Usage

- **Depends on:**
  - Unity Input System (`InputActionReference`).
  - `GalaxyMapUIManager` (optional, via reference or `GetComponentInChildren`).

- **Used by:**
  - A UI/root manager object in the scene.
  - Can be reused in other scenes that need to expose a galaxy map.

---

## UI / Contextual Prompts

Contextual prompts around wormhole gates are currently handled via a combination of:

- Serialized references on `ShipWormholeNavigator` to:
  - A `Canvas` or UI panel.
  - A TextMeshPro label for prompt text (e.g., destination system name).
- Optional dedicated prompt components attached to UI prefabs (e.g., a `GatePromptLabel` script) that:
  - Expose a method like `ShowPrompt(string text)` / `HidePrompt()`.
  - Are instantiated or referenced by the ship controller.

High-level behaviour:

- On entering gate proximity:
  - `ShipWormholeNavigator` sets the label text (e.g., “Approaching Wormhole: [System Name]”).
  - Enables the prompt canvas or UI element.
- On completing the jump or leaving the gate:
  - Hides the prompt.

---

## Data Flow Summary

1. **Galaxy Generation**
   - `GalaxyGenerator` builds systems and wormhole links at startup.

2. **Initial State**
   - `GameDiscoveryState` is initialized with the starting system.
   - Player ship spawns in that system near a `WormholeGate`.

3. **Exploration & Jump**
   - Ship enters a gate’s collider.
   - `ShipWormholeNavigator`:
     - Reads gate data (`WormholeGate` → `wormholeId`, `systemId`).
     - Uses `GalaxyGenerator` to find the destination system and gate.
     - Shows prompt and then executes jump.
     - Updates `GameDiscoveryState` (current system + discovered sets).

4. **UI Updates**
   - `GameDiscoveryState` raises change events.
   - `GalaxyMapUIManager` re-renders or updates icons/lines and highlights the new current system.
   - Other UI components update labels (e.g., current system name, exploration statistics).

5. **Map Interaction**
   - Player uses the map toggle input action.
   - `GalaxyMapToggle` enables/disables the map canvas and ensures the view is up to date.

---

## Extension Points

Some natural extension points for future work:

- Add **events** on `GalaxyGenerator` if runtime modifications to the network are desired.
- Extend `GameDiscoveryState` with:
  - Per-system intel (e.g., last visited time, known stations, resource data).
  - Save/load support.
- Enhance `GalaxyMapUIManager` to:
  - Show fleets, stations, and missions in each system.
  - Allow selecting a system and issuing high-level fleet orders.
- Extend `ShipWormholeNavigator` to:
  - Support queued jumps and autopilot routes through multiple systems.
  - Integrate with a fleet-level navigation layer.

This document should be updated as new mechanics and scripts are introduced.
