using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ART_DataRecorder : MonoBehaviour
{
    // private BaselineManager _blManager;

    private ART_ExperienceManager _experienceManager;
    private BaseDevice[] _physiologicals;
    private ART_AdditionalData[] _additionals;

    StreamWriter writer = null;
    private Coroutine _writingRoutine;
    public bool _recording = false;
    public StringBuilder _builder = new StringBuilder();


    public static ART_DataRecorder Instance;
    private void Awake()
    {
        if (Instance is not null)
            return;
        Instance = this;
        DontDestroyOnLoad(gameObject);

        TryLoadExperienceManager();


        _additionals = FindObjectsOfType<ART_AdditionalData>();
        _physiologicals = GetComponents<BaseDevice>().Concat(GetComponentsInChildren<BaseDevice>(false)).ToArray();
    }

    IEnumerator WriteCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(6.0f);

            WriteData();
        }
    }

    public void WriteData()
    {
        if (!_recording)
            return;
        writer.Write(_builder.ToString());
        writer.Flush();
        _builder.Clear();
    }

    public void TryLoadExperienceManager()
    {
        try
        {
            _experienceManager = FindObjectsByType<ART_ExperienceManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Single();

            if (_experienceManager == null)
                Debug.LogWarning("No experience manager found. This may not be an issue but this object is expected to return information such as Participant Number and gamestate for the logging.");
        }
        catch
        {
            Debug.LogError("Only 1 experience manager can be active in the scene!");
        }
    }

    public void Open()
    {
        if (_recording)
            return;

        if (_experienceManager == null)
            TryLoadExperienceManager();

        Directory.CreateDirectory(Application.dataPath + "/Participant/");

        writer = new StreamWriter(Application.dataPath + $"/Participant/P_{(_experienceManager != null ? _experienceManager.ParticipantID : "UKN")}_RAW_{DateTime.UtcNow:yy-MM-dd-hh-mm-ss}.csv");
        _builder = new StringBuilder();

        string header = _experienceManager.FileHeader();
        if (header.StartsWith(",")) header = header.Substring(1);
        if (header.EndsWith(",")) _builder.Append(header);
        else _builder.Append($"{header},");

        for (int i = 0; i< _additionals.Length; i++)
        {
            header = _additionals[i].FileHeader();
            if (header.StartsWith(",")) header = header.Substring(1);
            if (header.EndsWith(",")) _builder.Append(header);
            else _builder.Append($"{header},");
        }

        for (int i = 0; i < _physiologicals.Length; i++)
        {
            header = _physiologicals[i].FileHeader();
            if (header.StartsWith(",")) header = header.Substring(1);
            if (header.EndsWith(",")) _builder.Append(header);
            else _builder.Append($"{header},");
        }

        _recording = true;
        WriteData();

        _writingRoutine = StartCoroutine(WriteCoroutine());

    }

    public void Close()
    {
        if (!_recording)
            return;

        if (!(_writingRoutine is null))
            StopCoroutine(_writingRoutine);

        WriteData();

        writer.Close();

        _recording = false;
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // LateUpdate is called once per frame after all update functions are run
    void LateUpdate()
    {
        if (!_recording)
            return;

        _builder.AppendLine();

        string data = _experienceManager.GetData();
        if (data.StartsWith(",")) data = data.Substring(1);
        if (data.EndsWith(",")) _builder.Append(data);
        else _builder.Append($"{data},");

        for (int i = 0; i < _additionals.Length; i++)
        {
            data = _additionals[i].GetData();
            if (data.StartsWith(",")) data = data.Substring(1);
            if (data.EndsWith(",")) _builder.Append(data);
            else _builder.Append($"{data},");
        }

        for (int i = 0; i < _physiologicals.Length; i++)
        {
            data = _physiologicals[i].GetData();
            if(data.StartsWith(",")) data = data.Substring(1);
            if(data.EndsWith(",")) _builder.Append(data);
            else _builder.Append($"{data},");
        }
    }


    private void OnApplicationQuit()
    {
        Close();
    }
}
