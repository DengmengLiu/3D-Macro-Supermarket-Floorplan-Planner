using UnityEngine;
using UnityEngine.InputSystem;
using SupermarketPlanner.Data;
using System;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// Placement Preview Manager - responsible for managing the preview display of component placement
    /// </summary>
    public class PlacementPreviewManager : MonoBehaviour
    {
        [Header("Preview Settings")]
        [Tooltip("Material of Preview Object")]
        public Material previewMaterial;

        [Tooltip("Color of valid placement")]
        public Color validPlacementColor = new Color(0, 1, 0, 0.5f);

        [Tooltip("Color of invalid placement")]
        public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);

        [Tooltip("Placement layer mask")]
        public LayerMask placementLayerMask;

        [Tooltip("Placement offset based on floor thickness")]
        public float heightOffset = 0.05f; // This will be half the floor thickness

        [Header("Floor Reference")]
        [Tooltip("Floor Object Reference")]
        public GameObject floorObject;

        // Preview object
        private GameObject previewObject;

        // Component data
        private ComponentData componentData;

        // Rotation angle
        private float previewRotation = 0f;

        // Input handler
        private InputAction mousePositionAction;

        private void Awake()
        {
            // Get floor object
            if (floorObject == null)
            {
                floorObject = GameObject.FindWithTag("Floor");
                if (floorObject == null)
                {
                    Debug.LogWarning("PlacementPreviewManager: Floor object not found, using the scene object named 'SupermarketFloor'");
                    floorObject = GameObject.Find("SupermarketFloor");
                }
            }

            // Create a material
            if (previewMaterial == null)
            {
                // Create a simple translucent material
                previewMaterial = new Material(Shader.Find("Standard"));
                previewMaterial.color = validPlacementColor;
                previewMaterial.SetFloat("_Mode", 3); // Transparency mode
                previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMaterial.SetInt("_ZWrite", 0);
                previewMaterial.DisableKeyword("_ALPHATEST_ON");
                previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                previewMaterial.renderQueue = 3000;
            }

            // Create mouse position input action
            mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");
            mousePositionAction.Enable();
        }

        /// <summary>
        /// Create preview object
        /// </summary>
        public void CreatePreview(ComponentData component)
        {
            // Save component data
            componentData = component;

            // Make sure the previous preview is cleared
            ClearPreview();

            if (component != null && component.prefab != null)
            {
                try
                {
                    // Create preview object
                    previewObject = Instantiate(component.prefab);
                    previewObject.name = "Preview_" + component.displayName;

                    // Set as child object
                    previewObject.transform.SetParent(transform);

                    // Apply preview material
                    ApplyPreviewMaterial(previewObject);

                    // Disable colliders and any scripts
                    DisableComponents(previewObject);

                    // Set initial rotation
                    previewRotation = 0f;
                    previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);

                    // Set preview object to a special layer to ensure it does not conflict with collision detection
                    int previewLayer = LayerMask.NameToLayer("Ignore Raycast");
                    if (previewLayer != -1)
                    {
                        SetLayerRecursively(previewObject, previewLayer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error creating preview: {ex.Message}");
                    ClearPreview();
                }
            }
        }

        /// <summary>
        /// Recursively set the layers of a game object and all its children
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
        /// Apply preview material
        /// </summary>
        private void ApplyPreviewMaterial(GameObject obj)
        {
            // Get all renderers
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"Preview object '{obj.name}' has no renderer component");
                return;
            }

            // Save original material for restoration
            foreach (Renderer renderer in renderers)
            {
                // Make a copy of the preview material and apply 
                Material previewMaterialInstance = new Material(previewMaterial);

                // If the original material has a main texture, keep it
                if (renderer.material.mainTexture != null)
                {
                    previewMaterialInstance.mainTexture = renderer.material.mainTexture;
                }

                // Apply preview material
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterialInstance;
                }
                renderer.materials = materials;
            }
        }

        /// <summary>
        /// Disable components on an object
        /// </summary>
        private void DisableComponents(GameObject obj)
        {
            // Disable all colliders
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            // Disable all non-essential components
            MonoBehaviour[] behaviors = obj.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour behavior in behaviors)
            {
                // Disable all scripts except Transform and necessary visualization components
                behavior.enabled = false;
            }
        }

        /// <summary>
        /// Update preview position and status
        /// </summary>
        public void UpdatePreview()
        {
            if (previewObject == null)
                return;

            // Get mouse position
            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            Vector3 worldPos = GetWorldPosition(mousePos);

            // Set preview object position
            previewObject.transform.position = worldPos;

            // Set preview object rotation
            previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);
        }

        /// <summary>
        /// Get world coordinates
        /// </summary>
        private Vector3 GetWorldPosition(Vector2 screenPosition)
        {
            // Get the main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found");
                return Vector3.zero;
            }

            // Convert screen coordinates to rays
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            // Replace with a fixed height method here
            // Get the intersection of the ray and the XZ plane
            float t = 0;
            if (floorObject != null)
            {
                // If there is a floor object, use its Y coordinate plus the offset
                // Assume that the floor coordinate is the center point, so add half the thickness of the floor (ie heightOffset)
                float floorY = floorObject.transform.position.y + heightOffset;

                // Calculate the intersection of the ray and the XZ plane (Y=floorY)
                if (ray.direction.y != 0)
                {
                    t = (floorY - ray.origin.y) / ray.direction.y;
                }
            }
            else
            {
                // If there is no floor object, use the fixed Y=0 plane
                if (ray.direction.y != 0)
                {
                    t = (heightOffset - ray.origin.y) / ray.direction.y;
                }
            }

            if (t < 0)
            {
                // Ray is facing away from the floor, return a default position
                return new Vector3(0, heightOffset, 0);
            }

            // Get the intersection coordinates
            Vector3 worldPos = ray.origin + ray.direction * t;

            return worldPos;
        }

        /// <summary>
        /// Update preview color
        /// </summary>
        public void UpdatePreviewColor(bool canPlace)
        {
            if (previewObject == null)
                return;

            // Get all renderers
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();

            // Set the color based on whether it can be placed
            Color color = canPlace ? validPlacementColor : invalidPlacementColor;

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    material.color = color;
                }
            }
        }

        /// <summary>
        /// Rotate preview
        /// </summary>
        public void RotatePreview()
        {
            if (previewObject == null)
                return;

            // Rotate 90 degrees
            previewRotation = (previewRotation + 90) % 360;
            previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);
        }

        /// <summary>
        /// Clear preview
        /// </summary>
        public void ClearPreview()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }

        /// <summary> 
        /// Get preview object 
        /// </summary> 
        public GameObject GetPreviewObject()
        {
            return previewObject;
        }

        /// <summary> 
        /// Get the target location 
        /// </summary> 
        public Vector3 GetTargetPosition()
        {
            return previewObject?.transform.position ?? Vector3.zero;
        }
    }
}