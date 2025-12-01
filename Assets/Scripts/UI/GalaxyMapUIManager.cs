using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;   // New Input System

/// <summary>
/// Draws the 2D galaxy map and colors systems/wormholes based on GameDiscoveryState.
/// Also controls opening/closing the map via an input action (e.g. M key).
/// </summary>
public class GalaxyMapUIManager : MonoBehaviour
{
    private enum MapMode
    {
        System,
        Galaxy
    }
    public static GalaxyMapUIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GalaxyGenerator galaxy;

    [Tooltip("Root MapCanvas GameObject that contains the galaxy and system map canvases.")]
    [SerializeField] private GameObject mapCanvasRoot;

    [SerializeField] private GameObject galaxyMapRoot;

    [SerializeField] private RectTransform mapContentRect;

    [Tooltip("Prefab for system icons on the map. Should contain an Image.")]
    [SerializeField] private RectTransform systemIconPrefab;

    [Tooltip("Prefab or component used to draw wormhole lines (e.g. an Image-based line).")]
    [SerializeField] private Image wormholeLinePrefab;

    [Header("System Map")]
    [Tooltip("Root container for the system map mode.")]
    [SerializeField] private GameObject systemMapRoot;

    [SerializeField] private RectTransform systemMapContentRect;

    [Tooltip("Circle element that defines the boundary of the star system view.")]
    [SerializeField] private RectTransform systemBoundaryCircle;

    [Tooltip("Prefab used to render the star at the center of a system map.")]
    [SerializeField] private RectTransform systemStarIconPrefab;

    [Tooltip("Prefab used to render wormhole entry points in a system map.")]
    [SerializeField] private RectTransform systemWormholeIconPrefab;

    [Tooltip("Prefab used to render the player's ship in a system map.")]
    [SerializeField] private RectTransform playerShipIconPrefab;

    [Header("Visuals")]
    [Tooltip("Color for undiscovered frontier systems (neighbors of discovered systems).")]
    [SerializeField] private Color undiscoveredColor = Color.black;
    [SerializeField] private Color discoveredColor = Color.white;
    [SerializeField] private Color currentSystemColor = Color.cyan;

    [Header("Initial View")]
    [Tooltip("Zoom applied when the map opens or is rebuilt.")]
    [SerializeField] private float initialZoom = 1.25f;
    [Tooltip("If true, the map recenters on the current system when opened.")]
    [SerializeField] private bool focusOnCurrentSystemOnOpen = true;

    [Header("System Map Transition")]
    [Tooltip("Zoom level at which the map should be considered in system view.")]
    [SerializeField] private float systemMapZoomThreshold = 2f;

    [Header("Pan & Zoom")]
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private float zoomSpeed = 0.08f;
    [SerializeField] private float keyboardPanSpeed = 400f;
    [SerializeField] private float dragPanSpeed = 1f;
    [SerializeField] private bool clampPanning = true;

    [Header("Mode Switching")]
    [Tooltip("Zoom threshold below which the map switches from system view to galaxy view when zooming out.")]
    [SerializeField] private float systemToGalaxyZoomThreshold = 0.55f;

    [Tooltip("Zoom threshold above which the map switches from galaxy view to system view when zooming in.")]
    [SerializeField] private float galaxyToSystemZoomThreshold = 1.05f;

    [Tooltip("Minimum zoom applied when entering the galaxy map so it does not appear too small.")]
    [SerializeField] private float minimumGalaxyViewZoom = 0.95f;

    [Tooltip("Reference world-space radius represented by the system map circle. Actual systems scale relative to this size.")]
    [SerializeField] private float systemMapWorldRadius = 4000f;

    [Header("Map Toggle (New Input System)")]
    [Tooltip("Input action used to toggle the map (bind this to the M key in your Input Actions asset).")]
    [SerializeField] private InputActionReference toggleMapAction;

    [Tooltip("Whether the map should start open when the scene loads.")]
    [SerializeField] private bool openOnStart = false;

    private bool isOpen;

    private readonly Dictionary<int, RectTransform> systemIconsById = new Dictionary<int, RectTransform>();
    private readonly List<Image> wormholeLines = new List<Image>();
    private readonly Dictionary<int, Image> wormholeLinesById = new Dictionary<int, Image>();

    private Vector2 galaxyCenter;
    private float mapFitScale = 1f;
    private Vector2 minPos;
    private Vector2 maxPos;
    private float currentZoom = 1f;
    private bool isDragging;
    private Vector2 lastMousePosition;
    private Vector2 lastPointerMapPosition;
    private bool hasPointerMapPosition;

    private MapMode currentMode = MapMode.System;
    private int activeSystemMapSystemId = -1;
    private RectTransform systemStarIconInstance;
    private RectTransform playerShipIconInstance;
    private readonly Dictionary<int, RectTransform> systemWormholeIconsById = new Dictionary<int, RectTransform>();

    private GameDiscoveryState discovery;

    private RectTransform ActiveContentRect => currentMode == MapMode.Galaxy ? mapContentRect : systemMapContentRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Default mapCanvasRoot to the outermost canvas that contains the map content
        if (mapCanvasRoot == null)
        {
            mapCanvasRoot = FindMapCanvasRoot();

            if (mapCanvasRoot == null)
                mapCanvasRoot = gameObject;
        }

        if (galaxyMapRoot == null)
        {
            galaxyMapRoot = mapContentRect != null ? mapContentRect.gameObject : mapCanvasRoot;
        }

        if (systemMapRoot == null && systemMapContentRect != null)
        {
            systemMapRoot = systemMapContentRect.gameObject;
        }

        isOpen = openOnStart;
        if (mapCanvasRoot != null)
        {
            mapCanvasRoot.SetActive(isOpen);
        }

        SetMode(MapMode.System, true);
    }

    private void OnEnable()
    {
        if (toggleMapAction != null)
        {
            toggleMapAction.action.performed += OnToggleMapPerformed;
            toggleMapAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (toggleMapAction != null)
        {
            toggleMapAction.action.performed -= OnToggleMapPerformed;
            toggleMapAction.action.Disable();
        }
    }

    private void Start()
    {
        if (galaxy == null)
        {
            galaxy = GalaxyGenerator.Instance;
        }

        discovery = GameDiscoveryState.Instance;

        if (discovery != null)
        {
            discovery.DiscoveryChanged += HandleDiscoveryChanged;
            discovery.CurrentSystemChanged += HandleCurrentSystemChanged;
        }

        if (isOpen)
        {
            BuildMap();
        }
    }

    private void OnDestroy()
    {
        if (discovery != null)
        {
            discovery.DiscoveryChanged -= HandleDiscoveryChanged;
            discovery.CurrentSystemChanged -= HandleCurrentSystemChanged;
        }
    }

    private void OnToggleMapPerformed(InputAction.CallbackContext ctx)
    {
        ToggleMap();
    }

    /// <summary>
    /// Toggle map visibility. Can be called by input or UI.
    /// </summary>
    public void ToggleMap()
    {
        if (mapCanvasRoot == null)
            return;

        isOpen = !isOpen;
        mapCanvasRoot.SetActive(isOpen);

        // Ensure the correct map sub-root is shown or hidden when toggling.
        SetMode(currentMode);

        if (isOpen)
        {
            BuildMap();
        }
    }

    private GameObject FindMapCanvasRoot()
    {
        if (mapContentRect == null)
            return null;

        var parentCanvases = mapContentRect.GetComponentsInParent<Canvas>(true);
        if (parentCanvases == null || parentCanvases.Length == 0)
            return null;

        foreach (var canvas in parentCanvases)
        {
            if (canvas != null && canvas.gameObject.name == "MapCanvas")
            {
                return canvas.gameObject;
            }
        }

        return parentCanvases[parentCanvases.Length - 1].gameObject;
    }

    /// <summary>
    /// Open the map if it is closed.
    /// </summary>
    public void OpenMap()
    {
        if (!isOpen)
            ToggleMap();
    }

    /// <summary>
    /// Close the map if it is open.
    /// </summary>
    public void CloseMap()
    {
        if (isOpen)
            ToggleMap();
    }

    /// <summary>
    /// Rebuilds both the galaxy view and the currently focused system map.
    /// </summary>
    public void BuildMap()
    {
        ClearGalaxyMap();
        ClearSystemMap();

        if (mapContentRect != null)
        {
            mapContentRect.anchoredPosition = Vector2.zero;
        }

        if (galaxy == null || galaxy.Systems == null || galaxy.Systems.Count == 0)
            return;

        ComputeGalaxyBounds();
        CreateSystemIcons();
        CreateWormholeLines();

        activeSystemMapSystemId = discovery != null ? discovery.CurrentSystemId : 0;
        BuildSystemMap(activeSystemMapSystemId);

        ResetViewToCurrentSystem();
        RefreshVisuals();
        SetMode(MapMode.System, true);
    }

    private void ClearGalaxyMap()
    {
        foreach (var icon in systemIconsById.Values)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }

        systemIconsById.Clear();

        foreach (var line in wormholeLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        wormholeLines.Clear();
        wormholeLinesById.Clear();
    }

    private void ClearSystemMap()
    {
        foreach (var kvp in systemWormholeIconsById)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }

        systemWormholeIconsById.Clear();

        if (systemStarIconInstance != null)
        {
            Destroy(systemStarIconInstance.gameObject);
            systemStarIconInstance = null;
        }

        if (playerShipIconInstance != null)
        {
            Destroy(playerShipIconInstance.gameObject);
            playerShipIconInstance = null;
        }
    }

    private void ComputeGalaxyBounds()
    {
        minPos = galaxy.MinPosition;
        maxPos = galaxy.MaxPosition;

        galaxyCenter = (minPos + maxPos) * 0.5f;

        Vector2 size = maxPos - minPos;
        size.x = Mathf.Max(size.x, 0.01f);
        size.y = Mathf.Max(size.y, 0.01f);

        if (mapContentRect != null)
        {
            Vector2 rectSize = mapContentRect.rect.size;
            rectSize.x = Mathf.Max(rectSize.x, 0.01f);
            rectSize.y = Mathf.Max(rectSize.y, 0.01f);

            float scaleX = rectSize.x / size.x;
            float scaleY = rectSize.y / size.y;
            mapFitScale = Mathf.Min(scaleX, scaleY);
        }
    }

    private void CreateSystemIcons()
    {
        foreach (var system in galaxy.Systems)
        {
            var icon = Instantiate(systemIconPrefab, mapContentRect);
            icon.anchoredPosition = GalaxyToMapPosition(system.position);
            icon.name = $"SystemIcon_{system.id}";

            var button = icon.GetComponent<Button>();
            if (button != null)
            {
                int capturedId = system.id;
                button.onClick.AddListener(() => OnSystemIconClicked(capturedId));
            }

            systemIconsById[system.id] = icon;
        }
    }

    private void OnSystemIconClicked(int systemId)
    {
        ShowSystemMapForSystem(systemId);
        FocusOnSystem(systemId);
        ApplyZoom(systemMapZoomThreshold);

        if (clampPanning)
            ClampPanToBounds();
    }

    private void CreateWormholeLines()
    {
        foreach (var wormhole in galaxy.Wormholes)
        {
            if (!galaxy.SystemsById.TryGetValue(wormhole.fromSystemId, out var fromSystem) ||
                !galaxy.SystemsById.TryGetValue(wormhole.toSystemId, out var toSystem))
            {
                continue;
            }

            var line = Instantiate(wormholeLinePrefab, mapContentRect);
            line.name = $"Wormhole_{wormhole.id}";

            Vector2 fromPos = GalaxyToMapPosition(fromSystem.position);
            Vector2 toPos = GalaxyToMapPosition(toSystem.position);

            RectTransform rt = line.rectTransform;
            Vector2 midPoint = (fromPos + toPos) * 0.5f;
            rt.anchoredPosition = midPoint;

            Vector2 dir = (toPos - fromPos);
            float length = dir.magnitude;
            rt.sizeDelta = new Vector2(length, rt.sizeDelta.y);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            wormholeLines.Add(line);
            wormholeLinesById[wormhole.id] = line;
        }
    }

    private Vector2 GalaxyToMapPosition(Vector2 galaxyPos)
    {
        Vector2 centered = galaxyPos - galaxyCenter;
        return centered * mapFitScale;
    }

    /// <summary>
    /// Backwards-compatible wrapper.
    /// Prefer calling GameDiscoveryState.Instance.SetCurrentSystem directly.
    /// </summary>
    public void SetCurrentSystem(int systemId)
    {
        if (GameDiscoveryState.Instance != null)
        {
            GameDiscoveryState.Instance.SetCurrentSystem(systemId);
        }
    }

    /// <summary>
    /// Backwards-compatible wrapper.
    /// Prefer calling GameDiscoveryState.Instance.DiscoverSystem directly.
    /// </summary>
    public void DiscoverSystem(int systemId)
    {
        if (GameDiscoveryState.Instance != null)
        {
            GameDiscoveryState.Instance.DiscoverSystem(systemId);
        }
    }

    /// <summary>
    /// Backwards-compatible wrapper.
    /// Prefer calling GameDiscoveryState.Instance.DiscoverWormholeAndEndpoints directly.
    /// </summary>
    public void DiscoverSystemsAlongWormhole(int wormholeId)
    {
        if (GameDiscoveryState.Instance != null)
        {
            GameDiscoveryState.Instance.DiscoverWormholeAndEndpoints(wormholeId);
        }
    }

    /// <summary>
    /// Opens the system map focused on the specified system ID.
    /// </summary>
    public void ShowSystemMapForSystem(int systemId)
    {
        BuildSystemMap(systemId);
        SetMode(MapMode.System, true);
    }

    private void RefreshVisuals()
    {
        RefreshSystemVisuals();
        RefreshWormholeVisuals();
        RefreshSystemMapVisuals();
    }

    private void HandleDiscoveryChanged()
    {
        if (isOpen)
        {
            BuildSystemMap(activeSystemMapSystemId);
            RefreshVisuals();
        }
    }

    private void HandleCurrentSystemChanged(int systemId)
    {
        if (isOpen)
        {
            BuildSystemMap(systemId);
            RefreshVisuals();
        }
    }

    private void Update()
    {
        if (!isOpen || ActiveContentRect == null)
            return;

        HandleZoomInput();
        HandlePanInput();

        if (currentMode == MapMode.System)
        {
            UpdateSystemMapDynamicElements();
        }
    }

    private void ResetViewToCurrentSystem()
    {
        ApplyZoom(initialZoom);

        if (currentMode == MapMode.Galaxy)
        {
            if (focusOnCurrentSystemOnOpen && discovery != null)
            {
                FocusOnSystem(discovery.CurrentSystemId);
            }

            if (clampPanning)
                ClampPanToBounds();
        }
        else
        {
            if (focusOnCurrentSystemOnOpen && discovery != null)
            {
                BuildSystemMap(discovery.CurrentSystemId);
            }

            if (systemMapContentRect != null)
                systemMapContentRect.anchoredPosition = Vector2.zero;
        }
    }

    private void ApplyZoom(float targetZoom)
    {
        currentZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        if (mapContentRect != null)
            mapContentRect.localScale = Vector3.one * currentZoom;

        if (systemMapContentRect != null)
            systemMapContentRect.localScale = Vector3.one * currentZoom * GetSystemMapScale();
    }

    private void FocusOnSystem(int systemId)
    {
        if (mapContentRect == null || galaxy == null || systemId < 0)
            return;

        if (!galaxy.SystemsById.TryGetValue(systemId, out var node))
            return;

        var parentRect = mapContentRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector2 mapPos = GalaxyToMapPosition(node.position);
        mapContentRect.anchoredPosition = -mapPos;
    }

    private void HandleZoomInput()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        float targetZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 0.01f, minZoom, maxZoom);

        RectTransform contentRect = ActiveContentRect;
        RectTransform parentRect = contentRect != null ? contentRect.parent as RectTransform : null;
        Vector2 pointerLocal = Vector2.zero;
        bool hasPointer = parentRect != null &&
                          RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mouse.position.ReadValue(), null, out pointerLocal);

        if (contentRect != null && hasPointer)
        {
            Vector2 offsetBeforeZoom = (pointerLocal - contentRect.anchoredPosition) / currentZoom;
            lastPointerMapPosition = offsetBeforeZoom;
            hasPointerMapPosition = true;
            ApplyZoom(targetZoom);
            contentRect.anchoredPosition = pointerLocal - offsetBeforeZoom * currentZoom;
        }
        else
        {
            ApplyZoom(targetZoom);
            hasPointerMapPosition = false;
        }

        if (clampPanning)
            ClampPanToBounds();

        EvaluateZoomModeSwitch();
    }

    private void HandlePanInput()
    {
        if (currentMode != MapMode.Galaxy)
            return;

        Vector2 delta = Vector2.zero;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                delta.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                delta.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                delta.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                delta.x -= 1f;
        }

        if (delta.sqrMagnitude > 0.001f)
        {
            delta = delta.normalized * keyboardPanSpeed * Time.deltaTime;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMousePosition = mouse.position.ReadValue();
            }

            if (isDragging && (mouse.rightButton.isPressed || mouse.middleButton.isPressed))
            {
                Vector2 currentPosition = mouse.position.ReadValue();
                Vector2 dragDelta = currentPosition - lastMousePosition;
                lastMousePosition = currentPosition;
                delta += dragDelta * dragPanSpeed;
            }
            else if (isDragging)
            {
                isDragging = false;
            }
        }

        if (delta.sqrMagnitude > 0.0001f)
        {
            mapContentRect.anchoredPosition += delta;
            if (clampPanning)
                ClampPanToBounds();
        }
    }

    private void EvaluateZoomModeSwitch()
    {
        if (currentMode == MapMode.System && currentZoom <= systemToGalaxyZoomThreshold)
        {
            currentZoom = Mathf.Max(currentZoom, minimumGalaxyViewZoom);
            SetMode(MapMode.Galaxy, true);
            FocusOnSystem(activeSystemMapSystemId);
            ApplyZoom(currentZoom);
            if (clampPanning)
                ClampPanToBounds();
        }
        else if (currentMode == MapMode.Galaxy && currentZoom >= galaxyToSystemZoomThreshold)
        {
            UpdateActiveSystemFromPointer();
            BuildSystemMap(activeSystemMapSystemId);
            SetMode(MapMode.System, true);
            ApplyZoom(currentZoom);
        }
    }

    /// <summary>
    /// Updates the active system selection based on the system closest to the current zoom pivot.
    /// Prefers the pointer position (used as the zoom pivot) and falls back to the view center if needed.
    /// </summary>
    private void UpdateActiveSystemFromPointer()
    {
        if (galaxy == null || galaxy.Systems == null || galaxy.Systems.Count == 0 || mapContentRect == null)
            return;

        Vector2 viewCenterMapPos = -mapContentRect.anchoredPosition;
        int closestSystemId = hasPointerMapPosition
            ? FindClosestSystemToMapPoint(lastPointerMapPosition)
            : FindClosestSystemToMapPoint(viewCenterMapPos);

        if (closestSystemId < 0)
        {
            closestSystemId = FindClosestSystemToMapPoint(viewCenterMapPos);
        }

        if (closestSystemId >= 0)
        {
            activeSystemMapSystemId = closestSystemId;
        }
    }

    /// <summary>
    /// Returns the system ID whose icon is closest to the provided map-space point.
    /// </summary>
    private int FindClosestSystemToMapPoint(Vector2 mapPoint)
    {
        float bestDistance = float.MaxValue;
        int bestId = -1;

        foreach (var system in galaxy.Systems)
        {
            Vector2 systemMapPos = GalaxyToMapPosition(system.position);
            float sqrDistance = (systemMapPos - mapPoint).sqrMagnitude;

            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestId = system.id;
            }
        }

        return bestId;
    }

    private void RefreshSystemVisuals()
    {
        foreach (var kvp in systemIconsById)
        {
            int systemId = kvp.Key;
            RectTransform icon = kvp.Value;
            if (icon == null)
                continue;

            var image = icon.GetComponent<Image>();
            if (image == null)
                continue;

            if (discovery == null)
            {
                image.color = discoveredColor;
                icon.gameObject.SetActive(true);
                continue;
            }

            bool isCurrent = discovery.CurrentSystemId == systemId;
            bool isDiscovered = discovery.IsSystemDiscovered(systemId);
            bool shouldShow = ShouldShowSystemOnMap(systemId);
            bool isFrontier = shouldShow && !isDiscovered;

            icon.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            if (isCurrent)
            {
                image.color = currentSystemColor;
            }
            else if (isDiscovered)
            {
                image.color = discoveredColor;
            }
            else
            {
                image.color = undiscoveredColor;
            }
        }
    }

    private void RefreshSystemMapVisuals()
    {
        if (systemStarIconInstance != null)
        {
            var image = systemStarIconInstance.GetComponent<Image>();
            if (image != null)
            {
                bool isCurrent = discovery != null && discovery.CurrentSystemId == activeSystemMapSystemId;
                bool isDiscovered = discovery == null || discovery.IsSystemDiscovered(activeSystemMapSystemId);
                image.color = isCurrent ? currentSystemColor : (isDiscovered ? discoveredColor : undiscoveredColor);
            }
        }

        foreach (var kvp in systemWormholeIconsById)
        {
            int wormholeId = kvp.Key;
            RectTransform icon = kvp.Value;
            if (icon == null)
                continue;

            if (discovery == null || galaxy == null || !galaxy.WormholesById.TryGetValue(wormholeId, out var link))
            {
                icon.gameObject.SetActive(true);
                continue;
            }

            bool wormholeKnown = discovery.IsWormholeDiscovered(wormholeId);
            bool endpointsVisible = ShouldShowSystemOnMap(link.fromSystemId) && ShouldShowSystemOnMap(link.toSystemId);
            icon.gameObject.SetActive(wormholeKnown || endpointsVisible);
        }
    }

    private void SetMode(MapMode mode, bool forceRebuild = false)
    {
        bool showRoots = isOpen;

        if (galaxyMapRoot != null)
            galaxyMapRoot.SetActive(showRoots && mode == MapMode.Galaxy);

        if (systemMapRoot != null)
            systemMapRoot.SetActive(showRoots && mode == MapMode.System);

        if (!forceRebuild && currentMode == mode)
            return;

        currentMode = mode;

        ApplyZoom(currentZoom);

        if (mode == MapMode.Galaxy)
        {
            FocusOnSystem(activeSystemMapSystemId);
            if (clampPanning)
                ClampPanToBounds();
        }
        else if (forceRebuild)
        {
            BuildSystemMap(activeSystemMapSystemId);
        }
    }

    private void RefreshWormholeVisuals()
    {
        foreach (var kvp in wormholeLinesById)
        {
            int wormholeId = kvp.Key;
            Image lineImage = kvp.Value;
            if (lineImage == null)
                continue;

            if (discovery == null || galaxy == null || !galaxy.WormholesById.TryGetValue(wormholeId, out var link))
            {
                lineImage.enabled = true;
                continue;
            }

            bool wormholeKnown = discovery.IsWormholeDiscovered(wormholeId);
            bool endpointsVisible = ShouldShowSystemOnMap(link.fromSystemId) &&
                                    ShouldShowSystemOnMap(link.toSystemId);

            lineImage.enabled = wormholeKnown && endpointsVisible;
        }
    }

    private bool ShouldShowSystemOnMap(int systemId)
    {
        if (discovery == null)
            return true;

        if (discovery.IsSystemDiscovered(systemId))
            return true;

        return IsAdjacentToDiscovered(systemId);
    }

    private bool IsAdjacentToDiscovered(int systemId)
    {
        if (galaxy == null || discovery == null)
            return false;

        var neighbors = galaxy.GetNeighbors(systemId);
        if (neighbors == null)
            return false;

        for (int i = 0; i < neighbors.Count; i++)
        {
            if (discovery.IsSystemDiscovered(neighbors[i]))
                return true;
        }

        return false;
    }

    private void BuildSystemMap(int systemId)
    {
        ClearSystemMap();
        activeSystemMapSystemId = systemId;

        if (systemMapContentRect == null || galaxy == null || systemId < 0)
            return;

        if (!galaxy.SystemsById.TryGetValue(systemId, out var system))
            return;

        if (systemStarIconPrefab != null)
        {
            systemStarIconInstance = Instantiate(systemStarIconPrefab, systemMapContentRect);
            systemStarIconInstance.anchoredPosition = Vector2.zero;
            systemStarIconInstance.name = $"System_{systemId}_Star";
        }

        CreateSystemWormholeIcons(systemId, system.position);
        EnsurePlayerShipIcon();
        RefreshSystemMapVisuals();
        UpdatePlayerShipIconPosition();
        ApplyZoom(currentZoom);
    }

    private void CreateSystemWormholeIcons(int systemId, Vector2 systemPosition)
    {
        if (systemMapContentRect == null || galaxy == null || systemWormholeIconPrefab == null)
            return;

        var neighbors = galaxy.GetNeighbors(systemId);
        if (neighbors == null || neighbors.Count == 0)
            return;

        float angleStep = 360f / Mathf.Max(1, neighbors.Count);

        for (int i = 0; i < neighbors.Count; i++)
        {
            int neighborId = neighbors[i];
            int wormholeId = FindWormholeIdBetween(systemId, neighborId);

            Vector2 dir = GetDirectionToNeighbor(systemPosition, neighborId, i * angleStep);
            RectTransform icon = Instantiate(systemWormholeIconPrefab, systemMapContentRect);
            icon.name = $"System_{systemId}_Wormhole_{wormholeId}";
            icon.anchoredPosition = dir * GetSystemMapUiRadius();

            systemWormholeIconsById[wormholeId] = icon;
        }
    }

    private Vector2 GetDirectionToNeighbor(Vector2 systemPosition, int neighborId, float fallbackAngle)
    {
        if (galaxy.SystemsById.TryGetValue(neighborId, out var neighbor))
        {
            Vector2 dir = neighbor.position - systemPosition;
            if (dir.sqrMagnitude > 0.0001f)
            {
                return dir.normalized;
            }
        }

        float angleRad = fallbackAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    private void EnsurePlayerShipIcon()
    {
        if (playerShipIconInstance == null && playerShipIconPrefab != null && systemMapContentRect != null)
        {
            playerShipIconInstance = Instantiate(playerShipIconPrefab, systemMapContentRect);
            playerShipIconInstance.name = "System_PlayerShip";
        }
    }

    private void UpdateSystemMapDynamicElements()
    {
        UpdatePlayerShipIconPosition();
    }

    private void UpdatePlayerShipIconPosition()
    {
        if (playerShipIconInstance == null)
            return;

        var gm = GameManager.Instance;
        bool shouldShow = gm != null && discovery != null && discovery.CurrentSystemId == activeSystemMapSystemId && gm.PlayerShip != null;
        playerShipIconInstance.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        Vector3 center = gm.CurrentSystemWorldPosition;
        Vector3 shipPos = gm.PlayerShip.transform.position;
        Vector3 offset = shipPos - center;

        float uiRadius = GetSystemMapUiRadius();
        float worldRadius = GetSystemMapWorldRadius();
        if (worldRadius < 0.001f)
            worldRadius = 1f;

        Vector2 mapped = new Vector2(offset.x, offset.z) * (uiRadius / worldRadius);
        playerShipIconInstance.anchoredPosition = mapped;
    }

    private float GetSystemMapUiRadius()
    {
        if (systemBoundaryCircle != null)
        {
            return systemBoundaryCircle.rect.width * 0.5f;
        }

        if (systemMapContentRect != null)
        {
            return Mathf.Min(systemMapContentRect.rect.width, systemMapContentRect.rect.height) * 0.4f;
        }

        return 100f;
    }

    private float GetSystemMapScale()
    {
        float targetWorldRadius = GetSystemMapWorldRadius();
        float referenceWorldRadius = Mathf.Max(systemMapWorldRadius, 0.001f);

        if (targetWorldRadius <= 0.001f)
            return 1f;

        return referenceWorldRadius / targetWorldRadius;
    }

    private float GetSystemMapWorldRadius()
    {
        if (galaxy != null && activeSystemMapSystemId >= 0 &&
            galaxy.SystemsById.TryGetValue(activeSystemMapSystemId, out var systemNode) &&
            systemNode.systemRadius > 0f)
        {
            return systemNode.systemRadius;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentSystemRadius > 0f)
        {
            return GameManager.Instance.CurrentSystemRadius;
        }

        if (GameManager.Instance != null && GameManager.Instance.GateRingRadius > 0f)
        {
            return GameManager.Instance.GateRingRadius;
        }

        return systemMapWorldRadius;
    }

    private int FindWormholeIdBetween(int systemAId, int systemBId)
    {
        if (galaxy == null || galaxy.Wormholes == null)
            return -1;

        for (int i = 0; i < galaxy.Wormholes.Count; i++)
        {
            var w = galaxy.Wormholes[i];
            if (w.IsIncidentTo(systemAId) && w.GetOtherSystem(systemAId) == systemBId)
            {
                return w.id;
            }
        }

        return -1;
    }

    private void ClampPanToBounds()
    {
        if (mapContentRect == null)
            return;

        if (currentMode != MapMode.Galaxy)
            return;

        RectTransform parentRect = mapContentRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector2 scaledSize = Vector2.Scale(mapContentRect.rect.size, mapContentRect.localScale);
        Vector2 halfContent = scaledSize * 0.5f;
        Vector2 halfParent = parentRect.rect.size * 0.5f;
        Vector2 limit = Vector2.Max(halfContent - halfParent, Vector2.zero);

        Vector2 pos = mapContentRect.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -limit.x, limit.x);
        pos.y = Mathf.Clamp(pos.y, -limit.y, limit.y);
        mapContentRect.anchoredPosition = pos;
    }
}
