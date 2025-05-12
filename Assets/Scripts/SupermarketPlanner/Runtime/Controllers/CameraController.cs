using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Add this namespace to use EventSystem

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f; // Movement speed
    public float rotationSpeed = 50f; // Rotation speed
    public float zoomSpeed = 15f; // Zoom speed
    public float smoothTime = 0.1f; // Smooth transition time

    [Header("Boundary Settings")]
    public bool useBoundaries = true; // Whether to use boundary restrictions
    public float minHeight = 2f; // Minimum height
    public float maxHeight = 50f; // Maximum height
    public float groundOffset = 0.5f; // Ground offset to prevent the camera from entering the ground
    public float boundaryMargin = 5f; // Extra moving space outside the boundary

    [Header("UI Interaction Settings")]
    public bool ignoreInputOverUI = true; // Whether to ignore input on the UI

    // Floor reference and boundary
    private GameObject floorObject; // Floor object reference
    private Vector3 floorSize; // Floor size
    private float minX, maxX, minZ, maxZ; // Move boundary

    // Input system reference
    private InputAction moveAction; // Move input
    private InputAction rotateAction; // Rotate input
    private InputAction zoomAction; // Zoom input

    // Smooth movement variables
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentZoomVelocity = 0f;
    private float targetZoom = 0f;
    private float currentRotationVelocity = 0f;
    private float targetRotationY = 0f;

    // Camera reference
    private Camera cam;

    private void Awake()
    {
        // Get camera reference
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
            Debug.LogWarning("CameraController: Not directly attached to Camera component, use main camera");
        }

        // Initialize target position and zoom
        targetZoom = cam.orthographic ? cam.orthographicSize : transform.position.y;
        targetRotationY = transform.eulerAngles.y;

        // Create input actions
        CreateInputActions();
    }

    private void CreateInputActions()
    {
        // Create input action map
        var actionMap = new InputActionMap("CameraControls");

        // Move input (WASD/arrow keys)
        moveAction = actionMap.AddAction("Move", binding: "<Keyboard>/w,<Keyboard>/s,<Keyboard>/a,<Keyboard>/d,<Keyboard>/upArrow,<Keyboard>/downArrow,<Keyboard>/leftArrow,<Keyboard>/rightArrow");
        moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/w")
        .With("Down", "<Keyboard>/s")
        .With("Left", "<Keyboard>/a")
        .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/upArrow")
        .With("Down", "<Keyboard>/downArrow")
        .With("Left", "<Keyboard>/leftArrow")
        .With("Right", "<Keyboard>/rightArrow"); // Rotation input (QE)
        rotateAction = actionMap.AddAction("Rotate");
        rotateAction.AddCompositeBinding("1DAxis")
        .With("Negative", "<Keyboard>/q")
        .With("Positive", "<Keyboard>/e");

        // Zoom input (mouse wheel)
        zoomAction = actionMap.AddAction("Zoom", binding: "<Mouse>/scroll/y");

        // Enable all input actions
        moveAction.Enable();
        rotateAction.Enable();
        zoomAction.Enable();
    }

    private void OnDestroy()
    {
        // Disable and release input actions on destruction
        moveAction?.Dispose();
        rotateAction?.Dispose();
        zoomAction?.Dispose();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();

        // Apply smooth movement
        ApplySmoothMovement();
    }

    private void HandleMovement()
    {
        // Get movement input
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Calculate movement direction based on camera's current direction
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;

            // Calculate target position
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x) * moveSpeed * Time.deltaTime;
            targetPosition += moveDirection;
        }
    }

    private void HandleRotation()
    {
        // Get rotation input
        float rotateInput = rotateAction.ReadValue<float>();

        if (Mathf.Abs(rotateInput) > 0.01f)
        {
            // Calculate target rotation
            targetRotationY += rotateInput * rotationSpeed * Time.deltaTime;
        }
    }

    private void HandleZoom()
    {
        // If the mouse is on the UI and the input on the UI is set to be ignored, zoom is not processed
        if (ignoreInputOverUI && IsPointerOverUI())
        {
            return;
        }

        // Get zoom input (mouse wheel)
        float zoomInput = zoomAction.ReadValue<float>();

        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            // Calculate target zoom (reverse scrolling)
            float zoomDelta = -zoomInput * zoomSpeed * Time.deltaTime;

            if (cam.orthographic)
            {
                // Orthogonal camera uses orthogonal size
                targetZoom = Mathf.Clamp(targetZoom + zoomDelta, minHeight, maxHeight);
            }
            else
            {
                //Perspective camera uses Y position
                targetZoom = Mathf.Clamp(targetZoom + zoomDelta, minHeight, maxHeight);
            }
        }
    }

    private void ApplySmoothMovement()
    {
        // Apply smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // Apply smooth rotation
        float currentRotationY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotationY, ref currentRotationVelocity, smoothTime);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentRotationY, transform.eulerAngles.z);

        // Apply smooth scaling based on camera type
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref currentZoomVelocity, smoothTime);
        }
        else
        {
            // For perspective camera, adjust Y position as zoom
            Vector3 position = transform.position;
            position.y = Mathf.SmoothDamp(position.y, targetZoom, ref currentZoomVelocity, smoothTime);
            transform.position = position;
        }

        // Apply boundary limits
        if (useBoundaries)
        {
            ApplyBoundaries();
        }
    }

    private void ApplyBoundaries()
    {
        // Get current position
        Vector3 position = transform.position;

        // Limit height
        // Check ground height (if there is a collision body)
        if (Physics.Raycast(new Vector3(position.x, maxHeight, position.z), Vector3.down, out RaycastHit hit))
        {
            float minAllowedHeight = hit.point.y + groundOffset;
            position.y = Mathf.Max(position.y, minAllowedHeight);
        }

        // Apply height constraint
        position.y = Mathf.Clamp(position.y, minHeight, maxHeight);

        // If floor is set, apply XZ plane bounds constraint
        if (floorObject != null)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
        }

        // Update target and current positions
        targetPosition = new Vector3(
        Mathf.Clamp(targetPosition.x, minX, maxX),
        targetPosition.y,
        Mathf.Clamp(targetPosition.z, minZ, maxZ)
        );
        transform.position = position;
    }

    // Method to set bounds (called by FloorInitManager)
    public void SetBoundaries(GameObject floor, float width, float length)
    {
        floorObject = floor;
        floorSize = new Vector3(width, 0.1f, length);

        // Calculate the boundary
        // The center of the floor coordinate system is in the center, and the boundary extends boundaryMargin units
        minX = -width / 2 - boundaryMargin;
        maxX = width / 2 + boundaryMargin;
        minZ = -length / 2 - boundaryMargin;
        maxZ = length / 2 + boundaryMargin;

        // Debug.Log($"Camera boundaries set: X({minX} to {maxX}), Z({minZ} to {maxZ})");
    }

    // Check if the mouse pointer is over a UI element
    private bool IsPointerOverUI()
    {
        // Check if there is an EventSystem
        if (EventSystem.current == null)
            return false;

        // Check if the pointer is over a UI element
        return EventSystem.current.IsPointerOverGameObject();
    }
}