using UnityEngine;

namespace SpaceGame.Ships
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerShipController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float forwardAcceleration = 50f;
        [SerializeField] private float strafeAcceleration = 30f;
        [SerializeField] private float maxSpeed = 75f;
        [SerializeField] private float inertialDamping = 0.1f;

        [Header("Rotation")]
        [SerializeField] private float pitchSpeed = 60f;
        [SerializeField] private float yawSpeed = 60f;
        [SerializeField] private float rollSpeed = 70f;
        [SerializeField] private float rotationSmoothing = 8f;

        [Header("Input Axes")]
        [SerializeField] private string throttleAxis = "Vertical";
        [SerializeField] private string horizontalStrafeAxis = "Horizontal";
        [SerializeField] private string verticalStrafeAxis = "StrafeVertical";
        [SerializeField] private string pitchAxis = "Mouse Y";
        [SerializeField] private string yawAxis = "Mouse X";
        [SerializeField] private string rollAxis = "Roll";

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0.1f;
        }

        private void FixedUpdate()
        {
            ApplyTranslation();
            ApplyRotation();
        }

        private void ApplyTranslation()
        {
            float throttleInput = Input.GetAxisRaw(throttleAxis);
            float strafeInput = Input.GetAxisRaw(horizontalStrafeAxis);
            float verticalInput = Input.GetAxisRaw(verticalStrafeAxis);

            Vector3 thrustDirection = (transform.forward * throttleInput * forwardAcceleration) +
                                      (transform.right * strafeInput * strafeAcceleration) +
                                      (transform.up * verticalInput * strafeAcceleration);

            _rigidbody.AddForce(thrustDirection, ForceMode.Acceleration);

            if (_rigidbody.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * maxSpeed;
            }

            _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, Vector3.zero, inertialDamping * Time.fixedDeltaTime);
        }

        private void ApplyRotation()
        {
            float pitchInput = Input.GetAxisRaw(pitchAxis);
            float yawInput = Input.GetAxisRaw(yawAxis);
            float rollInput = Input.GetAxisRaw(rollAxis);

            Vector3 desiredAngular = new Vector3(-pitchInput * pitchSpeed, yawInput * yawSpeed, -rollInput * rollSpeed);
            Vector3 desiredRadians = desiredAngular * Mathf.Deg2Rad;

            Vector3 smoothedAngularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, desiredRadians, rotationSmoothing * Time.fixedDeltaTime);
            _rigidbody.angularVelocity = smoothedAngularVelocity;
        }
    }
}
