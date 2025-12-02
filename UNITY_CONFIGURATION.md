# Unity Configuration

## Basic Space Scene Setup
1. Create the scene [DONE]
   - Make a new scene named `SpaceSandbox`.
   - Set the root of the world at `(0,0,0)`; keep it clear for spawn calculations.

2. Input System bindings [DONE]
   - Open **Edit → Project Settings → Player → Other Settings** and confirm **Active Input Handling** is set to **Input System Package (New)**.
   - The flight controller reads devices directly via the Input System:
     - Keyboard defaults: `W/S` throttle, `A/D` yaw (also feeds horizontal strafe), `Up/Down Arrow` pitch, `Space/Left Ctrl` vertical strafe, `Q/E` roll.
     - Mouse delta drives pitch (Y) and yaw (X); adjust sensitivity on **PlayerShipController → Mouse Sensitivity** if needed. If you want yaw-only on `A/D` without horizontal strafe, lower **Strafe Acceleration** or rebind **Horizontal Strafe Keys** in the inspector. To disable keyboard pitch, set **Pitch Keys** to `None/None` or change them to your preference.
   - No legacy Input Manager axes are required.

3. Player ship prefab [DONE]
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

4. Space backdrop (stars only)
   - Import a high-resolution starfield skybox or HDRI (no nebulas/clouds). Recommended: a 4k–8k star-only cubemap or HDRI from a reputable source (e.g., NASA star catalogs or a “realistic starfield” pack).
   - Create a **Skybox/Cubemap** or **Skybox/Panoramic** material named `StarfieldSkybox` in `Assets/Materials/Environment/` and assign the star texture(s).
   - In **Window → Rendering → Lighting → Environment**, set the Skybox Material to `StarfieldSkybox` and reduce Ambient Intensity (~0.3) to keep ships readable.
   - Disable fog in the Lighting settings to keep the view clear.
   - If you need to generate a star-only HDRI locally (offline, no nebulas):
     1. In Blender, create a new scene, delete the default cube, and open the **Shader Editor → World**. Set **Background Color** to black.
     2. Add a **Noise Texture** node (Scale ~1500, Detail 0, Roughness 0), feed it into a **ColorRamp**, and set the ramp to a sharp/step contrast so only small white points remain (these are the stars).
     3. Set the camera to **Panoramic → Equirectangular**, resolution to **8192 × 4096**, and render with Cycles (samples ~64–128). Save as **16-bit EXR/HDR** (e.g., `Starfield_8k.hdr`).
     4. In Unity, import the HDRI, set **Texture Type: Default**, **sRGB (Color Texture): On**, and create a **Skybox/Panoramic** material referencing it. Assign that material to Lighting → Skybox.


5. Camera setup
   - Add a Cinemachine FreeLook or standard Camera as a child of the player ship.
   - Position slightly behind and above the hull (e.g., `(0, 2, -8)`) and enable smooth follow for stable X4-like control feel.
   - Ensure the camera’s Clear Flags are set to **Skybox** to render the starfield.

6. Testing checklist
   - Enter Play Mode and verify:
     - Mouse moves pitch/yaw responsively; Q/E rolls the ship.
     - W/S throttle forward/back; A/D strafe left/right; Space/Ctrl strafe up/down.
     - The ship spawns at the manually placed location under `SpawnController`.
     - The skybox shows sharp stars with no nebula or cloud visuals.

7. Future polish (optional)
   - Add gentle camera shake on acceleration/roll for feedback.
   - Replace placeholder hull visuals with faction-appropriate Horizon Initiative assets.
   - Add UI throttle/velocity readouts once HUD work begins.
