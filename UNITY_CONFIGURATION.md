# Unity Configuration

## Basic setup [TODO]
- Create ship prefab

## Basic Space Scene Setup [TODO]
1. **Create the scene**
   - Make a new scene named `SpaceSandbox`.
   - Set the root of the world at `(0,0,0)`; keep it clear for spawn calculations.

2. **Input axes (old Input Manager)**
   - Open **Edit → Project Settings → Input Manager**.
   - Add/confirm the following axes to mirror X4 Foundations style controls:
     - `Vertical`: Positive = `W`, Negative = `S`, Gravity = 3, Sensitivity = 3, Type = Key/Mouse Button.
     - `Horizontal`: Positive = `D`, Negative = `A`, Gravity = 3, Sensitivity = 3, Type = Key/Mouse Button (strafe left/right).
     - `StrafeVertical`: Positive = `Space`, Negative = `Left Control`, Gravity = 3, Sensitivity = 3, Type = Key/Mouse Button (vertical strafe).
     - `Mouse X`: default settings (yaw with mouse).
     - `Mouse Y`: default settings (pitch with mouse, inverted handled in the axis settings if desired).
     - `Roll`: Positive = `E`, Negative = `Q`, Gravity = 3, Sensitivity = 3, Type = Key/Mouse Button.

3. **Player ship prefab**
   - Create an empty GameObject named `PlayerShip` and reset its transform.
   - Add visuals (placeholder mesh or finalized model) as a child named `Hull`.
   - Add **Rigidbody** (Use Gravity = false, Drag = 0, Angular Drag ≈ 0.1).
   - Add collider(s) that match the hull.
   - Attach **PlayerShipController** (Assets/Scripts/Ships/PlayerShipController.cs) to the root object.
     - Forward Acceleration: ~50
     - Strafe Acceleration: ~30
     - Max Speed: ~75
     - Inertial Damping: ~0.1
     - Pitch/Yaw Speed: ~60; Roll Speed: ~70
     - Rotation Smoothing: ~8
   - Save the GameObject as a prefab in `Assets/Prefabs/Ships/PlayerShip.prefab`.

4. **Player spawn placement (manual)**
   - Create an empty GameObject named `SpawnController` at the world origin.
   - Parent the player ship prefab instance under `SpawnController` and position/rotate it exactly where you want the player to begin.
   - If you later want a randomized start (for non-player objects), add **RandomizedSpawnRadius** (Assets/Scripts/Environment/RandomizedSpawnRadius.cs) and enable **Randomize On Start**.

5. **Space backdrop (stars only)**
   - Import a high-resolution starfield skybox or HDRI (no nebulas/clouds). Recommended: a 4k–8k star-only cubemap or HDRI from a reputable source (e.g., NASA star catalogs or a “realistic starfield” pack).
   - Create a **Skybox/Cubemap** or **Skybox/Panoramic** material named `StarfieldSkybox` in `Assets/Materials/Environment/` and assign the star texture(s).
   - In **Window → Rendering → Lighting → Environment**, set the Skybox Material to `StarfieldSkybox` and reduce Ambient Intensity (~0.3) to keep ships readable.
   - Disable fog in the Lighting settings to keep the view clear.

6. **Camera setup**
   - Add a Cinemachine FreeLook or standard Camera as a child of the player ship.
   - Position slightly behind and above the hull (e.g., `(0, 2, -8)`) and enable smooth follow for stable X4-like control feel.
   - Ensure the camera’s Clear Flags are set to **Skybox** to render the starfield.

7. **Testing checklist**
   - Enter Play Mode and verify:
     - Mouse moves pitch/yaw responsively; Q/E rolls the ship.
     - W/S throttle forward/back; A/D strafe left/right; Space/Ctrl strafe up/down.
     - The ship spawns at the manually placed location under `SpawnController`.
     - The skybox shows sharp stars with no nebula or cloud visuals.

8. **Future polish (optional)**
   - Add gentle camera shake on acceleration/roll for feedback.
   - Replace placeholder hull visuals with faction-appropriate Horizon Initiative assets.
   - Add UI throttle/velocity readouts once HUD work begins.
