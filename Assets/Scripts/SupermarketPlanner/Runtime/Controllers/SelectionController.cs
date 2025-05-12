using UnityEngine;
using UnityEngine.InputSystem;
using SupermarketPlanner.Services;
using SupermarketPlanner.Data;
using System.Collections;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// Object Selection Controller - Detects and handles selection of placed objects
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        [Tooltip("Maximum distance for raycast detection")]
        public float maxRaycastDistance = 100f;

        [Tooltip("Layer mask for detectable objects")]
        public LayerMask detectableLayerMask;

        // Placement controller reference
        private PlacementController placementController;

        // Component placement service reference
        private ComponentPlacementService placementService;

        // Input action
        private InputAction mousePositionAction;
        private InputAction mouseLeftClickAction;

        // Object detected by the current ray
        private GameObject currentDetectedObject;

        private void Awake()
        {
            // Create mouse position input action
            mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");
            mousePositionAction.Enable();

            // Create mouse left button click input action
            mouseLeftClickAction = new InputAction("MouseLeftClick", binding: "<Mouse>/leftButton");
            mouseLeftClickAction.Enable();

            // Add left button click event
            mouseLeftClickAction.performed += OnMouseLeftClick;

            // Set detection layer (if not specified)
            if (detectableLayerMask.value == 0)
            {
                // Assume placed object uses "PlacedObject" layer
                int placedObjectLayer = LayerMask.NameToLayer("PlacedObject");
                if (placedObjectLayer != -1)
                {
                    detectableLayerMask = 1 << placedObjectLayer;
                }
                else
                {
                    // Default layer is used by default
                    Debug.LogWarning("PlacedObject layer not found, default layer will be used");
                    detectableLayerMask = 1 << 0; // Default layer
                }
            }

            // Get placement controller reference
            placementController = FindFirstObjectByType<PlacementController>();
            if (placementController == null)
            {
                Debug.LogWarning("PlacementController not found, object selection function will not be available");
            }

            // Get component placement service reference
            placementService = FindFirstObjectByType<ComponentPlacementService>();
            if (placementService == null)
            {
                Debug.LogWarning("ComponentPlacementService not found, object selection function will not be available");
            }
        }

        private void OnDestroy()
        {
            mouseLeftClickAction.performed -= OnMouseLeftClick;

            mousePositionAction?.Dispose();
            mouseLeftClickAction?.Dispose();
        }

        private void Update()
        {
            // Check if it is in placement mode
            if (placementController != null && placementController.IsInPlacementMode())
            {
                // If it is in placement mode, do not perform ray detection
                currentDetectedObject = null; // Clear the currently detected object
                return;
            }

            // Detect the intersection of the mouse ray and the object every frame
            DetectObject();
        }

        /// <summary>
        /// Mouse left button click event processing
        /// </summary>
        private void OnMouseLeftClick(InputAction.CallbackContext context)
        {
            // If an object is currently detected and is not in placement mode, and all required controllers exist
            if (currentDetectedObject != null &&
            placementController != null &&
            placementService != null &&
            !placementController.IsInPlacementMode())
            {
                // Handle object selection
                HandleObjectSelection(currentDetectedObject);
            }
        }

        /// <summary>
        /// Handle object selection
        /// </summary>
        private void HandleObjectSelection(GameObject selectedObject)
        {
            // Get the PlacedComponent component on the object
            PlacedComponent placedComponent = selectedObject.GetComponent<PlacedComponent>();
            if (placedComponent == null)
            {
                Debug.LogWarning("The selected object has no PlacedComponent component");
                return;
            }

            // Get component data
            ComponentData componentData = placedComponent.ComponentData;
            if (componentData == null)
            {
                Debug.LogWarning("The PlacedComponent component has no valid ComponentData");
                return;
            }

            // Record the current position and rotation of the object
            Vector3 originalPosition = selectedObject.transform.position;
            Quaternion originalRotation = selectedObject.transform.rotation;

            // Delete the original object
            placementService.RemoveComponent(selectedObject);

            // Enter placement mode, using the same component data
            StartCoroutine(DelayedStartPlacement(componentData, originalPosition, originalRotation));
        }

        private IEnumerator DelayedStartPlacement(ComponentData componentData, Vector3 position, Quaternion rotation)
        {
            yield return null;
            // Enter placement mode
            placementController.StartPlacement(componentData);

            // Set the preview position
            yield return null;
            StartCoroutine(SetPreviewPositionNextFrame(position, rotation));
        }
        /// <summary>
        /// Set the preview object's position and rotation in the next frame
        /// </summary>
        private IEnumerator SetPreviewPositionNextFrame(Vector3 position, Quaternion rotation)
        {
            // Wait one frame to make sure the preview object has been created
            yield return null;

            // Get the preview manager
            PlacementPreviewManager previewManager = placementController.previewManager;
            if (previewManager != null)
            {
                // Get the preview object
                GameObject previewObject = previewManager.GetPreviewObject();
                if (previewObject != null)
                {
                    // Set the preview object's position and rotation
                    previewObject.transform.position = position;
                    previewObject.transform.rotation = rotation;

                    // Update the preview manager's internal state
                    previewManager.UpdatePreview();
                    // Debug.Log($"Set preview object position: {position}, rotation: {rotation.eulerAngles}");
                }
            }
        }

        /// <summary>
        /// Detect the intersection of the mouse ray and the object
        /// </summary>
        private void DetectObject()
        {
            // Reset the currently detected object
            currentDetectedObject = null;

            // Get the main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found");
                return;
            }

            // Get the mouse position
            Vector2 mousePosition = mousePositionAction.ReadValue<Vector2>();

            // Create a ray
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // Perform ray detection
            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, detectableLayerMask))
            {
                GameObject hitObject = hit.collider.gameObject;

                // Find the root object (maybe the ray hits a child object)
                // Use the PlacedComponent type in the Services namespace
                PlacedComponent placedComponent = hitObject.GetComponent<PlacedComponent>();

                // If not found on the current object, try to find it on the parent object
                if (placedComponent == null)
                {
                    placedComponent = hitObject.GetComponentInParent<PlacedComponent>();
                }

                if (placedComponent != null)
                {
                    GameObject rootObject = placedComponent.gameObject;

                    // Save the currently detected object
                    currentDetectedObject = rootObject;
                }
            }
        }
    }
}