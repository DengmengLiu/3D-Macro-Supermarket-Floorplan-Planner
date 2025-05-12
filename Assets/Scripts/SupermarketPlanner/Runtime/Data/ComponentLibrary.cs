using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SupermarketPlanner.Data
{
    /// <summary>
/// Supermarket component library, storing all available components as ScriptableObject resources
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
        /// Get component list by category
        /// </summary>
        public List<ComponentData> GetComponentsByCategory(ComponentCategory category)
        {
            return components.Where(c => c.category == category && !c.isHidden).ToList();
        }

        /// <summary>
        /// Search components by keyword
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
        /// Find a component by ID
        /// </summary>
        public ComponentData GetComponentById(string id)
        {
            return components.FirstOrDefault(c => c.id == id);
        }

        /// <summary>
        /// Adding new components to the library
        /// </summary>
        public void AddComponent(ComponentData component)
        {
            // Make sure IDs are not repeated
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
        /// Removing a component from a library
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
        /// Get a list of categories in a library
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
    /// Category keyword mapping for automatic categorization of prefabs
    /// </summary>
    [System.Serializable]
    public class CategoryKeywordMap
    {
        public ComponentCategory category;
        [Tooltip("If the prefab name contains one of these keywords it will be assigned to this category")]
        public string[] keywords;
    }
}