using UnityEngine;

namespace AIDoctor.Experimental
{
    /// <summary>
    /// Bridges WebRTC audio from GPT Realtime to SALSA OneClick for iClone.
    /// 
    /// WHY THIS IS NEEDED:
    /// - SALSA OneClick expects AudioClip analysis
    /// - WebRTC uses SetTrack() which bypasses Unity's audio pipeline entirely
    /// - GetSpectrumData() and GetOutputData() return all zeros for WebRTC audio
    /// - This script receives raw audio samples from RealtimeAPIManager
    ///   via ProcessWebRTCAudioClip() and computes amplitude directly
    /// 
    /// DESIGNED FOR:
    /// - SALSA OneClick for iClone/Character Creator
    /// - ARKit-52 blend shape characters
    /// - GPT Realtime API audio via WebRTC
    /// 
    /// SETUP:
    /// 1. Add to same GameObject as your OneClick components
    /// 2. Assign webrtcAudioSource (AudioSource receiving GPT audio)
    /// 3. OneClick preset should already be configured for your character
    /// 4. RealtimeAPIManager feeds audio data via SetLipSyncBridge()
    /// </summary>
    public class WebRTCToSALSA : MonoBehaviour
    {
        [Header("═══ Audio Source ═══")]
        [Tooltip("AudioSource receiving WebRTC audio from GPT Realtime")]
        public AudioSource webrtcAudioSource;

        [Header("═══ Analysis Settings ═══")]
        [Tooltip("Sensitivity multiplier (higher = more lip movement)")]
        [Range(0.5f, 5f)]
        public float sensitivity = 1.5f;

        [Tooltip("Smoothing factor (higher = smoother but less responsive)")]
        [Range(0f, 0.9f)]
        public float smoothing = 0.3f;

        [Tooltip("Minimum amplitude threshold")]
        [Range(0f, 0.05f)]
        public float threshold = 0.005f;

        [Header("═══ Debug ═══")]
        public bool showDebugLogs = false;
        public bool showInspectorStatus = true;

        [Header("═══ Status (Read-Only) ═══")]
        [SerializeField] private bool salsaFound = false;
        [SerializeField] private float currentAmplitude = 0f;
        [SerializeField] private float currentSayAmount = 0f;
        [SerializeField] private bool receivingAudio = false;

        // SALSA OneClick components
        private Component salsaComponent;
        private System.Reflection.PropertyInfo sayAmountProperty;
        private System.Reflection.FieldInfo sayAmountField;

        // Audio analysis — driven by raw WebRTC samples
        private float rawAmplitudeFromWebRTC = 0f;
        private float smoothedAmplitude = 0f;
        private float lastAudioTime = 0f;
        private volatile bool newAudioReceived = false;

        #region Unity Lifecycle

        void Start()
        {
            FindSALSAComponents();
            ValidateSetup();
        }

        void Update()
        {
            if (!enabled || !salsaFound)
            {
                smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, 0f, Time.deltaTime * 5f);
                return;
            }

            if (newAudioReceived)
            {
                lastAudioTime = Time.time;
                newAudioReceived = false;
            }
            receivingAudio = (Time.time - lastAudioTime) < 0.2f;
            
            float targetAmplitude;
            if (receivingAudio)
            {
                targetAmplitude = rawAmplitudeFromWebRTC * sensitivity;
                if (targetAmplitude < threshold)
                    targetAmplitude = 0f;
            }
            else
            {
                targetAmplitude = 0f;
            }

            currentAmplitude = targetAmplitude;
            smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, targetAmplitude, 1f - smoothing);
            UpdateSALSA(smoothedAmplitude);

            if (showDebugLogs && smoothedAmplitude > 0.01f)
            {
                Debug.Log($"[OneClickBridge] 📊 Amplitude: {smoothedAmplitude:F4} (raw: {rawAmplitudeFromWebRTC:F4})");
            }
        }

        #endregion

        #region WebRTC Audio Input

        /// <summary>
        /// Called by RealtimeAPIManager when WebRTC audio samples arrive.
        /// This is the key method — it replaces the broken GetSpectrumData approach.
        /// 
        /// AudioStreamTrack.OnAudioReceived provides an AudioClip with the latest
        /// chunk of decoded audio. We compute RMS amplitude from it.
        /// </summary>
        public void ProcessWebRTCAudioClip(AudioClip clip)
        {
            if (clip == null) return;

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Compute RMS amplitude
            float sumSquares = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sumSquares += samples[i] * samples[i];
            }

            float rms = Mathf.Sqrt(sumSquares / samples.Length);
            rawAmplitudeFromWebRTC = rms;
            lastAudioTime = Time.time;
        }

        /// <summary>
        /// Alternative: feed raw float samples directly if OnAudioReceived
        /// doesn't provide an AudioClip in your Unity.WebRTC version.
        /// </summary>
        public void ProcessRawAudioSamples(float[] samples)
        {
            if (samples == null || samples.Length == 0) return;

            float sumSquares = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sumSquares += samples[i] * samples[i];
            }

            float rms = Mathf.Sqrt(sumSquares / samples.Length);
            rawAmplitudeFromWebRTC = rms;
            lastAudioTime = Time.time;
        }

        #endregion

        #region Setup

        void FindSALSAComponents()
        {
            // Try to find any SALSA-related component on this GameObject
            string[] possibleNames = {
                "Salsa",
                "SalsaSync",
                "SalsaAdvancedDynamicsSilenceAnalyzer",
                "OneClickConfiguration",
                "OneClickBase",
                "SalsaOneClick"
            };

            Component[] allComponents = GetComponents<Component>();

            foreach (string componentName in possibleNames)
            {
                foreach (Component comp in allComponents)
                {
                    if (comp != null && comp.GetType().Name.Contains(componentName.Replace("Salsa", "").Replace("OneClick", "")))
                    {
                        salsaComponent = comp;
                        Debug.Log($"[OneClickBridge] Found component: {comp.GetType().Name}");
                        break;
                    }
                }
                if (salsaComponent != null) break;
            }

            if (salsaComponent == null)
            {
                Debug.LogError("[OneClickBridge] ❌ No SALSA component found!");
                Debug.LogError("[OneClickBridge] Available components on this GameObject:");
                foreach (Component comp in allComponents)
                {
                    if (comp != null)
                        Debug.LogError($"  - {comp.GetType().Name}");
                }
                return;
            }

            // Try to find sayAmount property or field via reflection
            var type = salsaComponent.GetType();
            sayAmountProperty = type.GetProperty("sayAmount");

            if (sayAmountProperty == null)
            {
                sayAmountField = type.GetField("sayAmount");
                if (sayAmountField != null)
                {
                    Debug.Log("[OneClickBridge] Found sayAmount as public field");
                }
                else
                {
                    Debug.LogWarning("[OneClickBridge] ⚠️ sayAmount property/field not found");
                    Debug.LogWarning("[OneClickBridge] SALSA may not respond to audio input");
                }
            }
            else
            {
                Debug.Log("[OneClickBridge] Found sayAmount as property");
            }

            salsaFound = true;
        }

        void ValidateSetup()
        {
            if (webrtcAudioSource == null)
            {
                webrtcAudioSource = GetComponent<AudioSource>();
            }

            if (webrtcAudioSource == null)
            {
                Debug.LogError("[OneClickBridge] ❌ WebRTC AudioSource not assigned!");
                enabled = false;
                return;
            }

            if (!salsaFound)
            {
                Debug.LogError("[OneClickBridge] ❌ SALSA component not found - bridge disabled");
                enabled = false;
                return;
            }

            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║  🎤 WEBRTC → SALSA ONECLICK BRIDGE INITIALIZED             ║");
            Debug.Log("╠════════════════════════════════════════════════════════════╣");
            Debug.Log($"║  Audio Source: {webrtcAudioSource.gameObject.name.PadRight(44)}║");
            Debug.Log($"║  SALSA Type: {salsaComponent.GetType().Name.PadRight(46)}║");
            Debug.Log($"║  Method: Raw WebRTC samples (not GetSpectrumData)".PadRight(61) + "║");
            Debug.Log($"║  Sensitivity: {sensitivity:F2}".PadRight(61) + "║");
            Debug.Log("╚════════════════════════════════════════════════════════════╝");
        }

        #endregion

        #region SALSA Integration

        void UpdateSALSA(float amplitude)
        {
            if (salsaComponent == null) return;

            // Convert amplitude to sayAmount (0-1 range for SALSA)
            float sayAmount = Mathf.Clamp01(amplitude * 10f);
            currentSayAmount = sayAmount;

            // Set sayAmount via reflection
            if (sayAmountProperty != null && sayAmountProperty.CanWrite)
            {
                sayAmountProperty.SetValue(salsaComponent, sayAmount);
            }
            else if (sayAmountField != null)
            {
                sayAmountField.SetValue(salsaComponent, sayAmount);
            }

            if (showDebugLogs && sayAmount > 0.1f)
            {
                Debug.Log($"[OneClickBridge] Say Amount: {sayAmount:F2}");
            }
        }

        #endregion

        #region Context Menu Tests

        [ContextMenu("1. Test SALSA Connection (Ramp Up/Down)")]
        public void TestSALSAConnection()
        {
            if (salsaComponent == null)
            {
                Debug.LogError("SALSA component not found!");
                return;
            }

            Debug.Log("Testing SALSA connection — ramping sayAmount up then down...");
            StartCoroutine(TestSALSACoroutine());
        }

        System.Collections.IEnumerator TestSALSACoroutine()
        {
            for (float t = 0; t <= 1f; t += 0.05f)
            {
                UpdateSALSA(t * 0.5f);
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(1f);

            for (float t = 1f; t >= 0f; t -= 0.05f)
            {
                UpdateSALSA(t * 0.5f);
                yield return new WaitForSeconds(0.05f);
            }

            Debug.Log("Test complete. Did the mouth move?");
        }

        [ContextMenu("2. Show Bridge Status")]
        public void ShowBridgeStatus()
        {
            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║  WEBRTC → SALSA BRIDGE STATUS                               ║");
            Debug.Log("╠════════════════════════════════════════════════════════════╣");
            Debug.Log($"║  SALSA Found: {(salsaFound ? "YES ✓" : "NO ✗")}".PadRight(61) + "║");
            Debug.Log($"║  Receiving Audio: {(receivingAudio ? "YES ✓" : "NO ✗")}".PadRight(61) + "║");
            Debug.Log($"║  Current Amplitude: {currentAmplitude:F4}".PadRight(61) + "║");
            Debug.Log($"║  Smoothed Amplitude: {smoothedAmplitude:F4}".PadRight(61) + "║");
            Debug.Log($"║  Say Amount: {currentSayAmount:F2}".PadRight(61) + "║");

            if (salsaComponent != null)
            {
                Debug.Log($"║  SALSA Type: {salsaComponent.GetType().Name}".PadRight(61) + "║");
            }

            Debug.Log("╚════════════════════════════════════════════════════════════╝");
        }

        #endregion
    }
}