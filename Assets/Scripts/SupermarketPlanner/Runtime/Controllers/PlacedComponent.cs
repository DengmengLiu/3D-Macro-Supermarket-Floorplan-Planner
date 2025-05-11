using UnityEngine;
using SupermarketPlanner.Data;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// 已放置组件 - 附加到场景中放置的每个组件上
    /// </summary>
    public class PlacedComponent : MonoBehaviour
    {
        [Tooltip("组件ID")]
        public string componentId;
        
        [Tooltip("组件显示名称")]
        public string displayName;
        
        [Tooltip("组件类别")]
        public ComponentCategory category;
        
        [Tooltip("组件描述")]
        [TextArea(1, 3)]
        public string description;
        
        [Tooltip("是否被选中")]
        public bool isSelected = false;
        
        // 原始材质
        private Material[] originalMaterials;
        
        // 选中时的高亮材质
        private Material highlightMaterial;
        
        /// <summary>
        /// 初始化放置的组件
        /// </summary>
        public void Initialize(ComponentData componentData)
        {
            if (componentData == null)
            {
                Debug.LogError("尝试初始化放置的组件，但组件数据为空");
                return;
            }
            
            // 复制数据
            componentId = componentData.id;
            displayName = componentData.displayName;
            category = componentData.category;
            description = componentData.description;
            
            // 保存原始材质（用于选择高亮）
            SaveOriginalMaterials();
            
            // 创建高亮材质
            CreateHighlightMaterial();
        }
        
        /// <summary>
        /// 保存原始材质
        /// </summary>
        private void SaveOriginalMaterials()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterials = renderer.materials;
            }
            else
            {
                // 如果没有在根对象上找到渲染器，尝试查找子对象的渲染器
                Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
                if (childRenderers.Length > 0)
                {
                    originalMaterials = childRenderers[0].materials;
                }
            }
        }
        
        /// <summary>
        /// 创建高亮材质
        /// </summary>
        private void CreateHighlightMaterial()
        {
            highlightMaterial = new Material(Shader.Find("Specular"));
            highlightMaterial.color = new Color(1f, 0.8f, 0.2f, 1f);
            highlightMaterial.SetFloat("_Shininess", 0.7f);
        }
        
        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            // 应用高亮或恢复原始材质
            ApplyMaterials();
        }
        
        /// <summary>
        /// 应用材质（基于选中状态）
        /// </summary>
        private void ApplyMaterials()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                // 尝试获取子对象的渲染器
                Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
                if (childRenderers.Length > 0)
                {
                    renderer = childRenderers[0];
                }
                else
                {
                    return;
                }
            }
            
            if (isSelected)
            {
                // 创建高亮材质数组
                Material[] highlightMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < highlightMaterials.Length; i++)
                {
                    highlightMaterials[i] = highlightMaterial;
                }
                
                // 应用高亮材质
                renderer.materials = highlightMaterials;
            }
            else
            {
                // 恢复原始材质
                renderer.materials = originalMaterials;
            }
        }
    }
}