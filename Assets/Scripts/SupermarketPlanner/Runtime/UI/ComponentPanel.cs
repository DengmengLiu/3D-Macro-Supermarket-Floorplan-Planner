using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SupermarketPlanner.Data;
using SupermarketPlanner.Managers;
using SupermarketPlanner.Controllers;

namespace SupermarketPlanner.UI
{
    /// <summary>
    /// 组件面板 - 用于在UI中显示组件库中的组件
    /// </summary>
    public class ComponentPanel : MonoBehaviour
    {
        [Header("面板配置")]
        [Tooltip("使用组件管理器加载库")]
        public bool useComponentManager = true;
        
        [Tooltip("如果不使用组件管理器，可以直接指定库")]
        public ComponentLibrary directLibrary;
        
        [Tooltip("是否按类别分组显示")]
        public bool groupByCategory = true;
        
        [Tooltip("加载时自动刷新")]
        public bool refreshOnStart = true;
        
        [Header("UI引用")]
        [Tooltip("组件按钮的父容器")]
        public Transform contentContainer;
        
        [Tooltip("组件按钮预制件")]
        public GameObject componentButtonPrefab;
        
        [Tooltip("类别标题预制件")]
        public GameObject categoryHeaderPrefab;
        
        [Tooltip("默认组件图标")]
        public Sprite defaultIcon;
        
        [Header("按钮外观")]
        public Color buttonNormalColor = new Color(0.9f, 0.9f, 0.9f);
        public Color buttonHoverColor = new Color(0.8f, 0.8f, 1f);
        public Color buttonSelectedColor = new Color(0.7f, 0.7f, 1f);
        
        [Header("放置系统")]
        [Tooltip("放置控制器引用")]
        public PlacementController placementController;
        
        private ComponentLibrary currentLibrary;
        private Dictionary<string, GameObject> buttonInstances = new Dictionary<string, GameObject>();
        private string selectedComponentId;
        
        private void Start()
        {
            // 查找放置控制器（如果未指定）
            if (placementController == null)
            {
                placementController = FindFirstObjectByType<PlacementController>();
                if (placementController == null)
                {
                    Debug.LogWarning("未找到PlacementController组件，将无法放置组件");
                }
            }
            
            if (refreshOnStart)
            {
                RefreshPanel();
            }
        }
        
        /// <summary>
        /// 刷新组件面板
        /// </summary>
        public void RefreshPanel()
        {
            // 获取当前库
            if (useComponentManager && ComponentManager.Instance != null)
            {
                currentLibrary = ComponentManager.Instance.GetCurrentLibrary();
            }
            else if (directLibrary != null)
            {
                currentLibrary = directLibrary;
            }
            else
            {
                Debug.LogWarning("Component library not found. Please specify a library or configure the component manager.");
                return;
            }
            
            // 清空当前内容
            ClearPanel();
            
            if (currentLibrary == null)
            {
                CreateErrorMessage("Component library not found");
                return;
            }
            
            // 加载组件
            if (groupByCategory)
            {
                LoadComponentsByCategory();
            }
            else
            {
                LoadAllComponents();
            }
        }
        
        /// <summary>
        /// 清空面板
        /// </summary>
        private void ClearPanel()
        {
            buttonInstances.Clear();
            
            if (contentContainer != null)
            {
                foreach (Transform child in contentContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 创建错误消息
        /// </summary>
        private void CreateErrorMessage(string message)
        {
            if (contentContainer == null)
                return;
                
            GameObject messageObj = new GameObject("ErrorMessage");
            messageObj.transform.SetParent(contentContainer, false);
            
            TextMeshProUGUI text = messageObj.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.fontSize = 14;
            text.color = Color.red;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = messageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, 30);
        }
        
        /// <summary>
        /// 加载所有组件（不分类）
        /// </summary>
        private void LoadAllComponents()
        {
            List<ComponentData> components = currentLibrary.components;
            
            if (components.Count == 0)
            {
                CreateErrorMessage("The component library is empty");
                return;
            }
            
            foreach (var component in components)
            {
                if (!component.isHidden)
                {
                    CreateComponentButton(component);
                }
            }
        }
        
        /// <summary>
        /// 按类别加载组件
        /// </summary>
        private void LoadComponentsByCategory()
        {
            // 获取所有类别
            List<ComponentCategory> categories = currentLibrary.GetCategories();
            
            if (categories.Count == 0)
            {
                CreateErrorMessage("No categories or components available");
                return;
            }
            
            // 为每个类别创建组件
            foreach (var category in categories)
            {
                // 创建类别标题
                CreateCategoryHeader(category.ToString());
                
                // 加载该类别下的组件
                List<ComponentData> categoryComponents = currentLibrary.GetComponentsByCategory(category);
                
                foreach (var component in categoryComponents)
                {
                    CreateComponentButton(component);
                }
            }
        }
        
        /// <summary>
        /// 创建类别标题
        /// </summary>
        private void CreateCategoryHeader(string categoryName)
        {
            if (contentContainer == null)
                return;
                
            GameObject headerObj;
            
            if (categoryHeaderPrefab != null)
            {
                headerObj = Instantiate(categoryHeaderPrefab, contentContainer);
            }
            else
            {
                // 创建默认标题
                headerObj = new GameObject("Header_" + categoryName);
                headerObj.transform.SetParent(contentContainer, false);
                
                // 添加文本组件
                TextMeshProUGUI text = headerObj.AddComponent<TextMeshProUGUI>();
                text.text = categoryName;
                text.fontSize = 16;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                
                // 设置RectTransform
                RectTransform rect = headerObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.sizeDelta = new Vector2(0, 30);
            }
            
            // 查找并设置文本
            TextMeshProUGUI headerText = headerObj.GetComponentInChildren<TextMeshProUGUI>();
            if (headerText != null)
            {
                headerText.text = categoryName;
            }
        }
        
        /// <summary>
        /// 创建组件按钮
        /// </summary>
        private void CreateComponentButton(ComponentData component)
        {
            if (contentContainer == null || componentButtonPrefab == null)
                return;
                
            // 创建按钮
            GameObject buttonObj = Instantiate(componentButtonPrefab, contentContainer);
            buttonObj.name = "Button_" + component.displayName;
            
            // 存储按钮实例
            buttonInstances[component.id] = buttonObj;
            
            // 设置按钮文本
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = component.displayName;
            }
            
            // 设置按钮图标
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = component.icon != null ? component.icon : defaultIcon;
                iconImage.gameObject.SetActive(true);
            }
            
            // 设置按钮颜色
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                // 设置颜色过渡
                ColorBlock colors = button.colors;
                colors.normalColor = buttonNormalColor;
                colors.highlightedColor = buttonHoverColor;
                colors.selectedColor = buttonSelectedColor;
                colors.pressedColor = buttonSelectedColor;
                button.colors = colors;
                
                // 添加点击事件
                button.onClick.AddListener(() => OnComponentButtonClicked(component.id));
            }
            
            // 添加工具提示（如果有）
            if (!string.IsNullOrEmpty(component.description))
            {
                // 您可以在这里添加自定义的工具提示组件
                // 例如：buttonObj.AddComponent<TooltipTrigger>().tooltipText = component.description;
            }
        }
        
        /// <summary>
        /// 按钮点击事件处理
        /// </summary>
        private void OnComponentButtonClicked(string componentId)
        {
            // 取消选择之前的按钮
            if (!string.IsNullOrEmpty(selectedComponentId) && buttonInstances.ContainsKey(selectedComponentId))
            {
                Button prevButton = buttonInstances[selectedComponentId].GetComponent<Button>();
                if (prevButton != null)
                {
                    ColorBlock colors = prevButton.colors;
                    colors.normalColor = buttonNormalColor;
                    prevButton.colors = colors;
                }
            }
            
            // 选择新按钮
            selectedComponentId = componentId;
            
            if (buttonInstances.ContainsKey(componentId))
            {
                Button curButton = buttonInstances[componentId].GetComponent<Button>();
                if (curButton != null)
                {
                    ColorBlock colors = curButton.colors;
                    colors.normalColor = buttonSelectedColor;
                    curButton.colors = colors;
                }
            }
            
            // 获取组件数据
            ComponentData component = currentLibrary.GetComponentById(componentId);
            if (component != null)
            {
                // 通知其他系统
                OnComponentSelected(component);
            }
        }
        
        /// <summary>
        /// 组件选择事件
        /// </summary>
        private void OnComponentSelected(ComponentData component)
        {
            Debug.Log($"Selected component: {component.displayName}");
            
            // 启动放置模式
            if (placementController != null && component != null)
            {
                placementController.StartPlacement(component);
            }
        }
    }
}