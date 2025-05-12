using UnityEngine;

namespace SupermarketPlanner.Services
{
    /// <summary>
    /// Simple Placement Validation Service - Use Physics.OverlapBox to detect collisions directly
    /// </summary>
    public class PlacementValidationService : MonoBehaviour
    {
        [Header("Validation Settings")]
        [Tooltip("Allowed Collision Layers (which layers objects can collide with but still allow placement, such as floor layers)")]
        public LayerMask allowedCollisionLayers;

        [Tooltip("Debug Mode - Show Collision Detection Area")]
        public bool showDebugVisuals = false;

        [Tooltip("Collision Detection Outside Margin (meters)")]
        public float collisionMargin = 0.05f;

        // The bounds of the last check (for debug visualization)
        private Bounds lastCheckedBounds;
        private bool lastCheckResult = true;

        private void Start()
        {
            // Make sure the floor layer is added to the allowed collision layers
            if (allowedCollisionLayers.value == 0)
            {
                // Assuming the floor uses the "Floor" layer
                int floorLayer = LayerMask.NameToLayer("Floor");
                if (floorLayer != -1)
                {
                    allowedCollisionLayers = 1 << floorLayer;
                    // Debug.Log($"Automatically set allowed collision layer to Floor layer (Layer {floorLayer})");
                }
                else
                {
                    Debug.LogWarning("Floor layer not found, please manually set allowed collision layer");
                }
            }
        }

        /// <summary>
        /// Check if the object can be placed at the specified location
        /// </summary>
        public bool CanPlace(GameObject objectToPlace)
        {
            if (objectToPlace == null)
                return false;

            // Calculate the bounding box of the object
            Bounds bounds = GetObjectBounds(objectToPlace);

            // Expand bounds slightly to ensure more accurate collision detection
            bounds.Expand(collisionMargin);

            // Save for debug drawing
            lastCheckedBounds = bounds;

            // Check if there is a collision within the bounding box area
            bool isAreaClear = IsAreaClear(bounds, objectToPlace.transform.rotation);

            // Save result for debugging
            lastCheckResult = isAreaClear;

            return isAreaClear;
        }

        /// <summary>
        /// Check if the specified area has no collisions
        /// </summary>
        private bool IsAreaClear(Bounds bounds, Quaternion rotation)
        {
            // Calculate half extents (Physics.OverlapBox uses half extents)
            Vector3 halfExtents = bounds.extents;

            // Create a layer mask that does not include allowed layers (~ operator is bitwise inversion)
            int layerMask = ~allowedCollisionLayers;

            // Use Physics.OverlapBox to detect colliders within the area
            Collider[] colliders = Physics.OverlapBox(
            bounds.center, // center point
            halfExtents, // half extents
            rotation, // rotation
            layerMask, // layer mask
            QueryTriggerInteraction.Ignore // Ignore triggers
            );

            // If no collider is found, the region is empty
            return colliders.Length == 0;
        }

        /// <summary>
        /// Get the bounding box of the object
        /// </summary>
        private Bounds GetObjectBounds(GameObject obj)
        {
            // Get all renderers
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            // If there is a renderer, use the renderer's bounds
            if (renderers.Length > 0)
            {
                // Start with the first renderer
                Bounds bounds = renderers[0].bounds;

                // Merge all other renderers' bounds
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return bounds;
            }

            // If no renderer, try to use a collider
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;

                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }

                return bounds;
            }

            // If no renderer or collider, use the object's transform information
            return new Bounds(obj.transform.position, obj.transform.lossyScale);
        }

        /// <summary>
        /// Visualize collision detection areas (only in debug mode)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugVisuals || !Application.isPlaying)
                return;

            // Set color based on last check result
            Gizmos.color = lastCheckResult ? Color.green : Color.red;

            // Use wireframe to represent detected bounding box
            Gizmos.DrawWireCube(lastCheckedBounds.center, lastCheckedBounds.size);

            // Fill with semi-transparent cube
            Color fillColor = lastCheckResult ?
            new Color(0, 1, 0, 0.2f) : // Green semi-transparent
            new Color(1, 0, 0, 0.2f); // Red semi-transparent

            Gizmos.color = fillColor;
            Gizmos.DrawCube(lastCheckedBounds.center, lastCheckedBounds.size);
        }
    }
}