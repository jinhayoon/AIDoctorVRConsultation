using UnityEngine;
using UnityEngine.UI;
using AIDoctor.Flow;

namespace AIDoctor.UI
{
    /// <summary>
    /// Simple scene navigator component.
    /// Add this to any scene that needs "Next" or "Back" functionality.
    /// 
    /// Just drag your buttons to the references and it handles the rest!
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        [Header("Buttons (Optional - assign if you have them)")]
        [Tooltip("Button to go to next scene")]
        [SerializeField] private Button nextButton;

        [Tooltip("Button to go to previous scene")]
        [SerializeField] private Button backButton;

        [Header("Auto-Navigation (Optional)")]
        [Tooltip("Automatically load next scene after this many seconds (0 = disabled)")]
        [SerializeField] private float autoAdvanceAfterSeconds = 0f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void Start()
        {
            // Setup button listeners
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(GoToNextScene);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(GoToPreviousScene);
            }

            // Auto-advance if configured
            if (autoAdvanceAfterSeconds > 0)
            {
                Invoke(nameof(GoToNextScene), autoAdvanceAfterSeconds);
            }

            // Debug info
            if (showDebugInfo)
            {
                LogSceneInfo();
            }
        }

        /// <summary>
        /// Go to the next scene in the flow.
        /// Can be called from button OnClick or other scripts.
        /// </summary>
        public void GoToNextScene()
        {
            Debug.Log("[SceneNavigator] Next scene requested");

            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadNextScene();
            }
            else
            {
                Debug.LogError("[SceneNavigator] SceneFlowManager not found!");
            }
        }

        /// <summary>
        /// Go to the previous scene in the flow.
        /// Can be called from button OnClick or other scripts.
        /// </summary>
        public void GoToPreviousScene()
        {
            Debug.Log("[SceneNavigator] Previous scene requested");

            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadPreviousScene();
            }
            else
            {
                Debug.LogError("[SceneNavigator] SceneFlowManager not found!");
            }
        }

        /// <summary>
        /// Go directly to the end scene.
        /// </summary>
        public void GoToEndScene()
        {
            Debug.Log("[SceneNavigator] End scene requested");

            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.GoToEndScene();
            }
        }

        private void LogSceneInfo()
        {
            if (SceneFlowManager.Instance != null)
            {
                Debug.Log($"[SceneNavigator] Current scene: {SceneFlowManager.Instance.CurrentSceneName}");
                Debug.Log($"[SceneNavigator] Position: {SceneFlowManager.Instance.CurrentSceneIndex + 1} of {SceneFlowManager.Instance.TotalScenes}");
                Debug.Log($"[SceneNavigator] Is AI Doctor: {SceneFlowManager.Instance.IsAIDoctorCondition}");
            }
        }
    }
}