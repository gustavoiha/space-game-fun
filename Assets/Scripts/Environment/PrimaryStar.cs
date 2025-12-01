using UnityEngine;

/// <summary>
/// Handles visual configuration for a system's primary star instance.
/// Applies size and color to optional render and light targets so the
/// prefab can be reused across generated systems with different looks.
/// </summary>
public class PrimaryStar : MonoBehaviour
{
    [Tooltip("Root transform for the visual star mesh; scaled to match the configured radius.")]
    [SerializeField] private Transform starVisualRoot;

    [Tooltip("Renderer used to tint the star surface. Leave null if color should not be applied automatically.")]
    [SerializeField] private Renderer starRenderer;

    [Tooltip("Optional light component driven by the configured star color.")]
    [SerializeField] private Light starLight;

    [Tooltip("Radius applied to the star visual in world units (half the diameter scale).")]
    [SerializeField] private float starRadius = 200f;

    [Tooltip("Color applied to the visual and light when configured.")]
    [SerializeField] private Color starColor = Color.white;

    /// <summary>
    /// Updates the star visuals to reflect the provided color and radius.
    /// </summary>
    /// <param name="color">Tint color for the star surface and light.</param>
    /// <param name="radius">Radius applied to the visual root in world units.</param>
    public void Configure(Color color, float radius)
    {
        starColor = color;
        starRadius = Mathf.Max(radius, 0f);

        ApplyColor();
        ApplyScale();
    }

    /// <summary>
    /// Ensures any inspector changes propagate immediately while editing.
    /// </summary>
    private void OnValidate()
    {
        ApplyColor();
        ApplyScale();
    }

    /// <summary>
    /// Applies the configured color to known visual targets.
    /// </summary>
    private void ApplyColor()
    {
        if (starRenderer != null && starRenderer.material != null)
        {
            starRenderer.material.color = starColor;
        }

        if (starLight != null)
        {
            starLight.color = starColor;
        }
    }

    /// <summary>
    /// Scales the star visual root based on the configured radius.
    /// </summary>
    private void ApplyScale()
    {
        if (starVisualRoot == null)
            return;

        float diameter = starRadius * 2f;
        starVisualRoot.localScale = Vector3.one * diameter;
    }
}
