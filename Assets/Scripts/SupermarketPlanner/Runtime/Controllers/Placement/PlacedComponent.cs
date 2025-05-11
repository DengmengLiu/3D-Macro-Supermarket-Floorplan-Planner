using UnityEngine;
using SupermarketPlanner.Data;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 已放置的组件 - 附加到场景中放置的组件对象上
    /// </summary>
    public class PlacedComponent : MonoBehaviour
    {
        // 组件数据
        public ComponentData componentData { get; private set; }
        
        // 放置位置
        public Vector3 placementPosition { get; private set; }
        
        // 放置旋转
        public Quaternion placementRotation { get; private set; }
        
        // 放置日期时间
        public System.DateTime placementTime { get; private set; }
        
        // 是否被选中
        public bool isSelected { get; private set; }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        public void Initialize(ComponentData data)
        {
            componentData = data;
            placementPosition = transform.position;
            placementRotation = transform.rotation;
            placementTime = System.DateTime.Now;
            isSelected = false;
        }
        
        /// <summary>
        /// 选中组件
        /// </summary>
        public void Select()
        {
            isSelected = true;
            // 可以在这里添加选中效果
        }
        
        /// <summary>
        /// 取消选中组件
        /// </summary>
        public void Deselect()
        {
            isSelected = false;
            // 可以在这里移除选中效果
        }
        
        /// <summary>
        /// 获取组件信息
        /// </summary>
        public string GetComponentInfo()
        {
            if (componentData == null)
                return "Unknown Component";
                
            return $"{componentData.displayName} ({componentData.category})";
        }
    }
}