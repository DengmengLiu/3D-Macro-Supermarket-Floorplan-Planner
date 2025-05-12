using UnityEngine;
using UnityEngine.UI;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 摄像机视角控制器 - 负责在不同摄像机视角之间切换
    /// </summary>
    public class CameraViewController : MonoBehaviour
    {
        [Header("摄像机引用")]
        [Tooltip("主摄像机（标准视角）")]
        public Camera mainCamera;
        
        [Tooltip("俯视图摄像机")]
        public Camera topViewCamera;
        
        [Header("UI 引用")]
        [Tooltip("视角切换按钮")]
        public Button viewToggleButton;
        
        // 当前是否为俯视图
        private bool isTopView = false;
        
        // 保存的摄像机控制器引用
        private CameraController mainCameraController;
        private CameraController topViewCameraController;
        
        private void Start()
        {
            // 查找摄像机（如果未指定）
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (mainCamera == null)
            {
                Debug.LogError("未找到主摄像机！视角控制器无法工作。");
                enabled = false;
                return;
            }
            
            // 获取主摄像机的控制器
            mainCameraController = mainCamera.GetComponent<CameraController>();
            if (mainCameraController == null)
            {
                Debug.LogWarning("主摄像机上未找到CameraController组件。摄像机可能无法控制。");
            }
            
            // 如果未设置俯视图摄像机，尝试查找或创建
            if (topViewCamera == null)
            {
                // 尝试查找现有的顶视图摄像机
                topViewCamera = GameObject.Find("TopViewCamera")?.GetComponent<Camera>();
                
                // 如果未找到，创建一个新的
                if (topViewCamera == null)
                {
                    CreateTopViewCamera();
                }
            }
            
            // 获取或添加顶视图摄像机控制器
            topViewCameraController = topViewCamera.GetComponent<CameraController>();
            if (topViewCameraController == null)
            {
                // 复制主摄像机控制器的设置
                topViewCameraController = topViewCamera.gameObject.AddComponent<CameraController>();
                if (mainCameraController != null)
                {
                    // 复制相关设置
                    topViewCameraController.moveSpeed = mainCameraController.moveSpeed;
                    topViewCameraController.rotationSpeed = mainCameraController.rotationSpeed;
                    topViewCameraController.zoomSpeed = mainCameraController.zoomSpeed;
                    topViewCameraController.smoothTime = mainCameraController.smoothTime;
                    topViewCameraController.useBoundaries = mainCameraController.useBoundaries;
                    topViewCameraController.minHeight = mainCameraController.minHeight;
                    topViewCameraController.maxHeight = mainCameraController.maxHeight;
                    topViewCameraController.groundOffset = mainCameraController.groundOffset;
                    topViewCameraController.boundaryMargin = mainCameraController.boundaryMargin;
                }
            }
            
            // 初始化设置顶视图摄像机不可见
            topViewCamera.enabled = false;
            
            // 如果有视角切换按钮，设置点击事件
            if (viewToggleButton != null)
            {
                viewToggleButton.onClick.AddListener(ToggleView);
            }
            else
            {
                Debug.LogWarning("未指定视角切换按钮，使用键盘按键'V'切换视角。");
            }
        }
        
        private void Update()
        {
            // 如果没有设置按钮，可以通过按键切换视角（例如V键）
            if (viewToggleButton == null && Input.GetKeyDown(KeyCode.V))
            {
                ToggleView();
            }
        }
        
        /// <summary>
        /// 创建俯视图摄像机
        /// </summary>
        private void CreateTopViewCamera()
        {
            GameObject cameraObj = new GameObject("TopViewCamera");
            topViewCamera = cameraObj.AddComponent<Camera>();
            
            // 复制主摄像机的一些设置
            if (mainCamera != null)
            {
                topViewCamera.clearFlags = mainCamera.clearFlags;
                topViewCamera.backgroundColor = mainCamera.backgroundColor;
                topViewCamera.cullingMask = mainCamera.cullingMask;
                topViewCamera.fieldOfView = 60f; // 设置适合俯视图的视场角
                topViewCamera.nearClipPlane = mainCamera.nearClipPlane;
                topViewCamera.farClipPlane = mainCamera.farClipPlane;
                topViewCamera.depth = mainCamera.depth - 1; // 确保在主摄像机之下
            }
            
            // 设置初始位置和旋转
            Vector3 initialPosition = new Vector3(0, 50, 0);
            if (mainCamera != null)
            {
                // 使用主摄像机的XZ位置，但Y轴提高
                initialPosition = new Vector3(
                    mainCamera.transform.position.x,
                    50f,
                    mainCamera.transform.position.z
                );
            }
            
            topViewCamera.transform.position = initialPosition;
            topViewCamera.transform.rotation = Quaternion.Euler(90, 0, 0); // 向下看的旋转
            
            // 禁用摄像机
            topViewCamera.enabled = false;
        }
        
        /// <summary>
        /// 切换视角
        /// </summary>
        public void ToggleView()
        {
            isTopView = !isTopView;
            
            if (isTopView)
            {
                // 切换到俯视图
                SwitchToTopView();
            }
            else
            {
                // 切换到标准视图
                SwitchToNormalView();
            }
        }
        
        /// <summary>
        /// 切换到标准视图
        /// </summary>
        private void SwitchToNormalView()
        {
            if (mainCamera != null)
                mainCamera.enabled = true;
                
            if (topViewCamera != null)
                topViewCamera.enabled = false;
                
            // 启用/禁用相应的控制器
            if (mainCameraController != null)
                mainCameraController.enabled = true;
                
            if (topViewCameraController != null)
                topViewCameraController.enabled = false;
                
            Debug.Log("已切换到标准视图");
        }
        
        /// <summary>
        /// 切换到俯视图
        /// </summary>
        private void SwitchToTopView()
        {
            // 保存主相机的位置，以便顶视摄像机可以在相同的XZ平面上
            if (mainCamera != null && topViewCamera != null)
            {
                // 保持XZ位置不变，只改变Y轴高度和旋转
                Vector3 position = topViewCamera.transform.position;
                position.x = mainCamera.transform.position.x;
                position.z = mainCamera.transform.position.z;
                topViewCamera.transform.position = position;
            }
            
            if (mainCamera != null)
                mainCamera.enabled = false;
                
            if (topViewCamera != null)
                topViewCamera.enabled = true;
                
            // 启用/禁用相应的控制器
            if (mainCameraController != null)
                mainCameraController.enabled = false;
                
            if (topViewCameraController != null)
                topViewCameraController.enabled = true;
                
            Debug.Log("已切换到俯视图");
        }
        
    }
}