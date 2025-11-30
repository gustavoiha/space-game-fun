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
    public static GalaxyMapUIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GalaxyGenerator galaxy;

    [Tooltip("Root GameObject of the map UI (panel/canvas). This will be enabled/disabled when opening/closing the map.")]
    [SerializeField] private GameObject mapRoot;

    [SerializeField] private RectTransform mapContentRect;

    [Tooltip("Prefab for system icons on the map. Should contain an Image.")]
    [SerializeField] private RectTransform systemIconPrefab;

    [Tooltip("Prefab or component used to draw wormhole lines (e.g. an Image-based line).")]
    [SerializeField] private Image wormholeLinePrefab;

    [Header("Visuals")]
    [SerializeField] private Color undiscoveredColor = Color.black;
    [SerializeField] private Color discoveredColor = Color.white;
    [SerializeField] private Color currentSystemColor = Color.cyan;

    [Header("Pan & Zoom")]
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private float zoomSpeed = 0.08f;
    [SerializeField] private float keyboardPanSpeed = 400f;
    [SerializeField] private float dragPanSpeed = 1f;
    [SerializeField] private bool clampPanning = true;

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

    private GameDiscoveryState discovery;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Default mapRoot to this object or the rect's root if not assigned
        if (mapRoot == null)
        {
            if (mapContentRect != null)
                mapRoot = mapContentRect.root.gameObject;
            else
                mapRoot = gameObject;
        }

        isOpen = openOnStart;
        if (mapRoot != null)
        {
            mapRoot.SetActive(isOpen);
        }
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
            RefreshVisuals();
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
        if (mapRoot == null)
            return;

        isOpen = !isOpen;
        mapRoot.SetActive(isOpen);

        if (isOpen)
        {
            BuildMap();
            RefreshVisuals();
        }
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
    /// Rebuilds all system icons and wormhole lines from the current galaxy data.
    /// </summary>
    public void BuildMap()
    {
        ClearMap();

        if (mapContentRect != null)
        {
            mapContentRect.anchoredPosition = Vector2.zero;
            currentZoom = 1f;
            mapContentRect.localScale = Vector3.one;
        }

        if (galaxy == null || galaxy.Systems == null || galaxy.Systems.Count == 0)
            return;

        ComputeGalaxyBounds();
        CreateSystemIcons();
        CreateWormholeLines();

        RefreshVisuals();
    }

    private void ClearMap()
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

            systemIconsById[system.id] = icon;
        }
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

    private void RefreshVisuals()
    {
        // Systems
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
                // If there is no discovery state, treat all as discovered.
                image.color = discoveredColor;
            }
            else
            {
                if (!discovery.IsSystemDiscovered(systemId))
                {
                    image.color = undiscoveredColor;
                }
                else if (discovery.CurrentSystemId == systemId)
                {
                    image.color = currentSystemColor;
                }
                else
                {
                    image.color = discoveredColor;
                }
            }
        }

        // Wormholes
        foreach (var kvp in wormholeLinesById)
        {
            int wormholeId = kvp.Key;
            Image lineImage = kvp.Value;
            if (lineImage == null)
                continue;

            if (discovery == null)
            {
                lineImage.enabled = true;
            }
            else
            {
                bool discovered = discovery.IsWormholeDiscovered(wormholeId);
                lineImage.enabled = discovered;
            }
        }
    }

    private void HandleDiscoveryChanged()
    {
        if (isOpen)
        {
            RefreshVisuals();
        }
    }

    private void HandleCurrentSystemChanged(int systemId)
    {
        if (isOpen)
        {
            RefreshVisuals();
        }
    }

    private void Update()
    {
        if (!isOpen || mapContentRect == null)
            return;

        HandleZoomInput();
        HandlePanInput();
    }

    private void HandleZoomInput()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 0.01f, minZoom, maxZoom);
        mapContentRect.localScale = Vector3.one * currentZoom;

        if (clampPanning)
            ClampPanToBounds();
    }

    private void HandlePanInput()
    {
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

    private void ClampPanToBounds()
    {
        if (mapContentRect == null)
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
