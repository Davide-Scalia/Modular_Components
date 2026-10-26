using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ModularGrid
{
    /// <summary>Owns grid actions from the shared Input Actions asset and forwards them to gameplay consumers.</summary>
    public sealed class GridInputRouter : MonoBehaviour
    {
        private const string ActionMapName = "Grid";

        [SerializeField] private InputActionAsset actions;

        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private InputActionMap actionMap;
        private InputAction pointerPositionAction;
        private InputAction pointerDeltaAction;
        private InputAction placeAction;
        private InputAction rotateAction;
        private InputAction panAction;
        private PointerEventData pointerEvent;
        private Vector2 pointerPosition;
        private bool isPanning;

        public Vector2 PointerPosition => pointerPosition;
        public bool PointerOverUI => IsPointerOverUI();

        public event Action PlaceRequested;
        public event Action RotateRequested;
        public event Action<Vector2> CameraPanDelta;

        private void OnEnable()
        {
            if (!BindActions())
                return;

            pointerPositionAction.performed += OnPointerPosition;
            pointerDeltaAction.performed += OnPointerDelta;
            placeAction.performed += OnPlace;
            rotateAction.performed += OnRotate;
            panAction.started += OnPanStarted;
            panAction.canceled += OnPanCanceled;
            actionMap.Enable();
        }

        private void OnDisable()
        {
            if (actionMap == null)
                return;

            pointerPositionAction.performed -= OnPointerPosition;
            pointerDeltaAction.performed -= OnPointerDelta;
            placeAction.performed -= OnPlace;
            rotateAction.performed -= OnRotate;
            panAction.started -= OnPanStarted;
            panAction.canceled -= OnPanCanceled;
            actionMap.Disable();
            isPanning = false;
        }

        private bool BindActions()
        {
            if (actions == null)
            {
                Debug.LogError("GridInputRouter requires the project's Input Actions asset.", this);
                return false;
            }

            actionMap = actions.FindActionMap(ActionMapName, false);
            if (actionMap == null)
            {
                Debug.LogError($"Input Actions asset has no '{ActionMapName}' map.", this);
                return false;
            }

            pointerPositionAction = actionMap.FindAction("PointerPosition", false);
            pointerDeltaAction = actionMap.FindAction("PointerDelta", false);
            placeAction = actionMap.FindAction("Place", false);
            rotateAction = actionMap.FindAction("Rotate", false);
            panAction = actionMap.FindAction("Pan", false);
            if (pointerPositionAction == null || pointerDeltaAction == null || placeAction == null ||
                rotateAction == null || panAction == null)
            {
                Debug.LogError($"Input Actions map '{ActionMapName}' must define PointerPosition, PointerDelta, Place, Rotate, and Pan.", this);
                actionMap = null;
                return false;
            }

            return true;
        }

        private void OnPointerPosition(InputAction.CallbackContext context)
            => pointerPosition = context.ReadValue<Vector2>();

        private void OnPlace(InputAction.CallbackContext context)
        {
            if (!IsPointerOverUI())
                PlaceRequested?.Invoke();
        }

        private void OnRotate(InputAction.CallbackContext context)
        {
            if (!IsPointerOverUI())
                RotateRequested?.Invoke();
        }

        private void OnPanStarted(InputAction.CallbackContext context)
            => isPanning = !IsPointerOverUI();

        private void OnPanCanceled(InputAction.CallbackContext context)
            => isPanning = false;

        private void OnPointerDelta(InputAction.CallbackContext context)
        {
            if (isPanning && !IsPointerOverUI())
                CameraPanDelta?.Invoke(context.ReadValue<Vector2>());
        }

        private bool IsPointerOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (pointerEvent == null)
                pointerEvent = new PointerEventData(eventSystem);

            pointerEvent.position = pointerPosition;
            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerEvent, uiRaycastResults);
            foreach (RaycastResult result in uiRaycastResults)
                if (result.module is GraphicRaycaster)
                    return true;
            return false;
        }
    }
}
