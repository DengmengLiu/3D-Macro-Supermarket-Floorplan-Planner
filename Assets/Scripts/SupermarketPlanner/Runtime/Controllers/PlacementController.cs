using UnityEngine;
using UnityEngine.InputSystem;
using SupermarketPlanner.Data;
using SupermarketPlanner.Managers;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 放置控制器 - 处理组件的放置、移动和旋转
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("放置设置")]
        [Tooltip("组件放置高度")]
        public float placementHeight = 0.0f;
        
        [Tooltip("是否使用网格对齐")]
        public bool snapToGrid = true;
        
        [Tooltip("组件预览材质")]
        public Material previewMaterial;
        
        [Tooltip("允许的放置高度误差")]
        public float heightTolerance = 0.1f;
        
        [Tooltip("地板标签 - 用于确定是否在地板上")]
        public string floorTag = "Floor";
        
        [Tooltip("边界检查点的边缘偏移")]
        public float boundaryMargin = 0.05f;
        
        [Tooltip("位置平滑速度 (越高越平滑，但响应越慢)")]
        public float positionSmoothSpeed = 15f;
        
        [Tooltip("网格吸附阈值 (防止在网格边界徘徊时闪烁)")]
        public float gridSnapThreshold = 0.1f;
        
        [Header("鼠标设置")]
        [Tooltip("鼠标射线检测层")]
        public LayerMask placementLayerMask;
        
        // 当前选中的组件
        private ComponentData selectedComponent;
        
        // 预览对象
        private GameObject previewObject;
        
        // 平滑位置过渡
        private Vector3 targetPosition;
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 lastSnappedPosition;
        private float positionSwitchTimer = 0f;
        
        // 放置状态
        private bool isPlacementMode = false;
        private bool canPlace = false;
        private bool isOverFloor = false;
        
        // 引用
        private Camera mainCamera;
        private GridManager gridManager;
        
        // Input System 引用
        private InputAction mousePositionAction;
        private InputAction leftMouseAction;
        private InputAction rightMouseAction;
        private InputAction rotateAction;
        private InputAction cancelAction;
        private InputAction continuousPlacementAction;
        
        // 当前地板参考
        private GameObject currentFloor;
        private Bounds floorBounds;
        
        // 当前预览对象的边界点
        private Vector3[] previewBoundaryPoints;
        
        private void Awake()
        {
            // 创建输入动作
            CreateInputActions();
        }
        
        private void OnEnable()
        {
            // 启用输入动作
            EnableInputActions();
        }
        
        private void OnDisable()
        {
            // 禁用输入动作
            DisableInputActions();
        }
        
        private void CreateInputActions()
        {
            // 创建输入动作映射
            var actionMap = new InputActionMap("PlacementControls");
            
            // 鼠标位置
            mousePositionAction = actionMap.AddAction("MousePosition", binding: "<Mouse>/position");
            
            // 鼠标按钮
            leftMouseAction = actionMap.AddAction("LeftMouse", binding: "<Mouse>/leftButton");
            rightMouseAction = actionMap.AddAction("RightMouse", binding: "<Mouse>/rightButton");
            
            // 旋转键
            rotateAction = actionMap.AddAction("Rotate", binding: "<Keyboard>/r");
            
            // 取消键
            cancelAction = actionMap.AddAction("Cancel", binding: "<Keyboard>/escape");
            
            // Shift键（用于连续放置）
            continuousPlacementAction = actionMap.AddAction("ContinuousPlacement");
            continuousPlacementAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Keyboard>/shift")
                .With("Button", "<Mouse>/leftButton");
        }
        
        private void EnableInputActions()
        {
            mousePositionAction.Enable();
            leftMouseAction.Enable();
            rightMouseAction.Enable();
            rotateAction.Enable();
            cancelAction.Enable();
            continuousPlacementAction.Enable();
            
            // 添加回调
            leftMouseAction.performed += OnLeftMousePerformed;
            rightMouseAction.performed += OnRightMousePerformed;
            rotateAction.performed += OnRotatePerformed;
            cancelAction.performed += OnCancelPerformed;
        }
        
        private void DisableInputActions()
        {
            mousePositionAction.Disable();
            leftMouseAction.Disable();
            rightMouseAction.Disable();
            rotateAction.Disable();
            cancelAction.Disable();
            continuousPlacementAction.Disable();
            
            // 移除回调
            leftMouseAction.performed -= OnLeftMousePerformed;
            rightMouseAction.performed -= OnRightMousePerformed;
            rotateAction.performed -= OnRotatePerformed;
            cancelAction.performed -= OnCancelPerformed;
        }
        
        private void Start()
        {
            // 获取摄像机引用
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("PlacementController: 未找到主摄像机");
                enabled = false;
                return;
            }
            
            // 获取网格管理器引用
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("PlacementController: 未找到GridManager，将禁用网格对齐功能");
            }
            
            // 设置默认层掩码（如果未设置）
            if (placementLayerMask.value == 0)
            {
                placementLayerMask = LayerMask.GetMask("Default");
            }
            
            // 查找地板对象
            FindFloor();
        }
        
        /// <summary>
        /// 查找场景中的地板
        /// </summary>
        private void FindFloor()
        {
            // 查找带有地板标签的对象
            GameObject floorObject = GameObject.FindGameObjectWithTag(floorTag);
            
            // 如果找不到带标签的对象，尝试查找名为"SupermarketFloor"的对象
            if (floorObject == null)
            {
                floorObject = GameObject.Find("SupermarketFloor");
            }
            
            if (floorObject != null)
            {
                SetFloor(floorObject);
            }
            else
            {
                Debug.LogWarning("PlacementController: 未在场景中找到地板，请确保地板对象已创建并设置了相应的标签。");
            }
        }
        
        /// <summary>
        /// 设置地板引用
        /// </summary>
        public void SetFloor(GameObject floor)
        {
            currentFloor = floor;
            
            // 计算地板边界
            CalculateFloorBounds();
            
            Debug.Log($"放置控制器: 已设置地板引用，地板边界: {floorBounds}");
        }
        
        /// <summary>
        /// 计算地板边界
        /// </summary>
        private void CalculateFloorBounds()
        {
            if (currentFloor == null)
                return;
                
            // 获取渲染器（如果有）
            Renderer renderer = currentFloor.GetComponent<Renderer>();
            if (renderer != null)
            {
                floorBounds = renderer.bounds;
                return;
            }
            
            // 获取碰撞器（如果有）
            Collider collider = currentFloor.GetComponent<Collider>();
            if (collider != null)
            {
                floorBounds = collider.bounds;
                return;
            }
            
            // 如果都没有，使用变换和大小估计
            floorBounds = new Bounds(currentFloor.transform.position, currentFloor.transform.localScale);
        }
        
        private void Update()
        {
            // 如果不在放置模式，不处理
            if (!isPlacementMode || previewObject == null)
                return;
            
            // 更新预览对象位置
            UpdatePreviewPosition();
            
            // 应用平滑移动
            ApplySmoothPosition();
            
            // 检查是否可以放置
            CheckCanPlace();
            
            // 更新位置切换计时器
            positionSwitchTimer += Time.deltaTime;
        }
        
        /// <summary>
        /// 开始放置模式
        /// </summary>
        public void StartPlacement(ComponentData component)
        {
            // 确保有地板
            if (currentFloor == null)
            {
                FindFloor();
                if (currentFloor == null)
                {
                    Debug.LogError("无法开始放置：未找到地板对象");
                    return;
                }
            }
            
            // 如果已经在放置模式，先取消
            if (isPlacementMode)
            {
                CancelPlacement();
            }
            
            selectedComponent = component;
            
            // 创建预览对象
            if (component != null && component.prefab != null)
            {
                previewObject = Instantiate(component.prefab);
                previewObject.name = "Preview_" + component.displayName;
                
                // 应用预览材质
                ApplyPreviewMaterial(previewObject);
                
                // 调整位置到摄像机下方
                Vector3 initialPosition = mainCamera.transform.position + mainCamera.transform.forward * 5f;
                previewObject.transform.position = initialPosition;
                targetPosition = initialPosition;
                lastSnappedPosition = initialPosition;
                currentVelocity = Vector3.zero;
                
                // 计算预览对象的边界点
                CalculatePreviewBoundaryPoints();
                
                // 进入放置模式
                isPlacementMode = true;
                canPlace = false;
                isOverFloor = false;
            }
            else
            {
                Debug.LogError("无法开始放置：组件或预制件为空");
            }
        }
        
        /// <summary>
        /// 计算预览对象的边界点
        /// </summary>
        private void CalculatePreviewBoundaryPoints()
        {
            if (previewObject == null)
                return;
                
            // 获取预览对象的边界
            Bounds bounds = CalculateObjectBounds(previewObject);
            
            // 计算8个角点，再加上中心点和边缘中点，共17个点
            previewBoundaryPoints = new Vector3[17];
            
            // 中心点
            previewBoundaryPoints[0] = bounds.center;
            
            // 8个角点
            Vector3 extents = bounds.extents;
            previewBoundaryPoints[1] = bounds.center + new Vector3(extents.x, extents.y, extents.z);
            previewBoundaryPoints[2] = bounds.center + new Vector3(extents.x, extents.y, -extents.z);
            previewBoundaryPoints[3] = bounds.center + new Vector3(extents.x, -extents.y, extents.z);
            previewBoundaryPoints[4] = bounds.center + new Vector3(extents.x, -extents.y, -extents.z);
            previewBoundaryPoints[5] = bounds.center + new Vector3(-extents.x, extents.y, extents.z);
            previewBoundaryPoints[6] = bounds.center + new Vector3(-extents.x, extents.y, -extents.z);
            previewBoundaryPoints[7] = bounds.center + new Vector3(-extents.x, -extents.y, extents.z);
            previewBoundaryPoints[8] = bounds.center + new Vector3(-extents.x, -extents.y, -extents.z);
            
            // 边缘中点 - X轴边缘
            previewBoundaryPoints[9] = bounds.center + new Vector3(extents.x, 0, 0);
            previewBoundaryPoints[10] = bounds.center + new Vector3(-extents.x, 0, 0);
            
            // 边缘中点 - Z轴边缘
            previewBoundaryPoints[11] = bounds.center + new Vector3(0, 0, extents.z);
            previewBoundaryPoints[12] = bounds.center + new Vector3(0, 0, -extents.z);
            
            // 边缘中点 - 底部边缘中点（X方向）
            previewBoundaryPoints[13] = bounds.center + new Vector3(extents.x, -extents.y, 0);
            previewBoundaryPoints[14] = bounds.center + new Vector3(-extents.x, -extents.y, 0);
            
            // 边缘中点 - 底部边缘中点（Z方向）
            previewBoundaryPoints[15] = bounds.center + new Vector3(0, -extents.y, extents.z);
            previewBoundaryPoints[16] = bounds.center + new Vector3(0, -extents.y, -extents.z);
        }
        
        /// <summary>
        /// 计算对象的边界
        /// </summary>
        private Bounds CalculateObjectBounds(GameObject obj)
        {
            // 初始化一个空的边界
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            
            // 获取所有渲染器
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length > 0)
            {
                // 使用第一个渲染器的边界初始化
                bounds = renderers[0].bounds;
                
                // 包含其他所有渲染器的边界
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                // 如果没有渲染器，尝试使用碰撞器
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                
                if (colliders.Length > 0)
                {
                    bounds = colliders[0].bounds;
                    
                    for (int i = 1; i < colliders.Length; i++)
                    {
                        bounds.Encapsulate(colliders[i].bounds);
                    }
                }
                else
                {
                    // 如果既没有渲染器也没有碰撞器，使用默认尺寸
                    bounds.size = obj.transform.localScale;
                }
            }
            
            // 应用一个小的外边距，确保边界点略大于实际对象
            bounds.Expand(boundaryMargin);
            
            return bounds;
        }
        
        /// <summary>
        /// 应用预览材质到对象及其子对象
        /// </summary>
        private void ApplyPreviewMaterial(GameObject obj)
        {
            if (previewMaterial == null)
            {
                // 创建默认预览材质
                CreateDefaultPreviewMaterial();
            }
            
            // 获取所有渲染器组件
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                // 保存原始材质
                Material[] originalMaterials = renderer.materials;
                
                // 创建新的材质数组
                Material[] previewMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < previewMaterials.Length; i++)
                {
                    previewMaterials[i] = previewMaterial;
                }
                
                // 应用预览材质
                renderer.materials = previewMaterials;
            }
        }
        
        /// <summary>
        /// 创建默认预览材质
        /// </summary>
        private void CreateDefaultPreviewMaterial()
        {
            previewMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            previewMaterial.color = new Color(0.5f, 0.5f, 1f, 0.5f);
        }
        
        /// <summary>
        /// 更新预览对象位置
        /// </summary>
        private void UpdatePreviewPosition()
        {
            // 从鼠标位置发射射线
            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementLayerMask))
            {
                // 调整位置到射线碰撞点
                Vector3 position = hit.point;
                position.y = placementHeight;
                
                // 如果启用了网格对齐且找到了网格管理器
                if (snapToGrid && gridManager != null)
                {
                    Vector3 snappedPosition = gridManager.SnapToGrid(position);
                    
                    // 检查是否需要切换到新的网格位置
                    bool shouldSnapToNewPosition = ShouldSnapToNewPosition(snappedPosition);
                    
                    if (shouldSnapToNewPosition)
                    {
                        position = snappedPosition;
                        lastSnappedPosition = snappedPosition;
                        positionSwitchTimer = 0f;
                    }
                    else
                    {
                        // 保持在上一个网格位置
                        position = lastSnappedPosition;
                    }
                }
                
                // 确保位置在地板边界内，根据预览对象的边界调整
                position = AdjustPositionToStayWithinFloor(position);
                
                // 更新目标位置
                targetPosition = position;
            }
        }
        
        /// <summary>
        /// 决定是否应该切换到新的网格位置
        /// </summary>
        private bool ShouldSnapToNewPosition(Vector3 newSnappedPosition)
        {
            // 计算与上一个网格位置的距离
            float distance = Vector3.Distance(newSnappedPosition, lastSnappedPosition);
            
            // 如果距离足够大，立即切换
            if (distance >= gridManager.gridCellSize)
            {
                return true;
            }
            
            // 如果处于相邻网格单元之间的边界区域，使用滞后切换避免抖动
            if (distance > 0 && distance < gridManager.gridCellSize)
            {
                // 计算当前位置与两个网格点的距离
                float distanceToNew = Vector3.Distance(targetPosition, newSnappedPosition);
                float distanceToOld = Vector3.Distance(targetPosition, lastSnappedPosition);
                
                // 如果已经明显更接近新位置，或者鼠标一直保持在同一区域一段时间
                if (distanceToNew < distanceToOld - gridSnapThreshold || positionSwitchTimer > 0.5f)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 平滑应用位置变化
        /// </summary>
        private void ApplySmoothPosition()
        {
            if (previewObject == null)
                return;
                
            // 使用SmoothDamp实现平滑移动
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                previewObject.transform.position, 
                targetPosition, 
                ref currentVelocity, 
                1f / positionSmoothSpeed
            );
            
            // 应用平滑后的位置
            previewObject.transform.position = smoothedPosition;
            
            // 更新边界点（因为位置改变）
            CalculatePreviewBoundaryPoints();
        }
        
        /// <summary>
        /// 调整位置，确保对象完全在地板上
        /// </summary>
        private Vector3 AdjustPositionToStayWithinFloor(Vector3 proposedPosition)
        {
            if (previewObject == null || currentFloor == null)
                return proposedPosition;
            
            // 应用提议的位置
            Vector3 originalPosition = previewObject.transform.position;
            previewObject.transform.position = proposedPosition;
            
            // 重新计算边界点
            CalculatePreviewBoundaryPoints();
            
            // 检查边界点是否全部在地板上
            Vector2 adjustment = Vector2.zero;
            bool needsAdjustment = false;
            
            foreach (Vector3 point in previewBoundaryPoints)
            {
                // 仅检查X和Z坐标（水平面）
                Vector3 floorPoint = new Vector3(point.x, floorBounds.center.y, point.z);
                
                if (!floorBounds.Contains(floorPoint))
                {
                    needsAdjustment = true;
                    
                    // 计算需要的调整量
                    if (point.x < floorBounds.min.x)
                        adjustment.x = Mathf.Max(adjustment.x, floorBounds.min.x - point.x);
                    else if (point.x > floorBounds.max.x)
                        adjustment.x = Mathf.Min(adjustment.x, floorBounds.max.x - point.x);
                    
                    if (point.z < floorBounds.min.z)
                        adjustment.y = Mathf.Max(adjustment.y, floorBounds.min.z - point.z);
                    else if (point.z > floorBounds.max.z)
                        adjustment.y = Mathf.Min(adjustment.y, floorBounds.max.z - point.z);
                }
            }
            
            // 恢复原始位置
            previewObject.transform.position = originalPosition;
            
            // 如果需要调整，应用调整量
            if (needsAdjustment)
            {
                proposedPosition.x += adjustment.x;
                proposedPosition.z += adjustment.y;
            }
            
            return proposedPosition;
        }
        
        /// <summary>
        /// 检查当前位置是否可以放置
        /// </summary>
        private void CheckCanPlace()
        {
            // 检查所有边界点是否都在地板上
            isOverFloor = CheckAllBoundaryPointsOverFloor();
            canPlace = isOverFloor;
            
            // 如果在地板上，还需检查是否与其他对象重叠
            if (canPlace)
            {
                canPlace = !CheckOverlap();
            }
            
            // 更新预览对象的外观以反映是否可以放置
            UpdatePreviewAppearance();
        }
        
        /// <summary>
        /// 检查所有边界点是否都在地板上
        /// </summary>
        private bool CheckAllBoundaryPointsOverFloor()
        {
            if (previewObject == null || currentFloor == null || previewBoundaryPoints == null)
                return false;
            
            foreach (Vector3 point in previewBoundaryPoints)
            {
                // 对于每个点，检查是否在地板边界内
                Vector3 floorPoint = new Vector3(point.x, floorBounds.center.y, point.z);
                
                if (!floorBounds.Contains(floorPoint))
                {
                    return false; // 只要有一个点不在地板上，就返回false
                }
            }
            
            return true; // 所有点都在地板上
        }
        
        /// <summary>
        /// 检查是否与其他对象重叠
        /// </summary>
        private bool CheckOverlap()
        {
            if (previewObject == null)
                return false;
                
            // 获取预览对象的碰撞器（假设有）
            Collider previewCollider = previewObject.GetComponent<Collider>();
            if (previewCollider == null)
            {
                // 尝试获取子对象的碰撞器
                previewCollider = previewObject.GetComponentInChildren<Collider>();
                if (previewCollider == null)
                    return false; // 没有碰撞器，无法检查重叠
            }
            
            // 获取碰撞器的边界
            Bounds bounds = previewCollider.bounds;
            
            // 获取可能重叠的所有碰撞器
            Collider[] colliders = Physics.OverlapBox(
                bounds.center, 
                bounds.extents, 
                previewObject.transform.rotation, 
                placementLayerMask
            );
            
            // 检查是否有除预览对象外的碰撞器
            foreach (Collider collider in colliders)
            {
                // 跳过预览对象自身的碰撞器
                if (collider.gameObject == previewObject || collider.transform.IsChildOf(previewObject.transform))
                    continue;
                
                // 跳过地板的碰撞器
                if (collider.gameObject == currentFloor)
                    continue;
                
                // 找到一个重叠的碰撞器，返回true表示有重叠
                return true;
            }
            
            // 没有找到重叠的碰撞器
            return false;
        }
        
        /// <summary>
        /// 更新预览对象的外观
        /// </summary>
        private void UpdatePreviewAppearance()
        {
            if (previewObject == null || previewMaterial == null)
                return;
                
            // 获取所有渲染器
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                // 根据是否可以放置更改材质颜色
                Material[] materials = renderer.materials;
                
                foreach (Material material in materials)
                {
                    // 可以放置时使用蓝色，否则使用红色
                    Color color = canPlace ? new Color(0.5f, 0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f, 0.5f);
                    material.color = color;
                }
            }
        }
        
        /// <summary>
        /// 左键点击事件处理
        /// </summary>
        private void OnLeftMousePerformed(InputAction.CallbackContext context)
        {
            if (isPlacementMode && canPlace)
            {
                PlaceComponent();
            }
        }
        
        /// <summary>
        /// 右键点击事件处理
        /// </summary>
        private void OnRightMousePerformed(InputAction.CallbackContext context)
        {
            if (isPlacementMode)
            {
                CancelPlacement();
            }
        }
        
        /// <summary>
        /// R键旋转事件处理
        /// </summary>
        private void OnRotatePerformed(InputAction.CallbackContext context)
        {
            if (isPlacementMode)
            {
                RotatePreview();
            }
        }
        
        /// <summary>
        /// ESC键取消事件处理
        /// </summary>
        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (isPlacementMode)
            {
                CancelPlacement();
            }
        }
        
        /// <summary>
        /// 放置组件
        /// </summary>
        private void PlaceComponent()
        {
            if (previewObject == null || selectedComponent == null || !canPlace)
                return;
                
            // 记录当前预览对象的位置和旋转
            Vector3 position = previewObject.transform.position;
            Quaternion rotation = previewObject.transform.rotation;
            
            // 移除预览外观
            Destroy(previewObject);
            
            // 创建实际组件
            GameObject placedObject = Instantiate(selectedComponent.prefab, position, rotation);
            placedObject.name = selectedComponent.displayName;
            
            // 添加组件标识符组件（可以用于之后的选择和编辑）
            PlacedComponent componentIdentifier = placedObject.AddComponent<PlacedComponent>();
            componentIdentifier.Initialize(selectedComponent);
            
            Debug.Log($"已放置组件: {selectedComponent.displayName} 在位置: {position}");
            
            // 检查是否按下了Shift键（用于连续放置）
            bool continuousPlacement = Keyboard.current.shiftKey.isPressed;
            
            if (continuousPlacement)
            {
                StartPlacement(selectedComponent);
            }
            else
            {
                // 退出放置模式
                isPlacementMode = false;
                selectedComponent = null;
                previewObject = null;
            }
        }
        
        /// <summary>
        /// 取消放置
        /// </summary>
        public void CancelPlacement()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }
            
            isPlacementMode = false;
            selectedComponent = null;
            previewObject = null;
        }
        
        /// <summary>
        /// 旋转预览对象
        /// </summary>
        private void RotatePreview()
        {
            if (previewObject != null)
            {
                // 围绕Y轴旋转90度
                previewObject.transform.Rotate(0, 90, 0);
                
                // 旋转后重新计算边界点
                CalculatePreviewBoundaryPoints();
                
                // 旋转后调整位置，确保仍然在地板内
                Vector3 adjustedPosition = AdjustPositionToStayWithinFloor(previewObject.transform.position);
                previewObject.transform.position = adjustedPosition;
                targetPosition = adjustedPosition;
                lastSnappedPosition = adjustedPosition;
                currentVelocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// 设置网格对齐状态
        /// </summary>
        public void SetSnapToGrid(bool snap)
        {
            snapToGrid = snap;
            
            // 更新当前位置
            if (previewObject != null)
            {
                // 获取当前位置
                Vector3 currentPosition = previewObject.transform.position;
                
                // 如果开启了网格对齐，则立即对齐到最近的网格点
                if (snap && gridManager != null)
                {
                    Vector3 snappedPosition = gridManager.SnapToGrid(currentPosition);
                    targetPosition = snappedPosition;
                    lastSnappedPosition = snappedPosition;
                }
                else
                {
                    // 如果关闭了网格对齐，维持当前位置
                    targetPosition = currentPosition;
                    lastSnappedPosition = currentPosition;
                }
                
                // 重置速度
                currentVelocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// 设置位置平滑速度
        /// </summary>
        public void SetPositionSmoothSpeed(float speed)
        {
            positionSmoothSpeed = Mathf.Max(1f, speed);
        }
        
        /// <summary>
        /// 绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            // 如果不在编辑器模式或不在放置模式，不绘制Gizmos
            if (!isPlacementMode || previewObject == null || previewBoundaryPoints == null)
                return;
                
            // 绘制预览对象的边界点
            Gizmos.color = Color.yellow;
            foreach (Vector3 point in previewBoundaryPoints)
            {
                Gizmos.DrawSphere(point, 0.05f);
            }
            
            // 绘制地板边界
            if (currentFloor != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(floorBounds.center, floorBounds.size);
            }
            
            // 如果启用了网格对齐，绘制目标位置和最后一个对齐位置
            if (snapToGrid && gridManager != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(targetPosition, 0.1f);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lastSnappedPosition, 0.1f);
            }
        }
    }
}