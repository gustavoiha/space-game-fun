using System;
using UnityEngine;

/// <summary>
/// Coordinates a parallax starfield and optional skybox assignment for in-system space backdrops.
/// </summary>
public class SpaceBackgroundController : MonoBehaviour
{
    /// <summary>
    /// Configuration for a single parallax background layer.
    /// </summary>
    [Serializable]
    public class ParallaxLayerSettings
    {
        /// <summary>
        /// Renderer that displays the tiling star texture for this layer.
        /// </summary>
        [SerializeField] private Renderer layerRenderer;

        /// <summary>
        /// Movement multiplier applied to the camera delta when calculating texture offsets.
        /// Smaller values move more slowly and feel farther away.
        /// </summary>
        [SerializeField] private Vector2 motionMultiplier = new Vector2(0.01f, 0.01f);

        /// <summary>
        /// Initial texture offset used to align tiling with other layers.
        /// </summary>
        [SerializeField] private Vector2 baseOffset = Vector2.zero;

        /// <summary>
        /// Gets the renderer assigned to this parallax layer.
        /// </summary>
        public Renderer LayerRenderer => layerRenderer;

        /// <summary>
        /// Gets the camera movement multiplier for this parallax layer.
        /// </summary>
        public Vector2 MotionMultiplier => motionMultiplier;

        /// <summary>
        /// Gets the base texture offset configured for this parallax layer.
        /// </summary>
        public Vector2 BaseOffset => baseOffset;
    }

    /// <summary>
    /// Camera used to track movement for parallax and skybox updates.
    /// </summary>
    [SerializeField] private Camera targetCamera;

    /// <summary>
    /// Tag used to locate the primary camera if no reference is provided.
    /// </summary>
    [SerializeField] private string cameraTag = "MainCamera";

    /// <summary>
    /// If true, a simple six-sided skybox is applied using the provided texture.
    /// </summary>
    [SerializeField] private bool applySkybox = true;

    /// <summary>
    /// Texture assigned to every face of the generated skybox material.
    /// </summary>
    [SerializeField] private Texture2D skyboxFaceTexture;

    /// <summary>
    /// If true, parallax offsets are updated based on camera motion.
    /// </summary>
    [SerializeField] private bool enableParallaxLayers = true;

    /// <summary>
    /// Parallax star layers ordered from nearest to farthest.
    /// </summary>
    [SerializeField] private ParallaxLayerSettings[] parallaxLayers = Array.Empty<ParallaxLayerSettings>();

    /// <summary>
    /// Cached skybox material instance generated at runtime.
    /// </summary>
    private Material skyboxMaterial;

    /// <summary>
    /// Cached materials for each configured parallax layer.
    /// </summary>
    private Material[] parallaxMaterials = Array.Empty<Material>();

    /// <summary>
    /// Tracks the previous camera position used to compute parallax deltas.
    /// </summary>
    private Vector3 lastCameraPosition;

    /// <summary>
    /// Initializes cached references and applies the skybox when configured.
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            GameObject cameraObject = GameObject.FindWithTag(cameraTag);
            if (cameraObject != null)
            {
                targetCamera = cameraObject.GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        CacheParallaxMaterials();
        ResetCameraTracking();

        if (applySkybox)
        {
            ApplySkyboxMaterial();
        }
    }

    /// <summary>
    /// Updates parallax texture offsets based on camera motion.
    /// </summary>
    private void LateUpdate()
    {
        if (!enableParallaxLayers || targetCamera == null || parallaxMaterials.Length == 0)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        Vector3 delta = cameraPosition - lastCameraPosition;

        if (delta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        for (int i = 0; i < parallaxMaterials.Length; i++)
        {
            Material material = parallaxMaterials[i];
            ParallaxLayerSettings layer = parallaxLayers[i];

            if (material == null)
            {
                continue;
            }

            Vector2 currentOffset = material.mainTextureOffset;
            Vector2 parallaxOffset = new Vector2(delta.x * layer.MotionMultiplier.x, delta.y * layer.MotionMultiplier.y);
            material.mainTextureOffset = currentOffset + parallaxOffset;
        }

        lastCameraPosition = cameraPosition;
    }

    /// <summary>
    /// Applies a simple six-sided skybox that reuses the configured face texture.
    /// </summary>
    private void ApplySkyboxMaterial()
    {
        if (skyboxFaceTexture == null)
        {
            Debug.LogWarning("SpaceBackgroundController: Skybox face texture is missing; skipping skybox setup.");
            return;
        }

        Shader skyboxShader = Shader.Find("Skybox/6 Sided");
        if (skyboxShader == null)
        {
            Debug.LogError("SpaceBackgroundController: Skybox/6 Sided shader not found; cannot assign skybox.");
            return;
        }

        skyboxMaterial = new Material(skyboxShader)
        {
            name = "GeneratedSpaceSkybox"
        };

        skyboxMaterial.SetTexture("_FrontTex", skyboxFaceTexture);
        skyboxMaterial.SetTexture("_BackTex", skyboxFaceTexture);
        skyboxMaterial.SetTexture("_LeftTex", skyboxFaceTexture);
        skyboxMaterial.SetTexture("_RightTex", skyboxFaceTexture);
        skyboxMaterial.SetTexture("_UpTex", skyboxFaceTexture);
        skyboxMaterial.SetTexture("_DownTex", skyboxFaceTexture);

        RenderSettings.skybox = skyboxMaterial;
    }

    /// <summary>
    /// Caches material instances for each configured parallax layer and applies base offsets.
    /// </summary>
    private void CacheParallaxMaterials()
    {
        if (parallaxLayers == null || parallaxLayers.Length == 0)
        {
            parallaxMaterials = Array.Empty<Material>();
            return;
        }

        parallaxMaterials = new Material[parallaxLayers.Length];

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            Renderer renderer = parallaxLayers[i].LayerRenderer;
            if (renderer == null)
            {
                Debug.LogWarning($"SpaceBackgroundController: Parallax layer {i} is missing a renderer reference.");
                continue;
            }

            Material materialInstance = renderer.material;
            parallaxMaterials[i] = materialInstance;
            materialInstance.mainTextureOffset = parallaxLayers[i].BaseOffset;
        }
    }

    /// <summary>
    /// Resets parallax tracking to the current camera position, preventing large jumps when enabling the background mid-session.
    /// </summary>
    public void ResetCameraTracking()
    {
        lastCameraPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero;
    }
}
