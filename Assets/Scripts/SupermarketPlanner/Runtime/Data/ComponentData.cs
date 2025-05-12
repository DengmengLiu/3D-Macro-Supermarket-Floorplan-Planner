using UnityEngine;
using System;

namespace SupermarketPlanner.Data
{
    /// <summary>
    /// Represents the data of a single component in the supermarket planning tool
    /// </summary>
    [Serializable]
    public class ComponentData
    {
        [Header("Basic Information")]
        [Tooltip("Component Display Name")]
        public string displayName;

        [Tooltip("Component Internal ID (Automatically Generated)")]
        public string id;

        [Tooltip("Component Prefab Reference")]
        public GameObject prefab;

        [Tooltip("Component Icon")]
        public Sprite icon;

        [Header("Classification Information")]
        [Tooltip("Component Category (such as Shelf, Fridge, Checkout, Wall, etc.)")]
        public ComponentCategory category;

        [Tooltip("Subcategory (for example, a shelf can have subcategories such as 'End Shelf' and 'Standard Shelf')")]
        public string subCategory;

        [Tooltip("Tags (can be used for searching and filtering)")]
        public string[] tags;

        [Header("Display information")]
        [Tooltip("Component brief description")]
        [TextArea(1, 3)]
        public string description;

        [Tooltip("Hide this component in the component library")]
        public bool isHidden = false;

        [Header("Physical properties")]
        [Tooltip("Component dimensions (unit: meter)")]
        public Vector3 dimensions = Vector3.one;

        [Tooltip("Component price (can be used for statistical functions)")]
        public float price;

        // Custom constructor
        public ComponentData(string name, GameObject prefabRef, ComponentCategory componentCategory = ComponentCategory.Other)
        {
            displayName = name;
            prefab = prefabRef;
            category = componentCategory;
            id = Guid.NewGuid().ToString();
        }

        // Override the ToString method for easy debugging
        public override string ToString()
        {
            return $"{displayName} ({category})";
        }
    }

    /// <summary>
    /// Component category enumeration
    /// </summary>
    public enum ComponentCategory
    {
        Shelf, // Shelf
        Fridge, // Refrigerator/Freezer
        Checkout, // Cashier/Checkout Area
        Wall, // Wall/Partition
        Other // Other
    }
}