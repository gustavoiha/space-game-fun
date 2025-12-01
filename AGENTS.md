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
* Update `UNITY_CONFIGURATION.md` whenever scene or project setup changes are required because of the agent’s work.

---

## 2. Project Context

This repository contains a retro-futuristic space exploration and reconstruction game set in the **Wormhole Era**:

* Stable wormholes, created by a failed alien black-hole experiment, connect many star systems.
* Earth was ruined by **The Shrouding** and is now a toxic graveyard world.
* The **Helios Protectorate** controls the Solar System with an authoritarian regime.
* The **Horizon Initiative** is a hopeful exploratory organisation that escaped through a wormhole with a small fleet.

Before making changes, agents must review and rely on the following files:

* `LORE.md` – Canonical game lore, factions, tone, and terminology.
* `ARCHITECTURE.md` – High-level code and system architecture description.
* `BACKLOG.md` – Task list and prioritisation for features and technical work.
* `UNITY_CONFIGURATION.md` – Step-by-step scene and project setup instructions.

Do not contradict the lore or architectural intent in these files. If a change requires updating them, do so as part of the same work.

---

## 3. General Rules for Agents

When working in this repository, agents must:

1. **Work from the backlog**

   * Prefer tasks described in `BACKLOG.md`.
   * If you create new work items, add them under the appropriate section with clear titles and short descriptions.

2. **Minimise unnecessary changes**

   * Keep diffs focused on the relevant task.
   * Avoid unrelated refactors unless they are clearly small, safe, and clearly improve quality.

3. **Prefer clarity over cleverness**

   * Write code that is easy for human developers to understand.
   * Avoid overcomplicated patterns if simpler solutions are sufficient.

4. **Preserve behaviour unless the task explicitly states otherwise**

   * If behaviour changes, document it in code comments and relevant documentation files.

5. **No silent assumptions**

   * If you must make an assumption, encode it as a small comment in the code or in `BACKLOG.md` as a follow-up item.

---

## 4. Coding Standards and Best Practices (Unity / C#)

Agents must follow these guidelines for all Unity C# code:

### 4.1 Naming and Style

* Use **PascalCase** for:

  * Public classes, structs, interfaces.
  * Public methods and properties.
* Use **camelCase** for:

  * Local variables and parameters.
  * Private fields.
* Prefer `_fieldName` for private fields; use `[SerializeField]` for serialized private fields.
* Use **explicit access modifiers** (`public`, `private`, `protected`, `internal`).
* Group related fields and methods logically and keep classes small and focused.

### 4.2 Component and Script Design

* Apply **single responsibility**: each MonoBehaviour should have a clear primary purpose.
* Avoid centralised objects that do everything; prefer splitting responsibilities into smaller components.
* Avoid heavy logic inside `Update` when possible; use events, coroutines, or timers where appropriate.
* Do not allocate memory every frame inside `Update`; avoid LINQ and `new` allocations in hot paths.
* Prefer dependency injection and clear references over global singletons where feasible.
* Document every method, variable and class definition
* When adding new or changing existing code, verify if redundant code is introduced. Take time to analyze and refactor other parts of the codebase if necessary.

### 4.3 Safety and Robustness

* Check for `null` where references may be missing, and fail fast with clear errors.
* Use assertions or error logs for invalid states that should never happen.
* Avoid magic numbers; use constants or configuration ScriptableObjects.
* Add XML summary comments to public classes and key public methods, briefly describing their role in the game.

### 4.4 Project Structure

* Place scripts in sensible folders that match their purpose (e.g., `Scripts/Systems/`, `Scripts/UI/`, `Scripts/Ships/`).
* Keep namespaces consistent with folder structure when namespaces are used.
* When adding a new system, ensure it is reflected in `ARCHITECTURE.md` and that folders are logically named.

---

## 5. Documentation Requirements

Whenever an agent changes the code, scene, or project configuration, they must ensure the following files are up to date:

### 5.1 `LORE.md`

Update only when:

* New lore elements (factions, technologies, locations, terms) are added in code or content.
* Existing lore is expanded in a way that players or designers must understand.

Rules:

* Keep tone consistent with existing lore.
* Add new sections with clear headings and short descriptions.
* Do **not** casually rename existing factions, events, or key terms.

### 5.2 `ARCHITECTURE.md`

Update whenever:

* New systems, managers, or significant subsystems are introduced.
* Existing systems change responsibilities or relationships.
* Important scripts are added, renamed, or removed.

Include:

* A short description of the new/changed system.
* Where the scripts live in the folder hierarchy.
* How the system interacts with other systems (e.g., input, UI, save/load, fleets).

### 5.3 `BACKLOG.md`

Update whenever:

* You start working on a backlog item (mark as in-progress if relevant).
* You complete an item (mark as done and add any notes if behaviour changed).
* You discover follow-up work (create new backlog entries and link them to the original task when useful).

Keep entries concise and actionable.

### 5.4 `UNITY_CONFIGURATION.md`

This file must always reflect the actual Unity scene and project setup.

Update whenever your changes require **any** of the following:

* New scenes, or changes to which scenes should be opened for testing.
* New prefabs, tags, layers, physics settings, or project settings.
* New components added to existing GameObjects that are required for the game to run.
* Changes to required serialized fields or default values in the Inspector.

When updating `UNITY_CONFIGURATION.md`:

* Use clear, numbered step-by-step instructions (checklist format is preferred).
* Reference Unity menus and paths explicitly, for example:

  * `Unity > Project Settings > Physics` or
  * `Hierarchy: Select "GameRoot" > Add Component > FleetManager`.
* Specify:

  * **Scene name**
  * **GameObject name**
  * **Component type**
  * Any **required Inspector values**

Agents must ensure that a human following `UNITY_CONFIGURATION.md` from a clean project can successfully reproduce the working scene.

---

## 6. Unity Scene and Project Changes: Required Process

Whenever you modify or introduce any Unity scene or project configuration:

1. Apply changes in the Unity Editor.
2. Save all relevant scenes and assets.
3. Document all new requirements or steps in `UNITY_CONFIGURATION.md`.
4. Ensure the order of steps is logical for someone starting from an empty or freshly cloned project.
5. If you add or change core systems, also update `ARCHITECTURE.md`.
6. If the change corresponds to a backlog item, update `BACKLOG.md` accordingly.

Do not leave configuration knowledge only in your head or only in comments inside scripts.

---

## 7. Verification and Double-Checking

Agents must always double-check their work. At minimum, before considering work complete, perform this checklist:

1. **Compilation**

   * Ensure the project compiles with no errors or new warnings.

2. **Play Mode Sanity Check**

   * Enter Play Mode in the relevant scene(s).
   * Confirm there are no obvious runtime exceptions (no new errors in the Console).
   * Verify that the feature or change behaves as described by the backlog item.

3. **Code Review Self-Check**

   * Re-read your diff and check for:

     * Inconsistent naming or style.
     * Unused variables, imports, or dead code.
     * Debug logs left in production code.

4. **Documentation**

   * Confirm `LORE.md`, `ARCHITECTURE.md`, `BACKLOG.md`, and `UNITY_CONFIGURATION.md` are updated as needed.
   * Confirm instructions are clear, ordered, and actionable.

5. **Edge Cases**

   * Consider simple edge cases (empty lists, null references, missing data).
   * Handle them gracefully or document them explicitly.

---

## 8. File-Specific Notes

### 8.1 LORE.md

* Source of truth for names, factions, events, and narrative tone.
* When adding mechanics tied to lore (e.g., wormhole networks, factions, alien polities), ensure they are consistent with `LORE.md`.
* Keep sections short and structured; avoid lore bloat without gameplay reason.

### 8.2 ARCHITECTURE.md

* Must mirror the actual code architecture.
* For each major system, document:

  * Purpose and responsibilities.
  * Key classes and their locations.
  * How it communicates with other systems.
* Update when refactoring systems or adding/removing core classes.

### 8.3 BACKLOG.md

* Use to manage work for both humans and agents.
* Keep tasks small and understandable.
* When a task is completed, briefly note what was done if it is not obvious from the title.

### 8.4 UNITY_CONFIGURATION.md

* Treat this as the reproducible setup recipe for the project.
* If a human cannot set up the project and scene by following this document, treat that as a bug.

---

## 9. Agent Workflow Checklist (TL;DR)

Before starting:

1. Read or refresh:

   * `LORE.md`
   * `ARCHITECTURE.md`
   * `BACKLOG.md`
   * `UNITY_CONFIGURATION.md`
2. Pick or define a task in `BACKLOG.md`.

While working:

3. Implement changes with clean, readable Unity C# code.
4. Keep diffs focused and avoid unnecessary changes.
5. If you change scenes or project settings, immediately note the required steps for `UNITY_CONFIGURATION.md`.

Before finishing:

6. Ensure the project compiles with no new warnings or errors.
7. Run the relevant scene(s) in Play Mode and check basic behaviour.
8. Update:

   * `ARCHITECTURE.md` for system-level changes.
   * `BACKLOG.md` for task status and follow-ups.
   * `UNITY_CONFIGURATION.md` for any setup or scene changes.
   * `LORE.md` if new lore is introduced or expanded.
9. Perform a final self-review of code and docs for clarity and consistency.

If any of these steps cannot be fully completed (e.g., missing context or uncertainty), leave clear notes in `BACKLOG.md` and minimal comments in code describing what remains open.
