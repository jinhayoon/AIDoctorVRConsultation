using AIDoctor.Flow;
using AIDoctor.Networking;
using System.Collections;
using UnityEngine;

namespace AIDoctor.Experimental
{
    /// <summary>
    /// Controller for the AI Doctor Experimental Scene.
    /// 
    /// CONSOLE LOGGING: Handled by RealtimeAPIManager only.
    /// This script does NOT log conversation messages to avoid duplicates.
    /// 
    /// HAND SCAN INPUT: Handled by RealtimeAPIManager.Update().
    ///   ENTER    = User accepted scan  → complete scan, AI continues
    ///   SPACEBAR = User refused scan   → AI persuades and re-requests
    /// This script only updates VR status display to reflect scan state.
    /// 
    /// LIP SYNC: Uses WebRTCToSALSA to bridge WebRTC audio to SALSA OneClick.
    /// </summary>
    public class ExperimentalSceneManager : MonoBehaviour
    {
        [Header("═══ AUDIO (Required) ═══")]
        [Tooltip("AudioSource on avatar for AI voice playback")]
        [SerializeField] private AudioSource avatarAudioSource;

        [Header("═══ VR Status Display (Optional) ═══")]
        [SerializeField] private TMPro.TextMeshProUGUI statusText;
        [SerializeField] private float hideStatusDelay = 3f;

        // Managers
        private FlaskClient flaskClient;
        private RealtimeAPIManager realtimeManager;
        private ConversationLogger conversationLogger;
        // private WebRTCToSALSA lipSync;

        // Session data
        private string participantID;
        private string condition;
        private string conditionCode;

        // State
        private bool isConnected = false;
        private bool isAIDoctorCondition = false;
        private bool isInitialized = false;

        // Track scan status display to avoid spamming ShowStatus every frame
        private bool wasWaitingForScan = false;
        private bool wasScanInProgress = false;

        #region Unity Lifecycle

        private void Start()
        {
            Debug.Log("════════════════════════════════════════════════════════════");
            Debug.Log("  EXPERIMENTAL SCENE LOADED");
            Debug.Log("════════════════════════════════════════════════════════════");

            CheckCondition();

            if (isAIDoctorCondition)
            {
                Debug.Log($"[ExperimentalScene] LLM Condition: {condition}");

                if (avatarAudioSource == null)
                {
                    Debug.LogError("[ExperimentalScene] ❌ Avatar AudioSource not assigned!");
                    ShowStatus("Error: AudioSource not configured", Color.red);
                    return;
                }

                Debug.Log($"[ExperimentalScene] ✓ AudioSource: {avatarAudioSource.gameObject.name}");

                InitializeAIDoctor();
            }
            else
            {
                Debug.Log($"[ExperimentalScene] Standard condition: {condition}");
                Debug.Log("[ExperimentalScene] AI Doctor NOT active");
            }
        }

        private void Update()
        {
            if (!isAIDoctorCondition || !isConnected || realtimeManager == null)
                return;

            // ESCAPE: Researcher manually ends the consultation
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[ExperimentalScene] 🛑 Researcher pressed ESCAPE — ending consultation");
                ShowStatus("Ending consultation...", Color.yellow);
                isConnected = false; // Prevent further input processing
                realtimeManager.EndConsultationManually();
                return;
            }

            // Update VR status display based on scan state
            // (Keyboard input for scans is handled by RealtimeAPIManager.Update())

            bool waitingNow = realtimeManager.IsWaitingForScanDecision;
            bool scanningNow = realtimeManager.IsScanInProgress;

            // Show prompt when scan decision is needed
            if (waitingNow && !wasWaitingForScan)
            {
                ShowStatus("ENTER = Accept scan\nSPACEBAR = Refuse scan", Color.cyan);
            }

            // Show scanning status
            if (scanningNow && !wasScanInProgress)
            {
                ShowStatus("Scanning...", Color.yellow);
            }

            // Hide status when scan flow completes
            if (!waitingNow && !scanningNow && (wasWaitingForScan || wasScanInProgress))
            {
                ShowStatus("Scan Complete", Color.green);
                if (hideStatusDelay > 0) Invoke(nameof(HideStatus), hideStatusDelay);
            }

            wasWaitingForScan = waitingNow;
            wasScanInProgress = scanningNow;
        }

        private void OnDestroy()
        {
            // If still connected when scene is destroyed, save and disconnect
            if (isConnected && realtimeManager != null)
            {
                Debug.Log("[ExperimentalScene] Scene destroyed while connected — saving transcript");
                realtimeManager.EndConsultationManually();
            }

            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void CheckCondition()
        {
            participantID = PlayerPrefs.GetString("ppt", "");
            condition = PlayerPrefs.GetString("condition", "");
            conditionCode = PlayerPrefs.GetString("LLM_code", "");

            isAIDoctorCondition = condition.StartsWith("LLM_");

            Debug.Log($"[ExperimentalScene] Participant: {participantID}");
            Debug.Log($"[ExperimentalScene] Condition: {condition}");
            Debug.Log($"[ExperimentalScene] Is AI Doctor: {isAIDoctorCondition}");

            if (isAIDoctorCondition)
            {
                string promptType = conditionCode == "1" ? "EMPATHETIC" : "PROFESSIONAL";
                Debug.Log($"[ExperimentalScene] Prompt: {promptType}");
            }
        }

        private void InitializeAIDoctor()
        {
            if (isInitialized) return;

            // Find FlaskClient
            flaskClient = Object.FindFirstObjectByType<FlaskClient>();
            if (flaskClient == null)
            {
                Debug.LogError("[ExperimentalScene] ❌ FlaskClient not found!");
                ShowStatus("Error: Server connection lost", Color.red);
                return;
            }
            Debug.Log("[ExperimentalScene] ✓ FlaskClient found");

            // Create ConversationLogger
            var loggerObj = new GameObject("ConversationLogger");
            conversationLogger = loggerObj.AddComponent<ConversationLogger>();

            // Create RealtimeAPIManager
            var realtimeObj = new GameObject("RealtimeAPIManager");
            realtimeManager = realtimeObj.AddComponent<RealtimeAPIManager>();
            realtimeManager.SetFlaskClient(flaskClient);
            realtimeManager.SetConversationLogger(conversationLogger);
            realtimeManager.SetAudioSource(avatarAudioSource);

            Debug.Log("[ExperimentalScene] ✓ RealtimeAPIManager created");

            SubscribeToEvents();
            isInitialized = true;

            StartCoroutine(ConnectToOpenAI());
        }

        private void SubscribeToEvents()
        {
            if (realtimeManager != null)
            {
                realtimeManager.OnConnectionEstablished += OnConnected;
                realtimeManager.OnConnectionError += OnError;
                realtimeManager.OnConsultationEnded += OnConsultationEnded;
                realtimeManager.OnHandScanRequested += OnHandScan;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (realtimeManager != null)
            {
                realtimeManager.OnConnectionEstablished -= OnConnected;
                realtimeManager.OnConnectionError -= OnError;
                realtimeManager.OnConsultationEnded -= OnConsultationEnded;
                realtimeManager.OnHandScanRequested -= OnHandScan;
            }
        }

        #endregion

        #region Connection

        private IEnumerator ConnectToOpenAI()
        {
            ShowStatus("Connecting to AI Doctor...", Color.yellow);
            yield return new WaitForSeconds(0.5f);

            bool serverOk = false;
            yield return flaskClient.CheckConnection(ok => serverOk = ok);

            if (!serverOk)
            {
                ShowStatus("Error: Server not available", Color.red);
                Debug.LogError("[ExperimentalScene] Flask server not responding!");
                yield break;
            }

            Debug.Log("[ExperimentalScene] ✓ Flask server OK");
            realtimeManager.Connect();
        }

        #endregion

        #region Event Handlers

        private void OnConnected()
        {
            isConnected = true;
            ShowStatus("Connected!", Color.green);
            if (hideStatusDelay > 0) Invoke(nameof(HideStatus), hideStatusDelay);
        }

        private void OnError(string error)
        {
            Debug.LogError($"[ExperimentalScene] ❌ Error: {error}");
            ShowStatus($"Error: {error}", Color.red);
        }

        private void OnConsultationEnded()
        {
            isConnected = false;
            Debug.Log("[ExperimentalScene] ✅ Consultation ended and saved");
            ShowStatus("Consultation Complete!\nThank you for participating.", Color.cyan);
            PlayerPrefs.DeleteKey("LLM_code");
            StartCoroutine(NavigateToEndScene());
        }

        private IEnumerator NavigateToEndScene()
        {
            yield return new WaitForSeconds(3f);
            SceneFlowManager.Instance?.LoadNextScene();
        }

        private void OnHandScan()
        {
            ShowStatus("ENTER = Accept scan\nSPACEBAR = Refuse scan", Color.cyan);
        }

        #endregion

        #region UI

        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
                statusText.gameObject.SetActive(true);
            }
        }

        private void HideStatus()
        {
            if (statusText != null)
                statusText.gameObject.SetActive(false);
        }

        #endregion

        #region Public API

        public bool IsAIDoctorActive => isAIDoctorCondition && isInitialized;
        public bool IsConnected => isConnected;

        public void EndConsultation()
        {
            if (realtimeManager != null)
                realtimeManager.EndConsultationManually();
            else
                Debug.LogWarning("[ExperimentalScene] No RealtimeAPIManager to end");
        }

        #endregion
    }
}