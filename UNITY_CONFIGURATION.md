# Unity Configuration

## Star System Map

Follow these steps to wire the new dual-mode map UI (system + galaxy) into your scene:

1. **Galaxy Map roots (done)**
   - In `GalaxyMapUIManager`, assign `Map Root` to the canvas/panel that contains the map.
   - Assign `Galaxy Map Root` to the RectTransform that holds the galaxy map content (the same parent as `Map Content Rect`).

2. **System Map container (done)**
   - Create/identify a sibling GameObject under the map canvas for the star system view and assign it to `System Map Root`.
   - Set `System Map Content Rect` to the RectTransform that should be scaled/translated when zooming.
   - Set `System Boundary Circle` to the circular Image/RectTransform that defines the system bounds on-screen.

3. **Prefabs for system view (done)**
   - `System Star Icon Prefab`: simple star sprite placed at the center of the system view.
   - `System Wormhole Icon Prefab`: icon used for each wormhole entry; these are spawned around the system boundary.
   - `Player Ship Icon Prefab`: icon that tracks the player ship when the viewed system matches the active system.

4. **Galaxy system icon prefab (done)**
   - Add a `Button` component to the system icon prefab and keep the existing `Image` component.
   - Assign the prefab to `System Icon Prefab`; clicking a system icon will now open its star system map.

5. **Mode switching thresholds**
   - Tune `System To Galaxy Zoom Threshold` (zooming out) and `Galaxy To System Zoom Threshold` (zooming in) to match your desired feel.
   - The map opens in system mode by default; zooming out past the first threshold reveals the galaxy map and recenters on the selected system.

6. **Scale matching**
   - Set `System Map World Radius` to roughly match `GameManager.gateRingRadius` (default 50) so ship icons and wormhole positions align with the in-scene gate ring scale.

7. **References**
   - Ensure `GalaxyGenerator`, `GameDiscoveryState`, and `GameManager` singletons exist in the scene so the map can resolve positions and discovery state.

8. **System info panel wiring**
   - Create or identify an info panel under the map canvas and assign it to `System Info Panel`; the panel stays hidden until a system is selected.
   - Assign the child `Text` components that should display metadata to:
     - `System Name Text` – shows the selected system’s display name.
     - `System Faction Text` – shows the faction recorded for the system.
     - `System Hazard Text` – shows the hazard level string.
     - `System Stations Text` – lists known stations or indicates when none are documented.
   - Ensure each system icon prefab retains its `Button` component so clicks drive selection and panel updates.
