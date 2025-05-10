using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SideMenuController : MonoBehaviour
{
    [Header("Menu Settings")]
    public float animationSpeed = 0.3f;      // 菜单动画速度
    public RectTransform menuPanel;           // 菜单面板
    public bool startOpen = false;            // 菜单初始状态是否打开
    public float closedXPosition = 250f;      // 关闭状态X位置
    public float openXPosition = 0f;          // 打开状态X位置

    [Header("Toggle Button")]
    public Button toggleButton;               // 切换按钮
    public RectTransform toggleButtonRect;    // 切换按钮RectTransform
    public TextMeshProUGUI toggleButtonText;  // 切换按钮文本
    public float toggleClosedX = -50f;        // 切换按钮关闭状态X位置
    public float toggleOpenX = -250f;         // 切换按钮打开状态X位置

    [Header("Tab Settings")]
    public Button componentsTabButton;        // 组件标签按钮
    public Button settingsTabButton;          // 设置标签按钮
    public GameObject componentsPanel;         // 组件面板
    public GameObject settingsPanel;           // 设置面板
    public Color activeTabColor = new Color(0.8f, 0.8f, 0.8f);  // 激活标签颜色
    public Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f); // 未激活标签颜色

    private bool isOpen;                      // 菜单是否打开
    private Coroutine animationCoroutine;     // 动画协程

    void Start()
    {
        // 初始化菜单状态
        isOpen = startOpen;
        UpdateMenuPosition(isOpen ? openXPosition : closedXPosition);
        UpdateToggleButton(isOpen);

        // 设置按钮事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMenu);
        }

        if (componentsTabButton != null)
        {
            componentsTabButton.onClick.AddListener(() => SwitchTab(true));
        }

        if (settingsTabButton != null)
        {
            settingsTabButton.onClick.AddListener(() => SwitchTab(false));
        }

        // 默认显示组件标签页
        SwitchTab(true);
    }

    // 切换菜单状态
    public void ToggleMenu()
    {
        isOpen = !isOpen;
        
        // 停止当前动画（如果有）
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 启动新动画
        animationCoroutine = StartCoroutine(AnimateMenu(isOpen));
        
        // 更新切换按钮状态
        UpdateToggleButton(isOpen);
    }

    // 菜单动画协程
    private IEnumerator AnimateMenu(bool opening)
    {
        float startX = menuPanel.anchoredPosition.x;
        float targetX = opening ? openXPosition : closedXPosition;
        
        float startToggleX = toggleButtonRect.anchoredPosition.x;
        float targetToggleX = opening ? toggleOpenX : toggleClosedX;
        
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / animationSpeed;
            
            // 使用平滑的缓动函数
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // 更新菜单位置
            UpdateMenuPosition(Mathf.Lerp(startX, targetX, smoothT));
            
            // 更新切换按钮位置
            Vector2 togglePos = toggleButtonRect.anchoredPosition;
            togglePos.x = Mathf.Lerp(startToggleX, targetToggleX, smoothT);
            toggleButtonRect.anchoredPosition = togglePos;
            
            yield return null;
        }
        
        // 确保动画结束时位置准确
        UpdateMenuPosition(targetX);
        
        Vector2 finalTogglePos = toggleButtonRect.anchoredPosition;
        finalTogglePos.x = targetToggleX;
        toggleButtonRect.anchoredPosition = finalTogglePos;
        
        animationCoroutine = null;
    }

    // 更新菜单位置
    private void UpdateMenuPosition(float xPos)
    {
        if (menuPanel != null)
        {
            Vector2 pos = menuPanel.anchoredPosition;
            pos.x = xPos;
            menuPanel.anchoredPosition = pos;
        }
    }

    // 更新切换按钮状态
    private void UpdateToggleButton(bool isOpen)
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isOpen ? ">" : "<";
        }
    }

    // 切换标签页
    public void SwitchTab(bool showComponents)
    {
        // 激活/停用相应的面板
        if (componentsPanel != null)
            componentsPanel.SetActive(showComponents);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(!showComponents);
        
        // 更新标签按钮外观
        if (componentsTabButton != null)
        {
            componentsTabButton.GetComponent<Image>().color = showComponents ? activeTabColor : inactiveTabColor;
        }
        
        if (settingsTabButton != null)
        {
            settingsTabButton.GetComponent<Image>().color = showComponents ? inactiveTabColor : activeTabColor;
        }
    }
}