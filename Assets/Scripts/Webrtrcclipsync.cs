using UnityEngine;
using CrazyMinnow.SALSA; // SALSA LipSync

namespace AIDoctor.Audio
{
    /// <summary>
    /// Bridges WebRTC audio to SALSA lip sync.
    /// 
    /// PROBLEM: SALSA reads PCM data from AudioSource.clip buffers.
    ///          WebRTC's AudioStreamTrack plays audio through the AudioSource
    ///          but doesn't populate a readable clip buffer.
    /// 
    /// SOLUTION: Use OnAudioFilterRead to intercept PCM samples flowing through
    ///           the speaker AudioSource, then write them into a mirror AudioClip
    ///           on a separate AudioSource that SALSA can read.
    /// 
    /// SETUP:
    ///   1. Attach this component to the SAME GameObject as the speaker AudioSource
    ///      (the one that has SetTrack called on it in RealtimeAPIManager)
    ///   2. Assign the Salsa component reference in the inspector
    ///   3. This script auto-creates a mirror AudioSource and points SALSA at it
    /// 
    /// NOTE: OnAudioFilterRead runs on the audio thread, not the main thread.
    ///       We use a lock to safely pass data between threads.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class WebRTCLipSync : MonoBehaviour
    {
        [Header("═══ References ═══")]
        [Tooltip("The SALSA component on your character. If null, will try to find one.")]
        [SerializeField] private Salsa salsa;

        [Header("═══ Settings ═══")]
        [Tooltip("Buffer duration in seconds for the mirror AudioClip.")]
        [SerializeField] private float bufferDuration = 1f;

        [Tooltip("Sample rate must match the WebRTC output (OpenAI uses 24000).")]
        [SerializeField] private int sampleRate = 24000;

        [Header("═══ Debug ═══")]
        [SerializeField] private bool showAmplitude = false;

        // Mirror audio source that SALSA reads from
        private AudioSource mirrorAudioSource;
        private AudioClip mirrorClip;
        private int mirrorWritePos = 0;

        // Thread-safe PCM buffer
        private float[] pcmBuffer;
        private int pcmWriteIndex = 0;
        private readonly object pcmLock = new object();

        // Amplitude (useful for other lip sync systems or debugging)
        private float currentAmplitude = 0f;
        public float CurrentAmplitude => currentAmplitude;

        private void Start()
        {
            CreateMirrorAudioSource();
            ConfigureSALSA();
        }

        private void CreateMirrorAudioSource()
        {
            // Create a child GameObject so we don't conflict with the WebRTC AudioSource
            var mirrorGO = new GameObject("SALSA_MirrorAudio");
            mirrorGO.transform.SetParent(transform);
            mirrorGO.transform.localPosition = Vector3.zero;

            mirrorAudioSource = mirrorGO.AddComponent<AudioSource>();
            mirrorAudioSource.spatialBlend = 0f; // 2D — SALSA just needs the data
            mirrorAudioSource.volume = 0f;        // Silent — we don't want double audio
            mirrorAudioSource.loop = true;
            mirrorAudioSource.playOnAwake = false;

            int totalSamples = sampleRate * (int)bufferDuration;
            mirrorClip = AudioClip.Create("WebRTC_Mirror", totalSamples, 1, sampleRate, false);
            //mirrorClip = Resources.Load<AudioClip>("Audio/random_audio_input");
            //mirrorClip = randomAudioInput;
            mirrorAudioSource.clip = mirrorClip;
            mirrorAudioSource.Play();

            pcmBuffer = new float[totalSamples];

            Debug.Log("[WebRTCLipSync] ✓ Mirror AudioSource created for SALSA");
        }

        private void ConfigureSALSA()
        {
            if (salsa == null)
                salsa = FindObjectOfType<Salsa>();

            if (salsa != null)
            {
                salsa.audioSrc = mirrorAudioSource;
                Debug.Log($"[WebRTCLipSync] ✓ SALSA linked to mirror AudioSource on '{salsa.gameObject.name}'");
            }
            else
            {
                Debug.LogWarning("[WebRTCLipSync] ⚠ No SALSA component found. Assign manually or ensure one exists in the scene.");
            }
        }

        /// <summary>
        /// Called on the AUDIO THREAD by Unity's audio system.
        /// Intercepts PCM samples from the WebRTC stream as they flow
        /// through the speaker AudioSource.
        /// </summary>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (pcmBuffer == null) return;


            // Calculate amplitude for this chunk
            float sum = 0f;
            int monoSampleCount = data.Length / channels;

            lock (pcmLock)
            {
                for (int i = 0; i < data.Length; i += channels)
                {
                    float sample = data[i]; // Take left/first channel
                    sum += sample * sample;

                    pcmBuffer[pcmWriteIndex] = sample;
                    pcmWriteIndex = (pcmWriteIndex + 1) % pcmBuffer.Length;
                }
            }

            currentAmplitude = Mathf.Sqrt(sum / monoSampleCount); // RMS amplitude

            // IMPORTANT: Do NOT zero out `data` here — we still want the audio to play.
        }

        private void Update()
        {
            if (mirrorClip == null || pcmBuffer == null) return;

            // Write captured PCM data to the mirror clip so SALSA can read it
            lock (pcmLock)
            {
                mirrorClip.SetData(pcmBuffer, 0);
            }

            if (showAmplitude && currentAmplitude > 0.001f)
            {
                //Debug.Log($"[WebRTCLipSync] Amplitude: {currentAmplitude:F4}");
            }
        }

        private void OnDestroy()
        {
            if (mirrorAudioSource != null)
            {
                mirrorAudioSource.Stop();
                Destroy(mirrorAudioSource.gameObject);
            }
        }
    }
}