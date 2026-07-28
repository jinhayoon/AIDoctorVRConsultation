using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AIDoctor.Networking;
using AIDoctor.Flow;

/// <summary>
/// Menu scene controller for setting participant ID and condition.
/// Integrates with SceneFlowManager to handle different flows:
/// - Existing conditions (Alex, Ella, Kyle): 7 scenes WITH button training
/// - AI Doctor conditions: 6 scenes WITHOUT button training
/// </summary>
public class SetCondition : MonoBehaviour
{
    [Header("Required References")]
    public TMP_InputField logPPTID;

    [Header("Optional UI")]
    [Tooltip("Text to show error/status messages")]
    public TextMeshProUGUI statusText;

    [Tooltip("Button to start - disable during loading")]
    public Button continueButton;

    // FlaskClient for AI Doctor conditions
    private FlaskClient flaskClient;
    private ART_ExperienceManager _experienceManager;
    private bool isProcessing = false;

    void Start()
    {
        // Default condition
        PlayerPrefs.SetString("condition", "Ella_Control");

        // Create FlaskClient (will persist across scenes)
        CreateFlaskClient();

        ShowStatus("Ready", Color.white);
    }

    /// <summary>
    /// Creates a FlaskClient that persists between scenes (for AI Doctor conditions)
    /// </summary>
    void CreateFlaskClient()
    {
        flaskClient = FindObjectOfType<FlaskClient>();
        if (flaskClient == null)
        {
            GameObject flaskObj = new GameObject("FlaskClient");
            flaskClient = flaskObj.AddComponent<FlaskClient>();
            DontDestroyOnLoad(flaskObj);
            Debug.Log("[SetCondition] FlaskClient created");
        }
    }

    /// <summary>
    /// Called by dropdown when condition selection changes.
    /// </summary>
    public void HandleInputData(int val)
    {
        string condition = val switch
        {
            0 => "Ella_Control",
            1 => "Ella_Full",
            2 => "Ella_Progression",
            3 => "Alex_Control",
            4 => "Alex_Full",
            5 => "Alex_Progression",
            6 => "Kyle_Control",
            7 => "Kyle_Full",
            8 => "Kyle_Progression",
            // AI Doctor conditions
            9 => "LLM_Empathetic",
            10 => "LLM_Professional",
            _ => "Ella_Full"
        };

        PlayerPrefs.SetString("condition", condition);
        Debug.Log($"[SetCondition] Condition set to: {condition}");
    }

    /// <summary>
    /// Called when Continue button is clicked.
    /// Starts the appropriate flow based on condition.
    /// </summary>
    public void ContinueToSimulation()
    {
        if (isProcessing)
            return;

        // Validate participant ID
        string pid = logPPTID.text?.Trim();
        if (string.IsNullOrEmpty(pid))
        {
            ShowStatus("Please enter a Participant ID", Color.red);
            return;
        }

        // Save participant ID
        PlayerPrefs.SetString("ppt", pid);

        string condition = PlayerPrefs.GetString("condition");
        Debug.Log($"[SetCondition] Participant: {pid}, Condition: {condition}");

        // Setup experience manager (your existing code)
        TryLoadExperienceManager();
        if (_experienceManager != null)
        {
            if (int.TryParse(pid, out int pidInt))
            {
                _experienceManager.ParticipantID = pidInt;
            }
        }

        // Open data recorder (your existing code)
        if (ART_DataRecorder.Instance != null)
        {
            ART_DataRecorder.Instance.Open();
        }

        // Initialize the scene flow based on condition
        SceneFlowManager.Instance.InitializeFlow();

        // Check if it's an AI Doctor condition
        if (condition.StartsWith("LLM"))
        {
            // AI Doctor: Start Flask session first, then start flow
            isProcessing = true;
            SetButtonInteractable(false);
            StartCoroutine(StartAIDoctorFlowWithFlask(pid, condition));
        }
        else
        {
            // Existing conditions: Start flow immediately
            isProcessing = true;
            SceneFlowManager.Instance.StartFlow();
        }
    }

    /// <summary>
    /// For AI Doctor conditions: Start Flask session before starting scene flow
    /// </summary>
    private IEnumerator StartAIDoctorFlowWithFlask(string pid, string condition)
    {
        ShowStatus("Connecting to server...", Color.yellow);

        // Map condition to code: "1" = Empathetic, "2" = Non-Empathetic
        string conditionCode = condition == "LLM_Empathetic" ? "1" : "2";

        // Check Flask server connection
        bool serverRunning = false;
        yield return flaskClient.CheckConnection(connected => {
            serverRunning = connected;
        });

        if (!serverRunning)
        {
            ShowStatus("ERROR: Flask server not running!\nPlease start the server first.", Color.red);
            isProcessing = false;
            SetButtonInteractable(true);
            yield break;
        }

        ShowStatus("Starting session...", Color.yellow);

        // Start session on Flask server
        bool sessionStarted = false;
        string errorMessage = null;

        yield return flaskClient.StartSession(
            pid,
            conditionCode,
            onSuccess: (response) =>
            {
                sessionStarted = true;
                Debug.Log($"[SetCondition] Flask session started: {response.condition_name}");
            },
            onError: (error) =>
            {
                errorMessage = error;
                Debug.LogError($"[SetCondition] Flask session failed: {error}");
            }
        );

        if (sessionStarted)
        {
            // Save condition code for experimental scene
            PlayerPrefs.SetString("LLM_condition_code", conditionCode);

            ShowStatus("Starting study...", Color.green);
            yield return new WaitForSeconds(0.5f);

            // Start the scene flow
            SceneFlowManager.Instance.StartFlow();
        }
        else
        {
            ShowStatus($"ERROR: {errorMessage}", Color.red);
            isProcessing = false;
            SetButtonInteractable(true);
        }
    }

    /// <summary>
    /// Show status message to researcher
    /// </summary>
    private void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log($"[SetCondition] Status: {message}");
    }

    /// <summary>
    /// Enable/disable continue button
    /// </summary>
    private void SetButtonInteractable(bool interactable)
    {
        if (continueButton != null)
        {
            continueButton.interactable = interactable;
        }
    }

    public void TryLoadExperienceManager()
    {
        try
        {
            _experienceManager = FindObjectsByType<ART_ExperienceManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Single();
            if (_experienceManager == null)
                Debug.LogWarning("No experience manager found.");
        }
        catch
        {
            Debug.LogError("Only 1 experience manager can be active in the scene!");
        }
    }
}