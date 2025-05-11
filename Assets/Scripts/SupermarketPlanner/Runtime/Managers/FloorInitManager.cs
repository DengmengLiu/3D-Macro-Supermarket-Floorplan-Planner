using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SupermarketPlanner.Managers;

public class FloorInitManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiPanel;           // 设置面板
    public TMP_InputField widthInput;    // 宽度输入框
    public TMP_InputField lengthInput;   // 长度输入框
    public Button createButton;          // 创建按钮
    public TextMeshProUGUI errorText;    // 错误信息文本

    [Header("Floor Settings")]
    public GameObject floorPrefab;        // 地板预制件
    public Material floorMaterial;        // 地板材质
    public float maxSize = 1000f;        // 最大尺寸限制

    [Header("Wall Settings")]
    public float wallHeight = 2f;         // 墙壁高度
    public float wallThickness = 0.3f;    // 墙壁厚度

    private GameObject currentFloor;      // 当前创建的地板
    private List<GameObject> currentWalls = new List<GameObject>(); // 当前创建的墙壁
    private CameraController cameraController; // 相机控制器引用

    void Start()
    {
        // 初始化错误信息
        if (errorText != null)
        {
            errorText.text = "";
        }

        // 添加按钮点击事件
        if (createButton != null)
        {
            createButton.onClick.AddListener(CreateFloor);
        }

        // 检查是否有地板预制件
        if (floorPrefab == null)
        {
            // 创建默认地板预制件
            floorPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorPrefab.name = "DefaultFloorPrefab";
            floorPrefab.transform.localScale = new Vector3(1, 0.1f, 1);
            floorPrefab.SetActive(false);
        }

        // 检查是否有材质
        if (floorMaterial == null && floorPrefab.GetComponent<Renderer>() != null)
        {
            floorMaterial = floorPrefab.GetComponent<Renderer>().material;
        }

        // 获取相机控制器引用
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("主相机上未找到CameraController组件");
            }
        }
    }

    public void CreateFloor()
    {
        // 获取输入值
        if (!float.TryParse(widthInput.text, out float width) ||
            !float.TryParse(lengthInput.text, out float length))
        {
            ShowError("Please enter a valid number");
            return;
        }

        // 检查尺寸限制
        if (width <= 0 || length <= 0)
        {
            ShowError("Please enter a value greater than 0");
            return;
        }

        if (width > maxSize || length > maxSize)
        {
            ShowError($"The size cannot exceed {maxSize} meters");
            return;
        }

        // 如果已有地板和围墙，先删除
        if (currentFloor != null)
        {
            Destroy(currentFloor);
        }

        foreach (GameObject wall in currentWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
        currentWalls.Clear();

        // 创建新地板
        currentFloor = Instantiate(floorPrefab, Vector3.zero, Quaternion.identity);
        currentFloor.SetActive(true);
        currentFloor.name = "SupermarketFloor";

        // 调整地板大小
        currentFloor.transform.localScale = new Vector3(width, 0.1f, length);

        // 如果有材质，应用材质
        if (floorMaterial != null)
        {
            Renderer renderer = currentFloor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = floorMaterial;

                // 调整材质贴图比例以匹配地板尺寸
                renderer.material.mainTextureScale = new Vector2(width, length);
            }
        }

        // 创建围墙
        CreateWalls(width, length);

        // 隐藏UI面板
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        // 调整相机位置以查看整个地板
        AdjustCameraPosition(width, length);

        // 设置相机边界
        SetCameraBoundaries(width, length);

        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.SetFloor(currentFloor, width, length);
        }
        else
        {
            Debug.LogWarning("GridManager instance not found.");
        }

        Debug.Log($"已创建地板，尺寸: {width} x {length}");
    }

    private void CreateWalls(float width, float length)
    {
        float halfWidth = width / 2;
        float halfLength = length / 2;
        float floorHeight = 0.1f;
        float wallY = floorHeight / 2 + wallHeight / 2;

        Vector3[] positions = new Vector3[]
        {
        new Vector3(0, wallY, halfLength + wallThickness / 2),  // North
        new Vector3(0, wallY, -halfLength - wallThickness / 2), // South
        new Vector3(halfWidth + wallThickness / 2, wallY, 0),   // East
        new Vector3(-halfWidth - wallThickness / 2, wallY, 0),  // West
        };

        Vector3[] scales = new Vector3[]
        {
        new Vector3(width + wallThickness * 2, wallHeight, wallThickness), // North/South
        new Vector3(width + wallThickness * 2, wallHeight, wallThickness),
        new Vector3(wallThickness, wallHeight, length), // East/West
        new Vector3(wallThickness, wallHeight, length),
        };

        string[] names = { "NorthWall", "SouthWall", "EastWall", "WestWall" };

        for (int i = 0; i < 4; i++)
        {
            GameObject wall = Instantiate(floorPrefab, positions[i], Quaternion.identity); // 使用floorPrefab
            wall.name = names[i];
            wall.transform.localScale = scales[i];
            wall.layer = LayerMask.NameToLayer("PlacedObject");
            wall.SetActive(true);

            if (floorMaterial != null)
            {
                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = floorMaterial;
            }

            if (wall.GetComponent<Collider>() == null)
                wall.AddComponent<BoxCollider>();

            currentWalls.Add(wall);
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
        else
        {
            Debug.LogError(message);
        }
    }

    private void AdjustCameraPosition(float width, float length)
    {
        // 获取主相机
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        // 计算最大尺寸
        float maxDimension = Mathf.Max(width, length);

        // 计算相机高度和距离
        float cameraHeight = maxDimension * 0.5f;
        float cameraDistance = maxDimension * 0.5f;

        // 设置相机位置和旋转
        mainCamera.transform.position = new Vector3(width / 2, cameraHeight, -cameraDistance);
        mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
    }

    private void SetCameraBoundaries(float width, float length)
    {
        // 如果找到相机控制器，设置边界
        if (cameraController != null && currentFloor != null)
        {
            cameraController.SetBoundaries(currentFloor, width, length);
        }
    }
}