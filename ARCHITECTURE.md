# Unity Wormhole Era Prototype – Script Architecture

This document describes the main C# scripts in the Unity Wormhole Era prototype and how they fit together after introducing:

- A **multi-scene, multi-physics star system architecture**.
- A **galaxy-only strategic map** (no dedicated 2D in-system map).
- A **system intel / manifest layer** with delayed, unreliable information about remote systems.
- A logistics model where **ships and messages must physically travel through wormholes**, and where transports can go missing without the player ever seeing the moment they were lost.

It is intended as reference for code navigation and as context for AI-assisted tools (e.g. Codex).
