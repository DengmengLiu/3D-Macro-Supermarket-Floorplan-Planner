using UnityEngine;
using SupermarketPlanner.Data;
using SupermarketPlanner.Services;
using System;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// Placement Controller - Coordinates preview, grid snapping, and placement services
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("Service Reference")]
        [Tooltip("Preview Manager")]
        public PlacementPreviewManager previewManager;

        [Tooltip("Grid Alignment Service")]
        public GridAlignmentService gridAlignmentService;

        [Tooltip("New Simplified Placement Validation Service")]
        public PlacementValidationService validationService;

        [Tooltip("Component Placement Service")]
        public ComponentPlacementService placementService;

        [Header("Settings")]
        [Tooltip("Placement Layer Mask")]
        public LayerMask placementLayerMask;

        [Tooltip("Cursor when placement mode is active")]
        public Texture2D placementCursor;

        [Tooltip("Is grid snapping enabled")]
        public bool snapToGrid = true;

        // Placement state
        private bool isPlacementModeActive = false;
        private ComponentData currentComponent = null;

        // Input handler
        private PlacementInputHandler inputHandler;

        // Event
        public event Action<bool> OnPlacementModeChanged;
        public event Action<GameObject> OnComponentPlaced;

        private void Awake()
        {
            // Initialize input handler
            inputHandler = gameObject.AddComponent<PlacementInputHandler>();

            // Find or create necessary services
            FindOrCreateServices();
        }

        private void OnEnable()
        {
            // Setup input events
            SetupInputEvents();
        }

        void Start()
        {
            if (placementLayerMask.value == 0)
            {
                // Exclude grid layer, assuming you have created a layer named "Grid"
                placementLayerMask = ~(1 << LayerMask.NameToLayer("Grid"));
            }

            // Set validation service's allowed collision layer
            if (validationService != null)
            {
                // Set floor layer as collision-enabled layer
                int floorLayer = LayerMask.NameToLayer("Floor");
                if (floorLayer != -1)
                {
                    validationService.allowedCollisionLayers = 1 << floorLayer;
                }
            }
        }

        private void OnDisable()
        {
            // If in placement mode, exit placement mode
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// Find or create necessary services
        /// </summary>
        private void FindOrCreateServices()
        {
            // Find preview manager
            if (previewManager == null)
            {
                previewManager = GetComponent<PlacementPreviewManager>();
                if (previewManager == null)
                {
                    previewManager = gameObject.AddComponent<PlacementPreviewManager>();
                }
            }

            // Find grid alignment service
            if (gridAlignmentService == null)
            {
                gridAlignmentService = GetComponent<GridAlignmentService>();
                if (gridAlignmentService == null)
                {
                    gridAlignmentService = gameObject.AddComponent<GridAlignmentService>();
                }
            }

            // Find the new simplified placement validation service
            if (validationService == null)
            {
                validationService = GetComponent<PlacementValidationService>();
                if (validationService == null)
                {
                    validationService = gameObject.AddComponent<PlacementValidationService>();
                }
            }

            // Find component placement service
            if (placementService == null)
            {
                placementService = GetComponent<ComponentPlacementService>();
                if (placementService == null)
                {
                    placementService = gameObject.AddComponent<ComponentPlacementService>();
                }
            }
        }

        /// <summary>
        /// Set input events
        /// </summary>
        private void SetupInputEvents()
        {
            if (inputHandler == null)
                return;
            // Set input callback
            inputHandler.OnLeftClick += HandleLeftClick;
            inputHandler.OnRightClick += HandleRightClick;
            inputHandler.OnRotate += HandleRotate;
            inputHandler.OnCancel += HandleCancel;
        }

        private void Update()
        {
            // If not in placement mode, do not process
            if (!isPlacementModeActive)
                return;

            // Update preview
            UpdatePreview();

            // Use the new validation service to check whether it can be placed in real time
            CheckPlacementValidity();
        }

        /// <summary>
        /// Check placement validity in real time and update preview color
        /// </summary>
        private void CheckPlacementValidity()
        {
            if (previewManager == null || validationService == null)
                return;

            GameObject previewObject = previewManager.GetPreviewObject();
            if (previewObject == null)
                return;

            // Check if placement is possible using new validation service
            bool canPlace = validationService.CanPlace(previewObject);

            // Update the color of the preview object
            previewManager.UpdatePreviewColor(canPlace);
        }

        /// <summary>
        /// Start placement mode
        /// </summary>
        public void StartPlacement(ComponentData componentData)
        {
            if (componentData == null || componentData.prefab == null)
            {
                Debug.LogError("Unable to start placement: component data or prefab is empty");
                return;
            }

            // If already in placement mode, cancel first
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }

            // Save current component
            currentComponent = componentData;

            // Create preview
            previewManager.CreatePreview(componentData);

            // Set cursor
            SetPlacementCursor(true);

            // Enter placement mode
            isPlacementModeActive = true;

            // Trigger event
            OnPlacementModeChanged?.Invoke(true);

            // Debug.Log($"Start placement mode: {componentData.displayName}");
        }

        /// <summary>
        /// Update preview
        /// </summary>
        private void UpdatePreview()
        {
            // Update preview position
            previewManager.UpdatePreview();

            // Get preview position
            Vector3 previewPosition = previewManager.GetTargetPosition();

            // Apply grid alignment
            if (gridAlignmentService != null && snapToGrid)
            {
                previewPosition = gridAlignmentService.AlignToGrid(previewPosition);

                // Apply aligned position back to preview object
                GameObject previewObject = previewManager.GetPreviewObject();
                if (previewObject != null)
                {
                    previewObject.transform.position = previewPosition;
                }
            }
        }

        /// <summary>
        /// Handle left click
        /// </summary>
        private void HandleLeftClick()
        {
            if (!isPlacementModeActive)
                return;

            // Get preview object
            GameObject previewObject = previewManager.GetPreviewObject();
            if (previewObject == null)
                return;
            // Check if placement is possible using new validation service
            bool canPlace = validationService.CanPlace(previewObject);
            if (canPlace)
            {
                PlaceCurrentComponent();
            }
        }
        /// <summary>
        /// Handle right click
        /// </summary>
        private void HandleRightClick()
        {
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// Handle rotation key
        /// </summary>
        private void HandleRotate()
        {
            if (isPlacementModeActive)
            {
                RotatePreview();
            }
        }

        /// <summary>
        /// Handle cancel key
        /// </summary>
        private void HandleCancel()
        {
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// Place the current component
        /// </summary>
        private void PlaceCurrentComponent()
        {
            // Get the preview object
            GameObject previewObject = previewManager.GetPreviewObject();
            if (previewObject == null || currentComponent == null)
                return;

            // Get preview position and rotation
            Vector3 position = previewObject.transform.position;
            Quaternion rotation = previewObject.transform.rotation;

            // Place the component
            GameObject placedObject = placementService.PlaceComponent(currentComponent, position, rotation);

            if (placedObject != null)
            {
                // Set the placed object to the "PlacedObject" layer for collision detection
                int placedObjectLayer = LayerMask.NameToLayer("PlacedObject");
                if (placedObjectLayer != -1)
                {
                    SetLayerRecursively(placedObject, placedObjectLayer);
                }

                // Trigger the placement event
                OnComponentPlaced?.Invoke(placedObject);
                EndPlacementMode();
            }
        }

        /// <summary>
        /// Recursively set the layer of the game object and all its children
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
                return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Cancel placement
        /// </summary>
        public void CancelPlacement()
        {
            // Clear placement mode
            ClearPlacementMode();

            // Exit placement mode
            EndPlacementMode();
        }

        /// <summary>
        /// Clear placement mode
        /// </summary>
        private void ClearPlacementMode()
        {
            // Clear preview
            previewManager.ClearPreview();
        }

        /// <summary>
        /// Exit placement mode
        /// </summary>
        private void EndPlacementMode()
        {
            // Reset state
            isPlacementModeActive = false;
            currentComponent = null;

            // Restore cursor
            SetPlacementCursor(false);

            // Trigger event
            OnPlacementModeChanged?.Invoke(false);
        }

        /// <summary>
        /// Rotate preview
        /// </summary>
        private void RotatePreview()
        {
            // Rotate preview object
            previewManager.RotatePreview();
        }

        /// <summary>
        /// Set grid alignment state
        /// </summary>
        public void SetSnapToGrid(bool enabled)
        {
            snapToGrid = enabled;
        }

        /// <summary>
        /// Set placement cursor
        /// </summary>
        private void SetPlacementCursor(bool showPlacementCursor)
        {
            if (placementCursor != null && showPlacementCursor)
            {
                Cursor.SetCursor(placementCursor, new Vector2(placementCursor.width / 2, placementCursor.height / 2), CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        /// <summary>
        /// Check if it is in placement mode
        /// </summary>
        public bool IsInPlacementMode()
        {
            return isPlacementModeActive;
        }

        /// <summary>
        /// Get the currently selected component
        /// </summary>
        public ComponentData GetCurrentComponent()
        {
            return currentComponent;
        }
    }
}