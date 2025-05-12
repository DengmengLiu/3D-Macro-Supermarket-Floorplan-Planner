using UnityEngine;
using UnityEngine.UI;

namespace SupermarketPlanner.Controllers
{
    /// <summary>
    /// Minimalist camera view switcher - only switches between two preset cameras
    /// </summary>
    public class CameraViewController : MonoBehaviour
    {
        [Header("Camera reference")]
        [Tooltip("Main camera (standard view)")]
        public Camera mainCamera;

        [Tooltip("Top view camera")]
        public Camera topViewCamera;

        [Header("UI reference")]
        [Tooltip("View switch button")]
        public Button viewToggleButton;

        // Is the current top view?
        private bool isTopView = false;

        // Cache camera tag
        private const string MAIN_CAMERA_TAG = "MainCamera";

        private void Start()
        {
            // Find the camera (if not specified)
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found! View controller cannot work.");
                enabled = false;
                return;
            }

            // Initially set the top view camera to be invisible
            if (topViewCamera != null)
                topViewCamera.enabled = false;

            // If there is a view switch button, set the click event
            if (viewToggleButton != null)
            {
                viewToggleButton.onClick.AddListener(ToggleView);
            }
        }

        /// <summary>
        /// Switch view
        /// </summary>
        public void ToggleView()
        {
            isTopView = !isTopView;

            if (isTopView)
            {
                // Switch to top view
                if (mainCamera != null)
                    mainCamera.enabled = false;

                if (topViewCamera != null)
                    topViewCamera.enabled = true;
            }
            else
            {
                // Switch to standard view
                if (mainCamera != null)
                    mainCamera.enabled = true;

                if (topViewCamera != null)
                    topViewCamera.enabled = false;
            }

            // Make sure Camera.main reference is correct
            UpdateMainCameraReference();
        }

        /// <summary>
        /// Update main camera reference
        /// </summary>
        private void UpdateMainCameraReference()
        {
            // When switching cameras, make sure the currently active camera is tagged as "MainCamera"
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
        /// Get the currently active camera
        /// </summary>
        public Camera GetActiveCamera()
        {
            return isTopView ? topViewCamera : mainCamera;
        }
    }
}