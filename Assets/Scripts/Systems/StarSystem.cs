using UnityEngine;

/// <summary>
/// Runtime descriptor for a generated star system. Carries sizing and display metadata
/// so other systems (maps, gameplay) can reason about the space available in the system.
/// </summary>
public class StarSystem : MonoBehaviour
{
    [Tooltip("Unique identifier assigned by the galaxy generator.")]
    [SerializeField] private int systemId;

    [Tooltip("Display name of the system.")]
    [SerializeField] private string displayName;

    [Tooltip("Playable radius for this system in world units.")]
    [SerializeField] private float systemRadius = 4000f;

    [Tooltip("Primary star visual assigned when the system is spawned.")]
    [SerializeField] private PrimaryStar primaryStar;

    /// <summary>
    /// Stable identifier of the system.
    /// </summary>
    public int SystemId => systemId;

    /// <summary>
    /// Human-readable name for the system.
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// World-space radius that bounds gameplay within this system.
    /// </summary>
    public float SystemRadius => systemRadius;

    /// <summary>
    /// Reference to the primary star visual attached to this system (if configured).
    /// </summary>
    public PrimaryStar PrimaryStar => primaryStar;

    /// <summary>
    /// Initialize this instance using generated data.
    /// </summary>
    /// <param name="id">System identifier.</param>
    /// <param name="name">System display name.</param>
    /// <param name="radius">Playable radius for this system in world units.</param>
    public void Initialize(int id, string name, float radius)
    {
        systemId = id;
        displayName = name;
        systemRadius = Mathf.Max(radius, 0f);
    }

    /// <summary>
    /// Stores a reference to the instantiated primary star visual for this system.
    /// </summary>
    /// <param name="star">Primary star instance created for this system.</param>
    public void AttachPrimaryStar(PrimaryStar star)
    {
        primaryStar = star;
    }
}
