using UnityEngine;

namespace SpaceGame.Environment
{
    public class RandomizedSpawnRadius : MonoBehaviour
    {
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float minRadius = 500f;
        [SerializeField] private float maxRadius = 800f;
        [SerializeField] private bool alignOutwards = true;
        [SerializeField] private bool randomizeOnStart = false;

        private void Start()
        {
            if (!randomizeOnStart)
            {
                return;
            }

            Vector3 origin = centerPoint != null ? centerPoint.position : Vector3.zero;
            Vector3 direction = Random.onUnitSphere;
            float radius = Random.Range(minRadius, maxRadius);

            transform.position = origin + (direction * radius);

            if (alignOutwards)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }
}
