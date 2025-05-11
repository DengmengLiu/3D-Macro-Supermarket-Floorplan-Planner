using UnityEngine;
using SupermarketPlanner.Controllers;

namespace SupermarketPlanner.Services
{
    /// <summary>
    /// 简单放置验证服务 - 使用Physics.OverlapBox直接检测碰撞
    /// </summary>
    public class PlacementValidationService : MonoBehaviour
    {[Header("验证设置")]
        [Tooltip("允许的碰撞层 (哪些层的物体可以碰撞但仍允许放置，比如地板层)")]
        public LayerMask allowedCollisionLayers;

        [Tooltip("调试模式 - 显示碰撞检测区域")]
        public bool showDebugVisuals = false;

        [Tooltip("碰撞检测外扩大小 (米)")]
        public float collisionMargin = 0.05f;

        // 最后一次检测的边界 (用于调试可视化)
        private Bounds lastCheckedBounds;
        private bool lastCheckResult = true;

        private void Start()
        {
            // 确保地板层被添加到允许的碰撞层
            if (allowedCollisionLayers.value == 0)
            {
                // 假设地板使用的是"Floor"层
                int floorLayer = LayerMask.NameToLayer("Floor");
                if (floorLayer != -1)
                {
                    allowedCollisionLayers = 1 << floorLayer;
                    Debug.Log($"自动设置允许的碰撞层为Floor层 (Layer {floorLayer})");
                }
                else
                {
                    Debug.LogWarning("未找到Floor层，请手动设置允许的碰撞层");
                }
            }
        }

        /// <summary>
        /// 检查物体是否可以放置在指定位置
        /// </summary>
        public bool CanPlace(GameObject objectToPlace)
        {
            if (objectToPlace == null)
                return false;

            // 计算物体的边界框
            Bounds bounds = GetObjectBounds(objectToPlace);
            
            // 稍微扩大边界以确保更准确的碰撞检测
            bounds.Expand(collisionMargin);

            // 保存用于调试绘制
            lastCheckedBounds = bounds;

            // 检查边界框区域内是否有碰撞
            bool isAreaClear = IsAreaClear(bounds, objectToPlace.transform.rotation);
            
            // 保存结果用于调试
            lastCheckResult = isAreaClear;
            
            return isAreaClear;
        }

        /// <summary>
        /// 检查指定区域是否没有碰撞
        /// </summary>
        private bool IsAreaClear(Bounds bounds, Quaternion rotation)
        {
            // 计算半尺寸 (Physics.OverlapBox使用半尺寸)
            Vector3 halfExtents = bounds.extents;
            
            // 创建一个不包括允许层的层掩码 (~运算符是按位取反)
            int layerMask = ~allowedCollisionLayers;
            
            // 使用Physics.OverlapBox检测区域内的碰撞器
            Collider[] colliders = Physics.OverlapBox(
                bounds.center,  // 中心点
                halfExtents,    // 半尺寸
                rotation,       // 旋转
                layerMask,      // 层掩码
                QueryTriggerInteraction.Ignore // 忽略触发器
            );

            // 如果检测到碰撞器，输出调试信息
            if (colliders.Length > 0 && showDebugVisuals)
            {
                foreach (Collider collider in colliders)
                {
                    Debug.Log($"碰撞检测到: {collider.gameObject.name} (Layer: {LayerMask.LayerToName(collider.gameObject.layer)})");
                }
            }

            // 如果没有找到碰撞器，则区域是空的
            return colliders.Length == 0;
        }

        /// <summary>
        /// 获取物体的边界框
        /// </summary>
        private Bounds GetObjectBounds(GameObject obj)
        {
            // 获取所有渲染器
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            
            // 如果有渲染器，使用渲染器的边界
            if (renderers.Length > 0)
            {
                // 从第一个渲染器开始
                Bounds bounds = renderers[0].bounds;
                
                // 合并所有其他渲染器的边界
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                
                return bounds;
            }
            
            // 如果没有渲染器，则尝试使用碰撞器
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
                
                return bounds;
            }
            
            // 如果既没有渲染器也没有碰撞器，使用对象的变换信息
            return new Bounds(obj.transform.position, obj.transform.lossyScale);
        }

        /// <summary>
        /// 可视化碰撞检测区域 (仅在调试模式下)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugVisuals || !Application.isPlaying)
                return;

            // 根据最后的检查结果设置颜色
            Gizmos.color = lastCheckResult ? Color.green : Color.red;
            
            // 用线框表示检测的边界框
            Gizmos.DrawWireCube(lastCheckedBounds.center, lastCheckedBounds.size);
            
            // 使用半透明立方体填充
            Color fillColor = lastCheckResult ? 
                new Color(0, 1, 0, 0.2f) : // 绿色半透明
                new Color(1, 0, 0, 0.2f);  // 红色半透明
                
            Gizmos.color = fillColor;
            Gizmos.DrawCube(lastCheckedBounds.center, lastCheckedBounds.size);
        }
    }
}