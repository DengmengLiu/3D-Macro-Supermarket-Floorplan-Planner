using UnityEngine;
using System;

namespace SupermarketPlanner.Data
{
    /// <summary>
    /// 表示超市规划工具中单个组件的数据
    /// </summary>
    [Serializable]
    public class ComponentData
    {
        [Header("基础信息")]
        [Tooltip("组件显示名称")]
        public string displayName;
        
        [Tooltip("组件内部ID（自动生成）")]
        public string id;
        
        [Tooltip("组件预制件引用")]
        public GameObject prefab;
        
        [Tooltip("组件图标")]
        public Sprite icon;
        
        [Header("分类信息")]
        [Tooltip("组件类别（如Shelf, Fridge, Checkout, Wall等）")]
        public ComponentCategory category;
        
        [Tooltip("子类别（例如货架可以有'端货架'、'标准货架'等子类别）")]
        public string subCategory;
        
        [Tooltip("标签（可用于搜索和过滤）")]
        public string[] tags;
        
        [Header("显示信息")]
        [Tooltip("组件简短描述")]
        [TextArea(1, 3)]
        public string description;
        
        [Tooltip("是否在组件库中隐藏此组件")]
        public bool isHidden = false;
        
        [Header("物理属性")]
        [Tooltip("组件尺寸（单位：米）")]
        public Vector3 dimensions = Vector3.one;
        
        [Tooltip("组件价格（可用于统计功能）")]
        public float price;
        
        // 自定义构造函数
        public ComponentData(string name, GameObject prefabRef, ComponentCategory componentCategory = ComponentCategory.Other)
        {
            displayName = name;
            prefab = prefabRef;
            category = componentCategory;
            id = Guid.NewGuid().ToString();
        }
        
        // 重写ToString方法，便于调试
        public override string ToString()
        {
            return $"{displayName} ({category})";
        }
    }
    
    /// <summary>
    /// 组件类别枚举
    /// </summary>
    public enum ComponentCategory
    {
        Shelf,      // 货架
        Fridge,     // 冰箱/冷柜
        Checkout,   // 收银台/结账区
        Wall,       // 墙/隔断
        Other       // 其他
    }
}