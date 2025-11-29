using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Galaxy map UI overlay.
/// - Reads logical systems from GalaxyGenerator
/// - Shows only discovered systems
/// - Toggle with M (new Input System)
/// </summary>
public class GalaxyMapUIManager : MonoBehaviour
{
    [Header("References")]
    public GalaxyGenerator galaxy;
    public RectTransform mapPanel;   // child of a Canvas
    public GameObject iconPrefab;    // prefab with Image + RectTransform

    [Header("Input")]
    public Key toggleKey = Key.M;

    [Header("Debug")]
    public bool showUndiscoveredAsUnknown = false;

    private bool mapVisible = false;
    private bool iconsCreated = false;
    private readonly List<GameObject> iconInstances = new List<GameObject>();

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

        if (mapVisible && !iconsCreated)
        {
            CreateIcons();
            iconsCreated = true;
        }
    }

    private void CreateIcons()
    {
        if (galaxy == null || mapPanel == null || iconPrefab == null)
            return;

        foreach (var icon in iconInstances)
        {
            if (icon != null) Destroy(icon);
        }
        iconInstances.Clear();

        var systems = galaxy.Systems;
        if (systems == null || systems.Count == 0) return;

        float halfSize = galaxy.galaxySize * 0.5f;

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
                img.color = GetFactionColor(sys.faction);
            }

            iconInstances.Add(iconGO);
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
