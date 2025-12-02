# Unity Wormhole Era Prototype – Script Architecture

This document describes the main C# scripts in the Unity Wormhole Era prototype and how they fit together.

## Scripts

### Assets/Scripts/Ships/PlayerShipController.cs
- **Purpose:** Handles six-degree-of-freedom player flight with X4 Foundations-style controls using the classic Input Manager.
- **Responsibilities:**
  - Reads throttle, strafe, pitch, yaw, and roll input axes.
  - Applies forces/torques through a Rigidbody to move the ship with configurable acceleration, speed caps, and inertial damping.
  - Smooths angular velocity for stable camera/aiming behavior.
- **Key Configurable Fields:**
  - `forwardAcceleration`, `strafeAcceleration`, `maxSpeed`, `inertialDamping`
  - `pitchSpeed`, `yawSpeed`, `rollSpeed`, `rotationSmoothing`
  - Input axis names (`Vertical`, `Horizontal`, `StrafeVertical`, `Mouse X`, `Mouse Y`, `Roll`).

### Assets/Scripts/Environment/RandomizedSpawnRadius.cs
- **Purpose:** Optionally places an object at a random point around a center within a configurable radial band.
- **Responsibilities:**
  - When enabled, chooses a random direction on the unit sphere and a radius between min/max values on `Start`.
  - Positions the object relative to a specified center point (defaults to world origin) only when randomization is requested.
  - Optionally orients the object to face outward from the center.
- **Key Configurable Fields:**
  - `centerPoint`, `minRadius`, `maxRadius`, `alignOutwards`, `randomizeOnStart` (defaults off to allow manual placement).
