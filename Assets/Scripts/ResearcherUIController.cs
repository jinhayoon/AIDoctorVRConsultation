using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIDoctor.Networking;

namespace AIDoctor.UI
{
    /// <summary>
    /// UI controller for the researcher interface.
    /// Allows setting participant ID and condition before starting consultation.
    /// </summary>
    public class ResearcherUIController : MonoBehaviour
    {
        [Header("Setup Panel")]
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private TMP_InputField participantIdInput;
        [SerializeField] private TMP_Dropdown conditionDropdown;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("Status Panel")]
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI participantInfoText;
        [SerializeField] private TextMeshProUGUI transcriptPreviewText;
        [SerializeField] private Button endButton;

        [Header("References")]
        [SerializeField] private FlaskClient flaskClient;
        [SerializeField] private RealtimeAPIManager realtimeManager;
        [SerializeField] private ConversationLogger conversationLogger;

        [Header("Settings")]
        [SerializeField] private float connectionCheckInterval = 5f;
        [SerializeField] private int maxTranscriptPreviewLines = 5;

        private bool isServerConnected = false;
        private Coroutine connectionCheckCoroutine;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            ShowSetupPanel();
            StartConnectionCheck();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            StopConnectionCheck();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Setup condition dropdown
            if (conditionDropdown != null)
            {
                conditionDropdown.ClearOptions();
                conditionDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "1 - Empathetic (Patient-Centered)",
                    "2 - Non-Empathetic (Clinical/Professional)"
                });
            }

            // Setup button listeners
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            
            if (endButton != null)
                endButton.onClick.AddListener(OnEndClicked);

            // Clear error text
            if (errorText != null)
                errorText.text = "";
        }

        private void SubscribeToEvents()
        {
            if (realtimeManager != null)
            {
                realtimeManager.OnConnectionEstablished += OnConnected;
                realtimeManager.OnConnectionError += OnConnectionError;
                realtimeManager.OnConsultationEnded += OnConsultationEnded;
                realtimeManager.OnAssistantMessage += OnAssistantMessage;
                realtimeManager.OnUserTranscript += OnUserTranscript;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (realtimeManager != null)
            {
                realtimeManager.OnConnectionEstablished -= OnConnected;
                realtimeManager.OnConnectionError -= OnConnectionError;
                realtimeManager.OnConsultationEnded -= OnConsultationEnded;
                realtimeManager.OnAssistantMessage -= OnAssistantMessage;
                realtimeManager.OnUserTranscript -= OnUserTranscript;
            }
        }

        #endregion

        #region Connection Check

        private void StartConnectionCheck()
        {
            if (connectionCheckCoroutine != null)
                StopCoroutine(connectionCheckCoroutine);
            
            connectionCheckCoroutine = StartCoroutine(ConnectionCheckLoop());
        }

        private void StopConnectionCheck()
        {
            if (connectionCheckCoroutine != null)
            {
                StopCoroutine(connectionCheckCoroutine);
                connectionCheckCoroutine = null;
            }
        }

        private System.Collections.IEnumerator ConnectionCheckLoop()
        {
            while (true)
            {
                yield return CheckServerConnection();
                yield return new WaitForSeconds(connectionCheckInterval);
            }
        }

        private System.Collections.IEnumerator CheckServerConnection()
        {
            if (flaskClient == null) yield break;

            yield return flaskClient.CheckConnection(connected =>
            {
                isServerConnected = connected;
                UpdateConnectionStatus(connected);
            });
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (connectionStatusText == null) return;

            if (connected)
            {
                connectionStatusText.text = "✅ Server Connected";
                connectionStatusText.color = Color.green;
                if (startButton != null)
                    startButton.interactable = true;
            }
            else
            {
                connectionStatusText.text = "❌ Server Offline - Start Flask server first!";
                connectionStatusText.color = Color.red;
                if (startButton != null)
                    startButton.interactable = false;
            }
        }

        #endregion

        #region Button Handlers

        private void OnStartClicked()
        {
            // Validate input
            string participantId = participantIdInput?.text?.Trim();
            
            if (string.IsNullOrEmpty(participantId))
            {
                ShowError("Please enter a Participant ID");
                return;
            }

            if (!isServerConnected)
            {
                ShowError("Server not connected. Please start the Flask server.");
                return;
            }

            // Get condition (dropdown index + 1 = condition code)
            string condition = (conditionDropdown.value + 1).ToString();
            string conditionName = conditionDropdown.value == 0 ? "Empathetic" : "Non-Empathetic";

            // Disable UI during connection
            SetUIInteractable(false);
            ShowStatus($"Starting session for {participantId}...", Color.yellow);
            ClearError();

            // Start session on Flask server first
            StartCoroutine(StartSessionCoroutine(participantId, condition, conditionName));
        }

        private System.Collections.IEnumerator StartSessionCoroutine(string participantId, string condition, string conditionName)
        {
            bool success = false;
            string error = null;

            yield return flaskClient.StartSession(
                participantId,
                condition,
                onSuccess: (response) =>
                {
                    success = true;
                    Debug.Log($"[ResearcherUI] Session started: {response.condition_name}");
                },
                onError: (err) =>
                {
                    error = err;
                    Debug.LogError($"[ResearcherUI] Session start failed: {err}");
                }
            );

            if (success)
            {
                // Now connect to OpenAI
                ShowStatus($"Connecting to AI Doctor ({conditionName})...", Color.yellow);
                realtimeManager.Connect();
                
                // Update participant info
                if (participantInfoText != null)
                    participantInfoText.text = $"Participant: {participantId}\nCondition: {conditionName}";
                
                ShowStatusPanel();
            }
            else
            {
                ShowError($"Failed to start session: {error}");
                SetUIInteractable(true);
            }
        }

        private void OnEndClicked()
        {
            Debug.Log("[ResearcherUI] End button clicked");
            
            // Confirm end
            ShowStatus("Ending consultation...", Color.yellow);
            
            // Disconnect (this will save the conversation)
            if (realtimeManager != null)
            {
                realtimeManager.Disconnect();
            }

            // Also end session on server
            StartCoroutine(flaskClient.EndSession(() =>
            {
                ShowSetupPanel();
            }));
        }

        #endregion

        #region Event Handlers

        private void OnConnected()
        {
            Debug.Log("[ResearcherUI] Connected to OpenAI");
            ShowStatus("✅ Connected - Consultation in progress", Color.green);
        }

        private void OnConnectionError(string error)
        {
            Debug.LogError($"[ResearcherUI] Connection error: {error}");
            ShowStatus($"❌ Error: {error}", Color.red);
            
            // Return to setup after delay
            Invoke(nameof(ShowSetupPanel), 3f);
        }

        private void OnConsultationEnded()
        {
            Debug.Log("[ResearcherUI] Consultation ended");
            ShowStatus("✅ Consultation Complete - Transcript Saved", Color.cyan);
            
            // Show summary
            if (conversationLogger != null)
            {
                Debug.Log($"[ResearcherUI] {conversationLogger.GetSummary()}");
            }
            
            // Return to setup after delay
            Invoke(nameof(ShowSetupPanel), 5f);
        }

        private void OnAssistantMessage(string message)
        {
            UpdateTranscriptPreview("AI Doctor", message);
        }

        private void OnUserTranscript(string message)
        {
            UpdateTranscriptPreview("Patient", message);
        }

        private void UpdateTranscriptPreview(string speaker, string message)
        {
            if (transcriptPreviewText == null) return;

            // Truncate message if too long
            string truncated = message.Length > 80 
                ? message.Substring(0, 80) + "..." 
                : message;

            // Add to preview (keep last N lines)
            string current = transcriptPreviewText.text;
            string[] lines = current.Split('\n');
            
            System.Collections.Generic.List<string> lineList = 
                new System.Collections.Generic.List<string>(lines);
            
            lineList.Add($"<b>{speaker}:</b> {truncated}");
            
            while (lineList.Count > maxTranscriptPreviewLines)
            {
                lineList.RemoveAt(0);
            }

            transcriptPreviewText.text = string.Join("\n", lineList);
        }

        #endregion

        #region UI Helpers

        private void ShowSetupPanel()
        {
            if (setupPanel != null) setupPanel.SetActive(true);
            if (statusPanel != null) statusPanel.SetActive(false);
            
            SetUIInteractable(true);
            
            // Clear inputs for next participant
            if (participantIdInput != null)
                participantIdInput.text = "";
            
            if (transcriptPreviewText != null)
                transcriptPreviewText.text = "";

            ClearError();
            
            // Restart connection check
            StartConnectionCheck();
        }

        private void ShowStatusPanel()
        {
            if (setupPanel != null) setupPanel.SetActive(false);
            if (statusPanel != null) statusPanel.SetActive(true);
        }

        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Color.red;
            }
            Debug.LogWarning($"[ResearcherUI] {message}");
        }

        private void ClearError()
        {
            if (errorText != null)
                errorText.text = "";
        }

        private void SetUIInteractable(bool interactable)
        {
            if (startButton != null)
                startButton.interactable = interactable && isServerConnected;
            
            if (participantIdInput != null)
                participantIdInput.interactable = interactable;
            
            if (conditionDropdown != null)
                conditionDropdown.interactable = interactable;
        }

        #endregion

        #region Debug

        [ContextMenu("Test Connection")]
        public void TestConnection()
        {
            StartCoroutine(CheckServerConnection());
        }

        [ContextMenu("Show Setup Panel")]
        public void DebugShowSetup()
        {
            ShowSetupPanel();
        }

        #endregion
    }
}
