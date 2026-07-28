using UnityEngine;
using System.Linq;

/// <summary>
/// Add this script to any GameObject in your scene to debug microphone issues
/// It will show all available microphones and test input levels
/// </summary>
public class MicrophoneDebugger : MonoBehaviour
{
    [Header("Microphone Settings")]
    [Tooltip("Leave empty to use first available device")]
    public string deviceName = "";
    
    [Header("Debug Display")]
    public bool showInputLevel = true;
    public float updateInterval = 0.1f;
    
    [Header("Audio Visualization")]
    [Range(0.01f, 1f)]
    public float detectionThreshold = 0.01f;
    
    private AudioClip microphoneClip;
    private string selectedDevice;
    private bool isRecording = false;
    private float nextUpdateTime = 0f;
    private int lastMicPosition = 0;

    void Start()
    {
        Debug.Log("=== MICROPHONE DEBUGGER STARTED ===");
        CheckMicrophonePermissions();
        ListAvailableMicrophones();
        InitializeMicrophone();
    }

    void CheckMicrophonePermissions()
    {
        Debug.Log("Checking microphone permissions...");
        
#if UNITY_ANDROID || UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.LogWarning("⚠️ Microphone permission not granted. Requesting...");
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
        else
        {
            Debug.Log("✓ Microphone permission already granted");
        }
#else
        Debug.Log("✓ Desktop platform - no permission request needed");
#endif
    }

    void ListAvailableMicrophones()
    {
        string[] devices = Microphone.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("✗ NO MICROPHONES DETECTED!");
            Debug.LogError("Please check:");
            Debug.LogError("  1. Microphone is plugged in (if external)");
            Debug.LogError("  2. Microphone is enabled in system settings");
            Debug.LogError("  3. Unity has microphone permissions");
            return;
        }

        Debug.Log($"✓ Found {devices.Length} microphone device(s):");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"  [{i}] {devices[i]}");
        }
    }

    void InitializeMicrophone()
    {
        string[] devices = Microphone.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("Cannot initialize - no devices available");
            return;
        }

        // Select device
        if (string.IsNullOrEmpty(deviceName) || !devices.Contains(deviceName))
        {
            selectedDevice = devices[0];
            Debug.Log($"Using default device: {selectedDevice}");
        }
        else
        {
            selectedDevice = deviceName;
            Debug.Log($"Using specified device: {selectedDevice}");
        }

        StartMicrophoneRecording();
    }

    void StartMicrophoneRecording()
    {
        Debug.Log($"Starting microphone recording on: {selectedDevice}");
        
        try
        {
            // Start recording: looping, 10 seconds buffer, 44100 Hz
            microphoneClip = Microphone.Start(selectedDevice, true, 10, 44100);
            
            if (microphoneClip == null)
            {
                Debug.LogError("✗ Microphone.Start() returned null!");
                return;
            }

            // Wait for microphone to start
            int maxWaitTime = 100; // 100 frames
            int waitFrames = 0;
            while (Microphone.GetPosition(selectedDevice) <= 0 && waitFrames < maxWaitTime)
            {
                waitFrames++;
            }

            if (waitFrames >= maxWaitTime)
            {
                Debug.LogWarning("⚠️ Microphone took long to start");
            }

            isRecording = true;
            Debug.Log("✓ Microphone recording started successfully!");
            Debug.Log($"  Clip length: {microphoneClip.length}s");
            Debug.Log($"  Sample rate: {microphoneClip.frequency}Hz");
            Debug.Log($"  Channels: {microphoneClip.channels}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Failed to start microphone: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    void Update()
    {
        if (!isRecording || microphoneClip == null)
            return;

        if (Time.time >= nextUpdateTime)
        {
            CheckMicrophoneInput();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    void CheckMicrophoneInput()
    {
        int currentPosition = Microphone.GetPosition(selectedDevice);
        
        if (currentPosition < 0)
        {
            Debug.LogError("✗ Microphone position is negative - device may have stopped");
            return;
        }

        // Calculate how many samples to read
        int samplesToRead = currentPosition - lastMicPosition;
        if (samplesToRead < 0)
        {
            // Looped around
            samplesToRead = microphoneClip.samples - lastMicPosition + currentPosition;
        }

        if (samplesToRead > 0)
        {
            // Read audio data
            float[] samples = new float[samplesToRead * microphoneClip.channels];
            microphoneClip.GetData(samples, lastMicPosition);

            // Calculate RMS (Root Mean Square) for volume level
            float rms = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                rms += samples[i] * samples[i];
            }
            rms = Mathf.Sqrt(rms / samples.Length);

            // Display results
            if (showInputLevel)
            {
                if (rms > detectionThreshold)
                {
                    // Generate visual bar
                    int barLength = Mathf.CeilToInt(rms * 50);
                    string bar = new string('█', barLength);
                    Debug.Log($"🎤 MIC INPUT: {rms:F4} {bar}");
                }
            }
        }

        lastMicPosition = currentPosition;
    }

    void OnGUI()
    {
        if (!isRecording)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 300, 30), "MICROPHONE NOT RECORDING");
            return;
        }

        int position = Microphone.GetPosition(selectedDevice);
        
        GUI.color = Color.green;
        GUI.Label(new Rect(10, 10, 300, 30), $"Microphone: {selectedDevice}");
        GUI.Label(new Rect(10, 30, 300, 30), $"Position: {position} / {microphoneClip.samples}");
        
        // Show a simple volume meter
        float[] samples = new float[256];
        if (position > 256)
        {
            microphoneClip.GetData(samples, position - 256);
            float rms = 0f;
            foreach (float sample in samples)
            {
                rms += sample * sample;
            }
            rms = Mathf.Sqrt(rms / samples.Length);

            GUI.Label(new Rect(10, 50, 300, 30), $"Level: {rms:F4}");
            
            // Draw volume bar
            Rect barRect = new Rect(10, 70, 300, 20);
            GUI.Box(barRect, "");
            
            Rect fillRect = new Rect(10, 70, Mathf.Clamp01(rms * 10) * 300, 20);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        }
    }

    void OnDestroy()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedDevice))
        {
            Debug.Log("Stopping microphone recording...");
            Microphone.End(selectedDevice);
        }
    }

    void OnApplicationQuit()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedDevice))
        {
            Microphone.End(selectedDevice);
        }
    }
}
