using UnityEngine;
using SupermarketPlanner.Data;
using System;

namespace SupermarketPlanner.Services
{
    /// <summary>
    /// Component Placement Service - responsible for handling the actual placement logic of the component
    /// </summary>
    public class ComponentPlacementService : MonoBehaviour
    {
        [Header("Placement Settings")]
        [Tooltip("Parent Node of the Placed Object")]
        public Transform placedObjectsContainer;

        public event Action<GameObject, ComponentData> OnComponentPlaced;
        public event Action<GameObject> OnComponentRemoved;

        private void Start()
        {
            // If no placement container is specified, create one
            if (placedObjectsContainer == null)
            {
                GameObject container = new GameObject("PlacedObjects");
                placedObjectsContainer = container.transform;
            }
        }

        /// <summary>
        /// Placement Component
        /// </summary>
        public GameObject PlaceComponent(ComponentData componentData, Vector3 position, Quaternion rotation)
        {
            if (componentData == null || componentData.prefab == null)
            {
                Debug.LogError("Cannot place: component data or prefab is null");
                return null;
            }

            try
            {
                // Create the actual component
                GameObject placedObject = Instantiate(componentData.prefab, position, rotation);

                // Set the name
                placedObject.name = componentData.displayName;

                // Set the parent
                if (placedObjectsContainer != null)
                {
                    placedObject.transform.SetParent(placedObjectsContainer);
                }

                // Add component identifier
                PlacedComponent identifier = placedObject.AddComponent<PlacedComponent>();
                identifier.Initialize(componentData);

                // Activate all colliders
                Collider[] colliders = placedObject.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = true;
                }

                // Trigger placement event
                OnComponentPlaced?.Invoke(placedObject, componentData);

                // Debug.Log($"Placed component: {componentData.displayName} at position: {position}");

                return placedObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error occurred while placing component: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Remove component
        /// </summary>
        public bool RemoveComponent(GameObject component)
        {
            if (component == null)
                return false;

            try
            {
                // Trigger removal event
                OnComponentRemoved?.Invoke(component);

                // Destroy object
                Destroy(component);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error occurred while removing component: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get placed components
        /// </summary>
        public PlacedComponent[] GetAllPlacedComponents()
        {
            if (placedObjectsContainer == null)
                return new PlacedComponent[0];

            return placedObjectsContainer.GetComponentsInChildren<PlacedComponent>();
        }

        /// <summary>
        /// Clear all placed components
        /// </summary>
        public void ClearAllPlacedComponents()
        {
            PlacedComponent[] components = GetAllPlacedComponents();

            foreach (PlacedComponent component in components)
            {
                if (component != null && component.gameObject != null)
                {
                    RemoveComponent(component.gameObject);
                }
            }
        }

        /// <summary>
        /// Duplicate component
        /// </summary>
        public GameObject DuplicateComponent(GameObject original, Vector3 newPosition)
        {
            if (original == null)
                return null;

            // Get component identifier
            PlacedComponent identifier = original.GetComponent<PlacedComponent>();
            if (identifier == null || identifier.ComponentData == null)
            {
                Debug.LogWarning("Cannot copy: original object has no valid component data");
                return null;
            }

            // Create a new component using the data and rotation of the original component
            return PlaceComponent(
            identifier.ComponentData,
            newPosition,
            original.transform.rotation
            );
        }
    }

    /// <summary>
    /// Placed component identifier - used to identify the placed component
    /// </summary>
    public class PlacedComponent : MonoBehaviour
    {
        // Component data
        public ComponentData ComponentData { get; private set; }

        // Placement time
        public DateTime PlacementTime { get; private set; }

        // Unique identifier
        public string UniqueId { get; private set; }

        /// <summary>
        /// Initialize component identifier
        /// </summary>
        public void Initialize(ComponentData componentData)
        {
            ComponentData = componentData;
            PlacementTime = DateTime.Now;
            UniqueId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 获取组件类型
        /// </summary>
        public ComponentCategory GetComponentCategory()
        {
            return ComponentData?.category ?? ComponentCategory.Other;
        }
    }
}