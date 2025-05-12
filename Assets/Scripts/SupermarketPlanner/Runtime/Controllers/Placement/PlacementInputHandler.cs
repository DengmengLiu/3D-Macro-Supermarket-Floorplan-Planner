using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// Handle placement-related input operations, including click, rotate, cancel, etc.
    /// </summary>
    public class PlacementInputHandler : MonoBehaviour
    {
        // Input action
        private InputAction mousePositionAction;
        private InputAction leftMouseAction;
        private InputAction rightMouseAction;
        private InputAction rotateAction;
        private InputAction cancelAction;
        private InputAction continuousPlacementAction;

        private InputActionMap actionMap;

        // Event
        public event Action OnLeftClick;
        public event Action OnRightClick;
        public event Action OnRotate;
        public event Action OnCancel;

        private void Awake()
        {
            CreateInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void OnDestroy()
        {
            actionMap?.Dispose();
        }

        private void CreateInputActions()
        {
            actionMap = new InputActionMap("PlacementControls");

            mousePositionAction = actionMap.AddAction("MousePosition", binding: "<Mouse>/position");
            leftMouseAction = actionMap.AddAction("LeftMouse", binding: "<Mouse>/leftButton");
            rightMouseAction = actionMap.AddAction("RightMouse", binding: "<Mouse>/rightButton");
            rotateAction = actionMap.AddAction("Rotate", binding: "<Keyboard>/r");
            cancelAction = actionMap.AddAction("Cancel", binding: "<Keyboard>/escape");
        }

        private void EnableInputActions()
        {
            mousePositionAction.Enable();
            leftMouseAction.Enable();
            rightMouseAction.Enable();
            rotateAction.Enable();
            cancelAction.Enable();
            // continuousPlacementAction.Enable(); // Remove this line

            // Add callbacks
            leftMouseAction.performed += OnLeftMousePerformed;
            rightMouseAction.performed += OnRightMousePerformed;
            rotateAction.performed += OnRotatePerformed;
            cancelAction.performed += OnCancelPerformed;
        }

        private void DisableInputActions()
        {
            // Remove callbacks
            leftMouseAction.performed -= OnLeftMousePerformed;
            rightMouseAction.performed -= OnRightMousePerformed;
            rotateAction.performed -= OnRotatePerformed;
            cancelAction.performed -= OnCancelPerformed;

            mousePositionAction.Disable();
            leftMouseAction.Disable();
            rightMouseAction.Disable();
            rotateAction.Disable();
            cancelAction.Disable();
            // continuousPlacementAction.Disable(); // Remove this line
        }

        private void OnLeftMousePerformed(InputAction.CallbackContext context)
        {
            OnLeftClick?.Invoke();
        }

        private void OnRightMousePerformed(InputAction.CallbackContext context)
        {
            OnRightClick?.Invoke();
        }

        private void OnRotatePerformed(InputAction.CallbackContext context)
        {
            OnRotate?.Invoke();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            OnCancel?.Invoke();
        }

        /// <summary>
        /// Get the mouse position
        /// </summary>
        public Vector2 GetMousePosition()
        {
            return mousePositionAction.ReadValue<Vector2>();
        }

        /// <summary>
        /// Get input action
        /// </summary>
        public InputAction GetMousePositionAction()
        {
            return mousePositionAction;
        }
    }
}