using UnityEngine;
using SupermarketPlanner.Data;
using System.Collections.Generic;
using System.Linq;

namespace SupermarketPlanner.Managers
{
    /// <summary>
    /// Component Manager - used to manage component libraries at runtime
    /// </summary>
    public class ComponentManager : MonoBehaviour
    {
        [Tooltip("Default component library resource")]
        public ComponentLibrary defaultLibrary;

        [Tooltip("Currently loaded library")]
        private ComponentLibrary currentLibrary;

        // Singleton instance
        public static ComponentManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton mode setting
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load the default library 
            if (defaultLibrary != null)
            {
                currentLibrary = defaultLibrary;
            }
            else
            {
                //Try to load from Resources 
                currentLibrary = Resources.Load<ComponentLibrary>("ComponentLibraries/DefaultComponentLibrary");

                if (currentLibrary == null)
                {
                    Debug.LogWarning("The default component library was not found. Please specify it in the ComponentManager or create it in Resources.");
                }
            }
        }

        /// <summary> 
        /// Get the currently loaded library 
        /// </summary> 
        public ComponentLibrary GetCurrentLibrary()
        {
            return currentLibrary;
        }

        /// <summary> 
        /// Load new component library 
        /// </summary> 
        public bool LoadLibrary(ComponentLibrary library)
        {
            if (library != null)
            {
                currentLibrary = library; return true;
            }
            return false;
        }

        /// <summary>
        /// Load component library from Resources by path
        /// </summary>
        public bool LoadLibraryFromResources(string resourcePath)
        {
            ComponentLibrary library = Resources.Load<ComponentLibrary>(resourcePath);
            return LoadLibrary(library);
        }

        /// <summary>
        /// Get all visible components
        /// </summary>
        public List<ComponentData> GetAllVisibleComponents()
        {
            if (currentLibrary == null)
                return new List<ComponentData>();

            return currentLibrary.components.Where(c => !c.isHidden).ToList();
        }

        /// <summary>
        /// Get components by category
        /// </summary>
        public List<ComponentData> GetComponentsByCategory(ComponentCategory category)
        {
            if (currentLibrary == null)
                return new List<ComponentData>();

            return currentLibrary.GetComponentsByCategory(category);
        }

        /// <summary> 
        /// Search component 
        /// </summary> 
        public List<ComponentData> SearchComponents(string searchTerm)
        {
            if (currentLibrary == null)
                return new List<ComponentData>();

            return currentLibrary.SearchComponents(searchTerm);
        }

        /// <summary> 
        /// Get the component based on ID 
        /// </summary> 
        public ComponentData GetComponentById(string id)
        {
            if (currentLibrary == null)
                return null;

            return currentLibrary.GetComponentById(id);
        }

        /// <summary> 
        /// Get all available categories 
        /// </summary> 
        public List<ComponentCategory> GetAvailableCategories()
        {
            if (currentLibrary == null)
                return new List<ComponentCategory>();

            return currentLibrary.GetCategories();
        }
    }
}