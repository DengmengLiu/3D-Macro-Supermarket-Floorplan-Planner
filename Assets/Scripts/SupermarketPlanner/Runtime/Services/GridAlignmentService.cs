using UnityEngine;
using SupermarketPlanner.Managers;

namespace SupermarketPlanner.Services
{
    /// <summary>
    /// Grid Alignment Service - responsible for handling object to grid alignment
    /// </summary>
    public class GridAlignmentService : MonoBehaviour
    {
        [Header("Grid Settings")]
        [Tooltip("Enable grid alignment")]
        public bool enableGridAlignment = true;

        [Tooltip("Grid switching threshold (prevents flickering when hovering at the grid boundary)")]
        public float gridSwitchThreshold = 0.1f;

        // Last snapped grid position
        private Vector3 lastSnappedPosition;

        // Delayed switching timer
        private float positionSwitchTimer = 0f;

        // Grid manager reference
        private GridManager gridManager;

        private void Start()
        {
            // Get grid manager reference
            gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogWarning("GridAlignmentService: GridManager not found, using default grid size");
            }
        }

        /// <summary>
        /// Align position to grid
        /// </summary>
        public Vector3 AlignToGrid(Vector3 worldPosition)
        {
            // If grid alignment is not enabled or grid manager is not available, just return to original position
            if (!enableGridAlignment || gridManager == null)
            {
                return worldPosition;
            }

            // Get position aligned to grid
            Vector3 snappedPosition = gridManager.SnapToGrid(worldPosition);

            // Check if we need to switch to new grid position
            bool shouldSwitchToNewPosition = ShouldSwitchToNewPosition(snappedPosition);

            // Update timer
            positionSwitchTimer += Time.deltaTime;

            if (shouldSwitchToNewPosition)
            {
                // Switch to new position
                lastSnappedPosition = snappedPosition;
                positionSwitchTimer = 0f;
                return snappedPosition;
            }
            else
            {
                // Stay at the last grid position
                return lastSnappedPosition;
            }
        }

        /// <summary>
        /// Decide whether to switch to a new grid position
        /// </summary>
        private bool ShouldSwitchToNewPosition(Vector3 newSnappedPosition)
        {
            // If it is the first snap, return true directly
            if (lastSnappedPosition == Vector3.zero)
            {
                return true;
            }

            // Calculate the distance from the last grid position
            float distance = Vector3.Distance(newSnappedPosition, lastSnappedPosition);

            // If the distance is large enough, switch immediately
            if (distance >= (gridManager?.gridCellSize ?? 1.0f))
            {
                return true;
            }

            // If the position changes but not much, use a timer to prevent frequent switching
            if (distance > 0)
            {
                // If the mouse has been in the same area for a long enough time, switch the position
                if (positionSwitchTimer > 0.3f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Set the grid alignment state
        /// </summary>
        public void SetGridAlignmentEnabled(bool enabled)
        {
            enableGridAlignment = enabled;

            // Reset the last alignment position and timer
            if (enabled)
            {
                lastSnappedPosition = Vector3.zero;
                positionSwitchTimer = 0f;
            }
        }

        /// <summary>
        /// Get the grid cell size
        /// </summary>
        public float GetGridCellSize()
        {
            return gridManager?.gridCellSize ?? 1.0f;
        }

        /// <summary>
        /// Reset alignment state
        /// </summary>
        public void ResetAlignment()
        {
            lastSnappedPosition = Vector3.zero;
            positionSwitchTimer = 0f;
        }

        /// <summary>
        /// Set the last aligned position
        /// </summary>
        public void SetLastSnappedPosition(Vector3 position)
        {
            lastSnappedPosition = position;
        }
    }
}