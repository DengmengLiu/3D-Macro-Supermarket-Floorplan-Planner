using UnityEngine;
using SupermarketPlanner.Managers;

namespace SupermarketPlanner.Services
{
    /// <summary>
    /// 网格对齐服务 - 负责处理对象到网格的对齐功能
    /// </summary>
    public class GridAlignmentService : MonoBehaviour
    {
        [Header("网格设置")]
        [Tooltip("是否启用网格对齐")]
        public bool enableGridAlignment = true;
        
        [Tooltip("网格切换阈值 (防止在网格边界徘徊时闪烁)")]
        public float gridSwitchThreshold = 0.1f;
        
        // 上次捕捉的网格位置
        private Vector3 lastSnappedPosition;
        
        // 延迟切换计时器
        private float positionSwitchTimer = 0f;
        
        // 网格管理器引用
        private GridManager gridManager;
        
        private void Start()
        {
            // 获取网格管理器引用
            gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogWarning("GridAlignmentService: 未找到GridManager，将使用默认网格尺寸");
            }
        }
        
        /// <summary>
        /// 将位置对齐到网格
        /// </summary>
        public Vector3 AlignToGrid(Vector3 worldPosition)
        {
            // 如果未启用网格对齐或网格管理器不可用，直接返回原始位置
            if (!enableGridAlignment || gridManager == null)
            {
                return worldPosition;
            }
            
            // 获取对齐到网格的位置
            Vector3 snappedPosition = gridManager.SnapToGrid(worldPosition);
            
            // 检查是否需要切换到新的网格位置
            bool shouldSwitchToNewPosition = ShouldSwitchToNewPosition(snappedPosition);
            
            // 更新计时器
            positionSwitchTimer += Time.deltaTime;
            
            if (shouldSwitchToNewPosition)
            {
                // 切换到新位置
                lastSnappedPosition = snappedPosition;
                positionSwitchTimer = 0f;
                return snappedPosition;
            }
            else
            {
                // 保持在上一个网格位置
                return lastSnappedPosition;
            }
        }
        
        /// <summary>
        /// 决定是否应该切换到新的网格位置
        /// </summary>
        private bool ShouldSwitchToNewPosition(Vector3 newSnappedPosition)
        {
            // 如果是第一次对齐，直接返回true
            if (lastSnappedPosition == Vector3.zero)
            {
                return true;
            }
            
            // 计算与上一个网格位置的距离
            float distance = Vector3.Distance(newSnappedPosition, lastSnappedPosition);
            
            // 如果距离足够大，立即切换
            if (distance >= (gridManager?.gridCellSize ?? 1.0f))
            {
                return true;
            }
            
            // 如果位置有变化但不大，使用计时器防止频繁切换
            if (distance > 0)
            {
                // 如果鼠标已经在同一区域停留足够长时间，切换位置
                if (positionSwitchTimer > 0.3f)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 设置网格对齐状态
        /// </summary>
        public void SetGridAlignmentEnabled(bool enabled)
        {
            enableGridAlignment = enabled;
            
            // 重置上次对齐位置和计时器
            if (enabled)
            {
                lastSnappedPosition = Vector3.zero;
                positionSwitchTimer = 0f;
            }
        }
        
        /// <summary>
        /// 获取网格单元格大小
        /// </summary>
        public float GetGridCellSize()
        {
            return gridManager?.gridCellSize ?? 1.0f;
        }
        
        /// <summary>
        /// 重置对齐状态
        /// </summary>
        public void ResetAlignment()
        {
            lastSnappedPosition = Vector3.zero;
            positionSwitchTimer = 0f;
        }
        
        /// <summary>
        /// 设置最后一个已对齐的位置
        /// </summary>
        public void SetLastSnappedPosition(Vector3 position)
        {
            lastSnappedPosition = position;
        }
    }
}