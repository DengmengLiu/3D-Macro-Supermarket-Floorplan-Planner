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
        
        [Header("鼠标设置")]
        [Tooltip("鼠标射线检测层")]
        public LayerMask placementLayerMask;
        
        // 当前选中的组件
        private ComponentData selectedComponent;
        
        // 预览对象
        private GameObject previewObject;
        
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
            
            // 检查是否可以放置
            CheckCanPlace();
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
                previewObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 5f;
                
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
                // 检查是否击中地板
                isOverFloor = IsPointOverFloor(hit.point);
                
                // 调整位置到射线碰撞点
                Vector3 position = hit.point;
                position.y = placementHeight;
                
                // 如果启用了网格对齐且找到了网格管理器
                if (snapToGrid && gridManager != null)
                {
                    position = gridManager.SnapToGrid(position);
                }
                else if (!isOverFloor)
                {
                    // 如果不在地板上，将位置限制在地板边界内
                    position = ClampPositionToFloor(position);
                }
                
                // 应用位置到预览对象
                previewObject.transform.position = position;
            }
            else
            {
                // 如果射线没有击中任何物体，假设不在地板上
                isOverFloor = false;
            }
        }
        
        /// <summary>
        /// 检查点是否在地板上方
        /// </summary>
        private bool IsPointOverFloor(Vector3 point)
        {
            if (currentFloor == null)
                return false;
                
            // 检查点是否在地板边界内
            bool isInBounds = floorBounds.Contains(new Vector3(point.x, floorBounds.center.y, point.z));
                
            // 也可以使用射线检测向下射线是否击中地板
            bool hitFloor = false;
            Ray downRay = new Ray(new Vector3(point.x, point.y + 1f, point.z), Vector3.down);
            if (Physics.Raycast(downRay, out RaycastHit hit, 10f))
            {
                hitFloor = hit.collider.gameObject == currentFloor;
            }
                
            return isInBounds || hitFloor;
        }
        
        /// <summary>
        /// 将位置限制在地板边界内
        /// </summary>
        private Vector3 ClampPositionToFloor(Vector3 position)
        {
            if (currentFloor == null)
                return position;
                
            // 获取地板边界的最小和最大点
            Vector3 min = floorBounds.min;
            Vector3 max = floorBounds.max;
                
            // 限制X和Z坐标在地板边界内
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.z = Mathf.Clamp(position.z, min.z, max.z);
                
            return position;
        }
        
        /// <summary>
        /// 检查当前位置是否可以放置
        /// </summary>
        private void CheckCanPlace()
        {
            // 首先检查是否在地板上
            canPlace = isOverFloor;
            
            // 如果在地板上，可以添加更多条件检查，例如：
            if (canPlace)
            {
                // 检查是否与其他对象重叠
                canPlace = !CheckOverlap();
            }
            
            // 更新预览对象的外观以反映是否可以放置
            UpdatePreviewAppearance();
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
            }
        }
        
        /// <summary>
        /// 设置网格对齐状态
        /// </summary>
        public void SetSnapToGrid(bool snap)
        {
            snapToGrid = snap;
        }
    }
}