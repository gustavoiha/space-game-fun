# AGENTS

Guidelines for AI coding agents collaborating on this project.

## 1. Core Directives

- Write well-architected code using:
  - Domain-Driven Design (DDD)
  - Single Responsibility Principle (SRP)
  - Small, focused, isolated scripts
- Always preserve and respect the existing architecture and conventions.
- Always follow and preserve the canon in `GAME_LORE.md` when implementing features or content.
- Given a task, if no code is necessary, update `UNITY_CONFIGURATION.md` file with instructions for human game developer to implement and test.

## 2. Mandatory Files To Maintain

- `UNITY_CONFIGURATION.md`
  - Keep a comprehensive, step-by-step guide for setting up and updating the Unity project.
  - Whenever scenes, prefabs, project settings, or assets must change, add/update instructions.
  - Reference the related backlog item(s) when changes are required.
  - TODO / DONE statuses will be used based on human progress.

- `ARCHITECTURE.md`
  - Maintain an up-to-date, concise list of each script/class.
  - For each entry: purpose, main responsibilities, and important public APIs.
  - Update this file whenever you add, remove, or significantly refactor scripts.

## 3. Coding Standards

- Prioritize readability, clarity, and maintainability over brevity.
- Keep classes and methods small, cohesive, and testable.
- Avoid “god classes” and deeply nested logic; extract helper components or services.
- Prefer explicit, descriptive names over abbreviations.
- Avoid duplication; refactor shared logic into reusable modules.
- Add comments only where they clarify intent or non-obvious decisions.

## 4. Unity-Specific Practices

- Keep gameplay logic decoupled from Unity-specific code where practical.
- Separate concerns:
  - Domain/model logic (pure C#)
  - Presentation/MonoBehaviours
  - Infrastructure (persistence, services, etc.)
- When changing scenes, prefabs, or settings:
  - Update `UNITY_CONFIGURATION.md` with all manual steps.
  - Note any dependencies (script order, layers, tags, input settings, etc.).

## 5. Workflow Expectations

- Before coding, read:
  - `GAME_LORE.md`
  - `ARCHITECTURE.md`
  - `UNITY_CONFIGURATION.md`
- Prefer extending existing patterns over introducing new ones.
- Do not remove or rewrite large sections of code unless clearly necessary; refactor incrementally.
- When adding features, consider how they affect:
  - Existing domains and boundaries
  - Save/load, performance, and future extensibility

## 6. Quality Checks Before Finishing

- Code compiles without warnings where reasonably possible.
- New logic is covered by simple, focused tests where applicable (editor/runtime).
- Public APIs are consistent and documented in `ARCHITECTURE.md`.
- Relevant docs (`UNITY_CONFIGURATION.md`, `ARCHITECTURE.md`) are updated.
- Lore, naming, and behavior are consistent with `GAME_LORE.md`.
