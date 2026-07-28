using System;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ART_ExperienceManager : MonoBehaviour
{
    public int ParticipantID;


    private string SceneName = "";
    private int UnityFrame = 0;
    private DateTime FrameTimeStamp = DateTime.MinValue;

    internal virtual void Update()
    {
        UnityFrame = Time.frameCount;
        FrameTimeStamp = DateTime.UtcNow;
    }

    internal virtual string FileHeader()
    {
        return "ParticipantID,UnityFrame,FrameTimeStamp,CurrentScene,";
    }
    internal virtual string GetData()
    {
        return $"{ParticipantID},{UnityFrame},{FrameTimeStamp:u},{SceneName},";
    }

    internal void OnSceneChange(Scene newScene, LoadSceneMode loadMode)
    {
#if ART_DEV
        Debug.Log($"Loading Scene: {scene.name}, type: {mode}");
#endif
        if (loadMode == LoadSceneMode.Single)
            SceneName = newScene.name;
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneChange;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneChange;
    }
}
