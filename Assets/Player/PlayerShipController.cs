using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceGame.Ships
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerShipController : MonoBehaviour
    {
        [System.Serializable]
        private struct AxisKeys
        {
            public Key positive;
            public Key negative;
        }

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

        [Header("Keyboard Bindings")]
        [SerializeField] private AxisKeys throttleKeys = new AxisKeys { positive = Key.W, negative = Key.S };
        [SerializeField] private AxisKeys horizontalStrafeKeys = new AxisKeys { positive = Key.D, negative = Key.A };
        [SerializeField] private AxisKeys verticalStrafeKeys = new AxisKeys { positive = Key.Space, negative = Key.LeftCtrl };
        [SerializeField] private AxisKeys pitchKeys = new AxisKeys { positive = Key.UpArrow, negative = Key.DownArrow };
        [SerializeField] private AxisKeys yawKeys = new AxisKeys { positive = Key.D, negative = Key.A };
        [SerializeField] private AxisKeys rollKeys = new AxisKeys { positive = Key.E, negative = Key.Q };

        [Header("Mouse Bindings")]
        [SerializeField] private float mouseSensitivity = 0.02f;

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
            float throttleInput = ReadAxis(throttleKeys);
            float strafeInput = ReadAxis(horizontalStrafeKeys);
            float verticalInput = ReadAxis(verticalStrafeKeys);

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
            Vector2 mouseDelta = ReadMouseDelta();
            float pitchInput = mouseDelta.y + ReadAxis(pitchKeys);
            float yawInput = mouseDelta.x + ReadAxis(yawKeys);
            float rollInput = ReadAxis(rollKeys);

            Vector3 desiredLocalAngular = new Vector3(-pitchInput * pitchSpeed, yawInput * yawSpeed, -rollInput * rollSpeed);
            Vector3 desiredRadians = desiredLocalAngular * Mathf.Deg2Rad;
            Vector3 desiredWorldAngular = transform.TransformDirection(desiredRadians);

            Vector3 smoothedAngularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, desiredWorldAngular, rotationSmoothing * Time.fixedDeltaTime);
            _rigidbody.angularVelocity = smoothedAngularVelocity;
        }

        private float ReadAxis(AxisKeys axisKeys)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return 0f;
            }

            float value = 0f;

            if (axisKeys.positive != Key.None && keyboard[axisKeys.positive].isPressed)
            {
                value += 1f;
            }

            if (axisKeys.negative != Key.None && keyboard[axisKeys.negative].isPressed)
            {
                value -= 1f;
            }

            return value;
        }

        private Vector2 ReadMouseDelta()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return Vector2.zero;
            }

            return mouse.delta.ReadValue() * mouseSensitivity;
        }
    }
}
