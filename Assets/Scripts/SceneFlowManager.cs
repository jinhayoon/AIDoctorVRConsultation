using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIDoctor.Flow
{
    /// <summary>
    /// Manages scene flow/navigation based on condition type.
    /// Persists across all scenes using DontDestroyOnLoad.
    /// 
    /// Two flows supported:
    /// 1. Existing Conditions (Alex, Ella, Kyle): 7 scenes with Button Training
    /// 2. AI Doctor Conditions: 6 scenes, skips Button Training
    /// 
    /// Usage:
    /// - Call SceneFlowManager.Instance.StartFlow() from Menu after setting condition
    /// - Call SceneFlowManager.Instance.LoadNextScene() from each scene to advance
    /// - Call SceneFlowManager.Instance.LoadPreviousScene() to go back (if needed)
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        // ===== SINGLETON =====
        private static SceneFlowManager _instance;
        public static SceneFlowManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<SceneFlowManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneFlowManager");
                        _instance = go.AddComponent<SceneFlowManager>();
                    }
                }
                return _instance;
            }
        }

        // ===== SCENE NAMES - CONFIGURE THESE =====
        [Header("Scene Names (Configure in Inspector or here)")]

        [Tooltip("Menu/Start scene")]
        public string conditionSelection = "ConditionSelection";

        [Tooltip("Adjust Headset")]
        public string adjustHeadset = "AdjustHeadset";

        //[Tooltip("EMOSENSE")]
        //public string emoCalibrationScene = "EMOCalibrationScene";

        [Tooltip("Hand Selection")]
        public string handSelection = "HandSelection";

        //[Tooltip("Button training scene (existing conditions only)")]
        //public string buttonTraining = "ButtonTraining";

        [Tooltip("Waiting Room")]
        public string waitingRoom = "WaitingRoom";

        //[Tooltip("Main experimental condition scene")]
        //public string experimentCondition = PlayerPrefs.GetString("condition");

        [Tooltip("End/Thank you scene")]
        public string sceneEnd = "SceneEnd";

        // ===== STATE =====
        private List<string> currentFlow;
        private int currentSceneIndex = 0;
        private bool isAIDoctorCondition = false;

        // Navigation lock - prevents double transitions during async loads
        private bool isNavigating = false;
        private string targetSceneName = "";

        // ===== PROPERTIES =====

        /// <summary>Returns true if current condition is AI Doctor</summary>
        public bool IsAIDoctorCondition => isAIDoctorCondition;

        /// <summary>Returns the current scene name in the flow</summary>
        public string CurrentSceneName => currentFlow != null && currentSceneIndex < currentFlow.Count
            ? currentFlow[currentSceneIndex]
            : "";

        /// <summary>Returns current position in flow (0-based)</summary>
        public int CurrentSceneIndex => currentSceneIndex;

        /// <summary>Returns total number of scenes in current flow</summary>
        public int TotalScenes => currentFlow?.Count ?? 0;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[SceneFlowManager] Created and will persist across scenes");

                // Subscribe to scene loaded event to reset navigation lock
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Called when a new scene finishes loading - resets the navigation lock
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[SceneFlowManager] ===== SCENE LOADED: {scene.name} =====");

            // Reset navigation lock
            isNavigating = false;
            targetSceneName = "";

            // CRITICAL: Sync the index with the actual loaded scene
            if (currentFlow != null)
            {
                int actualIndex = currentFlow.IndexOf(scene.name);
                if (actualIndex >= 0)
                {
                    if (actualIndex != currentSceneIndex)
                    {
                        Debug.LogWarning($"[SceneFlowManager] INDEX MISMATCH CORRECTED: was {currentSceneIndex}, now {actualIndex}");
                    }
                    currentSceneIndex = actualIndex;
                    Debug.Log($"[SceneFlowManager] Current index synced to: {currentSceneIndex} ({scene.name})");
                }
                else
                {
                    Debug.Log($"[SceneFlowManager] Scene '{scene.name}' not in flow list (this is OK for experimental scenes)");
                }
            }
        }

        // ===== PUBLIC METHODS =====

        /// <summary>
        /// Initialize the scene flow based on the selected condition.
        /// Call this from Menu scene after condition is selected, before loading first scene.
        /// </summary>
        public void InitializeFlow()
        {
            // Read condition from PlayerPrefs
            string condition = PlayerPrefs.GetString("condition", "");
            isAIDoctorCondition = condition.StartsWith("LLM_");

            string experimentalSceneName = condition;

            Debug.Log($"[SceneFlowManager] Condition: {condition}");
            Debug.Log($"[SceneFlowManager] Is AI Doctor: {isAIDoctorCondition}");

            if (string.IsNullOrEmpty(experimentalSceneName))
            {
                Debug.LogError("[SceneFlowManager] Condition scene name is EMPTY!");
                return;
            }

            currentFlow = new List<string>
            {
                conditionSelection,
                //adjustHeadset,
                //emoCalibrationScene,
                //handSelection,
                //waitingRoom,
                experimentalSceneName, 
                sceneEnd
            };

            // Start at menu (index 0)
            currentSceneIndex = 0;

            LogCurrentFlow();
        }

        /// <summary>
        /// Start the flow by loading the first scene after menu (Instruction Scene 1).
        /// Call this from Menu after InitializeFlow().
        /// </summary>
        public void StartFlow()
        {
            if (currentFlow == null || currentFlow.Count == 0)
            {
                Debug.LogError("[SceneFlowManager] Flow not initialized! Call InitializeFlow() first.");
                InitializeFlow();
            }

            // Load the first scene after menu (index 1)
            LoadSceneAtIndex(1);
        }

        /// <summary>
        /// Load the next scene in the flow.
        /// Call this from any scene when ready to proceed.
        /// </summary>
        public void LoadNextScene()
        {
            // Navigation lock - prevent multiple calls during transition
            if (isNavigating)
            {
                Debug.LogWarning($"[SceneFlowManager] Already navigating to '{targetSceneName}' - ignoring duplicate call");
                return;
            }

            if (currentFlow == null || currentFlow.Count == 0)
            {
                Debug.LogError("[SceneFlowManager] Flow not initialized!");
                return;
            }

            int nextIndex = currentSceneIndex + 1;

            if (nextIndex >= currentFlow.Count)
            {
                Debug.Log("[SceneFlowManager] Already at end of flow");
                return;
            }

            LoadSceneAtIndex(nextIndex);
        }

        /// <summary>
        /// Load the previous scene in the flow.
        /// Call this if you need a "Back" button.
        /// </summary>
        public void LoadPreviousScene()
        {
            if (isNavigating)
            {
                Debug.LogWarning("[SceneFlowManager] Already navigating - ignoring");
                return;
            }

            if (currentFlow == null || currentFlow.Count == 0)
            {
                Debug.LogError("[SceneFlowManager] Flow not initialized!");
                return;
            }

            int prevIndex = currentSceneIndex - 1;
            if (prevIndex < 0) prevIndex = 0;

            LoadSceneAtIndex(prevIndex);
        }

        /// <summary>
        /// Load a specific scene by name (bypasses flow order).
        /// Use sparingly - prefer LoadNextScene() for normal flow.
        /// </summary>
        public void LoadSceneByName(string sceneName)
        {
            if (isNavigating)
            {
                Debug.LogWarning("[SceneFlowManager] Already navigating - ignoring");
                return;
            }

            int index = currentFlow?.IndexOf(sceneName) ?? -1;
            if (index >= 0)
            {
                LoadSceneAtIndex(index);
            }
            else
            {
                // Scene not in flow, load directly
                isNavigating = true;
                targetSceneName = sceneName;
                Debug.Log($"[SceneFlowManager] Loading scene directly: {sceneName}");
                DoLoadScene(sceneName);
            }
        }

        /// <summary>
        /// Go directly to the end scene.
        /// </summary>
        public void GoToEndScene()
        {
            if (currentFlow != null)
            {
                LoadSceneAtIndex(currentFlow.Count - 1);
            }
        }

        /// <summary>
        /// Reset the flow (call when returning to menu for a new participant).
        /// </summary>
        public void ResetFlow()
        {
            currentFlow = null;
            currentSceneIndex = 0;
            isAIDoctorCondition = false;
            isNavigating = false;
            targetSceneName = "";
            Debug.Log("[SceneFlowManager] Flow reset");
        }

        public bool HasNextScene()
        {
            return currentFlow != null && currentSceneIndex < currentFlow.Count - 1;
        }

        // ===== PRIVATE METHODS =====

        private void LoadSceneAtIndex(int index)
        {
            if (currentFlow == null || index < 0 || index >= currentFlow.Count)
            {
                Debug.LogError($"[SceneFlowManager] Invalid index: {index}");
                return;
            }

            string sceneName = currentFlow[index];

            // Set navigation lock
            isNavigating = true;
            targetSceneName = sceneName;

            // Note: We DON'T update currentSceneIndex here!
            // It will be synced in OnSceneLoaded when the scene actually loads.

            Debug.Log($"[SceneFlowManager] ===== LOADING: [{index + 1}/{currentFlow.Count}] {sceneName} =====");
            Debug.Log($"[SceneFlowManager] (Current index will sync to {index} when scene loads)");

            DoLoadScene(sceneName);
        }
        /// <summary>
        /// Actually perform the scene load, using SceneController if available.
        /// </summary>
        private void DoLoadScene(string sceneName)
        {
            SceneController controller = FindFirstObjectByType<SceneController>();
            if (controller != null)
            {
                Debug.Log("[SceneFlowManager] Using SceneController for fade transition");
                controller.LoadScene(sceneName);
            }
            else
            {
                Debug.Log("[SceneFlowManager] Using direct scene load");
                SceneManager.LoadScene(sceneName);
            }
        }

        private void LogCurrentFlow()
        {
            if (currentFlow == null) return;

            Debug.Log("[SceneFlowManager] ========== FLOW INITIALIZED ==========");
            Debug.Log($"[SceneFlowManager] Total scenes: {currentFlow.Count}");
            Debug.Log($"[SceneFlowManager] Is AI Doctor: {isAIDoctorCondition}");
            for (int i = 0; i < currentFlow.Count; i++)
            {
                string marker = i == currentSceneIndex ? " ← CURRENT" : "";
                Debug.Log($"[SceneFlowManager]   [{i}] {currentFlow[i]}{marker}");
            }
            Debug.Log("[SceneFlowManager] =====================================");
        }

        // ===== STATIC HELPER METHODS =====

        /// <summary>
        /// Quick check if current condition is AI Doctor (can be called without instance).
        /// </summary>
        public static bool CheckIsAIDoctorCondition()
        {
            string condition = PlayerPrefs.GetString("condition", "");
            return condition.StartsWith("LLM_");
        }
    }
}