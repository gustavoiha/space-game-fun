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
    public RectTransform mapPanel;        // child of a Canvas
    public GameObject iconPrefab;         // prefab with Image + RectTransform
    public GameObject connectionLinePrefab; // prefab with Image + RectTransform for lines

    [Header("Input")]
    public Key toggleKey = Key.M;

    [Header("Debug")]
    public bool showUndiscoveredAsUnknown = false;

    [Header("Current System UI")]
    public TextMeshProUGUI currentSystemLabel;   // TextMeshProUGUI for label
    public Color currentSystemHighlightColor = Color.white;
    public float currentSystemIconScale = 1.4f;

    [Header("Connection Line UI")]
    [Tooltip("Thickness of the connection lines in UI units.")]
    public float lineThickness = 2f;

    [Tooltip("Base color of connection lines.")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.3f);

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

    private void CreateIconsAndLines()
    {
        if (galaxy == null || mapPanel == null || iconPrefab == null)
            return;

        ClearVisuals();

        var systems = galaxy.Systems;
        if (systems == null || systems.Count == 0) return;

        float halfSize = galaxy.galaxySize * 0.5f;

        int currentId = -1;
        if (GameManager.Instance != null)
            currentId = GameManager.Instance.currentSystemId;

        // --- Create icons for systems ---
        foreach (var sys in systems)
        {
            bool visible = sys.discovered || showUndiscoveredAsUnknown;
            if (!visible) continue;

            float normX = (sys.position.x + halfSize) / galaxy.galaxySize;
            float normY = (sys.position.z + halfSize) / galaxy.galaxySize;

            float x = (normX - 0.5f) * mapPanel.rect.width;
            float y = (normY - 0.5f) * mapPanel.rect.height;

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
                    lineRT.localRotation = Quaternion.Euler(0f, 0f, angle);
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
