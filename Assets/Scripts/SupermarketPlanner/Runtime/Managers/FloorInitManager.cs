using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SupermarketPlanner.Managers;

public class FloorInitManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiPanel; // Setting panel
    public TMP_InputField widthInput; // Width input box
    public TMP_InputField lengthInput; // Length input box
    public Button createButton; // Create button
    public TextMeshProUGUI errorText; // Error message text

    [Header("Floor Settings")]
    public GameObject floorPrefab; // Floor prefab
    public Material floorMaterial; // Floor material
    public float maxSize = 1000f; // Maximum size limit

    [Header("Wall Settings")]
    public float wallHeight = 2f; // Wall height
    public float wallThickness = 0.3f; // Wall thickness

    private GameObject currentFloor; // Currently created floor
    private List<GameObject> currentWalls = new List<GameObject>(); // Currently created wall
    private CameraController cameraController; // Camera controller reference

    void Start()
    {
        // Initialize error message
        if (errorText != null)
        {
            errorText.text = "";
        }

        // Add button click event
        if (createButton != null)
        {
            createButton.onClick.AddListener(CreateFloor);
        }

        // Check if there is a floor prefab
        if (floorPrefab == null)
        {
            // Create a default floor prefab
            floorPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorPrefab.name = "DefaultFloorPrefab";
            floorPrefab.transform.localScale = new Vector3(1, 0.1f, 1);
            floorPrefab.SetActive(false);
        }

        // Check if there is a material
        if (floorMaterial == null && floorPrefab.GetComponent<Renderer>() != null)
        {
            floorMaterial = floorPrefab.GetComponent<Renderer>().material;
        }

        // Get a reference to the camera controller
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("CameraController component not found on main camera");
            }
        }
    }

    public void CreateFloor()
    {
        // Get input value
        if (!float.TryParse(widthInput.text, out float width) ||
        !float.TryParse(lengthInput.text, out float length))
        {
            ShowError("Please enter a valid number");
            return;
        }

        // Check size limits
        if (width <= 0 || length <= 0)
        {
            ShowError("Please enter a value greater than 0");
            return;
        }

        if (width > maxSize || length > maxSize)
        {
            ShowError($"The size cannot exceed {maxSize} meters");
            return;
        }

        // If there are already floors and walls, delete them first
        if (currentFloor != null)
        {
            Destroy(currentFloor);
        }

        foreach (GameObject wall in currentWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
        currentWalls.Clear();

        // Create a new floor
        currentFloor = Instantiate(floorPrefab, Vector3.zero, Quaternion.identity);
        currentFloor.SetActive(true);
        currentFloor.name = "SupermarketFloor";

        // Resize the floor
        currentFloor.transform.localScale = new Vector3(width, 0.1f, length);

        // Apply the material if it exists
        if (floorMaterial != null)
        {
            Renderer renderer = currentFloor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = floorMaterial;

                // Adjust the material texture scale to match the floor size
                renderer.material.mainTextureScale = new Vector2(width, length);
            }
        }

        // Create the walls
        CreateWalls(width, length);

        // Hide the UI panel
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        // Adjust the camera position to see the entire floor
        AdjustCameraPosition(width, length);

        // Set the camera bounds
        SetCameraBoundaries(width, length);

        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.SetFloor(currentFloor, width, length);
        }
        else
        {
            Debug.LogWarning("GridManager instance not found.");
        }

        // Debug.Log($"Floor created, size: {width} x {length}"); 
    }

    private void CreateWalls(float width, float length)
    {
        float halfWidth = width / 2;
        float halfLength = length / 2;
        float floorHeight = 0.1f;
        float wallY = floorHeight / 2 + wallHeight / 2;

        Vector3[] positions = new Vector3[]
        {
new Vector3(0, wallY, halfLength + wallThickness / 2), // North 
new Vector3(0, wallY, -halfLength - wallThickness / 2), // South 
new Vector3(halfWidth + wallThickness / 2, wallY, 0), // East 
new Vector3(-halfWidth - wallThickness / 2, wallY, 0), // West 
        };

        Vector3[] scales = new Vector3[]
        {
new Vector3(width + wallThickness * 2, wallHeight, wallThickness), // North/South
new Vector3(width + wallThickness * 2, wallHeight, wallThickness),
new Vector3(wallThickness, wallHeight, length), // East/West
new Vector3(wallThickness, wallHeight, length),
        };

        string[] names = { "NorthWall", "SouthWall", "EastWall", "WestWall" };

        for (int i = 0; i < 4; i++)
        {
            GameObject wall = Instantiate(floorPrefab, positions[i], Quaternion.identity); // 使用floorPrefab
            wall.name = names[i];
            wall.transform.localScale = scales[i];
            wall.layer = LayerMask.NameToLayer("PlacedObject");
            wall.SetActive(true);

            if (floorMaterial != null)
            {
                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = floorMaterial;
            }

            if (wall.GetComponent<Collider>() == null)
                wall.AddComponent<BoxCollider>();

            currentWalls.Add(wall);
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
        else
        {
            Debug.LogError(message);
        }
    }

    private void AdjustCameraPosition(float width, float length)
    {
        // Get the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        // Calculate the maximum size
        float maxDimension = Mathf.Max(width, length);

        // Calculate the camera height and distance
        float cameraHeight = maxDimension * 0.5f;
        float cameraDistance = maxDimension * 0.5f;

        // Set the camera position and rotation
        mainCamera.transform.position = new Vector3(width / 2, cameraHeight, -cameraDistance);
        mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
    }

    private void SetCameraBoundaries(float width, float length)
    {
        // If the camera controller is found, set the boundaries
        if (cameraController != null && currentFloor != null)
        {
            cameraController.SetBoundaries(currentFloor, width, length);
        }
    }
}