using UnityEngine;

/// <summary>
/// Boots the game: selects a Horizon start system and spawns the player ship
/// some distance away in empty space.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    public GalaxyGenerator galaxy;
    public GameObject playerShipPrefab;

    [Header("Spawn Settings")]
    public float startDistanceFromStar = 60f;

    private GameObject playerInstance;

    private void Start()
    {
        if (galaxy == null)
        {
            Debug.LogError("GameManager: Galaxy reference not assigned.");
            return;
        }
        if (playerShipPrefab == null)
        {
            Debug.LogError("GameManager: Player ship prefab not assigned.");
            return;
        }

        var startSystem = galaxy.GetStartSystem();
        if (startSystem == null)
        {
            Debug.LogError("GameManager: No start system found.");
            return;
        }

        // Random horizontal direction
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 spawnPos = startSystem.position + dir * startDistanceFromStar;

        playerInstance = Instantiate(playerShipPrefab, spawnPos, Quaternion.identity);
        playerInstance.transform.LookAt(startSystem.position);
    }
}
