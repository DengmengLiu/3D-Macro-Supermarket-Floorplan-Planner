using UnityEngine;
using UnityEngine.UI;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 极简相机视角切换器 - 只在两个预设相机之间切换
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
        
        // 缓存摄像机标签
        private const string MAIN_CAMERA_TAG = "MainCamera";
        
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
            
            // 初始设置顶视图摄像机不可见
            if (topViewCamera != null)
                topViewCamera.enabled = false;
            
            // 如果有视角切换按钮，设置点击事件
            if (viewToggleButton != null)
            {
                viewToggleButton.onClick.AddListener(ToggleView);
            }
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
                if (mainCamera != null)
                    mainCamera.enabled = false;
                
                if (topViewCamera != null)
                    topViewCamera.enabled = true;
            }
            else
            {
                // 切换到标准视图
                if (mainCamera != null)
                    mainCamera.enabled = true;
                
                if (topViewCamera != null)
                    topViewCamera.enabled = false;
            }
            
            // 确保Camera.main引用是正确的
            UpdateMainCameraReference();
        }
        
        /// <summary>
        /// 更新主摄像机引用
        /// </summary>
        private void UpdateMainCameraReference()
        {
            // 当切换摄像机时，确保当前活跃的摄像机被标记为"MainCamera"
            if (isTopView)
            {
                if (mainCamera.tag == MAIN_CAMERA_TAG)
                {
                    mainCamera.tag = "Untagged";
                }
                topViewCamera.tag = MAIN_CAMERA_TAG;
            }
            else
            {
                if (topViewCamera.tag == MAIN_CAMERA_TAG)
                {
                    topViewCamera.tag = "Untagged";
                }
                mainCamera.tag = MAIN_CAMERA_TAG;
            }
        }
        
        /// <summary>
        /// 获取当前活跃的摄像机
        /// </summary>
        public Camera GetActiveCamera()
        {
            return isTopView ? topViewCamera : mainCamera;
        }
    }
}