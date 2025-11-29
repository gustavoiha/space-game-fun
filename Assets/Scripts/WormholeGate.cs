using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents a wormhole gate in space. When the player is close and presses
/// the interact key, the gate tells GameManager to jump to its target system.
/// </summary>
public class WormholeGate : MonoBehaviour
{
    [Tooltip("System id this gate leads to (GalaxyGenerator index).")]
    public int targetSystemId = -1;

    [Tooltip("Distance at which the player can activate the gate.")]
    public float activationDistance = 30f;

    [Tooltip("Key used to activate the gate.")]
    public Key interactKey = Key.F;

    private Transform player;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerTransform == null)
            return;

        if (targetSystemId < 0)
            return;

        if (player == null)
            player = GameManager.Instance.PlayerTransform;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= activationDistance)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame)
            {
                GameManager.Instance.JumpToSystem(targetSystemId);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
