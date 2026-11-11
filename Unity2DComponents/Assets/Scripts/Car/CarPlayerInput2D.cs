using UnityEngine;
using UnityEngine.InputSystem;

namespace ModularComponents.Driving2D
{
    public struct CarControlInput2D
    {
        public float Steering;
        public float Throttle;

        public CarControlInput2D(float steering, float throttle)
        {
            Steering = Mathf.Clamp(steering, -1f, 1f);
            Throttle = Mathf.Clamp(throttle, -1f, 1f);
        }
    }

    /// <summary>Reads the Driving action map and sends control values to a car controller.</summary>
    public sealed class CarPlayerInput2D : MonoBehaviour
    {
        private const string ActionMapName = "Driving";

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private CarController2D receiver;

        private InputActionMap drivingMap;
        private InputAction steeringAction;
        private InputAction throttleAction;
        private float steering;
        private float throttle;

        private void Awake()
        {
            if (receiver == null)
                receiver = GetComponentInChildren<CarController2D>();
        }

        private void OnEnable()
        {
            if (!BindActions())
                return;

            steeringAction.performed += OnInputChanged;
            steeringAction.canceled += OnInputChanged;
            throttleAction.performed += OnInputChanged;
            throttleAction.canceled += OnInputChanged;
            drivingMap.Enable();
        }

        private void OnDisable()
        {
            if (drivingMap != null)
            {
                steeringAction.performed -= OnInputChanged;
                steeringAction.canceled -= OnInputChanged;
                throttleAction.performed -= OnInputChanged;
                throttleAction.canceled -= OnInputChanged;
                drivingMap.Disable();
            }

            steering = 0f;
            throttle = 0f;
            if (receiver != null)
                receiver.SetInput(default);
        }

        private bool BindActions()
        {
            if (actions == null)
            {
                Debug.LogError("CarPlayerInput2D requires the project's Input Actions asset.", this);
                return false;
            }

            drivingMap = actions.FindActionMap(ActionMapName, false);
            if (drivingMap == null)
            {
                Debug.LogError($"Input Actions asset has no '{ActionMapName}' map.", this);
                return false;
            }

            steeringAction = drivingMap.FindAction("Steer", false);
            throttleAction = drivingMap.FindAction("Throttle", false);
            if (steeringAction == null || throttleAction == null)
            {
                Debug.LogError($"Input Actions map '{ActionMapName}' must define Steer and Throttle actions.", this);
                drivingMap = null;
                return false;
            }

            if (receiver == null)
            {
                Debug.LogError("CarPlayerInput2D needs a CarController2D receiver.", this);
                drivingMap = null;
                return false;
            }

            return true;
        }

        private void OnInputChanged(InputAction.CallbackContext context)
        {
            steering = steeringAction.ReadValue<float>();
            throttle = throttleAction.ReadValue<float>();
            receiver.SetInput(new CarControlInput2D(steering, throttle));
        }
    }
}
