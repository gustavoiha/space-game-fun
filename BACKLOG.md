# BACKLOG

Status tags:
- `[TODO]` = Not started.
- `[IN PROGRESS]` = Codex has generated/updated scripts, assets, and documentation, but a human has not yet implemented changes in Unity scenes/prefabs or play-tested.
- `[DONE]` = Changes are implemented in Unity, game has been tested/played by a human, and acceptance criteria are met.

For each task:
- Codex updates the status from `[TODO]` to `[IN PROGRESS]` when code/assets/docs are generated.
- Human updates from `[IN PROGRESS]` to `[DONE]` after implementing in Unity + testing and filling the “Tester feedback” section.

---

## Milestone 1 – Project & Architecture Foundation

### M1.1 [TODO] Core folder structure, namespaces, and assemblies

**Description**
Create the base folder structure, namespaces, and assembly definitions for the project, aligned with the domain-driven architecture (Core, Domain, Simulation, Presentation/UI, Player).

**Scripts/assets to create/modify**
- New: `Game.Core` assembly definition and folders.
- New: `Game.Domain.Galaxy`, `Game.Domain.Systems`, `Game.Domain.Economy`, `Game.Domain.Factions`, `Game.Simulation`, `Game.Presentation`, `Game.Presentation.UI`, `Game.Player` assemblies/folders.
- Update: `ARCHITECTURE.md` to reflect namespace and folder layout.

**Acceptance criteria**
- Folder structure in Unity mirrors intended namespaces (e.g., `Scripts/Game/Domain/Galaxy`, etc.).
- Assembly definition files enforce directional dependencies (Domain does not depend on Presentation).
- `ARCHITECTURE.md` is updated with a brief description of each namespace and its responsibilities.
- If any scenes, prefabs, or hierarchy are created/modified, `UNITY_CONFIGURATION.md` is updated with step-by-step instructions and an explicit reference to `M1.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed (new backlog items):

---

### M1.2 [TODO] GameBootstrapper and core services wiring

**Description**
Implement a `GameBootstrapper` that initializes core services (event bus, time controller, settings, and save/load stub) before gameplay scenes load.

**Scripts/assets to create/modify**
- New (Core): `GameBootstrapper` (MonoBehaviour in a bootstrap scene).
- New (Core): `GameEventBus`.
- New (Core): `TimeController`.
- New (Core): `SettingsManager`.
- New (Core): `SaveLoadManager` (skeleton only).
- Update: `ARCHITECTURE.md` to document service initialization and lifetime.

**Acceptance criteria**
- “Bootstrap” scene exists and contains `GameBootstrapper`.
- On play, core services (`GameEventBus`, `TimeController`, `SettingsManager`, `SaveLoadManager`) are initialized exactly once and available to other systems.
- Core services contain no UI/scene-specific logic.
- `UNITY_CONFIGURATION.md` documents Bootstrap scene setup (GameObject hosting `GameBootstrapper`, load order) and references `M1.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M1.3 [TODO] SceneController and basic scene flow

**Description**
Implement scene flow between Main Menu, Galaxy, and Star System scenes through a `SceneController`.

**Scripts/assets to create/modify**
- New (Core/Presentation): `SceneController`.
- New: Minimal `MainMenu` scene.
- New: Placeholder `Galaxy` scene.
- New: Placeholder `StarSystem` scene.
- Update: `ARCHITECTURE.md` describing flow: Bootstrap → Main Menu → Galaxy → Star System.

**Acceptance criteria**
- From Bootstrap, game loads Main Menu, then Galaxy, then Star System via `SceneController` methods (e.g., `EnterGalaxy()`, `EnterStarSystem(systemId)`).
- Asynchronous scene loading with at least simple loading feedback (UI or logs).
- `SceneController` only orchestrates scenes (no game rule logic).
- `UNITY_CONFIGURATION.md` documents Main Menu, Galaxy, and Star System scenes, their expected components, and how they link via `SceneController`, referencing `M1.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M1.4 [TODO] TimeController game time model

**Description**
Implement `TimeController` to maintain a canonical game time and expose pause/fast-forward.

**Scripts/assets to create/modify**
- Update (Core): `TimeController` (full implementation).
- New (Core): Interfaces/events for time tick subscription.

**Acceptance criteria**
- Game time tracked in clear units and exposed via `TimeController`.
- Systems can subscribe to regular ticks without using `Update` directly (e.g., events).
- Pause and time-scale changes affect all time subscribers consistently.
- `TimeController` has no UI or scene-specific dependencies.
- If any new GameObjects or scene setup are required, `UNITY_CONFIGURATION.md` is updated and references `M1.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M1.5 [TODO] SaveLoadManager skeleton and data model hooks

**Description**
Create the initial `SaveLoadManager` structure and define interfaces for serializable game state without full serialization yet.

**Scripts/assets to create/modify**
- Update (Core): `SaveLoadManager` with `SaveGame(slotId)` and `LoadGame(slotId)` stubs.
- New (Domain): simple interfaces such as `ISerializableGameState`, `IGameStateProvider`.
- Update: `ARCHITECTURE.md` with persistence boundaries.

**Acceptance criteria**
- `SaveLoadManager` exposes clear APIs to save/load a root game state object.
- Stubs can log and hold mock state in memory without file IO.
- Domain state objects (e.g., `GalaxyState`) are designed to be serializable (no scene refs).
- `UNITY_CONFIGURATION.md` updated if any host GameObject/scene setup is required, referencing `M1.5`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 2 – Galaxy Layer: Wormhole Network & System Manifests

### M2.1 [TODO] Galaxy data definitions (systems, wormholes, factions, ships, stations, commodities)

**Description**
Define ScriptableObject-based data for star systems, wormholes, factions, ship classes, stations, and commodities.

**Scripts/assets to create/modify**
- New (Domain/Galaxy): `StarSystemDefinition` (SO).
- New (Domain/Galaxy): `WormholeDefinition` (SO/struct).
- New (Domain/Factions): `FactionDefinition` (SO).
- New (Domain/Systems): `StationDefinition` (SO).
- New (Domain/Ships): `ShipClassDefinition` (SO).
- New (Domain/Economy): `CommodityDefinition` (SO).

**Acceptance criteria**
- Each definition contains only static data (no runtime or scene references).
- Sample set exists (2–3 systems, 1–2 factions, a few stations/ships/commodities).
- `ARCHITECTURE.md` documents definitions as domain data and shows how they are used.
- `UNITY_CONFIGURATION.md` documents where these ScriptableObjects live and how to create/edit them, referencing `M2.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M2.2 [TODO] GalaxyState and wormhole routing (RoutePlanner)

**Description**
Implement `GalaxyState` as the runtime representation of the galaxy and `RoutePlanner` for pathfinding over the wormhole network.

**Scripts/assets to create/modify**
- New (Domain/Galaxy): `GalaxyState`.
- New (Domain/Galaxy): `RoutePlanner`.

**Acceptance criteria**
- `GalaxyState` holds systems, wormholes, and runtime metadata.
- `RoutePlanner` computes routes between systems via wormholes (at least shortest path).
- No Unity types (`MonoBehaviour`, `Transform`, etc.) appear in these domain classes.
- If a scene/adapter is introduced to host `GalaxyState`, `UNITY_CONFIGURATION.md` documents it and references `M2.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M2.3 [TODO] GalaxyMapController, system nodes, and wormhole link views

**Description**
Implement the galaxy map visualization: nodes for systems, links for wormholes, and camera controls.

**Scripts/assets to create/modify**
- New (Presentation/Galaxy): `GalaxyMapController`.
- New (Presentation/Galaxy): `GalaxySystemNodeView`.
- New (Presentation/Galaxy): `GalaxyWormholeLinkView`.
- New/Update: galaxy map prefabs.

**Acceptance criteria**
- Galaxy scene instantiates node and link views based on `GalaxyState`/definitions.
- Player can pan/zoom and select systems/wormholes; selection events are exposed to UI.
- View classes are “dumb” (render + events, no game rules).
- `UNITY_CONFIGURATION.md` documents Galaxy scene hierarchy (map root, node/link prefabs, camera setup), referencing `M2.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M2.4 [TODO] SystemManifest and SystemManifestManager

**Description**
Implement `SystemManifest` as “last-known” information per system and `SystemManifestManager` to update them when systems are visited or reports arrive.

**Scripts/assets to create/modify**
- New (Domain/Galaxy): `SystemManifest`.
- New (Domain/Galaxy): `SystemManifestManager`.

**Acceptance criteria**
- `SystemManifest` holds system ID, summary of stations/fleets, last update time, and staleness info.
- `SystemManifestManager` can get/update manifests from system snapshots and report staleness.
- Manifests are view models, not authoritative simulation state.
- Any scene components added to show manifests are documented in `UNITY_CONFIGURATION.md` with reference to `M2.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M2.5 [TODO] GalaxyFleetRecord for inter-system fleets

**Description**
Create `GalaxyFleetRecord` to represent fleets in transit between systems and integrate with `GalaxyState`.

**Scripts/assets to create/modify**
- New (Domain/Galaxy): `GalaxyFleetRecord`.
- Update (Domain/Galaxy): `GalaxyState` to manage a collection of `GalaxyFleetRecord`.

**Acceptance criteria**
- `GalaxyFleetRecord` holds ID, faction, origin, destination, departure time, ETA, and composition summary.
- `GalaxyState` can add/remove/query fleets in transit.
- Domain classes avoid Unity types.
- If any Galaxy-level visualization is added, `UNITY_CONFIGURATION.md` documents the setup and references `M2.5`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 3 – In-System Flight & Local Simulation (Single System)

### M3.1 [TODO] StarSystemController and SystemSimulationState

**Description**
Implement `StarSystemController` to own the active star system scene and `SystemSimulationState` as its runtime data model.

**Scripts/assets to create/modify**
- New (Domain/Systems): `SystemSimulationState`.
- New (Presentation/Systems): `StarSystemController`.
- New/Update: star system scene prefab structure.

**Acceptance criteria**
- `StarSystemController` creates in-scene objects (stations, ships, gates) from `SystemSimulationState` + definitions.
- `SystemSimulationState` holds system entities but no scene references.
- Transition from Galaxy → Star System loads the correct system and initializes `SystemSimulationState`.
- Star System scene hierarchy and components are explained in `UNITY_CONFIGURATION.md`, referencing `M3.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M3.2 [TODO] Player ship: ShipController, ShipMovement, ShipInputController

**Description**
Implement basic player-controlled ship: state machine, physics-based movement using gravity/ion drives, and input handling.

**Scripts/assets to create/modify**
- New (Domain/Ships): `ShipRuntimeData` (optional).
- New (Presentation/Ships): `ShipController`.
- New (Presentation/Ships): `ShipMovement`.
- New (Presentation/Ships): `ShipInputController`.
- New/Update: player ship prefab.

**Acceptance criteria**
- Player ship can thrust, rotate, and coast smoothly in a system under gravity influence.
- `ShipController` manages ship state; `ShipMovement` handles physics; `ShipInputController` maps input.
- Runtime stats are data-driven from definitions or `ShipRuntimeData`.
- `UNITY_CONFIGURATION.md` documents the ship prefab, required components, and scene wiring, referencing `M3.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M3.3 [TODO] StationController and DockingController

**Description**
Add basic stations to the system and allow the player ship to dock/undock with simple rules.

**Scripts/assets to create/modify**
- New (Presentation/Stations): `StationController`.
- New (Presentation/Stations): `DockingController`.
- New/Update: station prefabs and docking colliders.

**Acceptance criteria**
- Stations can be placed from definitions or initial state.
- Player can request docking in range under basic conditions (speed/angle where applicable).
- Docking transitions ship to docked state (movement disabled) and undocking returns control.
- Docking logic is encapsulated in `DockingController`.
- `UNITY_CONFIGURATION.md` documents station prefabs, docking triggers, and usage, referencing `M3.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M3.4 [TODO] WormholeGateController stub and entry/exit mechanics

**Description**
Represent in-system wormhole gates and basic activation behavior (no full cross-system yet).

**Scripts/assets to create/modify**
- New (Presentation/Systems): `WormholeGateController`.
- New/Update: wormhole gate prefab.

**Acceptance criteria**
- Wormhole gates exist in Star System scenes and detect ship entry into activation area.
- On activation, gates trigger callbacks/logs signaling intended travel (integration with transit later).
- Clear interface exists for integration with `GalaxyState`/`TransitResolver`.
- `UNITY_CONFIGURATION.md` documents gate prefabs, colliders, and placement, referencing `M3.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M3.5 [TODO] Basic in-system HUD (HUDController, ShipStatusUI, TargetingUI)

**Description**
Implement a minimal in-system HUD for the player ship.

**Scripts/assets to create/modify**
- New (Presentation/UI): `HUDController`.
- New (Presentation/UI): `ShipStatusUI`.
- New (Presentation/UI): `TargetingUI`.
- New/Update: HUD canvas and UI prefabs.

**Acceptance criteria**
- HUD shows ship speed, hull, and basic drive status.
- Selecting a station/ship shows name and distance.
- HUD uses events/data-binding, not direct domain logic.
- `UNITY_CONFIGURATION.md` documents the HUD canvas setup and references `M3.5`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 4 – Multi-System Architecture & Background Simulation

### M4.1 [TODO] Scene flow integration between Galaxy and Star Systems

**Description**
Wire `SceneController`, `GalaxyState`, and `StarSystemController` to support entering/leaving star systems.

**Scripts/assets to create/modify**
- Update (Core/Presentation): `SceneController`.
- Update (Domain/Galaxy): `GalaxyState`.
- Update (Presentation/Systems): `StarSystemController`.

**Acceptance criteria**
- Selecting a system on the galaxy map and choosing “Enter System” loads the appropriate star system.
- Returning to galaxy persists modifications back into `GalaxyState`.
- Flow is clean; no cross-layer coupling beyond identifiers/state transfer.
- `UNITY_CONFIGURATION.md` documents navigation flow and scene hooks, referencing `M4.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M4.2 [TODO] BackgroundSimulationManager and RemoteSystemSnapshot

**Description**
Implement simplified simulation for non-active systems via `BackgroundSimulationManager` and `RemoteSystemSnapshot`.

**Scripts/assets to create/modify**
- New (Simulation): `BackgroundSimulationManager`.
- New (Domain/Systems): `RemoteSystemSnapshot`.

**Acceptance criteria**
- Non-active systems are represented by `RemoteSystemSnapshot` with aggregate state only.
- `BackgroundSimulationManager` updates snapshots periodically based on `TimeController`.
- Background rules are simplified but consistent with “full” simulation.
- If a scene object hosts `BackgroundSimulationManager`, `UNITY_CONFIGURATION.md` documents it and references `M4.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M4.3 [TODO] TransitResolver and wormhole travel integration

**Description**
Connect wormhole gates, `GalaxyFleetRecord`, and system entry/exit via `TransitResolver`.

**Scripts/assets to create/modify**
- New (Simulation): `TransitResolver`.
- Update (Domain/Galaxy): `GalaxyState` to use `TransitResolver`.
- Update (Presentation/Systems): `WormholeGateController` to send ships into transit.

**Acceptance criteria**
- Entering a wormhole gate removes ship/fleet from local `SystemSimulationState` and adds a `GalaxyFleetRecord` in transit.
- `TransitResolver` moves fleets from “in transit” to arrival system based on game time.
- Arrival integrates with destination `SystemSimulationState` and triggers events.
- `UNITY_CONFIGURATION.md` documents any scene wiring needed for transitions, referencing `M4.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M4.4 [TODO] PlayerGalaxyState and discovery

**Description**
Track player-specific knowledge of systems and wormholes.

**Scripts/assets to create/modify**
- New (Player): `PlayerGalaxyState`.

**Acceptance criteria**
- `PlayerGalaxyState` tracks discovered systems/wormholes and last visit times.
- Galaxy UI uses `PlayerGalaxyState` to distinguish known/unknown systems.
- `PlayerGalaxyState` is serializable and Unity-free.
- If scene components are required for initialization, `UNITY_CONFIGURATION.md` documents them and references `M4.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 5 – Economy & Logistics Core

### M5.1 [TODO] Commodities and CargoHold

**Description**
Implement `CargoHold` to store `CommodityDefinition` instances on ships and stations.

**Scripts/assets to create/modify**
- New (Domain/Economy): `CargoHold`.
- Update: ship/station runtime data to include cargo.

**Acceptance criteria**
- `CargoHold` supports add/remove/query with capacity enforcement.
- Ships/stations can hold commodities via `CargoHold`.
- No Unity engine types in `CargoHold`.
- If ship/station prefabs require new components, `UNITY_CONFIGURATION.md` documents changes, referencing `M5.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M5.2 [TODO] StationProductionModule and PopulationModule

**Description**
Add simple economic production and population modules to stations.

**Scripts/assets to create/modify**
- New (Domain/Economy): `StationProductionModule`.
- New (Domain/Economy): `PopulationModule`.
- Update (Domain/Systems): station runtime model.

**Acceptance criteria**
- `StationProductionModule` consumes inputs and produces outputs over time.
- `PopulationModule` tracks population and basic consumption of key commodities.
- Modules are domain-only; any Unity integration happens through adapters.
- `UNITY_CONFIGURATION.md` updated if station prefabs gain new components for these modules, referencing `M5.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M5.3 [TODO] FleetController, FleetOrder, TransportOrder

**Description**
Implement fleets as collections of ships with orders, focusing on `TransportOrder`.

**Scripts/assets to create/modify**
- New (Domain/Fleets): `FleetOrder` (base/interface).
- New (Domain/Fleets): `TransportOrder`.
- New (Presentation/Fleets): `FleetController`.

**Acceptance criteria**
- `TransportOrder` defines origin, destination, and cargo plan.
- `FleetController` executes `TransportOrder`: load cargo, travel, unload.
- Fleets integrate with `GalaxyFleetRecord` and `SystemSimulationState`.
- Any new fleet-related prefabs/scenes are documented in `UNITY_CONFIGURATION.md`, referencing `M5.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M5.4 [TODO] LogisticsPlanner basic suggestions

**Description**
Introduce `LogisticsPlanner` to propose `TransportOrder`s based on surpluses/deficits.

**Scripts/assets to create/modify**
- New (Domain/Economy): `LogisticsPlanner`.

**Acceptance criteria**
- `LogisticsPlanner` scans station inventories and suggests valid `TransportOrder`s.
- Logic is domain-only; UI/AI consume its output via clear APIs.
- `UNITY_CONFIGURATION.md` updated only if a scene adapter/debug tool is created, referencing `M5.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 6 – Factions, AI, and Events

### M6.1 [TODO] FactionManager and FactionController

**Description**
Implement faction runtime control and registry to manage ownership and relationships.

**Scripts/assets to create/modify**
- New (Domain/Factions): `FactionController`.
- New (Domain/Factions): `FactionManager`.

**Acceptance criteria**
- `FactionManager` registers factions and manages relations (neutral/hostile/allied).
- `FactionController` tracks assets (systems, stations, fleets).
- Domain-only; no UI/scene logic.
- If scene components are used to configure initial factions, `UNITY_CONFIGURATION.md` documents them, referencing `M6.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M6.2 [TODO] FactionAIController basic strategic behavior

**Description**
Add simple faction AI that uses `LogisticsPlanner` and `RoutePlanner` to generate trade fleets and patrols.

**Scripts/assets to create/modify**
- New (Simulation/AI): `FactionAIController`.
- Update (Domain/Factions): `FactionManager` (AI references).

**Acceptance criteria**
- AI periodically evaluates economic state and creates `TransportOrder`s for its stations.
- AI uses `RoutePlanner` to avoid nonsensical paths.
- AI works on domain state; scene integration done via controlled adapters.
- Any persistent AI host objects in scenes are documented in `UNITY_CONFIGURATION.md`, referencing `M6.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M6.3 [TODO] IncidentGenerator and “missing ship” events

**Description**
Implement incident generation for pirate attacks and “missing ship” outcomes.

**Scripts/assets to create/modify**
- New (Simulation): `IncidentGenerator`.
- Update (Simulation): integrate with `TransitResolver`.

**Acceptance criteria**
- `IncidentGenerator` applies probabilistic outcomes (safe, damaged, missing) based on route risk and faction presence.
- Missing fleets update their orders and `GalaxyFleetRecord`, and raise events on `GameEventBus`.
- Incident logic is domain-driven and time-based.
- Any scene-level debugging/visualization is documented in `UNITY_CONFIGURATION.md`, referencing `M6.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M6.4 [TODO] EventLogManager and base notifications model

**Description**
Create `EventLogManager` to store notable events and a simple notification model.

**Scripts/assets to create/modify**
- New (Domain/Meta): `EventLogManager`.
- New (Domain/Meta): `GameEventRecord` / `NotificationRecord`.

**Acceptance criteria**
- Significant events (fleet missing/arrived, system visited, new wormhole discovered) are recorded.
- Event records are serializable and UI-agnostic.
- Consumers can query or subscribe for new events.
- If scene components consume/display logs, `UNITY_CONFIGURATION.md` documents them and references `M6.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 7 – UI/UX & Game Loop Cohesion

### M7.1 [TODO] GalaxyMapUI and SystemManifestUI integration

**Description**
Connect galaxy map visuals with system manifests to show last-known information and staleness.

**Scripts/assets to create/modify**
- New (Presentation/UI): `GalaxyMapUI`.
- New (Presentation/UI): `SystemManifestUI`.
- Update (Presentation/Galaxy): `GalaxyMapController` (selection events).

**Acceptance criteria**
- Selecting a system on the galaxy map displays its `SystemManifest`.
- Unknown systems are clearly distinguished from known ones.
- UI reads from `SystemManifestManager` and `PlayerGalaxyState` via a clean facade.
- `UNITY_CONFIGURATION.md` documents the UI canvas and wiring for galaxy + manifest panels, referencing `M7.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M7.2 [TODO] OrdersUI for creating transport orders

**Description**
Allow the player to create `TransportOrder`s via UI, using `RoutePlanner` for paths.

**Scripts/assets to create/modify**
- New (Presentation/UI): `OrdersUI`.
- Update (Domain/Fleets): `TransportOrder` if needed for UI labeling.
- New/Update: UI prefabs for order creation.

**Acceptance criteria**
- Player can select origin/destination stations, choose commodities/amounts, and create a `TransportOrder`.
- Created orders are integrated into domain flow and executed by fleets.
- UI prevents obviously invalid orders (no ships, no cargo, etc.).
- `UNITY_CONFIGURATION.md` documents the Orders UI layout and wiring, referencing `M7.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M7.3 [TODO] NotificationsUI and “transport ship is missing” UX

**Description**
Display notifications for important events, focusing on “transport ship is missing”.

**Scripts/assets to create/modify**
- New (Presentation/UI): `NotificationsUI`.
- Update (Domain/Meta): `EventLogManager` if needed.

**Acceptance criteria**
- When a fleet is marked missing by `IncidentGenerator`, a notification appears in the UI.
- Notifications show key details (fleet/order, origin, destination, time).
- Notifications UI consumes `EventLogManager` via a clear interface.
- `UNITY_CONFIGURATION.md` documents Notifications UI and references `M7.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M7.4 [TODO] PauseMenuUI and SettingsManager integration

**Description**
Implement a pause menu that integrates with `SettingsManager`.

**Scripts/assets to create/modify**
- New (Presentation/UI): `PauseMenuUI`.
- Update (Core): `SettingsManager`.
- New/Update: Pause menu UI canvas.

**Acceptance criteria**
- Player can pause/unpause, adjust basic settings, and return to main menu.
- `SettingsManager` stores/applies settings; `PauseMenuUI` only reads/writes via it.
- Game time pause integrates with `TimeController`.
- `UNITY_CONFIGURATION.md` documents pause menu UI and scene wiring, referencing `M7.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M7.5 [TODO] TutorialManager skeleton and basic onboarding

**Description**
Add a `TutorialManager` for simple onboarding steps (flight, docking, creating first transport order).

**Scripts/assets to create/modify**
- New (Player/Meta): `TutorialManager`.
- New/Update: tutorial UI hints/panels.

**Acceptance criteria**
- `TutorialManager` tracks completion flags for basic tutorials.
- It listens to domain/UI events and triggers contextual hints.
- Tutorial logic is separate from domain and UI control logic.
- `UNITY_CONFIGURATION.md` documents tutorial UI and wiring, referencing `M7.5`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

## Milestone 8 – Persistence, Balancing & Content Pass

### M8.1 [TODO] Full save/load implementation

**Description**
Complete `SaveLoadManager` to fully serialize/deserialize galaxy, systems, fleets, orders, player state, and events.

**Scripts/assets to create/modify**
- Update (Core): `SaveLoadManager`.
- Update (Domain): state objects (`GalaxyState`, `SystemSimulationState`, `PlayerGalaxyState`, etc.).
- Update: serialization models/versioning where needed.

**Acceptance criteria**
- Save/load restores a consistent game state from both galaxy and system views.
- No scene references are serialized directly; only IDs and data.
- Save format is robust to incremental schema changes (e.g., version field).
- `UNITY_CONFIGURATION.md` documents any UI or scene changes for save/load menus, referencing `M8.1`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M8.2 [TODO] Initial wormhole cluster content authoring

**Description**
Author a small yet coherent cluster of systems, wormholes, stations, and factions as a playable slice.

**Scripts/assets to create/modify**
- Update (Domain/Galaxy): `StarSystemDefinition` / `WormholeDefinition` assets.
- Update (Domain/Factions): `FactionDefinition` assets.
- Update (Domain/Systems): `StationDefinition` assets.

**Acceptance criteria**
- 4–6 interconnected systems with clear routes and risk profiles.
- At least two major factions (Horizon, Helios) and a pirate presence defined and integrated.
- Systems have stations with basic production and population parameters.
- `UNITY_CONFIGURATION.md` documents any specific scene/prefab configuration for this content, referencing `M8.2`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M8.3 [TODO] Travel, risk, and economy balancing pass

**Description**
Perform an initial balancing pass on travel times, incident rates, and basic economic relationships.

**Scripts/assets to create/modify**
- Update (Domain/Galaxy): risk/time in `WormholeDefinition`.
- Update (Domain/Economy): production recipes and resource demands.
- Update (Simulation): `IncidentGenerator` parameters.

**Acceptance criteria**
- Travel times feel meaningful but not tedious; risks produce occasional, not constant, losses.
- Core trade routes are economically viable and understandable.
- Balancing parameters are centralized and documented for future tuning.
- If debug tools or scene elements are added for balancing, `UNITY_CONFIGURATION.md` documents them, referencing `M8.3`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:

---

### M8.4 [TODO] Architecture cleanup and documentation alignment

**Description**
Final pass to ensure codebase matches the DDD architecture and documentation.

**Scripts/assets to create/modify**
- Update: various scripts to fix layering violations, naming, and structure.
- Update: `ARCHITECTURE.md`, `BACKLOG.md`, `AGENTS.md`, `UNITY_CONFIGURATION.md`.

**Acceptance criteria**
- No Presentation/UI namespaces own domain logic; domain remains Unity-free.
- Cross-layer dependencies follow the intended direction (Core/Domain → Simulation → Presentation).
- Documentation matches current architecture and key script responsibilities.
- `UNITY_CONFIGURATION.md` is fully up to date with scene/prefab/hierarchy expectations and references `M8.4`.

**Tester feedback (human to fill after testing)**
- Notes:
- Issues found:
- Follow-up tasks needed:
