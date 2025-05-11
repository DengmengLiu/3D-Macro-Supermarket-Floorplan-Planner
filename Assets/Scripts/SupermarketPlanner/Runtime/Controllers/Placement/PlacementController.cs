using UnityEngine;
using SupermarketPlanner.Data;
using SupermarketPlanner.Services;
using System;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 放置控制器 - 协调预览、网格对齐和放置服务
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("服务引用")]
        [Tooltip("预览管理器")]
        public PlacementPreviewManager previewManager;

        [Tooltip("网格对齐服务")]
        public GridAlignmentService gridAlignmentService;

        [Tooltip("新的简化放置验证服务")]
        public PlacementValidationService validationService;

        [Tooltip("组件放置服务")]
        public ComponentPlacementService placementService;

        [Header("设置")]
        [Tooltip("放置层掩码")]
        public LayerMask placementLayerMask;

        [Tooltip("放置模式激活时的光标")]
        public Texture2D placementCursor;

        [Tooltip("是否启用网格对齐")]
        public bool snapToGrid = true;

        // 放置状态
        private bool isPlacementModeActive = false;
        private ComponentData currentComponent = null;

        // 输入处理器
        private PlacementInputHandler inputHandler;

        // 事件
        public event Action<bool> OnPlacementModeChanged;
        public event Action<GameObject> OnComponentPlaced;

        private void Awake()
        {
            // 初始化输入处理器
            inputHandler = gameObject.AddComponent<PlacementInputHandler>();

            // 查找或创建必要的服务
            FindOrCreateServices();
        }

        private void OnEnable()
        {
            // 设置输入事件
            SetupInputEvents();
        }

        void Start()
        {
            if (placementLayerMask.value == 0)
            {
                // 排除网格层，假设你已创建了名为"Grid"的层
                placementLayerMask = ~(1 << LayerMask.NameToLayer("Grid"));
            }

            // 设置验证服务的允许碰撞层
            if (validationService != null)
            {
                // 设置地板层为允许碰撞的层
                int floorLayer = LayerMask.NameToLayer("Floor");
                if (floorLayer != -1)
                {
                    validationService.allowedCollisionLayers = 1 << floorLayer;
                }
            }
        }

        private void OnDisable()
        {
            // 如果处于放置模式，退出放置模式
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// 查找或创建必要的服务
        /// </summary>
        private void FindOrCreateServices()
        {
            // 查找预览管理器
            if (previewManager == null)
            {
                previewManager = GetComponent<PlacementPreviewManager>();
                if (previewManager == null)
                {
                    previewManager = gameObject.AddComponent<PlacementPreviewManager>();
                }
            }

            // 查找网格对齐服务
            if (gridAlignmentService == null)
            {
                gridAlignmentService = GetComponent<GridAlignmentService>();
                if (gridAlignmentService == null)
                {
                    gridAlignmentService = gameObject.AddComponent<GridAlignmentService>();
                }
            }

            // 查找新的简化放置验证服务
            if (validationService == null)
            {
                validationService = GetComponent<PlacementValidationService>();
                if (validationService == null)
                {
                    validationService = gameObject.AddComponent<PlacementValidationService>();
                }
            }

            // 查找组件放置服务
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
        /// 设置输入事件
        /// </summary>
        private void SetupInputEvents()
        {
            if (inputHandler == null)
                return;

            // 设置输入回调
            inputHandler.OnLeftClick += HandleLeftClick;
            inputHandler.OnRightClick += HandleRightClick;
            inputHandler.OnRotate += HandleRotate;
            inputHandler.OnCancel += HandleCancel;
        }

        private void Update()
        {
            // 如果不在放置模式，不处理
            if (!isPlacementModeActive)
                return;

            // 更新预览
            UpdatePreview();

            // 使用新的验证服务实时检查是否可以放置
            CheckPlacementValidity();
        }

        /// <summary>
        /// 实时检查放置有效性并更新预览颜色
        /// </summary>
        private void CheckPlacementValidity()
        {
            if (previewManager == null || validationService == null)
                return;

            GameObject previewObject = previewManager.GetPreviewObject();
            if (previewObject == null)
                return;

            // 使用新的验证服务检查是否可以放置
            bool canPlace = validationService.CanPlace(previewObject);

            // 更新预览对象的颜色
            previewManager.UpdatePreviewColor(canPlace);
        }

        /// <summary>
        /// 开始放置模式
        /// </summary>
        public void StartPlacement(ComponentData componentData)
        {
            if (componentData == null || componentData.prefab == null)
            {
                Debug.LogError("无法开始放置：组件数据或预制件为空");
                return;
            }

            // 如果已经在放置模式，先取消
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }

            // 保存当前组件
            currentComponent = componentData;

            // 创建预览
            previewManager.CreatePreview(componentData);

            // 设置光标
            SetPlacementCursor(true);

            // 进入放置模式
            isPlacementModeActive = true;

            // 触发事件
            OnPlacementModeChanged?.Invoke(true);

            Debug.Log($"开始放置模式: {componentData.displayName}");
        }

        /// <summary>
        /// 更新预览
        /// </summary>
        private void UpdatePreview()
        {
            // 更新预览位置
            previewManager.UpdatePreview();

            // 获取预览位置
            Vector3 previewPosition = previewManager.GetTargetPosition();

            // 应用网格对齐
            if (gridAlignmentService != null && snapToGrid)
            {
                previewPosition = gridAlignmentService.AlignToGrid(previewPosition);

                // 将对齐后的位置应用回预览对象
                GameObject previewObject = previewManager.GetPreviewObject();
                if (previewObject != null)
                {
                    previewObject.transform.position = previewPosition;
                }
            }
        }

        /// <summary>
        /// 处理左键点击
        /// </summary>
        private void HandleLeftClick()
        {
            if (!isPlacementModeActive)
                return;

            // 获取预览对象
            GameObject previewObject = previewManager.GetPreviewObject();
            if (previewObject == null)
                return;

            // 使用新的验证服务检查是否可以放置
            bool canPlace = validationService.CanPlace(previewObject);

            if (canPlace)
            {
                PlaceCurrentComponent();
            }
            else
            {
                Debug.Log("无法放置：与其他对象碰撞");
            }
        }

        /// <summary>
        /// 处理右键点击
        /// </summary>
        private void HandleRightClick()
        {
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// 处理旋转键
        /// </summary>
        private void HandleRotate()
        {
            if (isPlacementModeActive)
            {
                RotatePreview();
            }
        }

        /// <summary>
        /// 处理取消键
        /// </summary>
        private void HandleCancel()
        {
            if (isPlacementModeActive)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// 放置当前组件
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
        /// 递归设置游戏对象及其所有子对象的层
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
        /// 取消放置
        /// </summary>
        public void CancelPlacement()
        {
            // 清理放置模式
            ClearPlacementMode();

            // 退出放置模式
            EndPlacementMode();
        }

        /// <summary>
        /// 清理放置模式
        /// </summary>
        private void ClearPlacementMode()
        {
            // 清理预览
            previewManager.ClearPreview();
        }

        /// <summary>
        /// 退出放置模式
        /// </summary>
        private void EndPlacementMode()
        {
            // 重置状态
            isPlacementModeActive = false;
            currentComponent = null;

            // 恢复光标
            SetPlacementCursor(false);

            // 触发事件
            OnPlacementModeChanged?.Invoke(false);
        }

        /// <summary>
        /// 旋转预览
        /// </summary>
        private void RotatePreview()
        {
            // 旋转预览对象
            previewManager.RotatePreview();
        }

        /// <summary>
        /// 设置网格对齐状态
        /// </summary>
        public void SetSnapToGrid(bool enabled)
        {
            snapToGrid = enabled;
        }

        /// <summary>
        /// 设置放置光标
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
        /// 检查是否处于放置模式
        /// </summary>
        public bool IsInPlacementMode()
        {
            return isPlacementModeActive;
        }

        /// <summary>
        /// 获取当前选中的组件
        /// </summary>
        public ComponentData GetCurrentComponent()
        {
            return currentComponent;
        }
    }
}