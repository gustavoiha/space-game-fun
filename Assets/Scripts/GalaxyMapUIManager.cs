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

    [Header("Map Toggle (New Input System)")]
    [Tooltip("Input action used to toggle the map (bind this to the M key in your Input Actions asset).")]
    [SerializeField] private InputActionReference toggleMapAction;

    [Tooltip("Whether the map should start open when the scene loads.")]
    [SerializeField] private bool openOnStart = false;

    private bool isOpen;

    private readonly Dictionary<int, RectTransform> systemIconsById = new Dictionary<int, RectTransform>();
    private readonly List<Image> wormholeLines = new List<Image>();
    private readonly Dictionary<int, Image> wormholeLinesById = new Dictionary<int, Image>();

    private Vector2 minPos;
    private Vector2 maxPos;

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
        minPos = new Vector2(float.MaxValue, float.MaxValue);
        maxPos = new Vector2(float.MinValue, float.MinValue);

        foreach (var system in galaxy.Systems)
        {
            minPos = Vector2.Min(minPos, system.position);
            maxPos = Vector2.Max(maxPos, system.position);
        }

        // Avoid zero-size bounds
        if (Mathf.Approximately(minPos.x, maxPos.x))
        {
            maxPos.x = minPos.x + 1f;
        }

        if (Mathf.Approximately(minPos.y, maxPos.y))
        {
            maxPos.y = minPos.y + 1f;
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
        Vector2 normalized = new Vector2(
            Mathf.InverseLerp(minPos.x, maxPos.x, galaxyPos.x),
            Mathf.InverseLerp(minPos.y, maxPos.y, galaxyPos.y)
        );

        Vector2 size = mapContentRect.rect.size;

        // Centered in rect
        Vector2 anchored = new Vector2(
            (normalized.x - 0.5f) * size.x,
            (normalized.y - 0.5f) * size.y
        );

        return anchored;
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
}
