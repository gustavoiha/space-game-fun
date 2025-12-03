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
  - Attach **PlayerShipController** (Assets/Player/Scripts/PlayerShipController.cs) to the root object.
    - Forward Acceleration: ~50
    - Strafe Acceleration: ~30
    - Max Speed: ~75
    - Inertial Damping: ~0.1
    - Pitch/Yaw Speed: ~60; Roll Speed: ~70
    - Rotation Smoothing: ~8
  - Save the GameObject as a prefab in `Assets/Player/PlayerShip.prefab`.

4. Space backdrop (stars only) [DONE]
  - Import a high-resolution starfield skybox or HDRI (no nebulas/clouds). Recommended: a 4k–8k star-only cubemap or HDRI from a reputable source (e.g., NASA star catalogs or a “realistic starfield” pack).
  - Create a **Skybox/Cubemap** or **Skybox/Panoramic** material named `StarfieldSkybox` in `Assets/Scenarios/Background/` and assign the star texture(s).
  - In **Window → Rendering → Lighting → Environment**, set the Skybox Material to `StarfieldSkybox` and reduce Ambient Intensity (~0.3) to keep ships readable.
  - Disable fog in the Lighting settings to keep the view clear.
  - If you need to generate a star-only HDRI locally (offline, no nebulas):
    1. In Blender, `File → New → General`, delete the default cube so only Camera and Light remain.
    2. In **Render Properties**, set **Render Engine: Cycles** (GPU if available). In **Output Properties**, set **Resolution X: 8192**, **Resolution Y: 4096**, and **Percent: 100%**.
    3. Select the **Camera** in the Outliner. In **Camera Properties (green camera icon)**, set **Lens Type: Panoramic** and **Panorama Type: Equirectangular**. Leave the camera at the origin; Cycles will render the world as a 360° map.
    4. Open the **Shader Editor → World**. Set **Background Color** to black. Add **Noise Texture** (Scale ~1500, Detail 0, Roughness 0) → **ColorRamp**; tighten the ramp so only tiny white points remain (the stars).
    5. To mix two star color layers (e.g., blue + white): duplicate the Noise→ColorRamp chain so you have **Noise A → ColorRamp A** and **Noise B → ColorRamp B**. Offset one noise via the **W** slider or a **Mapping** node (e.g., Location X = 0.25) so patterns differ. Set ramp A to black/white, ramp B to black/blue. Combine with **MixRGB (Add)** and enable **Clamp**, then feed the result into **Background**.
    6. In **Output Properties → Output**, set **File Format** to **OpenEXR** or **Radiance HDR**, **Color Depth: 16 bit**, and pick an output path (e.g., `Assets/Scenarios/Background/Starfield_8k.hdr`).
    7. Render via **Render → Render Image** (F12). In the Render Result window, choose **Image → Save As** to confirm format/path and save the HDRI.
  - In Unity, import the HDRI, set **Texture Type: Default**, **sRGB (Color Texture): On**, and create a **Skybox/Panoramic** material referencing it. Assign that material to Lighting → Skybox.

5. Camera setup [DONE]
  - Standard camera (quick setup):
    - Make `Main Camera` a child of `PlayerShip`, reset its local position/rotation, then set local position to `(0, 2, -8)` and local rotation to `(0, 0, 0)`.
    - Set **Clear Flags: Skybox**. The skybox follows camera rotation automatically, so the starfield should move when the ship (and thus the camera) turns.

6. Testing checklist [DONE]
  - Enter Play Mode and verify:
    - Mouse moves pitch/yaw responsively; Q/E rolls the ship.
    - W/S throttle forward/back; A/D strafe left/right; Space/Ctrl strafe up/down.
    - The ship spawns at the manually placed location.
    - The skybox shows sharp stars with no nebula or cloud visuals.

## Main Menu Scene (UI Toolkit)
1. Create the scene [DONE]
   - Add a new scene named `MainMenu` and place it at the top of **File → Build Settings → Scenes In Build** so it loads before gameplay scenes.
   - Add an empty GameObject named `MenuBootstrap` and attach **MainMenuController** (`Assets/MainMenu/Scripts/MainMenuController.cs`).

2. Create the UI Toolkit layout [DONE]
   - Add a **UIDocument** component to `MenuBootstrap` (or a dedicated UI GameObject) with a Panel Settings asset suitable for your target platform (match the Game view resolution and reference DPI scaling).
   - Use `Assets/MainMenu/UI/MainMenu.uxml`:
     - Root `VisualElement` (`name="main-menu-root"`) uses a full-screen flex layout with padding and a Horizon-inspired gradient background (see `Assets/MainMenu/UI/MainMenu.uss`).
     - `Button` named `new-game-button` labeled "New Game" remains focusable for keyboard/console. USS sets a minimum 240×56 size plus hover/active styles that retain high text contrast.
     - `VisualElement` named `loading-overlay` stretches absolutely over the view with a translucent dark veil and centered spinner/text. Its default **display** is **none**; MainMenuController toggles it while loading.
   - Styling lives in `Assets/MainMenu/UI/MainMenu.uss` (referenced by the UXML). Adjust colors there if you need alternate themes while preserving contrast.

3. Wire the MainMenuController [DONE]
   - In the **MainMenuController** component:
     - Set **Initialization Strategy** to a `NewGameInitializer` asset (see below).
     - Assign **Menu Document** to the `UIDocument` on the same GameObject (or your UI host).
     - Leave **New Game Button Name** as `new-game-button` unless you change the UXML name.
     - Assign **Loading Overlay Name** to `loading-overlay` (matches the UXML element that should show while loading).
     - Optionally set **Loading Screen** to a dedicated GameObject overlay (for 3D/Canvas-based spinners); MainMenuController will toggle both the GameObject and the UI Toolkit overlay.

4. Create the initialization asset [DONE]
   - In the Project window, right-click → **Create → Game State → New Game Initializer** and save it under `Assets/MainMenu/Scripts/NewGameInitializer.asset`.
   - Set **Initial Scene Name** to the gameplay scene you want to load (default `SpaceSandbox`). Ensure this scene is listed in **Build Settings**.

5. Hook up the button [DONE]
   - Enter Play Mode in `MainMenu` and click the "New Game" button. MainMenuController will start the coroutine, enable the loading overlay, and asynchronously load the specified gameplay scene.
   - When the scene load completes, the loading overlay hides automatically. Future "Load Game" behavior can plug in a different initializer without changing the menu wiring.
