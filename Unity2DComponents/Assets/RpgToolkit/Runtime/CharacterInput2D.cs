using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace RpgToolkit
{
    public sealed class CharacterInput2D : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private CharacterIntent intent;
        [SerializeField] private ExplorationMode mode;
        [SerializeField] private Camera worldCamera;
        private InputActionAsset instanceActions;
        private InputActionMap map;
        private InputAction move;
        private InputAction click;
        private Vector2 previousStep;

        public void Configure(InputActionAsset asset, CharacterIntent target, ExplorationMode movementMode)
        {
            actions = asset;
            intent = target;
            mode = movementMode;
        }

        public void SetMode(ExplorationMode movementMode)
        {
            mode = movementMode;
            previousStep = Vector2.zero;
            intent?.Move(Vector2.zero);
        }

        private void Awake()
        {
            if (actions != null) instanceActions = Instantiate(actions);
        }

        private void OnEnable()
        {
            if (instanceActions == null || intent == null) return;
            map = instanceActions.FindActionMap("Player", false);
            move = map?.FindAction("Move", false);
            click = instanceActions.FindActionMap("UI", false)?.FindAction("Click", false);
            if (move == null)
            {
                Debug.LogError("RPG Toolkit requires a Player/Move input action.", this);
                return;
            }
            move.performed += OnMove;
            move.canceled += OnMove;
            if (click != null)
            {
                click.performed += OnClick;
                click.actionMap.Enable();
            }
            map.Enable();
        }

        private void OnDisable()
        {
            if (move != null)
            {
                move.performed -= OnMove;
                move.canceled -= OnMove;
            }
            if (click != null) click.performed -= OnClick;
            map?.Disable();
            if (click != null) click.actionMap.Disable();
            previousStep = Vector2.zero;
            if (intent != null) intent.Move(Vector2.zero);
        }

        private void OnDestroy()
        {
            if (instanceActions != null) Destroy(instanceActions);
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            if (mode == ExplorationMode.Free) intent.Move(direction);
            else if (mode == ExplorationMode.Grid && direction.sqrMagnitude > 0.4f && previousStep.sqrMagnitude < 0.4f)
            {
                Vector2Int step = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                    ? new Vector2Int(direction.x > 0 ? 1 : -1, 0)
                    : new Vector2Int(0, direction.y > 0 ? 1 : -1);
                intent.Step(step);
            }
            previousStep = direction;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (mode != ExplorationMode.PointAndClick) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null) return;
            Vector2 screen = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            intent.GoTo(world);
        }
    }
}
