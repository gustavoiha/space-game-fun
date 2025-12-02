# Unity Wormhole Era Prototype – Script Architecture

This document describes the main C# scripts in the Unity Wormhole Era prototype and how they fit together.

## Scripts

### Assets/Scripts/Ships/PlayerShipController.cs
- **Purpose:** Handles six-degree-of-freedom player flight with X4 Foundations-style controls using the Unity Input System (keyboard + mouse delta).
- **Responsibilities:**
  - Reads throttle, strafe, pitch, yaw, and roll input directly from `Keyboard`/`Mouse` devices (mouse delta plus optional keyboard pitch on arrows and yaw on `A/D`).
  - Applies forces/torques through a Rigidbody to move the ship with configurable acceleration, speed caps, and inertial damping.
  - Smooths angular velocity for stable camera/aiming behavior, applying rotation in the ship’s local space so yaw follows the current roll orientation.
- **Key Configurable Fields:**
  - `forwardAcceleration`, `strafeAcceleration`, `maxSpeed`, `inertialDamping`
  - `pitchSpeed`, `yawSpeed`, `rollSpeed`, `rotationSmoothing`
  - Keyboard bindings (`W/S` throttle, `A/D` yaw + horizontal strafe, `Space/Ctrl` vertical strafe, `Q/E` roll) and `mouseSensitivity` applied to mouse delta for pitch/yaw.

### Assets/Scripts/Environment/RandomizedSpawnRadius.cs
- **Purpose:** Optionally places an object at a random point around a center within a configurable radial band.
- **Responsibilities:**
  - When enabled, chooses a random direction on the unit sphere and a radius between min/max values on `Start`.
  - Positions the object relative to a specified center point (defaults to world origin) only when randomization is requested.
  - Optionally orients the object to face outward from the center.
- **Key Configurable Fields:**
  - `centerPoint`, `minRadius`, `maxRadius`, `alignOutwards`, `randomizeOnStart` (defaults off to allow manual placement).
