using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SupermarketPlanner.Managers;
using SupermarketPlanner.Controllers;
using SupermarketPlanner.Services;

namespace SupermarketPlanner.UI
{
    /// <summary>
    /// 网格设置面板 - 在设置标签页中管理网格的可见性和大小
    /// </summary>
    public class GridSettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("网格可见性切换开关")]
        public Toggle gridVisibilityToggle;
        
        [Tooltip("网格大小滑块")]
        public Slider gridSizeSlider;
        
        [Tooltip("当前网格大小显示文本")]
        public TextMeshProUGUI gridSizeText;
        
        [Tooltip("网格对齐切换开关")]
        public Toggle snapToGridToggle;
        
        [Header("设置范围")]
        [Tooltip("最小网格大小（米）")]
        public float minGridSize = 0.1f;
        
        [Tooltip("最大网格大小（米）")]
        public float maxGridSize = 10f;
        
        // 管理器引用
        private GridManager gridManager;
        private PlacementController placementController;
        private GridAlignmentService gridAlignmentService;
        
        // 当前值
        private bool currentGridVisibility = true;
        private float currentGridSize = 1.0f;
        private bool currentSnapToGrid = true;
        
        private void Start()
        {
            // 查找网格管理器
            gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("网格设置面板未找到GridManager组件");
                // 禁用网格相关设置
                if (gridVisibilityToggle != null) gridVisibilityToggle.interactable = false;
                if (gridSizeSlider != null) gridSizeSlider.interactable = false;
            }
            
            // 查找放置控制器
            placementController = FindFirstObjectByType<PlacementController>();
            
            // 查找网格对齐服务
            gridAlignmentService = FindFirstObjectByType<GridAlignmentService>();
            if (gridAlignmentService == null && placementController != null)
            {
                // 尝试从放置控制器获取网格对齐服务
                gridAlignmentService = placementController.gridAlignmentService;
            }
            
            if (gridAlignmentService == null && snapToGridToggle != null)
            {
                Debug.LogWarning("网格设置面板未找到GridAlignmentService组件");
                // 禁用网格对齐设置
                snapToGridToggle.interactable = false;
            }
            
            // 初始化UI元素
            InitializeUI();
        }
        
        /// <summary>
        /// 初始化UI元素
        /// </summary>
        private void InitializeUI()
        {
            // 初始化网格可见性开关
            if (gridVisibilityToggle != null && gridManager != null)
            {
                // 设置初始值
                currentGridVisibility = gridManager.gridVisible;
                gridVisibilityToggle.isOn = currentGridVisibility;
                
                // 添加监听器
                gridVisibilityToggle.onValueChanged.AddListener(OnGridVisibilityChanged);
            }
            
            // 初始化网格大小滑块
            if (gridSizeSlider != null && gridManager != null)
            {
                // 配置滑块范围
                gridSizeSlider.minValue = minGridSize;
                gridSizeSlider.maxValue = maxGridSize;
                
                // 设置初始值
                currentGridSize = gridManager.gridCellSize;
                gridSizeSlider.value = currentGridSize;
                
                // 添加监听器
                gridSizeSlider.onValueChanged.AddListener(OnGridSizeChanged);
            }
            
            // 初始化网格对齐开关
            if (snapToGridToggle != null && gridAlignmentService != null)
            {
                // 设置初始值
                currentSnapToGrid = gridAlignmentService.enableGridAlignment;
                snapToGridToggle.isOn = currentSnapToGrid;
                
                // 添加监听器
                snapToGridToggle.onValueChanged.AddListener(OnSnapToGridChanged);
            }
            
            // 更新文本显示
            UpdateGridSizeText();
        }
        
        /// <summary>
        /// 当网格可见性改变时调用
        /// </summary>
        public void OnGridVisibilityChanged(bool isVisible)
        {
            if (gridManager != null)
            {
                currentGridVisibility = isVisible;
                gridManager.SetGridVisibility(isVisible);
            }
        }
        
        /// <summary>
        /// 当网格大小改变时调用
        /// </summary>
        public void OnGridSizeChanged(float size)
        {
            if (gridManager != null)
            {
                currentGridSize = size;
                gridManager.SetGridCellSize(size);
                
                // 更新文本显示
                UpdateGridSizeText();
            }
        }
        
        /// <summary>
        /// 当网格对齐设置改变时调用
        /// </summary>
        public void OnSnapToGridChanged(bool snapToGrid)
        {
            if (gridAlignmentService != null)
            {
                currentSnapToGrid = snapToGrid;
                gridAlignmentService.SetGridAlignmentEnabled(snapToGrid);
            }
            else if (placementController != null)
            {
                currentSnapToGrid = snapToGrid;
                gridAlignmentService.SetGridAlignmentEnabled(snapToGrid);
            }
        }
        
        /// <summary>
        /// 更新网格大小文本显示
        /// </summary>
        private void UpdateGridSizeText()
        {
            if (gridSizeText != null)
            {
                // 格式化为一位小数
                string sizeText = string.Format("{0:0.0} m", currentGridSize);
                gridSizeText.text = sizeText;
            }
        }
    }
}