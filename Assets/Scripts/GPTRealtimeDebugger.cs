using AIDoctor.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this script to debug GPT Realtime API connection and audio flow
/// This is a template - adapt it to match your actual GPT integration
/// </summary>
public class GPTRealtimeDebugger : MonoBehaviour
{
    [Header("Debug Options")]
    public bool showConnectionStatus = true;
    public bool showAudioFlow = true;
    public bool showWebRTCStatus = true;

    [Header("Status (Read-Only)")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private bool hasAudioSource = false;
    [SerializeField] private bool hasMicrophone = false;
    [SerializeField] private bool webRTCActive = false;
    [SerializeField] private string currentStatus = "Initializing...";

    private RealtimeAPIManager manager;
    private AudioSource audioSource;
    private bool hasSubscribedToEvents = false;

    void Start()
    {
        manager = GetComponent<RealtimeAPIManager>();

        if (manager == null)
        {
            Debug.LogError("[RealtimeDebugger] ❌ RealtimeAPIManager not found!");
            return;
        }

        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 REALTIME API DEBUGGER STARTED                          ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");

        SubscribeToEvents();
        CheckInitialState();
    }

    void SubscribeToEvents()
    {
        if (manager == null || hasSubscribedToEvents) return;

        manager.OnConnectionEstablished += OnConnected;
        manager.OnConnectionError += OnError;
        manager.OnAssistantMessage += OnAIMessage;
        manager.OnUserTranscript += OnUserMessage;
        manager.OnConsultationEnded += OnEnded;
        manager.OnHandScanRequested += OnScanRequested;

        hasSubscribedToEvents = true;
        Debug.Log("[RealtimeDebugger] ✓ Subscribed to RealtimeAPI events");
    }

    void CheckInitialState()
    {
        // Check audio source
        audioSource = manager?.SpeakerAudioSource;
        hasAudioSource = audioSource != null;

        if (hasAudioSource)
        {
            Debug.Log($"[RealtimeDebugger] ✓ AudioSource: {audioSource.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[RealtimeDebugger] ⚠️ AudioSource not assigned");
        }

        // Check microphone
        hasMicrophone = Microphone.devices.Length > 0;

        if (hasMicrophone)
        {
            Debug.Log($"[RealtimeDebugger] ✓ Microphone detected: {Microphone.devices[0]}");
        }
        else
        {
            Debug.LogError("[RealtimeDebugger] ❌ No microphone detected!");
        }

        currentStatus = "Waiting for connection...";
    }

    #region Event Handlers

    void OnConnected()
    {
        isConnected = true;
        webRTCActive = true;
        currentStatus = "Connected - Ready!";

        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  ✅ REALTIME API CONNECTED                                 ║");
        Debug.Log("║  WebRTC connection established                             ║");
        Debug.Log("║  Microphone → OpenAI → AudioSource                         ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
    }

    void OnError(string error)
    {
        isConnected = false;
        currentStatus = $"Error: {error}";

        Debug.LogError("╔════════════════════════════════════════════════════════════╗");
        Debug.LogError("║  ❌ CONNECTION ERROR                                       ║");
        Debug.LogError($"║  {error.PadRight(58)}║");
        Debug.LogError("╚════════════════════════════════════════════════════════════╝");
    }

    void OnAIMessage(string message)
    {
        if (!showAudioFlow) return;

        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  🤖 AI DOCTOR SPEAKING                                     ║");
        Debug.Log($"║  {TruncateString(message, 58).PadRight(58)}║");
        Debug.Log("║  → Audio should be playing through WebRTC stream          ║");
        Debug.Log("║  → Lip sync should be active                               ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");

        // Verify audio is actually playing
        StartCoroutine(CheckAudioPlayback());
    }

    void OnUserMessage(string transcript)
    {
        if (!showAudioFlow) return;

        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  🎤 PATIENT SPOKE                                          ║");
        Debug.Log($"║  {TruncateString(transcript, 58).PadRight(58)}║");
        Debug.Log("║  → Microphone captured audio                               ║");
        Debug.Log("║  → Sent to OpenAI via WebRTC                               ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
    }

    void OnScanRequested()
    {
        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  👐 HAND SCAN REQUESTED                                    ║");
        Debug.Log("║  Press SPACEBAR to start scan                              ║");
        Debug.Log("║  Press RETURN to complete scan                             ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
    }

    void OnEnded()
    {
        isConnected = false;
        webRTCActive = false;
        currentStatus = "Consultation ended";

        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  ✅ CONSULTATION COMPLETED                                 ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
    }

    #endregion

    #region Audio Verification

    System.Collections.IEnumerator CheckAudioPlayback()
    {
        yield return new WaitForSeconds(0.1f);

        if (audioSource == null)
        {
            Debug.LogWarning("[RealtimeDebugger] ⚠️ Cannot verify audio - AudioSource is null");
            yield break;
        }

        // For WebRTC, audio might not show as "playing" in traditional sense
        // But we can check if the AudioSource exists and is enabled
        if (!audioSource.enabled)
        {
            Debug.LogWarning("[RealtimeDebugger] ⚠️ AudioSource is DISABLED!");
            Debug.LogWarning("[RealtimeDebugger]    This will prevent audio playback");
        }

        if (audioSource.mute)
        {
            Debug.LogWarning("[RealtimeDebugger] ⚠️ AudioSource is MUTED!");
        }

        if (audioSource.volume < 0.1f)
        {
            Debug.LogWarning($"[RealtimeDebugger] ⚠️ AudioSource volume is very low: {audioSource.volume}");
        }
    }

    #endregion

    #region GUI

    void OnGUI()
    {
        if (!showConnectionStatus) return;

        GUILayout.BeginArea(new Rect(10, 10, 450, 250));
        GUILayout.BeginVertical("box");

        GUILayout.Label("═══ REALTIME API STATUS ═══", GetHeaderStyle());

        // Connection
        GUI.color = isConnected ? Color.green : Color.red;
        GUILayout.Label($"Connection: {(isConnected ? "✓ CONNECTED" : "✗ NOT CONNECTED")}");

        // WebRTC
        GUI.color = webRTCActive ? Color.green : Color.gray;
        GUILayout.Label($"WebRTC: {(webRTCActive ? "✓ ACTIVE" : "○ INACTIVE")}");

        // Audio Source
        GUI.color = hasAudioSource ? Color.green : Color.red;
        GUILayout.Label($"Audio Output: {(hasAudioSource ? "✓ READY" : "✗ MISSING")}");

        // Microphone
        GUI.color = hasMicrophone ? Color.green : Color.red;
        GUILayout.Label($"Microphone: {(hasMicrophone ? "✓ DETECTED" : "✗ NOT FOUND")}");

        // Status
        GUI.color = Color.white;
        GUILayout.Space(10);
        GUILayout.Label("Status:");
        GUILayout.Label($"  {currentStatus}");

        // Instructions
        if (isConnected)
        {
            GUILayout.Space(10);
            GUI.color = Color.cyan;
            GUILayout.Label("🎤 Speak to start conversation");
            GUILayout.Label("👐 AI will request hand scan when needed");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    GUIStyle GetHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        return style;
    }

    #endregion

    #region Utilities

    string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion

    void OnDestroy()
    {
        if (manager != null && hasSubscribedToEvents)
        {
            manager.OnConnectionEstablished -= OnConnected;
            manager.OnConnectionError -= OnError;
            manager.OnAssistantMessage -= OnAIMessage;
            manager.OnUserTranscript -= OnUserMessage;
            manager.OnConsultationEnded -= OnEnded;
            manager.OnHandScanRequested -= OnScanRequested;
        }
    }
}
