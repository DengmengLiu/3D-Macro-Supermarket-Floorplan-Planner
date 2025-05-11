using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // 添加这个命名空间来使用EventSystem

public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 10f;        // 移动速度
    public float rotationSpeed = 50f;    // 旋转速度
    public float zoomSpeed = 15f;        // 缩放速度
    public float smoothTime = 0.1f;      // 平滑过渡时间

    [Header("边界设置")]
    public bool useBoundaries = true;    // 是否使用边界限制
    public float minHeight = 2f;         // 最小高度
    public float maxHeight = 50f;        // 最大高度
    public float groundOffset = 0.5f;    // 地面偏移量，防止摄像机进入地面
    public float boundaryMargin = 5f;    // 边界外的额外移动空间

    [Header("UI交互设置")]
    public bool ignoreInputOverUI = true; // 是否在UI上忽略输入

    // 地板参考和边界
    private GameObject floorObject;      // 地板对象引用
    private Vector3 floorSize;           // 地板尺寸
    private float minX, maxX, minZ, maxZ; // 移动边界

    // 输入系统引用
    private InputAction moveAction;      // 移动输入
    private InputAction rotateAction;    // 旋转输入
    private InputAction zoomAction;      // 缩放输入

    // 平滑移动变量
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentZoomVelocity = 0f;
    private float targetZoom = 0f;
    private float currentRotationVelocity = 0f;
    private float targetRotationY = 0f;

    // 摄像机引用
    private Camera cam;

    private void Awake()
    {
        // 获取摄像机引用
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
            Debug.LogWarning("CameraController: 未直接附加到Camera组件，使用主摄像机");
        }

        // 初始化目标位置和缩放
        targetZoom = cam.orthographic ? cam.orthographicSize : transform.position.y;
        targetRotationY = transform.eulerAngles.y;

        // 创建输入动作
        CreateInputActions();
    }

    private void CreateInputActions()
    {
        // 创建输入动作映射
        var actionMap = new InputActionMap("CameraControls");

        // 移动输入 (WASD/方向键)
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
            .With("Right", "<Keyboard>/rightArrow");

        // 旋转输入 (QE)
        rotateAction = actionMap.AddAction("Rotate");
        rotateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");

        // 缩放输入 (鼠标滚轮)
        zoomAction = actionMap.AddAction("Zoom", binding: "<Mouse>/scroll/y");

        // 启用所有输入动作
        moveAction.Enable();
        rotateAction.Enable();
        zoomAction.Enable();
    }

    private void OnDestroy()
    {
        // 销毁时禁用和释放输入动作
        moveAction?.Dispose();
        rotateAction?.Dispose();
        zoomAction?.Dispose();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
        
        // 应用平滑移动
        ApplySmoothMovement();
    }

    private void HandleMovement()
    {
        // 获取移动输入
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // 根据摄像机当前方向计算移动方向
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            
            // 计算目标位置
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x) * moveSpeed * Time.deltaTime;
            targetPosition += moveDirection;
        }
    }

    private void HandleRotation()
    {
        // 获取旋转输入
        float rotateInput = rotateAction.ReadValue<float>();
        
        if (Mathf.Abs(rotateInput) > 0.01f)
        {
            // 计算目标旋转
            targetRotationY += rotateInput * rotationSpeed * Time.deltaTime;
        }
    }

    private void HandleZoom()
    {
        // 如果鼠标在UI上，并且设置了忽略UI上的输入，则不处理缩放
        if (ignoreInputOverUI && IsPointerOverUI())
        {
            return;
        }
        
        // 获取缩放输入 (鼠标滚轮)
        float zoomInput = zoomAction.ReadValue<float>();
        
        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            // 计算目标缩放 (反向滚动)
            float zoomDelta = -zoomInput * zoomSpeed * Time.deltaTime;
            
            if (cam.orthographic)
            {
                // 正交相机使用正交大小
                targetZoom = Mathf.Clamp(targetZoom + zoomDelta, minHeight, maxHeight);
            }
            else
            {
                // 透视相机使用Y位置
                targetZoom = Mathf.Clamp(targetZoom + zoomDelta, minHeight, maxHeight);
            }
        }
    }

    private void ApplySmoothMovement()
    {
        // 应用平滑移动
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        
        // 应用平滑旋转
        float currentRotationY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotationY, ref currentRotationVelocity, smoothTime);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentRotationY, transform.eulerAngles.z);
        
        // 根据相机类型应用平滑缩放
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref currentZoomVelocity, smoothTime);
        }
        else
        {
            // 对于透视相机，调整Y位置作为缩放
            Vector3 position = transform.position;
            position.y = Mathf.SmoothDamp(position.y, targetZoom, ref currentZoomVelocity, smoothTime);
            transform.position = position;
        }
        
        // 应用边界限制
        if (useBoundaries)
        {
            ApplyBoundaries();
        }
    }

    private void ApplyBoundaries()
    {
        // 获取当前位置
        Vector3 position = transform.position;
        
        // 限制高度
        // 检测地面高度（如果有碰撞体）
        if (Physics.Raycast(new Vector3(position.x, maxHeight, position.z), Vector3.down, out RaycastHit hit))
        {
            float minAllowedHeight = hit.point.y + groundOffset;
            position.y = Mathf.Max(position.y, minAllowedHeight);
        }
        
        // 应用高度限制
        position.y = Mathf.Clamp(position.y, minHeight, maxHeight);
        
        // 如果地板已设置，应用XZ平面边界限制
        if (floorObject != null)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
        }
        
        // 更新目标位置和当前位置
        targetPosition = new Vector3(
            Mathf.Clamp(targetPosition.x, minX, maxX),
            targetPosition.y,
            Mathf.Clamp(targetPosition.z, minZ, maxZ)
        );
        transform.position = position;
    }

    // 设置边界限制的方法 (由FloorInitManager调用)
    public void SetBoundaries(GameObject floor, float width, float length)
    {
        floorObject = floor;
        floorSize = new Vector3(width, 0.1f, length);
        
        // 计算边界
        // 地板坐标系中心在中央，边界扩展boundaryMargin个单位
        minX = -width / 2 - boundaryMargin;
        maxX = width / 2 + boundaryMargin;
        minZ = -length / 2 - boundaryMargin;
        maxZ = length / 2 + boundaryMargin;
        
        Debug.Log($"Camera boundaries set: X({minX} to {maxX}), Z({minZ} to {maxZ})");
    }
    
    // 检查鼠标指针是否位于UI元素上
    private bool IsPointerOverUI()
    {
        // 检查当前是否有EventSystem
        if (EventSystem.current == null)
            return false;
            
        // 检查指针是否在UI元素上
        return EventSystem.current.IsPointerOverGameObject();
    }
}