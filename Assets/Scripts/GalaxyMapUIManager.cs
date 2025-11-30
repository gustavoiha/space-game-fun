using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Galaxy map UI overlay.
/// - Reads logical systems from GalaxyGenerator
/// - Shows only discovered systems (unless debug flag is enabled)
/// - Highlights current system and shows its name
/// - Renders UI lines between discovered systems that are connected by wormholes
/// - Toggle with M (new Input System)
/// - Automatically refreshes when galaxy state changes
/// </summary>
public class GalaxyMapUIManager : MonoBehaviour
{
    [Header("References")]
    public GalaxyGenerator galaxy;
    public RectTransform mapPanel;            // child of a Canvas
    public GameObject iconPrefab;             // prefab with Image + RectTransform
    public GameObject connectionLinePrefab;   // prefab with Image + RectTransform for lines

    [Header("Input")]
    public Key toggleKey = Key.M;

    [Header("Debug")]
    public bool showUndiscoveredAsUnknown = false;

    [Header("Current System UI")]
    public TextMeshProUGUI currentSystemLabel;   // label for current system
    public Color currentSystemHighlightColor = Color.white;
    public float currentSystemIconScale = 1.4f;

    [Header("Connection Line UI")]
    [Tooltip("Thickness of the connection lines in UI units.")]
    public float lineThickness = 2f;

    [Tooltip("Base color of connection lines.")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Layout")]
    [Tooltip("Extra padding added around the outermost systems when fitting them into the map.")]
    [Range(0f, 0.5f)]
    public float boundsPaddingFraction = 0.05f;

    private bool mapVisible = false;

    private readonly List<GameObject> iconInstances = new List<GameObject>();
    private readonly List<GameObject> lineInstances = new List<GameObject>();
    private readonly Dictionary<int, RectTransform> iconBySystemId = new Dictionary<int, RectTransform>();

    private void OnEnable()
    {
        GameManager.OnGalaxyStateChanged += HandleGalaxyStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGalaxyStateChanged -= HandleGalaxyStateChanged;
    }

    private void Start()
    {
        if (mapPanel != null)
            mapPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[toggleKey].wasPressedThisFrame)
        {
            ToggleMap();
        }
    }

    private void ToggleMap()
    {
        mapVisible = !mapVisible;

        if (mapPanel != null)
            mapPanel.gameObject.SetActive(mapVisible);

        if (mapVisible)
        {
            CreateIconsAndLines();
        }
    }

    private void HandleGalaxyStateChanged()
    {
        if (!mapVisible)
            return;

        CreateIconsAndLines();
    }

    private void ClearVisuals()
    {
        foreach (var icon in iconInstances)
        {
            if (icon != null) Destroy(icon);
        }
        iconInstances.Clear();

        foreach (var line in lineInstances)
        {
            if (line != null) Destroy(line);
        }
        lineInstances.Clear();

        iconBySystemId.Clear();
    }

    /// <summary>
    /// Creates/refreshes the icons and connection lines for the galaxy map.
    /// Automatically infers the galaxy extents from the positions of the systems
    /// we intend to display, instead of relying on a fixed galaxySize.
    /// </summary>
    private void CreateIconsAndLines()
    {
        if (galaxy == null || mapPanel == null || iconPrefab == null)
            return;

        ClearVisuals();

        var systems = galaxy.Systems;
        if (systems == null || systems.Count == 0) return;

        // Collect systems that should be visible on the map.
        List<StarSystemData> visibleSystems = new List<StarSystemData>();
        foreach (var sys in systems)
        {
            bool visible = sys.discovered || showUndiscoveredAsUnknown;
            if (visible)
                visibleSystems.Add(sys);
        }

        if (visibleSystems.Count == 0)
        {
            // Nothing to render yet.
            if (currentSystemLabel != null)
                currentSystemLabel.text = "Current System: Unknown";
            return;
        }

        // Compute bounds (XZ) of visible systems.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var sys in visibleSystems)
        {
            Vector3 p = sys.position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }

        // Compute center and max extent, then fit that into the panel.
        float centerX = 0.5f * (minX + maxX);
        float centerZ = 0.5f * (minZ + maxZ);

        float extentX = Mathf.Max(Mathf.Abs(maxX - centerX), Mathf.Abs(centerX - minX));
        float extentZ = Mathf.Max(Mathf.Abs(maxZ - centerZ), Mathf.Abs(centerZ - minZ));
        float halfExtent = Mathf.Max(extentX, extentZ);

        if (halfExtent < 1f)
            halfExtent = 1f;

        // Apply a small padding so icons are not stuck to the edges.
        halfExtent *= (1f + boundsPaddingFraction);

        int currentId = -1;
        if (GameManager.Instance != null)
            currentId = GameManager.Instance.currentSystemId;

        // --- Create icons for systems ---
        foreach (var sys in visibleSystems)
        {
            Vector3 p = sys.position;

            // Normalized in [-1, 1] relative to inferred bounds.
            float normX = (p.x - centerX) / halfExtent;
            float normY = (p.z - centerZ) / halfExtent;

            float x = normX * (mapPanel.rect.width * 0.5f);
            float y = normY * (mapPanel.rect.height * 0.5f);

            GameObject iconGO = Instantiate(iconPrefab, mapPanel);
            var rt = iconGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);

            var img = iconGO.GetComponent<Image>();
            if (img != null)
            {
                Color baseColor = GetFactionColor(sys.faction);

                if (sys.id == currentId)
                {
                    baseColor = currentSystemHighlightColor;
                    rt.sizeDelta = rt.sizeDelta * currentSystemIconScale;
                }

                img.color = baseColor;
            }

            iconInstances.Add(iconGO);
            iconBySystemId[sys.id] = rt;
        }

        // --- Create connection lines between discovered systems ---
        CreateConnectionLines();

        // --- Update label ---
        if (currentSystemLabel != null)
        {
            if (currentId >= 0)
            {
                var curSys = galaxy.GetSystem(currentId);
                if (curSys != null)
                    currentSystemLabel.text = $"Current System: {curSys.displayName}";
                else
                    currentSystemLabel.text = "Current System: Unknown";
            }
            else
            {
                currentSystemLabel.text = "Current System: Unknown";
            }
        }
    }

    private void CreateConnectionLines()
    {
        if (connectionLinePrefab == null || galaxy == null)
            return;

        var systems = galaxy.Systems;
        if (systems == null || systems.Count == 0) return;

        // For each discovered system, draw lines to higher-index neighbours
        // to avoid drawing duplicates (wormholeLinks are symmetric).
        for (int i = 0; i < systems.Count; i++)
        {
            var sys = systems[i];
            if (!sys.discovered && !showUndiscoveredAsUnknown) continue;

            RectTransform rtA;
            if (!iconBySystemId.TryGetValue(sys.id, out rtA))
                continue;

            foreach (int neighbourId in sys.wormholeLinks)
            {
                if (neighbourId <= sys.id) continue; // avoid duplicates and self

                var neighbour = galaxy.GetSystem(neighbourId);
                if (neighbour == null) continue;
                if (!neighbour.discovered && !showUndiscoveredAsUnknown)
                    continue;

                RectTransform rtB;
                if (!iconBySystemId.TryGetValue(neighbourId, out rtB))
                    continue;

                Vector2 a = rtA.anchoredPosition;
                Vector2 b = rtB.anchoredPosition;
                Vector2 dir = b - a;
                float length = dir.magnitude;
                if (length <= 0.001f) continue;

                Vector2 mid = (a + b) * 0.5f;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                GameObject lineGO = Instantiate(connectionLinePrefab, mapPanel);
                var lineRT = lineGO.GetComponent<RectTransform>();
                if (lineRT != null)
                {
                    lineRT.anchoredPosition = mid;
                    lineRT.sizeDelta = new Vector2(length, lineThickness);
                    lineRT.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                var img = lineGO.GetComponent<Image>();
                if (img != null)
                {
                    img.color = lineColor;
                }

                lineInstances.Add(lineGO);
            }
        }
    }

    private Color GetFactionColor(Faction faction)
    {
        switch (faction)
        {
            case Faction.HorizonInitiative: return new Color(0.4f, 0.8f, 1f);   // teal/blue
            case Faction.HeliosProtectorate: return new Color(1f, 0.85f, 0.3f); // pale gold
            case Faction.MyriadCombine: return new Color(0.8f, 0.4f, 1f);
            case Faction.VerdureHegemony: return new Color(0.5f, 1f, 0.5f);
            case Faction.AelariConcord: return new Color(0.6f, 0.9f, 1f);
            case Faction.KarthanAssemblies: return new Color(1f, 0.6f, 0.4f);
            case Faction.SerathiEnclave: return new Color(0.8f, 0.8f, 1f);
            default: return Color.white;
        }
    }
}
