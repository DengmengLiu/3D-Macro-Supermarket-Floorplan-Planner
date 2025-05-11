using UnityEngine;
using SupermarketPlanner.Data;
using System.Collections.Generic;
using System.Linq;

namespace SupermarketPlanner.Managers
{
    /// <summary>
    /// 组件管理器 - 用于在运行时管理组件库
    /// </summary>
    public class ComponentManager : MonoBehaviour
    {
        [Tooltip("默认组件库资源")]
        public ComponentLibrary defaultLibrary;
        
        [Tooltip("当前加载的库")]
        private ComponentLibrary currentLibrary;
        
        // 单例实例
        public static ComponentManager Instance { get; private set; }
        
        private void Awake()
        {
            // 单例模式设置
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 加载默认库
            if (defaultLibrary != null)
            {
                currentLibrary = defaultLibrary;
            }
            else
            {
                // 尝试从Resources加载
                currentLibrary = Resources.Load<ComponentLibrary>("ComponentLibraries/DefaultComponentLibrary");
                
                if (currentLibrary == null)
                {
                    Debug.LogWarning("The default component library was not found. Please specify it in the ComponentManager or create it in Resources.");
                }
            }
        }
        
        /// <summary>
        /// 获取当前加载的库
        /// </summary>
        public ComponentLibrary GetCurrentLibrary()
        {
            return currentLibrary;
        }
        
        /// <summary>
        /// 加载新的组件库
        /// </summary>
        public bool LoadLibrary(ComponentLibrary library)
        {
            if (library != null)
            {
                currentLibrary = library;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 通过路径从Resources加载组件库
        /// </summary>
        public bool LoadLibraryFromResources(string resourcePath)
        {
            ComponentLibrary library = Resources.Load<ComponentLibrary>(resourcePath);
            return LoadLibrary(library);
        }
        
        /// <summary>
        /// 获取所有可见组件
        /// </summary>
        public List<ComponentData> GetAllVisibleComponents()
        {
            if (currentLibrary == null)
                return new List<ComponentData>();
                
            return currentLibrary.components.Where(c => !c.isHidden).ToList();
        }
        
        /// <summary>
        /// 根据类别获取组件
        /// </summary>
        public List<ComponentData> GetComponentsByCategory(ComponentCategory category)
        {
            if (currentLibrary == null)
                return new List<ComponentData>();
                
            return currentLibrary.GetComponentsByCategory(category);
        }
        
        /// <summary>
        /// 搜索组件
        /// </summary>
        public List<ComponentData> SearchComponents(string searchTerm)
        {
            if (currentLibrary == null)
                return new List<ComponentData>();
                
            return currentLibrary.SearchComponents(searchTerm);
        }
        
        /// <summary>
        /// 根据ID获取组件
        /// </summary>
        public ComponentData GetComponentById(string id)
        {
            if (currentLibrary == null)
                return null;
                
            return currentLibrary.GetComponentById(id);
        }
        
        /// <summary>
        /// 获取所有可用类别
        /// </summary>
        public List<ComponentCategory> GetAvailableCategories()
        {
            if (currentLibrary == null)
                return new List<ComponentCategory>();
                
            return currentLibrary.GetCategories();
        }
    }
}