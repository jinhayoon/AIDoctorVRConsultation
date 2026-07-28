using UnityEngine;
using Whisper;

public class SpeechToTextTest : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public WhisperManager whisper;

    private AudioClip micClip;
    private bool isRecording = false;
    private int sampleRate = 16000;

    void Start()
    {
        if (whisper == null)
        {
            Debug.LogError("WhisperManager not assigned! Drag it into the Inspector.");
            return;
        }
        Debug.Log("Ready! Press SPACE to record.");
    }

    void Update()
    {
        // Press SPACE to start recording
        if (Input.GetKeyDown(KeyCode.Space) && !isRecording)
        {
            StartRecording();
        }

        // Release SPACE to stop and transcribe
        if (Input.GetKeyUp(KeyCode.Space) && isRecording)
        {
            StopAndTranscribe();
        }
    }

    void StartRecording()
    {
        // Check for microphone
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
            return;
        }

        Debug.Log("Recording... (release SPACE to stop)");
        micClip = Microphone.Start(null, false, 30, sampleRate);
        isRecording = true;
    }

    async void StopAndTranscribe()
    {
        isRecording = false;

        // Get recording length
        int samples = Microphone.GetPosition(null);
        Microphone.End(null);

        if (samples == 0)
        {
            Debug.Log("No audio recorded");
            return;
        }

        Debug.Log("Transcribing...");

        // Get audio data
        float[] audioData = new float[samples];
        micClip.GetData(audioData, 0);

        // Transcribe using correct API
        var result = await whisper.GetTextAsync(audioData, sampleRate, 1);

        if (result == null)
        {
            Debug.Log("Transcription failed");
            return;
        }

        // Show result - use .Result property
        Debug.Log($"=== YOU SAID: {result.Result} ===");
    }
}