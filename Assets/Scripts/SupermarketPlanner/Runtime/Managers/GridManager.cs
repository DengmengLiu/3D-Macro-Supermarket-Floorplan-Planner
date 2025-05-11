using UnityEngine;
using System.Collections.Generic;

namespace SupermarketPlanner.Managers
{
    /// <summary>
    /// 网格管理器 - 负责在场景中创建和管理网格
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("网格设置")]
        [Tooltip("网格单元大小（米）")]
        public float gridCellSize = 1.0f;

        [Tooltip("网格是否可见")]
        public bool gridVisible = true;

        [Tooltip("网格线条颜色")]
        public Color gridColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        [Tooltip("主轴线颜色（每5或10个单位）")]
        public Color majorGridColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

        [Tooltip("每隔多少个单位绘制主轴线")]
        public int majorGridInterval = 5;

        [Header("网格组件")]
        [Tooltip("网格线条预制件")]
        public GameObject gridLinePrefab;

        // 网格线条对象池
        private List<GameObject> horizontalLines = new List<GameObject>();
        private List<GameObject> verticalLines = new List<GameObject>();

        // 当前地板参考
        private GameObject floorObject;
        private Vector3 floorSize;
        private Vector3 floorPosition;

        // 网格原点 - 添加这个变量来存储网格的左下角原点
        private Vector3 gridOrigin;

        // 单例实例
        public static GridManager Instance { get; private set; }

        private void Awake()
        {
            // 单例模式设置
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 初始化网格线条预制件
            if (gridLinePrefab == null)
            {
                CreateDefaultLinePrefab();
            }
        }

        /// <summary>
        /// 创建默认线条预制件
        /// </summary>
        private void CreateDefaultLinePrefab()
        {
            gridLinePrefab = new GameObject("GridLine");

            // 添加线渲染器组件
            LineRenderer lineRenderer = gridLinePrefab.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = gridColor;
            lineRenderer.endColor = gridColor;
            lineRenderer.positionCount = 2;

            // 将预制件设为不可见
            gridLinePrefab.SetActive(false);
        }

        /// <summary>
        /// 设置地板引用并创建网格
        /// </summary>
        public void SetFloor(GameObject floor, float width, float length)
        {
            floorObject = floor;
            floorSize = new Vector3(width, 0.1f, length);
            floorPosition = floor.transform.position;

            // 使用当前设置创建网格
            CreateGrid();
        }

        /// <summary>
        /// 创建网格
        /// </summary>
        public void CreateGrid()
        {
            if (floorObject == null)
            {
                Debug.LogWarning("GridManager: 未设置地板引用，无法创建网格");
                return;
            }

            // 先清理现有网格
            ClearGrid();

            // 计算地板边界 - 确保使用准确的边界
            Vector3 floorMin = floorPosition - new Vector3(floorSize.x, 0, floorSize.z) / 2f;
            Vector3 floorMax = floorPosition + new Vector3(floorSize.x, 0, floorSize.z) / 2f;
            
            // 存储网格原点 (左下角)
            gridOrigin = floorMin;
            
            // 设置网格高度（略高于地板表面）
            float gridHeight = floorPosition.y + 0.05f;

            // 修改网格生成逻辑，确保网格线只在地板范围内绘制
            // 首先计算有多少个网格单元在地板范围内
            int horizontalCellCount = Mathf.FloorToInt(floorSize.z / gridCellSize) + 1;
            int verticalCellCount = Mathf.FloorToInt(floorSize.x / gridCellSize) + 1;

            // 创建水平线（Z轴方向，从前到后）
            for (int i = 0; i <= horizontalCellCount; i++)
            {
                float z = floorMin.z + i * gridCellSize;
                
                // 确保不超出地板边界
                if (z > floorMax.z)
                    continue;

                // 计算网格线索引，用于确定是否为主轴线
                bool isMajor = i % majorGridInterval == 0;

                // 创建的线段起点和终点都应该在地板范围内
                Vector3 startPoint = new Vector3(floorMin.x, gridHeight, z);
                Vector3 endPoint = new Vector3(floorMax.x, gridHeight, z);

                // 创建网格线
                GameObject line = CreateGridLine(startPoint, endPoint, isMajor ? majorGridColor : gridColor);
                horizontalLines.Add(line);
            }

            // 创建垂直线（X轴方向，从左到右）
            for (int i = 0; i <= verticalCellCount; i++)
            {
                float x = floorMin.x + i * gridCellSize;
                
                // 确保不超出地板边界
                if (x > floorMax.x)
                    continue;

                // 计算网格线索引，用于确定是否为主轴线
                bool isMajor = i % majorGridInterval == 0;

                // 创建的线段起点和终点都应该在地板范围内
                Vector3 startPoint = new Vector3(x, gridHeight, floorMin.z);
                Vector3 endPoint = new Vector3(x, gridHeight, floorMax.z);

                // 创建网格线
                GameObject line = CreateGridLine(startPoint, endPoint, isMajor ? majorGridColor : gridColor);
                verticalLines.Add(line);
            }

            // 设置网格可见性
            SetGridVisibility(gridVisible);

            Debug.Log($"创建了网格: {horizontalLines.Count} 水平线, {verticalLines.Count} 垂直线");
        }

        /// <summary>
        /// 创建单条网格线
        /// </summary>
        private GameObject CreateGridLine(Vector3 start, Vector3 end, Color color)
        {
            GameObject line = Instantiate(gridLinePrefab, floorObject.transform);
            line.SetActive(true);
            line.name = "GridLine";

            // Set the layer to a dedicated "Grid" layer
            int gridLayer = LayerMask.NameToLayer("Grid");
            if (gridLayer != -1)
            {
                line.layer = gridLayer;
            }

            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            return line;
        }
        
        /// <summary>
        /// 清理网格
        /// </summary>
        public void ClearGrid()
        {
            // 销毁水平线
            foreach (GameObject line in horizontalLines)
            {
                if (line != null)
                {
                    Destroy(line);
                }
            }
            horizontalLines.Clear();

            // 销毁垂直线
            foreach (GameObject line in verticalLines)
            {
                if (line != null)
                {
                    Destroy(line);
                }
            }
            verticalLines.Clear();
        }

        /// <summary>
        /// 设置网格单元大小
        /// </summary>
        public void SetGridCellSize(float size)
        {
            // 确保大小在有效范围内
            gridCellSize = Mathf.Clamp(size, 0.1f, 10f);

            // 重新创建网格
            CreateGrid();
        }

        /// <summary>
        /// 设置网格可见性
        /// </summary>
        public void SetGridVisibility(bool visible)
        {
            gridVisible = visible;

            // 设置所有网格线的可见性
            foreach (GameObject line in horizontalLines)
            {
                if (line != null)
                {
                    line.SetActive(visible);
                }
            }

            foreach (GameObject line in verticalLines)
            {
                if (line != null)
                {
                    line.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// 将世界坐标对齐到网格
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            // 计算物体位置相对于网格原点(左下角)的偏移
            float xOffset = worldPosition.x - gridOrigin.x;
            float zOffset = worldPosition.z - gridOrigin.z;
            
            // 将偏移量四舍五入到最近的网格单位
            float xGridOffset = Mathf.Round(xOffset / gridCellSize) * gridCellSize;
            float zGridOffset = Mathf.Round(zOffset / gridCellSize) * gridCellSize;
            
            // 将网格偏移量转换回世界坐标
            float x = gridOrigin.x + xGridOffset;
            float z = gridOrigin.z + zGridOffset;
            
            // 确保位置在地板范围内
            Vector3 floorMin = floorPosition - floorSize / 2;
            Vector3 floorMax = floorPosition + floorSize / 2;

            x = Mathf.Clamp(x, floorMin.x, floorMax.x);
            z = Mathf.Clamp(z, floorMin.z, floorMax.z);

            return new Vector3(x, worldPosition.y, z);
        }
    }
}