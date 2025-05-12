using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SupermarketPlanner.UI
{
    /// <summary>
    /// Help Panel - Display Operation Guide
    /// </summary>
    public class HelpPanel : MonoBehaviour
    {
        [Header("UI Reference")]
        [Tooltip("Content Container")]
        public Transform contentContainer;

        [Tooltip("Close Button")]
        public Button closeButton;

        [Tooltip("Title Text")]
        public TextMeshProUGUI titleText;

        [Header("Category Options")]
        [Tooltip("Camera Operation Button")]
        public Button cameraButton;

        [Tooltip("Object Edit Button")]
        public Button editButton;

        [Header("Content Panel")]
        [Tooltip("Camera Operation Panel")]
        public GameObject cameraPanel;

        [Tooltip("Object Edit Panel")]
        public GameObject editPanel;

        private GameObject currentPanel;
        private Button currentButton;

        private void Start()
        {
            // Initialize UI
            InitializeUI();

            // Default display of camera operation panel
            SwitchPanel(cameraPanel, cameraButton);
        }

        private void InitializeUI()
        {
            // Set close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            // Set title
            if (titleText != null)
            {
                titleText.text = "Operation Guide";
            }

            // Set category button
            if (cameraButton != null)
            {
                cameraButton.onClick.AddListener(() => SwitchPanel(cameraPanel, cameraButton));
            }

            if (editButton != null)
            {
                editButton.onClick.AddListener(() => SwitchPanel(editPanel, editButton));
            }

            // Initialize all panels to be invisible
            if (cameraPanel != null) cameraPanel.SetActive(false);
            if (editPanel != null) editPanel.SetActive(false);
        }

        private void SwitchPanel(GameObject panel, Button button)
        {
            // If the panel does not exist, do nothing
            if (panel == null)
                return;

            // Hide the current panel
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }

            // Reset the current button color
            if (currentButton != null)
            {
                ColorBlock colorBlock = currentButton.colors;
                currentButton.colors = colorBlock;
            }

            // Show the new panel
            panel.SetActive(true);
            currentPanel = panel;

            // Set the new button color
            if (button != null)
            {
                ColorBlock colorBlock = button.colors;
                button.colors = colorBlock;

                currentButton = button;
            }
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        public void ShowPanel()
        {
            gameObject.SetActive(true);
        }
    }
}