using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SupermarketPlanner.Data
{
    /// <summary>
    /// 超市组件库，作为ScriptableObject资源存储所有可用的组件
    /// </summary>
    [CreateAssetMenu(fileName = "SupermarketComponentLibrary", menuName = "Supermarket Planner/Component Library")]
    public class ComponentLibrary : ScriptableObject
    {
        [Header("Library Information")]
        [Tooltip("Component Library Name")]
        public string libraryName = "Default Component Library";

        [Tooltip("Component Library Version")]
        public string version = "1.0";

        [Tooltip("Component Library Description")]
        [TextArea(2, 5)]
        public string description = "Default component library for the Supermarket Planner.";

        [Header("Component List")]
        [Tooltip("All Components in this Library")]
        public List<ComponentData> components = new List<ComponentData>();

        [Header("Folder Configuration")]
        [Tooltip("Prefab Folder Path (Editor Only)")]
        public string prefabFolderPath = "Assets/Prefabs/Components";

        [Tooltip("Icon Folder Path (Editor Only)")]
        public string iconFolderPath = "Assets/Textures/Icons";

        [Header("Automatic classification configuration")]
        [Tooltip("Keyword mapping for automatic classification")]
        public CategoryKeywordMap[] categoryKeywords;

        /// <summary>
        /// 根据类别获取组件列表
        /// </summary>
        public List<ComponentData> GetComponentsByCategory(ComponentCategory category)
        {
            return components.Where(c => c.category == category && !c.isHidden).ToList();
        }

        /// <summary>
        /// 根据关键字搜索组件
        /// </summary>
        public List<ComponentData> SearchComponents(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return components.Where(c => !c.isHidden).ToList();

            searchTerm = searchTerm.ToLower();

            return components.Where(c =>
                !c.isHidden && (
                    c.displayName.ToLower().Contains(searchTerm) ||
                    c.description.ToLower().Contains(searchTerm) ||
                    c.subCategory.ToLower().Contains(searchTerm) ||
                    c.tags.Any(t => t.ToLower().Contains(searchTerm))
                )
            ).ToList();
        }

        /// <summary>
        /// 通过ID查找组件
        /// </summary>
        public ComponentData GetComponentById(string id)
        {
            return components.FirstOrDefault(c => c.id == id);
        }

        /// <summary>
        /// 添加新组件到库中
        /// </summary>
        public void AddComponent(ComponentData component)
        {
            // 确保ID不重复
            if (string.IsNullOrEmpty(component.id))
            {
                component.id = System.Guid.NewGuid().ToString();
            }
            else if (components.Any(c => c.id == component.id))
            {
                Debug.LogWarning($"Component ID '{component.id}' already exists, generating a new ID.");
                component.id = System.Guid.NewGuid().ToString();
            }

            components.Add(component);
        }

        /// <summary>
        /// 从库中移除组件
        /// </summary>
        public bool RemoveComponent(string id)
        {
            int index = components.FindIndex(c => c.id == id);
            if (index >= 0)
            {
                components.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取库中的类别列表
        /// </summary>
        public List<ComponentCategory> GetCategories()
        {
            return components
                .Where(c => !c.isHidden)
                .Select(c => c.category)
                .Distinct()
                .OrderBy(c => c.ToString())
                .ToList();
        }
    }

    /// <summary>
    /// 类别关键字映射，用于自动分类预制件
    /// </summary>
    [System.Serializable]
    public class CategoryKeywordMap
    {
        public ComponentCategory category;
        [Tooltip("如果预制件名称包含这些关键字之一，它将被分配给此类别")]
        public string[] keywords;
    }
}