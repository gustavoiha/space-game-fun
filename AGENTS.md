# AGENTS

Guidelines for OpenAI Codex and other automated agents working on this Unity project.

---

## 1. Purpose

Agents working in this repository must:

* Understand and respect the game’s fiction, tone, and long-term goals.
* Focus on code quality, maintainability, and readability.
* When adding or changing code, always assess if code respects best-practice principles.
* Follow Unity and C# best practices consistently.
* Keep documentation up to date with every meaningful change.
* Double-check work, even if that means taking extra steps.
* Update `UNITY_CONFIGURATION.md` whenever scene or project setup changes are required because of the agent’s work, and explicitly reference the relevant `BACKLOG.md` item (e.g., `M3.2`).

---

## 2. Project Context

This repository contains a retro-futuristic space exploration and reconstruction game set in the **Wormhole Era**:

* Stable wormholes, created by a failed alien black-hole experiment, connect many star systems.
* Earth was ruined by **The Shrouding** and is now a toxic graveyard world.
* The **Helios Protectorate** controls the Solar System with an authoritarian regime.
* The **Horizon Initiative** is a hopeful exploratory organisation that escaped through a wormhole with a small fleet.

Before making changes, agents must review and rely on the following files:

* `GAME_LORE.md` – Canonical game lore, factions, tone, and terminology.
* `BACKLOG.md` – Task list and prioritisation for features and technical work.
* `UNITY_CONFIGURATION.md` – Step-by-step scene and project setup instructions.
* `ARCHITECTURE.md` – High-level architecture, domains, layers, and namespaces.

Do not contradict the lore or architectural intent in these files. If a change requires updating them, do so as part of the same work.

---

## 3. Task Workflow and Status Tags

### 3.1 Status tags

`BACKLOG.md` uses three statuses:

* `[TODO]` – Not started.
* `[IN PROGRESS]` – Codex/agents have generated or modified scripts, assets, and documentation for the item, but a human has not yet implemented changes in Unity scenes/prefabs or play-tested.
* `[DONE]` – Human has implemented changes in Unity, tested/played the game, verified acceptance criteria, and filled the Tester feedback section.

Rules:

* Agents move items from `[TODO]` → `[IN PROGRESS]` when they perform work (code, assets, docs).
* Agents **must not** set items to `[DONE]`. Only human testers can move `[IN PROGRESS]` → `[DONE]` after Unity implementation and play-testing.
* Use the exact tag format shown above (`[IN PROGRESS]`, not `[IN_PROGRESS]`).

### 3.2 Tester feedback section

Each backlog item includes a **Tester feedback** section. It is for humans, after testing, to record:

* Notes on behaviour and feel.
* Issues found.
* Follow-up tasks needed (new backlog items).

Agents must:

* Leave the Tester feedback section intact and unfilled, unless copying over relevant notes from an existing description.
* When issues are raised by a human in Tester feedback, create new backlog items as needed, and link them back to the original item in the description.

### 3.3 Working from the backlog

When working in this repository, agents must:

1. **Work from `BACKLOG.md`**
   * Prefer tasks already described there.
   * If new work items are needed, add them under the appropriate milestone with:
     * Clear title.
     * Short description.
     * Initial status `[TODO]`.
     * Optional simple “Tester feedback” block if relevant.
   * Always preserve existing item IDs (e.g., `M3.2`) and references.

2. **Status updates**
   * On starting a task: update its status to `[IN PROGRESS]` and briefly summarise what you will do if helpful.
   * On finishing your code/docs/assets: leave status `[IN PROGRESS]` and ensure acceptance criteria and docs are updated. Do not mark `[DONE]`.

---

## 4. Architectural and DDD Guidelines

The project follows a domain-driven design (DDD) and layered architecture. Agents must respect these boundaries:

* **Domain layers (pure C#)**:
  * `Game.Domain.*` (e.g., `Game.Domain.Galaxy`, `Game.Domain.Systems`, `Game.Domain.Economy`, `Game.Domain.Factions`).
  * `Game.Simulation` for application services orchestrating domain behaviour (e.g., background simulation, transit resolution, incidents).
  * Domain and simulation code must not depend on Unity types (`MonoBehaviour`, `Transform`, `GameObject`, `ScriptableObject`, etc.) except for configuration ScriptableObjects and clearly isolated adapters.
* **Core & infrastructure**:
  * `Game.Core` (e.g., `GameBootstrapper`, `TimeController`, `GameEventBus`, `SaveLoadManager`, `SettingsManager`) provides infrastructure services used by domains.
* **Presentation layers (Unity/MonoBehaviour)**:
  * `Game.Presentation.*` for in-scene controllers, views, and MonoBehaviours.
  * `Game.Presentation.UI` for UI-specific logic.
  * Presentation code should adapt domain models and services to Unity scenes, not own game rules.
* **Player/meta**:
  * `Game.Player`, `Game.Meta` for player profile, discovery state, tutorials, and event logs.

Rules:

* Domain logic belongs in domain or simulation layers, not in UI or MonoBehaviours.
* MonoBehaviours should delegate to domain services/models rather than embed complex rules.
* Follow the namespaces and folder structure described in `ARCHITECTURE.md` and referenced by `BACKLOG.md` milestones.

---

## 5. Coding Standards and Best Practices (Unity / C#)

Agents must follow these guidelines for all Unity C# code:

### 5.1 Naming and Style

* Use **PascalCase** for:
  * Public classes, structs, interfaces.
  * Public methods and properties.
* Use **camelCase** for:
  * Local variables and parameters.
  * Private fields.
* Prefer `_fieldName` for private fields; use `[SerializeField]` for serialized private fields.
* Use **explicit access modifiers** (`public`, `private`, `protected`, `internal`).
* Group related fields and methods logically and keep classes small and focused.

### 5.2 Component and Script Design

* Apply **single responsibility**: each MonoBehaviour should have a clear primary purpose.
* Avoid centralised objects that do everything; prefer splitting responsibilities into smaller components.
* Avoid heavy logic inside `Update` when possible; use events, coroutines, or timers where appropriate.
* Do not allocate memory every frame inside `Update`; avoid LINQ and `new` allocations in hot paths.
* Prefer dependency injection and clear references over global singletons where feasible.
* Document every method, variable and class definition with a short summary when it is not self-explanatory.
* Do not load resources or reference hard-coded file paths in scripts. Instead, use serialized fields that humans assign in Unity.
* When adding or changing code, check whether redundant or overlapping code exists and refactor as needed.

### 5.3 Safety and Robustness

* Check for `null` where references may be missing, and fail fast with clear errors.
* Use assertions or error logs for invalid states that should never happen.
* Avoid magic numbers; use constants or configuration ScriptableObjects.
* Add XML summary comments to public classes and key public methods, briefly describing their role in the game.

### 5.4 Project Structure

* Place scripts in sensible folders that match their purpose and namespace (e.g., `Assets/Scripts/Game/Domain/Galaxy`, `Assets/Scripts/Game/Presentation/UI`).
* Keep namespaces consistent with folder structure.
* Ensure new scripts align with the domains and layers described in `ARCHITECTURE.md` and `BACKLOG.md`.

---

## 6. Documentation Requirements

Whenever an agent changes the code, scene, or project configuration, they must ensure the following files are up to date:

### 6.1 `GAME_LORE.md`

Update only when:

* New lore elements (factions, technologies, locations, terms) are added in code or content.
* Existing lore is expanded in a way that players or designers must understand.

Rules:

* Keep tone consistent with existing lore.
* Add new sections with clear headings and short descriptions.
* Do **not** casually rename existing factions, events, or key terms.

### 6.2 `BACKLOG.md`

Update whenever:

* You start working on a backlog item:
  * Set status to `[IN PROGRESS]`.
* You complete agent work on an item (scripts/assets/docs ready for testing):
  * Leave status as `[IN PROGRESS]`.
  * If behaviour has changed in a non-obvious way, add a brief note in the item’s description.
* You discover follow-up work:
  * Create new backlog entries, link them to the original task in the description.
* Do not change `[IN PROGRESS]` to `[DONE]`. That is done by a human tester only, after they test and fill the Tester feedback section.

### 6.3 `UNITY_CONFIGURATION.md`

This file must always reflect the actual Unity scene and project setup.

Update whenever your changes require **any** of the following:

* New scenes, or changes to which scenes should be opened for testing.
* New prefabs, tags, layers, physics settings, or project settings.
* New components added to existing GameObjects that are required for the game to run.
* Changes to required serialized fields or default values in the Inspector.

When updating `UNITY_CONFIGURATION.md`:

* Use clear, numbered step-by-step instructions (checklist format is preferred).
* Reference Unity menus and paths explicitly, for example:
  * `Unity > Project Settings > Physics`
  * `Hierarchy: Select "GameRoot" > Add Component > FleetManager`
* Specify:
  * **Scene name**
  * **GameObject name**
  * **Component type**
  * Any **required Inspector values**
* Explicitly reference the backlog item you are implementing (e.g., “This setup is required for `M3.2` – Player ship: ShipController, ShipMovement, ShipInputController”).

### 6.4 `ARCHITECTURE.md`

Update when:

* Introducing new domains, bounded contexts, or layers.
* Adding or significantly changing core services (e.g., simulation, routing, factions).
* Changing responsibilities of key classes defined there.

Keep descriptions short, focused on responsibilities, and aligned with DDD terminology.

---

## 7. File-Specific Notes

### 7.1 GAME_LORE.md

* Source of truth for names, factions, events, and narrative tone.
* When adding mechanics tied to lore (e.g., wormhole networks, factions, alien polities), ensure they are consistent with `GAME_LORE.md`.
* Keep sections short and structured; avoid lore bloat without gameplay reason.

### 7.2 BACKLOG.md

* Use to manage work for both humans and agents.
* Keep tasks small and understandable.
* Respect status semantics:
  * `[TODO]` – untouched.
  * `[IN PROGRESS]` – agent work done or ongoing; awaiting Unity implementation and testing.
  * `[DONE]` – human-confirmed; acceptance criteria met; Tester feedback filled.
* When a task is completed by a human, they should:
  * Set status to `[DONE]`.
  * Fill in the “Tester feedback” section with notes, issues, and follow-up items.

### 7.3 UNITY_CONFIGURATION.md

* Treat this as the reproducible setup recipe for the project.
* If a human cannot set up the project and scene by following this document, treat that as a bug.
* Every change to scenes, prefabs, or project setup driven by a backlog item must be reflected here and mention that item explicitly.

---

## 8. Agent Workflow Checklist (TL;DR)

Before starting:

1. Read or refresh:
   * `GAME_LORE.md`
   * `ARCHITECTURE.md`
   * `BACKLOG.md`
   * `UNITY_CONFIGURATION.md`
2. Pick a task in `BACKLOG.md` (status `[TODO]`).

While working:

3. Change task status to `[IN PROGRESS]`.
4. Implement changes with clean, readable Unity C# code respecting DDD layering.
5. Keep diffs focused and avoid unnecessary format-only changes.
6. If you change scenes or project settings, update `UNITY_CONFIGURATION.md` with explicit steps and a reference to the backlog item.

Before finishing agent work:

7. Ensure the project compiles (to the extent visible from the repository) with no obvious new errors.
8. Update:
   * `BACKLOG.md` for task status (`[IN PROGRESS]`) and any notes.
   * `UNITY_CONFIGURATION.md` for setup/scene changes, with backlog item reference.
   * `ARCHITECTURE.md` and/or `GAME_LORE.md` if new architecture or lore has been introduced.
9. If you had to make assumptions or leave open questions, note them clearly in:
   * Code comments, and/or
   * The relevant backlog item (e.g., “Open questions / follow-up”).

Human testers are responsible for:

* Implementing changes in Unity scenes/prefabs as described in `UNITY_CONFIGURATION.md`.
* Testing the game in Play Mode.
* Filling in the Tester feedback section of the backlog item.
* Moving the item from `[IN PROGRESS]` to `[DONE]` once all acceptance criteria are satisfied.
