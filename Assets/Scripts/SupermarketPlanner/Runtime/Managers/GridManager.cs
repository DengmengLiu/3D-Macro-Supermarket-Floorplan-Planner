using UnityEngine;
using System.Collections.Generic;

namespace SupermarketPlanner.Managers
{
    /// <summary>
    /// Grid Manager - responsible for creating and managing grids in the scene
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [Tooltip("Grid Cell Size (meters)")]
        public float gridCellSize = 1.0f;

        [Tooltip("Is the grid visible")]
        public bool gridVisible = true;

        [Tooltip("Grid Line Color")]
        public Color gridColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        [Tooltip("Main axis color (every 5 or 10 units)")]
        public Color majorGridColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

        [Tooltip("Draw the main axis every x units")]
        public int majorGridInterval = 5;

        [Header("Grid component")]
        [Tooltip("Grid line prefab")]
        public GameObject gridLinePrefab;

        // Grid line object pool
        private List<GameObject> horizontalLines = new List<GameObject>();
        private List<GameObject> verticalLines = new List<GameObject>();

        // Current floor reference
        private GameObject floorObject;
        private Vector3 floorSize;
        private Vector3 floorPosition;

        // Grid origin - add this variable to store the lower left corner of the grid
        private Vector3 gridOrigin;

        // Singleton instance
        public static GridManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton mode setting
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // Initialize the grid line prefab
            if (gridLinePrefab == null)
            {
                CreateDefaultLinePrefab();
            }
        }

        /// <summary>
        /// Create a default line prefab
        /// </summary>
        private void CreateDefaultLinePrefab()
        {
            gridLinePrefab = new GameObject("GridLine");

            // Add a line renderer component
            LineRenderer lineRenderer = gridLinePrefab.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = gridColor;
            lineRenderer.endColor = gridColor;
            lineRenderer.positionCount = 2;

            // Set the prefab to invisible
            gridLinePrefab.SetActive(false);
        }

        /// <summary>
        /// Set the floor reference and create the grid
        /// </summary>
        public void SetFloor(GameObject floor, float width, float length)
        {
            floorObject = floor;
            floorSize = new Vector3(width, 0.1f, length);
            floorPosition = floor.transform.position;

            // Create the grid using the current settings
            CreateGrid();
        }

        /// <summary>
        /// Create the grid
        /// </summary>
        public void CreateGrid()
        {
            if (floorObject == null)
            {
                Debug.LogWarning("GridManager: Floor reference not set, can't create grid");
                return;
            }

            // Clear the existing grid first
            ClearGrid();

            // Calculate the floor bounds - make sure to use the exact bounds
            Vector3 floorMin = floorPosition - new Vector3(floorSize.x, 0, floorSize.z) / 2f;
            Vector3 floorMax = floorPosition + new Vector3(floorSize.x, 0, floorSize.z) / 2f;
            // Store grid origin (lower left corner)
            gridOrigin = floorMin;

            // Set the grid height (slightly above the floor surface)
            float gridHeight = floorPosition.y + 0.05f;

            // Modify the grid generation logic to ensure that the grid lines are only drawn within the floor range
            // First calculate how many grid cells are within the floor range
            int horizontalCellCount = Mathf.FloorToInt(floorSize.z / gridCellSize) + 1;
            int verticalCellCount = Mathf.FloorToInt(floorSize.x / gridCellSize) + 1;

            // Create a horizontal line (Z-axis direction, from front to back)
            for (int i = 0; i <= horizontalCellCount; i++)
            {
                float z = floorMin.z + i * gridCellSize;

                // Make sure it does not exceed the floor boundary
                if (z > floorMax.z)
                    continue;

                // Calculate the grid line index to determine whether it is the major axis
                bool isMajor = i % majorGridInterval == 0;

                // The start and end points of the created line segment should be within the floor range
                Vector3 startPoint = new Vector3(floorMin.x, gridHeight, z);
                Vector3 endPoint = new Vector3(floorMax.x, gridHeight, z);

                // Create a grid line
                GameObject line = CreateGridLine(startPoint, endPoint, isMajor ? majorGridColor : gridColor);
                horizontalLines.Add(line);
            }

            // Create a vertical line (X-axis direction, from left to right)
            for (int i = 0; i <= verticalCellCount; i++)
            {
                float x = floorMin.x + i * gridCellSize;

                // Make sure it does not exceed the floor boundary
                if (x > floorMax.x)
                    continue;

                // Calculate the grid line index to determine whether it is the main axis
                bool isMajor = i % majorGridInterval == 0;

                // The start and end points of the created line segment should be within the floor range
                Vector3 startPoint = new Vector3(x, gridHeight, floorMin.z);
                Vector3 endPoint = new Vector3(x, gridHeight, floorMax.z);

                // Create a grid line
                GameObject line = CreateGridLine(startPoint, endPoint, isMajor ? majorGridColor : gridColor);
                verticalLines.Add(line);
            }

            // Set grid visibility
            SetGridVisibility(gridVisible);
        }

        /// <summary>
        /// Create a single grid line
        /// </summary>
        private GameObject CreateGridLine(Vector3 start, Vector3 end, Color color)
        {
            GameObject line = Instantiate(gridLinePrefab, floorObject.transform);
            line.SetActive(true);
            line.name = "GridLine";

            // Set the layer to a dedicated "Grid" layer
            int gridLayer = LayerMask.NameToLayer("Grid");
            if (gridLayer != -1)
            {
                line.layer = gridLayer;
            }

            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            return line;
        }

        /// <summary> 
        /// Clean up the grid 
        /// </summary> 
        public void ClearGrid()
        {
            // Destroy the horizontal line 
            foreach (GameObject line in horizontalLines)
            {
                if (line != null)
                {
                    Destroy(line);
                }
            }
            horizontalLines.Clear();

            // Destroy the vertical line 
            foreach (GameObject line in verticalLines)
            {
                if (line != null)
                {
                    Destroy(line);
                }
            }
            verticalLines.Clear();
        }

        /// <summary>
        /// Set the grid cell size
        /// </summary>
        public void SetGridCellSize(float size)
        {
            // Make sure the size is within the valid range
            gridCellSize = Mathf.Clamp(size, 0.1f, 10f);

            // Recreate the grid
            CreateGrid();
        }

        /// <summary>
        /// Set grid visibility
        /// </summary>
        public void SetGridVisibility(bool visible)
        {
            gridVisible = visible;

            // Set the visibility of all grid lines
            foreach (GameObject line in horizontalLines)
            {
                if (line != null)
                {
                    line.SetActive(visible);
                }
            }

            foreach (GameObject line in verticalLines)
            {
                if (line != null)
                {
                    line.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Snap world coordinates to grid
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            // Calculate the offset of the object's position relative to the grid origin (lower left corner)
            float xOffset = worldPosition.x - gridOrigin.x;
            float zOffset = worldPosition.z - gridOrigin.z;

            // Round the offset to the nearest grid unit
            float xGridOffset = Mathf.Round(xOffset / gridCellSize) * gridCellSize;
            float zGridOffset = Mathf.Round(zOffset / gridCellSize) * gridCellSize;

            // Convert the grid offset back to world coordinates
            float x = gridOrigin.x + xGridOffset;
            float z = gridOrigin.z + zGridOffset;

            // Make sure the position is within the floor
            Vector3 floorMin = floorPosition - floorSize / 2;
            Vector3 floorMax = floorPosition + floorSize / 2;

            x = Mathf.Clamp(x, floorMin.x, floorMax.x);
            z = Mathf.Clamp(z, floorMin.z, floorMax.z);

            return new Vector3(x, worldPosition.y, z);
        }
    }
}