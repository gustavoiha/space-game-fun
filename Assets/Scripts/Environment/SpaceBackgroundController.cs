using UnityEngine;

/// <summary>
/// Applies a simple six-sided skybox for in-system space backdrops.
/// </summary>
public class SpaceBackgroundController : MonoBehaviour
{
    /// <summary>
    /// If true, a simple six-sided skybox is applied using the provided texture.
    /// </summary>
    [SerializeField] private bool applySkybox = true;

    /// <summary>
    /// Texture assigned to every face of the generated skybox material.
    /// </summary>
    [SerializeField] private Texture2D skyboxFaceTexture;

    /// <summary>
    /// Cached skybox material instance generated at runtime.
    /// </summary>
    private Material skyboxMaterial;

    /// <summary>
    /// Initializes cached references and applies the skybox when configured.
    /// </summary>
    private void Awake()
    {
        if (applySkybox)
        {
            ApplySkyboxMaterial();
        }
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
}
