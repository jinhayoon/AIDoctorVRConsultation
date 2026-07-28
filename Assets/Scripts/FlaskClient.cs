using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AIDoctor.Data;

namespace AIDoctor.Networking
{
    /// <summary>
    /// Handles communication with the Flask backend server.
    /// Manages session state, token retrieval, and conversation saving.
    /// </summary>
    public class FlaskClient : MonoBehaviour
    {
        [Header("Server Configuration")]
        [Tooltip("URL of the Flask server (default: http://127.0.0.1:5050)")]
        [SerializeField] private string serverUrl = "http://127.0.0.1:5050";
        
        public static FlaskClient Instance { get; private set; }

        public string ServerUrl => serverUrl;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Start a new session with participant ID and condition.
        /// Must be called before GetToken().
        /// </summary>
        /// <param name="participantId">Unique identifier for the participant</param>
        /// <param name="condition">"1" for Empathetic, "2" for Non-Empathetic</param>
        /// <param name="onSuccess">Callback on successful session start</param>
        /// <param name="onError">Callback on error</param>
        public IEnumerator StartSession(string participantId, string condition, 
            Action<SessionStartResponse> onSuccess, Action<string> onError)
        {
            var request = new SessionStartRequest
            {
                participant_id = participantId,
                condition = condition
            };

            string jsonBody = JsonUtility.ToJson(request);
            Debug.Log($"[FlaskClient] Starting session: {jsonBody}");
            
            using (UnityWebRequest webRequest = new UnityWebRequest($"{serverUrl}/start_session", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SessionStartResponse>(webRequest.downloadHandler.text);
                    Debug.Log($"[FlaskClient] Session started: {response.condition_name}");
                    onSuccess?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[FlaskClient] StartSession failed: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
            }
        }

        /// <summary>
        /// Get ephemeral token for GPT Realtime API connection.
        /// Session must be started first via StartSession().
        /// </summary>
        /// <param name="onSuccess">Callback with token and system prompt</param>
        /// <param name="onError">Callback on error</param>
        public IEnumerator GetToken(Action<TokenResponse> onSuccess, Action<string> onError)
        {
            Debug.Log("[FlaskClient] Requesting token...");
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{serverUrl}/get_token"))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<TokenResponse>(webRequest.downloadHandler.text);
                    
                    if (!string.IsNullOrEmpty(response.error))
                    {
                        Debug.LogError($"[FlaskClient] Token error: {response.error}");
                        onError?.Invoke(response.error);
                    }
                    else
                    {
                        Debug.Log($"[FlaskClient] Token received for participant: {response.participant_id}");
                        onSuccess?.Invoke(response);
                    }
                }
                else
                {
                    Debug.LogError($"[FlaskClient] GetToken failed: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
            }
        }

        /// <summary>
        /// Save conversation transcript to the server.
        /// Called when consultation ends.
        /// </summary>
        /// <param name="data">Transcript data to save</param>
        /// <param name="onSuccess">Callback on successful save</param>
        /// <param name="onError">Callback on error</param>
        public IEnumerator SaveConversation(SaveConversationRequest data, 
            Action<SaveConversationResponse> onSuccess, Action<string> onError)
        {
            string jsonBody = JsonUtility.ToJson(data);
            Debug.Log($"[FlaskClient] Saving conversation with {data.transcript.Count} messages");
            
            using (UnityWebRequest webRequest = new UnityWebRequest($"{serverUrl}/save_conversation", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SaveConversationResponse>(webRequest.downloadHandler.text);
                    Debug.Log($"[FlaskClient] Conversation saved: {response.filename}");
                    onSuccess?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[FlaskClient] SaveConversation failed: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
            }
        }

        /// <summary>
        /// Check if the Flask server is reachable.
        /// </summary>
        /// <param name="callback">True if connected, false otherwise</param>
        public IEnumerator CheckConnection(Action<bool> callback)
        {
            Debug.Log("[FlaskClient] Checking server connection...");
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{serverUrl}/session_status"))
            {
                webRequest.timeout = 5;
                yield return webRequest.SendWebRequest();
                
                bool connected = webRequest.result == UnityWebRequest.Result.Success;
                Debug.Log($"[FlaskClient] Server connection: {(connected ? "OK" : "FAILED")}");
                callback?.Invoke(connected);
            }
        }

        /// <summary>
        /// End the current session on the server.
        /// </summary>
        public IEnumerator EndSession(Action onComplete = null)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest($"{serverUrl}/end_session", "POST"))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return webRequest.SendWebRequest();
                
                Debug.Log("[FlaskClient] Session ended");
                onComplete?.Invoke();
            }
        }
    }
}
