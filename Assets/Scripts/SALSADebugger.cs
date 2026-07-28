using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Add this script to the same GameObject as your AudioSource and SALSA components
/// It will monitor audio playback and SALSA lip sync activity
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SALSADebugger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your SALSA component (usually on same GameObject)")]
    public MonoBehaviour salsaComponent;
    
    [Header("Debug Options")]
    public bool logAudioPlayback = true;
    public bool logSALSAActivity = true;
    public bool visualDebug = true;
    
    [Header("Status (Read-Only)")]
    [SerializeField] private bool audioSourceFound = false;
    [SerializeField] private bool salsaFound = false;
    [SerializeField] private bool isAudioPlaying = false;
    [SerializeField] private float currentAudioTime = 0f;
    [SerializeField] private float clipLength = 0f;
    
    private AudioSource audioSource;
    private SkinnedMeshRenderer faceMesh;
    private bool lastPlayingState = false;
    private float lastLogTime = 0f;
    private float logInterval = 0.5f;

    void Start()
    {
        Debug.Log("=== SALSA DEBUGGER STARTED ===");
        ValidateComponents();
        FindFaceMesh();
    }

    void ValidateComponents()
    {
        // Check AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("✗ AudioSource component not found on this GameObject!");
            audioSourceFound = false;
        }
        else
        {
            Debug.Log("✓ AudioSource found");
            Debug.Log($"  Play On Awake: {audioSource.playOnAwake}");
            Debug.Log($"  Volume: {audioSource.volume}");
            Debug.Log($"  Mute: {audioSource.mute}");
            Debug.Log($"  Spatialize: {audioSource.spatialize}");
            audioSourceFound = true;
        }

        // Check SALSA component
        if (salsaComponent == null)
        {
            // Try to find it automatically
            var salsaComponents = GetComponents<MonoBehaviour>();
            foreach (var comp in salsaComponents)
            {
                if (comp.GetType().Name.Contains("Salsa") || 
                    comp.GetType().Name.Contains("SALSA"))
                {
                    salsaComponent = comp;
                    Debug.Log($"✓ Found SALSA component automatically: {comp.GetType().Name}");
                    break;
                }
            }
        }

        if (salsaComponent == null)
        {
            Debug.LogWarning("⚠️ SALSA component not assigned or found!");
            Debug.LogWarning("Please assign it manually in the Inspector");
            salsaFound = false;
        }
        else
        {
            Debug.Log($"✓ SALSA component: {salsaComponent.GetType().Name}");
            Debug.Log($"  Enabled: {salsaComponent.enabled}");
            salsaFound = true;
            
            // Try to get audioSrc field using reflection
            var audioSrcField = salsaComponent.GetType().GetField("audioSrc");
            if (audioSrcField != null)
            {
                var linkedAudioSource = audioSrcField.GetValue(salsaComponent) as AudioSource;
                if (linkedAudioSource == audioSource)
                {
                    Debug.Log("  ✓ SALSA is correctly linked to this AudioSource");
                }
                else if (linkedAudioSource == null)
                {
                    Debug.LogError("  ✗ SALSA's audioSrc field is NULL!");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ SALSA is linked to a different AudioSource: {linkedAudioSource.name}");
                }
            }
        }
    }

    void FindFaceMesh()
    {
        faceMesh = GetComponent<SkinnedMeshRenderer>();
        
        if (faceMesh == null)
        {
            faceMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (faceMesh != null)
        {
            Debug.Log($"✓ Found SkinnedMeshRenderer: {faceMesh.name}");
            Debug.Log($"  Blend shape count: {faceMesh.sharedMesh.blendShapeCount}");
            
            // List some blend shapes
            if (faceMesh.sharedMesh.blendShapeCount > 0)
            {
                Debug.Log("  Sample blend shapes:");
                for (int i = 0; i < Mathf.Min(10, faceMesh.sharedMesh.blendShapeCount); i++)
                {
                    string shapeName = faceMesh.sharedMesh.GetBlendShapeName(i);
                    float weight = faceMesh.GetBlendShapeWeight(i);
                    Debug.Log($"    [{i}] {shapeName} = {weight}");
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No SkinnedMeshRenderer found for blend shape monitoring");
        }
    }

    void Update()
    {
        if (!audioSourceFound) return;

        isAudioPlaying = audioSource.isPlaying;
        currentAudioTime = audioSource.time;
        
        if (audioSource.clip != null)
        {
            clipLength = audioSource.clip.length;
        }

        // Detect state changes
        if (isAudioPlaying != lastPlayingState)
        {
            if (isAudioPlaying)
            {
                OnAudioStarted();
            }
            else
            {
                OnAudioStopped();
            }
            lastPlayingState = isAudioPlaying;
        }

        // Periodic logging
        if (isAudioPlaying && logAudioPlayback && Time.time >= lastLogTime + logInterval)
        {
            Debug.Log($"🔊 Audio playing: {currentAudioTime:F2}s / {clipLength:F2}s");
            MonitorBlendShapes();
            lastLogTime = Time.time;
        }
    }

    void OnAudioStarted()
    {
        Debug.Log("▶️ AUDIO PLAYBACK STARTED");
        Debug.Log($"  Clip: {(audioSource.clip != null ? audioSource.clip.name : "NULL")}");
        Debug.Log($"  Length: {clipLength:F2}s");
        Debug.Log($"  Volume: {audioSource.volume}");
        
        if (salsaComponent != null && !salsaComponent.enabled)
        {
            Debug.LogWarning("⚠️ WARNING: SALSA component is DISABLED!");
        }

        if (faceMesh != null)
        {
            Debug.Log($"  Face mesh: {faceMesh.name} ({faceMesh.sharedMesh.blendShapeCount} blend shapes)");
        }
    }

    void OnAudioStopped()
    {
        Debug.Log("⏹️ AUDIO PLAYBACK STOPPED");
        Debug.Log($"  Total time played: {currentAudioTime:F2}s");
    }

    void MonitorBlendShapes()
    {
        if (faceMesh == null || !logSALSAActivity) return;

        // Check if any blend shapes are being animated
        bool anyActive = false;
        for (int i = 0; i < faceMesh.sharedMesh.blendShapeCount; i++)
        {
            float weight = faceMesh.GetBlendShapeWeight(i);
            if (weight > 1f) // Threshold to detect activity
            {
                if (!anyActive)
                {
                    Debug.Log("👄 SALSA ACTIVITY DETECTED:");
                    anyActive = true;
                }
                string shapeName = faceMesh.sharedMesh.GetBlendShapeName(i);
                Debug.Log($"  {shapeName} = {weight:F1}");
            }
        }

        if (!anyActive && isAudioPlaying)
        {
            Debug.LogWarning("⚠️ No blend shape activity detected while audio is playing!");
            Debug.LogWarning("   This suggests SALSA is not working properly.");
        }
    }

    void OnGUI()
    {
        if (!visualDebug) return;

        GUILayout.BeginArea(new Rect(10, 100, 400, 300));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== SALSA DEBUGGER ===");
        
        // AudioSource status
        GUI.color = audioSourceFound ? Color.green : Color.red;
        GUILayout.Label($"AudioSource: {(audioSourceFound ? "✓" : "✗")}");
        
        // SALSA status  
        GUI.color = salsaFound ? Color.green : Color.yellow;
        GUILayout.Label($"SALSA: {(salsaFound ? "✓" : "⚠")}");
        
        // Playing status
        GUI.color = isAudioPlaying ? Color.green : Color.white;
        GUILayout.Label($"Playing: {isAudioPlaying}");
        
        if (isAudioPlaying && audioSource.clip != null)
        {
            GUILayout.Label($"Time: {currentAudioTime:F2}s / {clipLength:F2}s");
            GUILayout.Label($"Clip: {audioSource.clip.name}");
            
            // Progress bar
            float progress = clipLength > 0 ? currentAudioTime / clipLength : 0;
            Rect barRect = GUILayoutUtility.GetRect(380, 20);
            GUI.Box(barRect, "");
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height);
            GUI.color = Color.green;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        }

        // Blend shape activity
        if (faceMesh != null)
        {
            GUI.color = Color.white;
            GUILayout.Label($"Blend Shapes: {faceMesh.sharedMesh.blendShapeCount}");
            
            // Show active blend shapes
            int activeCount = 0;
            for (int i = 0; i < faceMesh.sharedMesh.blendShapeCount; i++)
            {
                float weight = faceMesh.GetBlendShapeWeight(i);
                if (weight > 5f)
                {
                    activeCount++;
                }
            }
            
            GUI.color = activeCount > 0 ? Color.green : Color.gray;
            GUILayout.Label($"Active: {activeCount}");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SALSADebugger))]
    public class SALSADebuggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            SALSADebugger debugger = (SALSADebugger)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This debugger monitors AudioSource playback and SALSA lip sync activity. " +
                "Run the scene and check the Console for detailed logs.",
                MessageType.Info
            );
            
            if (debugger.salsaComponent == null)
            {
                EditorGUILayout.HelpBox(
                    "SALSA component not assigned! Please drag your SALSA component here.",
                    MessageType.Warning
                );
            }
        }
    }
#endif
}
