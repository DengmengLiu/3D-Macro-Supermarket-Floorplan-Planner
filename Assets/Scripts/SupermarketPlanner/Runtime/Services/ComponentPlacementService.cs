using UnityEngine;
using SupermarketPlanner.Data;
using System;

namespace SupermarketPlanner.Services
{
    /// <summary>
    /// 组件放置服务 - 负责处理组件的实际放置逻辑
    /// </summary>
    public class ComponentPlacementService : MonoBehaviour
    {
        [Header("放置设置")]
        [Tooltip("放置后的物体父节点")]
        public Transform placedObjectsContainer;

        public event Action<GameObject, ComponentData> OnComponentPlaced;
        public event Action<GameObject> OnComponentRemoved;

        private void Start()
        {
            // 如果没有指定放置容器，创建一个
            if (placedObjectsContainer == null)
            {
                GameObject container = new GameObject("PlacedObjects");
                placedObjectsContainer = container.transform;
            }
        }

        /// <summary>
        /// 放置组件
        /// </summary>
        public GameObject PlaceComponent(ComponentData componentData, Vector3 position, Quaternion rotation)
        {
            if (componentData == null || componentData.prefab == null)
            {
                Debug.LogError("无法放置：组件数据或预制件为空");
                return null;
            }

            try
            {
                // 创建实际组件
                GameObject placedObject = Instantiate(componentData.prefab, position, rotation);

                // 设置名称
                placedObject.name = componentData.displayName;

                // 设置父对象
                if (placedObjectsContainer != null)
                {
                    placedObject.transform.SetParent(placedObjectsContainer);
                }

                // 添加组件标识符
                PlacedComponent identifier = placedObject.AddComponent<PlacedComponent>();
                identifier.Initialize(componentData);

                // 激活所有碰撞体
                Collider[] colliders = placedObject.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = true;
                }

                // 触发放置事件
                OnComponentPlaced?.Invoke(placedObject, componentData);

                Debug.Log($"已放置组件: {componentData.displayName} 在位置: {position}");

                return placedObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"放置组件时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        public bool RemoveComponent(GameObject component)
        {
            if (component == null)
                return false;

            try
            {
                // 触发移除事件
                OnComponentRemoved?.Invoke(component);

                // 销毁对象
                Destroy(component);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"移除组件时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取已放置的组件
        /// </summary>
        public PlacedComponent[] GetAllPlacedComponents()
        {
            if (placedObjectsContainer == null)
                return new PlacedComponent[0];

            return placedObjectsContainer.GetComponentsInChildren<PlacedComponent>();
        }

        /// <summary>
        /// 清空所有已放置的组件
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
        /// 复制组件
        /// </summary>
        public GameObject DuplicateComponent(GameObject original, Vector3 newPosition)
        {
            if (original == null)
                return null;

            // 获取组件标识符
            PlacedComponent identifier = original.GetComponent<PlacedComponent>();
            if (identifier == null || identifier.ComponentData == null)
            {
                Debug.LogWarning("无法复制：原始对象没有有效的组件数据");
                return null;
            }

            // 使用原始组件的数据和旋转创建新组件
            return PlaceComponent(
                identifier.ComponentData,
                newPosition,
                original.transform.rotation
            );
        }
    }

    /// <summary>
    /// 已放置组件标识符 - 用于标识已放置的组件
    /// </summary>
    public class PlacedComponent : MonoBehaviour
    {
        // 组件数据
        public ComponentData ComponentData { get; private set; }

        // 放置时间
        public DateTime PlacementTime { get; private set; }

        // 唯一标识符
        public string UniqueId { get; private set; }

        /// <summary>
        /// 初始化组件标识符
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