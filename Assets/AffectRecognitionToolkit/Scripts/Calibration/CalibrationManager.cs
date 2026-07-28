using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CalibrationManager : MonoBehaviour
{
    public bool Calibrating = false;

    public void StartCalibrations()
    {
        if(ART_Framework.Instance == null)
        {
            Debug.LogError("There is no ART framework in the scene. Can't calibrate nothing");
            return;
        }

        if (Calibrating)
            return;

        Calibrating = true;

        calibrationData = new ART_CalibrationData();
        DoCalibration();

        //SceneManager.LoadScene("CalibrationScene");
    }

    public static CalibrationManager Instance;

    public ART_CalibrationData calibrationData;

    // Start is called before the first frame update
    private ICalibrator[] calibrators;

    void Awake()
    {
        if (Instance != null)
            Destroy(Instance);
        Instance = this;

        calibrators = FindObjectsByType<ICalibrator>(FindObjectsSortMode.None);
    }

    private void Start()
    {
        //DoCalibration();
    }

    private IEnumerator WaitForStart(ICalibrator calibrator)
    {
        while(calibrator.MustBeAfter.calibrationStatus != CalibrationStatus.Calibrated && calibrator.MustBeAfter.calibrationStatus != CalibrationStatus.Failed)
        {
            yield return new WaitForSeconds(1.0f);
        }

        calibrator.BeginCalibration();

        yield return null;
    }

    void DoCalibration()
    {
        Debug.Log("Starting Calibrations");
        foreach (var calibrator in calibrators)
        {
            if(calibrator.MustBeAfter == null)
            {
                calibrator.BeginCalibration();
            }else
            {
                StartCoroutine(WaitForStart(calibrator));
            }
        }

        StartCoroutine(WaitForAllDone());
    }

    IEnumerator WaitForAllDone()
    {
        foreach (var calibrator in calibrators)
        {
            while (calibrator.calibrationStatus != CalibrationStatus.Calibrated && calibrator.calibrationStatus != CalibrationStatus.Failed)
            {
                yield return new WaitForSeconds(1.0f);
            }
        }

        SceneManager.LoadScene("HandSelection");
    }

    void LoadExistingCalibration(int participantId)
    {

    }

    void LoadExistingCalibration(string path)
    {

    }
}

public enum CalibrationStatus
{
    ReadyToCalibrate,
    NotCalibrating,
    Calibrating,
    Calibrated,
    Failed
}

public class ART_CalibrationData
{
    public int ParticipantID { get; set; }
    public int ParticipantAge { get; set; }
    public float Calibration_HeartRate_RR { get; set; }
    public float Calibration_HeartRate_BPM { get; set; }
    public float Calibration_GSRConductance { get; set; }
    public float Calibration_GSRResistance { get; set; }
    public Dictionary<int, float> Calibration_LeftEye_Dilation { get; set; }
    public Dictionary<int, float> Calibration_RightEye_Dilation { get; set; }
    public float Calibration_BlinkRate { get; set; }
    public float Calibration_HR_MAX { get; set; }
    public float Calibration_HR_RESERVE { get; set; }

    //public Dictionary<LipShape, float> Calibration_LipWeightings { get; set; }

    public KeyValuePair<int, float> FindClosestKeyValueLeft(float targetValue)
    {
        KeyValuePair<int, float> closestKeyValue = new KeyValuePair<int, float>(0, 0f);
        float smallestDifference = Mathf.Infinity;

        foreach (KeyValuePair<int, float> pair in Calibration_LeftEye_Dilation)
        {
            int key = pair.Key;
            float value = pair.Value;
            float difference = Mathf.Abs(targetValue - key);

            if (difference < smallestDifference)
            {
                smallestDifference = difference;
                closestKeyValue = new KeyValuePair<int, float>(key, value);
            }
        }

        return closestKeyValue;
    }

    public KeyValuePair<int, float> FindClosestKeyValueRight(float targetValue)
    {
        KeyValuePair<int, float> closestKeyValue = new KeyValuePair<int, float>(0, 0f);
        float smallestDifference = Mathf.Infinity;

        foreach (KeyValuePair<int, float> pair in Calibration_RightEye_Dilation)
        {
            int key = pair.Key;
            float value = pair.Value;
            float difference = Mathf.Abs(targetValue - key);

            if (difference < smallestDifference)
            {
                smallestDifference = difference;
                closestKeyValue = new KeyValuePair<int, float>(key, value);
            }
        }

        return closestKeyValue;
    }


    public bool SaveCalibration(string path)
    {
        try
        {
            string json = JsonUtility.ToJson(this);
            return true;
        }
        catch { }

        return false;
    }

    public bool LoadCalibration(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);

                var data = JsonUtility.FromJson<ART_CalibrationData>(path);
                this.ParticipantID = data.ParticipantID;
                this.ParticipantAge = data.ParticipantAge;
                this.Calibration_HeartRate_RR = data.Calibration_HeartRate_RR;
                this.Calibration_HeartRate_BPM = data.Calibration_HeartRate_BPM;
                this.Calibration_GSRConductance = data.Calibration_GSRConductance;
                this.Calibration_GSRResistance = data.Calibration_GSRResistance;
                this.Calibration_LeftEye_Dilation = data.Calibration_LeftEye_Dilation;
                this.Calibration_RightEye_Dilation = data.Calibration_RightEye_Dilation;
                this.Calibration_BlinkRate = data.Calibration_BlinkRate;
                this.Calibration_HR_MAX = data.Calibration_HR_MAX;
                this.Calibration_HR_RESERVE = data.Calibration_HR_RESERVE;
                //this.Calibration_LipWeightings = data.Calibration_LipWeightings;
                return true;
            }
            catch { }
        }

        return false;
    }
}