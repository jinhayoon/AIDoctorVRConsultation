using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Debug = UnityEngine.Debug;

public class RecordScene : MonoBehaviour
{
    private Stopwatch stopwatch;
    private Dictionary<string, int> buttonClickCounts = new Dictionary<string, int>();
    private string filePath;
    private bool summaryWritten = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        stopwatch = new Stopwatch();
        stopwatch.Start();

        string pid = LogPPTID.PID ?? "Unknown";
        string logDir = Path.Combine(Application.persistentDataPath, "AIDocLogs");
        Directory.CreateDirectory(logDir);
        filePath = Path.Combine(logDir, $"ButtonClickLog_{pid}.csv");

        Debug.Log($"The persistent data path on {Application.platform} is located at: {Application.persistentDataPath}");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void ButtonClicked(Button button)
    {
        string buttonName = button.name;
        if (buttonClickCounts.ContainsKey(buttonName))
            buttonClickCounts[buttonName]++;
        else
            buttonClickCounts[buttonName] = 1;

        string pid = LogPPTID.PID ?? "";
        string logEntry = $"{pid},ButtonClick,{buttonName},{Time.time}\n";

        if (!File.Exists(filePath))
            File.AppendAllText(filePath, "PID,EventType,Value,TimeSeconds\n");

        File.AppendAllText(filePath, logEntry);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string pid = LogPPTID.PID ?? "";
        string transitionLog = $"{pid},SceneChange,{scene.name},{Time.time}\n";
        File.AppendAllText(filePath, transitionLog);

        if (scene.name == "SceneEnd" && stopwatch.IsRunning)
        {
            stopwatch.Stop();
            string durationLog = $"{pid},SessionDuration,,{stopwatch.Elapsed.TotalSeconds}\n";
            File.AppendAllText(filePath, durationLog);

            WriteButtonClickSummary(pid);
        }
        else if (scene.name == "SessionStart")
        {
            stopwatch.Reset();
            stopwatch.Start();
            File.AppendAllText(filePath, $"{pid},NewSession,,\n");
        }
    }

    void OnApplicationQuit()
    {
        stopwatch.Stop();
        string pid = LogPPTID.PID ?? "";
        string durationLog = $"{pid},SessionDuration,,{stopwatch.Elapsed.TotalSeconds}\n";
        File.AppendAllText(filePath, durationLog);

        WriteButtonClickSummary(pid);
    }

    private void WriteButtonClickSummary(string pid)
    {
        if (summaryWritten || buttonClickCounts.Count == 0)
            return;

        // Write a header for the summary if not already present
        File.AppendAllText(filePath, "PID,ButtonClickSummary,ButtonName,ClickCount\n");
        foreach (var kvp in buttonClickCounts)
        {
            string summaryEntry = $"{pid},ButtonClickSummary,{kvp.Key},{kvp.Value}\n";
            File.AppendAllText(filePath, summaryEntry);
        }
        summaryWritten = true;
    }
}
