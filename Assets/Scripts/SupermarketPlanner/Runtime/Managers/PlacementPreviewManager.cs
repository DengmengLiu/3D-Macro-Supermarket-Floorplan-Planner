using UnityEngine;
using UnityEngine.InputSystem;
using SupermarketPlanner.Data;
using System;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 放置预览管理器 - 负责管理组件放置的预览显示
    /// </summary>
    public class PlacementPreviewManager : MonoBehaviour
    {
        [Header("预览设置")]
        [Tooltip("预览对象的材质")]
        public Material previewMaterial;

        [Tooltip("有效放置时的颜色")]
        public Color validPlacementColor = new Color(0, 1, 0, 0.5f);

        [Tooltip("无效放置时的颜色")]
        public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);

        [Tooltip("放置层掩码")]
        public LayerMask placementLayerMask;

        [Tooltip("根据地板厚度的高度放置偏移")]
        public float heightOffset = 0.05f; // 这将是地板厚度的一半

        [Header("地板参考")]
        [Tooltip("地板对象引用")]
        public GameObject floorObject;

        // 预览对象
        private GameObject previewObject;

        // 组件数据
        private ComponentData componentData;

        // 旋转角度
        private float previewRotation = 0f;

        // 输入处理器
        private InputAction mousePositionAction;

        private void Awake()
        {
            // 获取地板对象
            if (floorObject == null)
            {
                floorObject = GameObject.FindWithTag("Floor");
                if (floorObject == null)
                {
                    Debug.LogWarning("PlacementPreviewManager: 未找到地板对象，将使用场景中名为'SupermarketFloor'的对象");
                    floorObject = GameObject.Find("SupermarketFloor");
                }
            }

            // 创建一个材质
            if (previewMaterial == null)
            {
                // 创建一个简单的半透明材质
                previewMaterial = new Material(Shader.Find("Standard"));
                previewMaterial.color = validPlacementColor;
                previewMaterial.SetFloat("_Mode", 3); // 透明模式
                previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMaterial.SetInt("_ZWrite", 0);
                previewMaterial.DisableKeyword("_ALPHATEST_ON");
                previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                previewMaterial.renderQueue = 3000;
            }

            // 创建鼠标位置输入动作
            mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");
            mousePositionAction.Enable();
        }

        /// <summary>
        /// 创建预览对象
        /// </summary>
        public void CreatePreview(ComponentData component)
        {
            // 保存组件数据
            componentData = component;

            // 确保之前的预览已清理
            ClearPreview();

            if (component != null && component.prefab != null)
            {
                try
                {
                    // 创建预览对象
                    previewObject = Instantiate(component.prefab);
                    previewObject.name = "Preview_" + component.displayName;

                    // 设置为子对象
                    previewObject.transform.SetParent(transform);

                    // 应用预览材质
                    ApplyPreviewMaterial(previewObject);

                    // 禁用碰撞器和任何脚本
                    DisableComponents(previewObject);

                    // 设置初始旋转
                    previewRotation = 0f;
                    previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);

                    // 将预览对象设置到特殊层，确保不会与碰撞检测冲突
                    int previewLayer = LayerMask.NameToLayer("Ignore Raycast");
                    if (previewLayer != -1)
                    {
                        SetLayerRecursively(previewObject, previewLayer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"创建预览时出错: {ex.Message}");
                    ClearPreview();
                }
            }
        }

        /// <summary>
        /// 递归设置游戏对象及其所有子对象的层
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
                return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 应用预览材质
        /// </summary>
        private void ApplyPreviewMaterial(GameObject obj)
        {
            // 获取所有渲染器
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"预览对象 '{obj.name}' 没有渲染器组件");
                return;
            }

            // 保存原始材质以便恢复
            foreach (Renderer renderer in renderers)
            {
                // 制作预览材质的副本并应用
                Material previewMaterialInstance = new Material(previewMaterial);

                // 如果原始材质有主纹理，保留
                if (renderer.material.mainTexture != null)
                {
                    previewMaterialInstance.mainTexture = renderer.material.mainTexture;
                }

                // 应用预览材质
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterialInstance;
                }
                renderer.materials = materials;
            }
        }

        /// <summary>
        /// 禁用对象上的组件
        /// </summary>
        private void DisableComponents(GameObject obj)
        {
            // 禁用所有碰撞器
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            // 禁用所有非必要的组件
            MonoBehaviour[] behaviours = obj.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                // 除了Transform和必要的可视化组件外，禁用所有脚本
                behaviour.enabled = false;
            }
        }

        /// <summary>
        /// 更新预览位置和状态
        /// </summary>
        public void UpdatePreview()
        {
            if (previewObject == null)
                return;

            // 获取鼠标位置
            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            Vector3 worldPos = GetWorldPosition(mousePos);

            // 设置预览对象位置
            previewObject.transform.position = worldPos;

            // 设置预览对象旋转
            previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);
        }

        /// <summary>
        /// 获取世界坐标
        /// </summary>
        private Vector3 GetWorldPosition(Vector2 screenPosition)
        {
            // 获取主相机
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("未找到主相机");
                return Vector3.zero;
            }

            // 将屏幕坐标转换为射线
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            // 这里替换为固定高度的方法
            // 获取光线与XZ平面的交点
            float t = 0;
            if (floorObject != null)
            {
                // 如果有地板对象，使用其Y坐标加上偏移量
                // 假设地板坐标是中心点，所以加上地板厚度的一半（即heightOffset）
                float floorY = floorObject.transform.position.y + heightOffset;

                // 计算射线与XZ平面（Y=floorY）的交点
                if (ray.direction.y != 0)
                {
                    t = (floorY - ray.origin.y) / ray.direction.y;
                }
            }
            else
            {
                // 如果没有地板对象，使用固定的Y=0平面
                if (ray.direction.y != 0)
                {
                    t = (heightOffset - ray.origin.y) / ray.direction.y;
                }
            }

            if (t < 0)
            {
                // 射线朝向远离地板，返回一个默认位置
                return new Vector3(0, heightOffset, 0);
            }

            // 获取交点坐标
            Vector3 worldPos = ray.origin + ray.direction * t;

            return worldPos;
        }

        /// <summary>
        /// 更新预览颜色
        /// </summary>
        public void UpdatePreviewColor(bool canPlace)
        {
            if (previewObject == null)
                return;

            // 获取所有渲染器
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();

            // 根据是否可以放置设置颜色
            Color color = canPlace ? validPlacementColor : invalidPlacementColor;

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    material.color = color;
                }
            }
        }

        /// <summary>
        /// 旋转预览
        /// </summary>
        public void RotatePreview()
        {
            if (previewObject == null)
                return;

            // 旋转90度
            previewRotation = (previewRotation + 90) % 360;
            previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);
        }

        /// <summary>
        /// 清理预览
        /// </summary>
        public void ClearPreview()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }

        /// <summary>
        /// 获取预览对象
        /// </summary>
        public GameObject GetPreviewObject()
        {
            return previewObject;
        }

        /// <summary>
        /// 获取目标位置
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            return previewObject?.transform.position ?? Vector3.zero;
        }
    }
}