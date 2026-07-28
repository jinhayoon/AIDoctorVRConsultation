using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Unity.WebRTC;
using AIDoctor.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AIDoctor.Networking
{
    /// <summary>
    /// Manages WebRTC connection to OpenAI GPT Realtime API.
    /// 
    /// MICROPHONE: Uses Unity's Microphone API → WebRTC AudioStreamTrack
    /// AUDIO OUTPUT: WebRTC AudioStreamTrack → AudioSource
    /// LIP SYNC: Requires WebRTCLipSync component (SALSA cannot read WebRTC streams)
    /// 
    /// HAND SCAN FLOW:
    ///   AI requests scan → trigger_hand_scan function called → waiting for operator input
    ///   ENTER    = User accepted scan  → complete scan, AI continues with results
    ///   SPACEBAR = User refused scan   → AI persuades and re-requests
    /// </summary>
    public class RealtimeAPIManager : MonoBehaviour
    {
        [Header("═══ Audio ═══")]
        [SerializeField] private AudioSource speakerAudioSource;
        [SerializeField] private string microphoneDevice = "";
        [SerializeField] private int sampleRate = 24000;

        [Header("═══ References ═══")]
        [SerializeField] private FlaskClient flaskClient;
        [SerializeField] private ConversationLogger conversationLogger;

        [Header("═══ API Settings ═══")]
        [SerializeField] private string model = "gpt-realtime-mini";
        [SerializeField] private string voice = "shimmer";

        [Header("═══ Debug ═══")]
        [SerializeField] private bool verboseLogging = true;

        // Events
        public event Action OnConnectionEstablished;
        public event Action<string> OnConnectionError;
        public event Action<string> OnAssistantMessage;
        public event Action<string> OnUserTranscript;
        public event Action OnConsultationEnded;
        public event Action OnHandScanRequested;

        // Hand scan state
        private string pendingScanCallId = null;
        private bool scanInProgress = false;
        private bool waitingForScanDecision = false;

        /// <summary>True when AI has requested a scan and we're waiting for ENTER or SPACE.</summary>
        public bool IsWaitingForScanDecision => waitingForScanDecision;
        /// <summary>True after ENTER was pressed and scan is "in progress".</summary>
        public bool IsScanInProgress => scanInProgress;

        // End consultation state — waits for response.done before saving
        private bool isEndingConsultation = false;
        private string pendingEndSummary;
        private bool pendingEndTreatmentAccepted;
        private Coroutine endConsultationTimeoutCoroutine;

        // Response tracking
        private bool isAISpeaking = false;
        /// <summary>True while the AI is generating a response.</summary>
        public bool IsAISpeaking => isAISpeaking;

        // WebRTC
        private RTCPeerConnection peerConnection;
        private RTCDataChannel dataChannel;
        private MediaStream localStream;
        private AudioStreamTrack localAudioTrack;

        // State
        private string currentToken;
        private string currentSystemPrompt;
        private string currentParticipantId;
        private string currentConditionName;
        private bool isConnected = false;
        private bool isSessionConfigured = false;
        private AudioClip microphoneClip;
        private AudioSource microphoneAudioSource;

        public static RealtimeAPIManager Instance { get; private set; }
        public bool IsConnected => isConnected;
        public AudioSource SpeakerAudioSource => speakerAudioSource;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            DiagnoseMicrophone();
        }

        private void Update()
        {
            HandleScanInput();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        #endregion

        #region Scan Input Handling

        /// <summary>
        /// Polls for keyboard input during the hand scan decision window.
        /// ENTER  = User accepted the scan  → complete scan with results
        /// SPACE  = User refused the scan   → AI persuades and re-requests
        /// </summary>
        private void HandleScanInput()
        {
            if (!waitingForScanDecision) return;

            // ENTER = User raised hands / accepted scan
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("[RealtimeAPI] ✓ ENTER pressed → User accepted scan");
                waitingForScanDecision = false;
                StartCoroutine(CompleteScanAccepted());
            }
            // SPACEBAR = User refused scan
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[RealtimeAPI] ✗ SPACE pressed → User refused scan, AI will persuade");
                waitingForScanDecision = false;
                HandleScanRefused();
            }
        }

        #endregion

        #region Microphone Diagnostics

        private void DiagnoseMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("❌ NO MICROPHONES DETECTED!");
                return;
            }

            Debug.Log($"✓ Found {Microphone.devices.Length} microphone(s):");
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                string device = Microphone.devices[i];
                Debug.Log($"[{i}] {device}");
            }

            if (string.IsNullOrEmpty(microphoneDevice))
            {
                microphoneDevice = Microphone.devices[0];
            }

            Debug.Log($"Using: {microphoneDevice}");
        }

        #endregion

        #region Public Setters

        public void SetAudioSource(AudioSource source)
        {
            speakerAudioSource = source;
            Debug.Log($"[RealtimeAPI] AudioSource set: {source?.gameObject.name}");
        }

        public void SetFlaskClient(FlaskClient client) => flaskClient = client;
        public void SetConversationLogger(ConversationLogger logger) => conversationLogger = logger;

        #endregion

        #region Connection

        public void Connect()
        {
            if (isConnected) { Debug.LogWarning("[RealtimeAPI] Already connected"); return; }
            if (flaskClient == null) { OnConnectionError?.Invoke("FlaskClient not set"); return; }
            if (speakerAudioSource == null)
            {
                Debug.LogWarning("[RealtimeAPI] No AudioSource - creating one");
                speakerAudioSource = gameObject.AddComponent<AudioSource>();
            }
            StartCoroutine(ConnectCoroutine());
        }

        public void Disconnect()
        {
            if (!isConnected && peerConnection == null) return;

            Debug.Log("[RealtimeAPI] Disconnecting...");
            isConnected = false;
            isSessionConfigured = false;
            isAISpeaking = false;
            waitingForScanDecision = false;
            scanInProgress = false;
            pendingScanCallId = null;
            isEndingConsultation = false;
            pendingEndSummary = null;
            pendingEndTreatmentAccepted = false;

            if (endConsultationTimeoutCoroutine != null)
            {
                StopCoroutine(endConsultationTimeoutCoroutine);
                endConsultationTimeoutCoroutine = null;
            }

            if (!string.IsNullOrEmpty(microphoneDevice) && Microphone.IsRecording(microphoneDevice))
                Microphone.End(microphoneDevice);

            if (microphoneAudioSource != null)
            {
                microphoneAudioSource.Stop();
                Destroy(microphoneAudioSource);
                microphoneAudioSource = null;
            }

            dataChannel?.Close(); dataChannel = null;
            localAudioTrack?.Dispose(); localAudioTrack = null;
            localStream?.Dispose(); localStream = null;

            if (peerConnection != null)
            {
                try { peerConnection.Close(); } catch { }
                peerConnection.Dispose();
                peerConnection = null;
            }

            Debug.Log("[RealtimeAPI] Disconnected");
        }

        private IEnumerator ConnectCoroutine()
        {
            Debug.Log("[RealtimeAPI] Connecting...");

            bool tokenReceived = false;
            string error = null;

            yield return flaskClient.GetToken(
                onSuccess: (response) =>
                {
                    currentToken = response.token;
                    currentSystemPrompt = response.system_prompt;
                    currentParticipantId = response.participant_id;
                    currentConditionName = response.condition_name;
                    tokenReceived = true;
                    Debug.Log($"[RealtimeAPI] ✓ Token for: {currentConditionName} - {response.token}");
                },
                onError: (err) => { error = err; Debug.LogError($"[RealtimeAPI] ✗ Token error: {err}"); }
            );

            if (!tokenReceived) { OnConnectionError?.Invoke(error ?? "Failed to get token"); yield break; }

            conversationLogger?.StartNewConversation(currentParticipantId, currentConditionName);
            yield return SetupWebRTC();
        }

        private IEnumerator SetupWebRTC()
        {
            Debug.Log("[RealtimeAPI] Setting up WebRTC...");

            var config = new RTCConfiguration
            {
                iceServers = new RTCIceServer[] { new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } } }
            };

            peerConnection = new RTCPeerConnection(ref config);
            peerConnection.OnTrack = OnTrack;
            peerConnection.OnDataChannel = c => SetupDataChannelHandlers(c);

            peerConnection.OnIceCandidate = (RTCIceCandidate candidate) =>
            {
                Debug.Log($"[RealtimeAPI] ICE Candidate generated: {candidate.Candidate}");
            };

            peerConnection.OnIceConnectionChange = (RTCIceConnectionState state) =>
            {
                Debug.Log($"[RealtimeAPI] ICE Connection State: {state}");
            };

            yield return SetupMicrophoneAudio();

            if (localAudioTrack == null)
            {
                Debug.LogError("[RealtimeAPI] ✗ Microphone setup failed!");
                OnConnectionError?.Invoke("Microphone setup failed");
                yield break;
            }

            var dcInit = new RTCDataChannelInit();
            dataChannel = peerConnection.CreateDataChannel("oai-events", dcInit);
            SetupDataChannelHandlers(dataChannel);

            var offerOp = peerConnection.CreateOffer();
            yield return offerOp;
            if (offerOp.IsError) { OnConnectionError?.Invoke(offerOp.Error.message); yield break; }

            var offer = offerOp.Desc;
            var setLocalOp = peerConnection.SetLocalDescription(ref offer);
            yield return setLocalOp;
            if (setLocalOp.IsError) { OnConnectionError?.Invoke(setLocalOp.Error.message); yield break; }

            yield return ExchangeSDP(offer.sdp);
        }

        private IEnumerator SetupMicrophoneAudio()
        {
            if (Microphone.devices.Length == 0) { Debug.LogError("[RealtimeAPI] No microphone!"); yield break; }

            Debug.Log($"[RealtimeAPI] Starting mic: {microphoneDevice} @ {sampleRate}Hz");

            microphoneClip = Microphone.Start(microphoneDevice, true, 1, sampleRate);
            if (microphoneClip == null) { Debug.LogError("[RealtimeAPI] ✗ Mic.Start returned null!"); yield break; }

            float elapsed = 0f;
            while (elapsed < 5f && Microphone.GetPosition(microphoneDevice) <= 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (Microphone.GetPosition(microphoneDevice) <= 0)
            {
                Debug.LogError("[RealtimeAPI] ✗ Mic timeout! Check permissions.");
                yield break;
            }

            Debug.Log("[RealtimeAPI] ✓ Microphone started");

            microphoneAudioSource = gameObject.AddComponent<AudioSource>();
            microphoneAudioSource.clip = microphoneClip;
            microphoneAudioSource.loop = true;
            microphoneAudioSource.Play();

            localStream = new MediaStream();
            localAudioTrack = new AudioStreamTrack(microphoneAudioSource);
            localStream.AddTrack(localAudioTrack);
            peerConnection.AddTrack(localAudioTrack, localStream);

            Debug.Log("[RealtimeAPI] ✓ Audio track added");
        }

        private IEnumerator ExchangeSDP(string offerSdp)
        {
            Debug.Log("[RealtimeAPI] Exchanging SDP...");

            using (var request = new UnityWebRequest($"https://api.openai.com/v1/realtime/calls", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(offerSdp));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", $"Bearer {currentToken}");
                request.SetRequestHeader("Content-Type", "application/sdp");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[RealtimeAPI] SDP failed: {request.error}");
                    OnConnectionError?.Invoke(request.error);
                    yield break;
                }

                var answer = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = request.downloadHandler.text };
                var setRemoteOp = peerConnection.SetRemoteDescription(ref answer);
                yield return setRemoteOp;

                if (setRemoteOp.IsError) { OnConnectionError?.Invoke(setRemoteOp.Error.message); yield break; }

                Debug.Log("[RealtimeAPI] ✓ WebRTC ready");
            }
        }

        #endregion

        #region WebRTC Handlers

        private void OnTrack(RTCTrackEvent e)
        {
            Debug.Log($"[RealtimeAPI] Track received! Kind: {e.Track.Kind}, Enabled: {e.Track.Enabled}");
            if (e.Track is AudioStreamTrack audioTrack)
            {
                Debug.Log($"[RealtimeAPI] 🔊 AI voice → {speakerAudioSource.gameObject.name}");
                speakerAudioSource.SetTrack(audioTrack);
                speakerAudioSource.loop = true;
                speakerAudioSource.Play();
                Debug.Log("[RealtimeAPI] ✓ Audio playback started");
            }
        }

        private void SetupDataChannelHandlers(RTCDataChannel channel)
        {
            channel.OnOpen = () => { Debug.Log("[RealtimeAPI] ✓ Data channel open"); ConfigureSession(); };
            channel.OnClose = () => Debug.Log("[RealtimeAPI] Data channel closed");
            channel.OnMessage = bytes => HandleServerEvent(Encoding.UTF8.GetString(bytes));
        }

        #endregion

        #region Session

        private void ConfigureSession()
        {
            Debug.Log("[RealtimeAPI] Configuring session...");

            SendEvent(new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "audio", "text" },
                    instructions = currentSystemPrompt,
                    voice = voice,
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    input_audio_transcription = new { model = "whisper-1" },
                    turn_detection = new { type = "server_vad", threshold = 0.5f, prefix_padding_ms = 300, silence_duration_ms = 700 },
                    tools = GetToolDefinitions()
                }
            });

            isSessionConfigured = true;
        }

        private object[] GetToolDefinitions() => new object[]
        {
            new
            {
                type = "function",
                name = "trigger_hand_scan",
                description = "Triggers a hand scan when the AI doctor needs to examine the patient's hands. Call this when you want to perform a visual scan of the patient's hands for diagnosis.",
                parameters = new { type = "object", properties = new { }, required = new string[] { } }
            },
            new
            {
                type = "function",
                name = "end_consultation",
                description = "Call this function to end the consultation. You MUST call this when ANY of the following occur: " +
                    "(1) The patient accepts the proposed treatment or prescription. " +
                    "(2) The patient says goodbye, thanks you, or indicates they are done. " +
                    "(3) You have delivered your diagnosis, treatment plan, and aftercare advice and the patient has no more questions. " +
                    "(4) The conversation has naturally concluded. " +
                    "Before calling this function, say a warm goodbye to the patient (e.g. 'Take care, and don't hesitate to come back if you need anything!'). " +
                    "Then immediately call this function.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        summary = new { type = "string", description = "Brief summary of the consultation including diagnosis and treatment plan" },
                        treatment_accepted = new { type = "boolean", description = "Whether the patient accepted the proposed treatment" }
                    },
                    required = new[] { "summary", "treatment_accepted" }
                }
            }
        };

        #endregion

        #region Events

        private void SendEvent(object data)
        {
            if (dataChannel == null || dataChannel.ReadyState != RTCDataChannelState.Open) return;
            dataChannel.Send(JsonConvert.SerializeObject(data));
        }

        private void HandleServerEvent(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                string type = obj["type"]?.ToString();

                switch (type)
                {
                    // ── Session lifecycle ──────────────────────────────────
                    case "session.created":
                        string sessionId = obj["session"]?["id"]?.ToString();
                        Debug.Log($"[RealtimeAPI] ✓ Session created: {sessionId}");
                        isConnected = true;
                        OnConnectionEstablished?.Invoke();
                        break;

                    case "session.updated":
                        Debug.Log("[RealtimeAPI] ✓ Session configuration applied");
                        if (verboseLogging)
                        {
                            var tools = obj["session"]?["tools"];
                            int toolCount = tools != null ? ((JArray)tools).Count : 0;
                            Debug.Log($"[RealtimeAPI]   Voice: {obj["session"]?["voice"]}, Tools: {toolCount}");
                        }
                        break;

                    // ── Audio input ────────────────────────────────────────
                    case "input_audio_buffer.speech_started":
                        Debug.Log("[RealtimeAPI] 🎤 Speech detected...");
                        break;

                    case "input_audio_buffer.speech_stopped":
                        Debug.Log("[RealtimeAPI] 🎤 Speech ended, processing...");
                        break;

                    // ── Response lifecycle ─────────────────────────────────
                    case "response.created":
                        isAISpeaking = true;
                        if (verboseLogging)
                        {
                            string responseId = obj["response"]?["id"]?.ToString();
                            Debug.Log($"[RealtimeAPI] 💬 Response started: {responseId}");
                        }
                        break;

                    case "response.done":
                        isAISpeaking = false;
                        HandleResponseDone(obj);
                        break;

                    // ── Transcription ──────────────────────────────────────
                    case "response.audio_transcript.done":
                        string aiText = obj["transcript"]?.ToString();
                        if (!string.IsNullOrEmpty(aiText))
                        {
                            Debug.Log($"AI Doctor: {aiText}");
                            conversationLogger?.AddAssistantMessage(aiText);
                            OnAssistantMessage?.Invoke(aiText);
                        }
                        break;

                    case "conversation.item.input_audio_transcription.completed":
                        string userText = obj["transcript"]?.ToString();
                        if (!string.IsNullOrEmpty(userText))
                        {
                            Debug.Log($"Patient: {userText}");
                            conversationLogger?.AddUserMessage(userText);
                            OnUserTranscript?.Invoke(userText);
                        }
                        break;

                    // ── Function calls ─────────────────────────────────────
                    case "response.function_call_arguments.done":
                        HandleFunctionCall(obj);
                        break;

                    // ── Errors ─────────────────────────────────────────────
                    case "error":
                        string errorCode = obj["error"]?["code"]?.ToString();
                        string errorMsg = obj["error"]?["message"]?.ToString();
                        string eventId = obj["event_id"]?.ToString();
                        Debug.LogError($"[RealtimeAPI] ❌ Error [{errorCode}]: {errorMsg} (event: {eventId})");
                        break;

                    // ── Rate limits ────────────────────────────────────────
                    case "rate_limits.updated":
                        if (verboseLogging)
                        {
                            var limits = obj["rate_limits"];
                            Debug.Log($"[RealtimeAPI] 📊 Rate limits updated: {limits}");
                        }
                        break;

                    default:
                        if (verboseLogging)
                            Debug.Log($"[RealtimeAPI] Event: {type}");
                        break;
                }
            }
            catch (Exception e) { Debug.LogError($"[RealtimeAPI] Parse error: {e.Message}"); }
        }

        /// <summary>
        /// Handles the response.done server event. If we're waiting for the
        /// goodbye response to finish, this triggers save and disconnect.
        /// Also logs response status for debugging.
        /// </summary>
        private void HandleResponseDone(JObject obj)
        {
            string status = obj["response"]?["status"]?.ToString();
            string responseId = obj["response"]?["id"]?.ToString();

            if (verboseLogging)
                Debug.Log($"[RealtimeAPI] ✓ Response complete: {responseId} (status: {status})");

            // If we were waiting for the goodbye response to finish, now save and disconnect
            if (isEndingConsultation)
            {
                Debug.Log("[RealtimeAPI] ✓ Goodbye response finished — saving and disconnecting");

                // Cancel the safety timeout
                if (endConsultationTimeoutCoroutine != null)
                {
                    StopCoroutine(endConsultationTimeoutCoroutine);
                    endConsultationTimeoutCoroutine = null;
                }

                isEndingConsultation = false;
                StartCoroutine(SaveAndDisconnect(pendingEndSummary, pendingEndTreatmentAccepted));
            }
        }

        private void HandleFunctionCall(JObject obj)
        {
            string name = obj["name"]?.ToString();
            string callId = obj["call_id"]?.ToString();
            string args = obj["arguments"]?.ToString();

            Debug.Log($"[RealtimeAPI] 🔧 Function: {name}");

            if (name == "trigger_hand_scan")
            {
                HandleHandScanRequested(callId);
            }
            else if (name == "end_consultation")
            {
                HandleEndConsultation(callId, args);
            }
        }

        #endregion

        #region Hand Scan

        /// <summary>
        /// Called when the AI doctor triggers the hand scan function.
        /// Enters the decision-waiting state for operator input.
        /// </summary>
        private void HandleHandScanRequested(string callId)
        {
            pendingScanCallId = callId;
            scanInProgress = false;
            waitingForScanDecision = true;
            OnHandScanRequested?.Invoke();
        }

        /// <summary>
        /// ENTER pressed: User accepted the scan.
        /// Sends a brief "scanning" message, waits, then returns scan results.
        /// </summary>
        private IEnumerator CompleteScanAccepted()
        {
            scanInProgress = true;
            string callId = pendingScanCallId;

            Debug.Log("[RealtimeAPI] ▶ Scan accepted — performing scan...");

            // Have AI acknowledge the scan is happening
            SendEvent(new
            {
                type = "response.create",
                response = new
                {
                    modalities = new[] { "audio", "text" },
                    instructions = "The patient has raised their hands for the scan. Say briefly: 'Thank you, let me take a look... scanning now.' Keep it short and natural."
                }
            });

            // Simulate scan duration
            yield return new WaitForSeconds(3f);

            // Return scan results to the AI — pass the condition so GPT
            // generates contextually appropriate findings
            SendFunctionResult(callId, new
            {
                success = true,
                scan_completed = true,
                condition = currentConditionName,
                message = $"Hand scan completed successfully. The patient's condition is: {currentConditionName}. Describe what the scan visually revealed on the patient's hands that is consistent with this condition, then continue the consultation."
            });

            // Trigger AI to continue the conversation with the scan results
            SendEvent(new { type = "response.create" });

            Debug.Log("[RealtimeAPI] ✓ Scan complete — AI continuing with results");

            pendingScanCallId = null;
            scanInProgress = false;
        }

        /// <summary>
        /// SPACEBAR pressed: User refused the scan.
        /// Returns a "refused" result so the AI explains importance and re-requests.
        /// When the AI asks again, it will call trigger_hand_scan again,
        /// bringing us back to the ENTER/SPACE decision point.
        /// </summary>
        private void HandleScanRefused()
        {
            string callId = pendingScanCallId;

            Debug.Log("[RealtimeAPI] ✗ Scan refused — AI will persuade and re-request");

            // Tell the AI the patient refused
            SendFunctionResult(callId, new
            {
                success = false,
                scan_completed = false,
                patient_refused = true,
                message = "The patient refused or hesitated to show their hands for the scan."
            });

            // Instruct AI to explain importance and ask again
            SendEvent(new
            {
                type = "response.create",
                response = new
                {
                    modalities = new[] { "audio", "text" },
                    instructions = "The patient refused the hand scan. Gently and empathetically explain why the hand scan is important for an accurate diagnosis. Reassure them it's quick, painless, and non-invasive. Then ask them again if they'd be willing to show their hands. If they agree, call the trigger_hand_scan function again."
                }
            });

            // Reset so the next trigger_hand_scan call re-enters this flow
            pendingScanCallId = null;
            scanInProgress = false;
        }

        private void SendFunctionResult(string callId, object output) =>
            SendEvent(new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = JsonConvert.SerializeObject(output)
                }
            });

        #endregion

        #region End Consultation

        private void HandleEndConsultation(string callId, string args)
        {
            var p = JObject.Parse(args ?? "{}");
            string summary = p["summary"]?.ToString() ?? "";
            bool accepted = p["treatment_accepted"]?.Value<bool>() ?? false;

            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║  🏁 CONSULTATION ENDING                                    ║");
            Debug.Log($"║  Treatment accepted: {accepted}                            ");
            Debug.Log($"║  Summary: {(summary.Length > 50 ? summary.Substring(0, 50) + "..." : summary)}");
            Debug.Log("╚════════════════════════════════════════════════════════════╝");

            // Store pending end data — will be used when response.done fires
            pendingEndSummary = summary;
            pendingEndTreatmentAccepted = accepted;
            isEndingConsultation = true;

            // Acknowledge the function call
            SendFunctionResult(callId, new { success = true });

            // Trigger the AI to say a final goodbye
            SendEvent(new { type = "response.create" });

            // Safety timeout: if response.done never arrives (network issue, etc),
            // force save and disconnect after 15 seconds
            endConsultationTimeoutCoroutine = StartCoroutine(EndConsultationSafetyTimeout(summary, accepted));
        }

        /// <summary>
        /// Safety timeout in case response.done is never received.
        /// Waits 15 seconds, then forces save and disconnect.
        /// </summary>
        private IEnumerator EndConsultationSafetyTimeout(string summary, bool accepted)
        {
            yield return new WaitForSeconds(15f);

            if (isEndingConsultation)
            {
                Debug.LogWarning("[RealtimeAPI] ⚠️ Goodbye response.done not received after 15s — forcing save");
                isEndingConsultation = false;
                yield return SaveAndDisconnect(summary, accepted);
            }
        }

        private IEnumerator SaveAndDisconnect(string summary, bool accepted)
        {
            Debug.Log("💾 ATTEMPTING TO SAVE CONVERSATION");

            yield return new WaitForSeconds(2f);

            // 1. Save JSON locally (always — works even if server is down)
            SaveTranscriptToJSON(summary, accepted);

            // 2. Save to Flask server
            if (flaskClient != null && conversationLogger != null)
            {
                var transcript = conversationLogger.GetTranscript();
                Debug.Log($"[RealtimeAPI] Transcript has {transcript.Count} messages");

                bool done = false;

                yield return flaskClient.SaveConversation(
                    new SaveConversationRequest
                    {
                        transcript = transcript,
                        treatment_accepted = accepted,
                        summary = summary
                    },
                    response =>
                    {
                        Debug.Log("✅ CONVERSATION SAVED TO SERVER!");
                        done = true;
                    },
                    error =>
                    {
                        Debug.LogError("❌ SERVER SAVE FAILED!");
                        Debug.LogError($"{error.PadRight(58)}");
                        done = true;
                    }
                );

                while (!done) yield return null;
            }
            else
            {
                if (flaskClient == null)
                    Debug.LogError("[RealtimeAPI] ❌ FlaskClient is NULL!");
                if (conversationLogger == null)
                    Debug.LogError("[RealtimeAPI] ❌ ConversationLogger is NULL!");
            }

            Disconnect();
            OnConsultationEnded?.Invoke();
        }

        /// <summary>
        /// Called by the researcher to manually end the consultation (e.g. via Escape key).
        /// Saves transcript locally and to server, then disconnects.
        /// </summary>
        public void EndConsultationManually()
        {
            if (!isConnected && peerConnection == null)
            {
                Debug.LogWarning("[RealtimeAPI] Not connected — nothing to end");
                return;
            }

            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║  🛑 MANUAL END — Researcher ended the consultation        ║");
            Debug.Log("╚════════════════════════════════════════════════════════════╝");

            StartCoroutine(SaveAndDisconnect("Consultation ended manually by researcher.", false));
        }

        #endregion

        #region Local JSON Save

        /// <summary>
        /// Saves the full conversation transcript as a JSON file to the persistent data path.
        /// File format: conversation_{participantId}_{timestamp}.json
        /// </summary>
        private void SaveTranscriptToJSON(string summary, bool treatmentAccepted)
        {
            if (conversationLogger == null)
            {
                Debug.LogError("[RealtimeAPI] ❌ Cannot save JSON — ConversationLogger is NULL!");
                return;
            }

            try
            {
                var transcript = conversationLogger.GetTranscript();

                var data = new
                {
                    participant_id = currentParticipantId ?? "unknown",
                    condition = currentConditionName ?? "unknown",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    time = DateTime.Now.ToString("HH:mm:ss"),
                    summary = summary,
                    treatment_accepted = treatmentAccepted,
                    message_count = transcript.Count,
                    transcript = transcript
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);

                // Save to Unity's persistent data path (survives app restarts)
                string directory = Path.Combine(Application.persistentDataPath, "Transcripts");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string participantTag = !string.IsNullOrEmpty(currentParticipantId) ? currentParticipantId : "unknown";
                string filename = $"conversation_{participantTag}_{timestamp}.json";
                string filepath = Path.Combine(directory, filename);

                File.WriteAllText(filepath, json);

                Debug.Log($"✅ TRANSCRIPT SAVED LOCALLY:");
                Debug.Log($"   📁 {filepath}");
                Debug.Log($"   📝 {transcript.Count} messages");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealtimeAPI] ❌ Failed to save JSON locally: {e.Message}");
            }
        }

        #endregion

    }
}