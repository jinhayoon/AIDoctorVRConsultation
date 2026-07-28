using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BlinkCalibration : ICalibrator
{
    public bool Calibrated = false;
    public float TimeLeft = 120.0f;

    public GameObject CubePrefab;

    private IEyeTrackingService _eyeTracker;

    List<float> blinkDurations = new List<float>();
    List<float> interBlinkIntervals = new List<float>();

    private bool blinking = false;
    private float calibrationTime_Total;

    internal override void BeginCalibration()
    {
        _eyeTracker = ART_Framework.Instance.eyeTrackerExport;

        blinkDurations.Clear();
        interBlinkIntervals.Clear();

        blinking = _eyeTracker.GetLatestEyeTrackingData().isBlinking;


        var miniCube = Instantiate(CubePrefab, Camera.main.transform.position, Quaternion.identity);
        miniCube.transform.Translate(Vector3.forward * 5, Space.Self);

        calibrationStatus = CalibrationStatus.Calibrating;
    }

    internal override void CalibrationUpdate()
    {
        TimeLeft -= Time.deltaTime;
        calibrationTime_Total += Time.deltaTime;
        if (TimeLeft <= 0.0)
        {
            Calibrated = true;

            CollateCalibrationData();

            calibrationStatus = CalibrationStatus.Calibrated;
            return;
        }

        var data = _eyeTracker.GetLatestEyeTrackingData();
        if(blinking != data.isBlinking)
        {
            if (blinking)
                blinkDurations.Add(data.current_blinkDuration);
            else
                interBlinkIntervals.Add(data.current_interBlinkInterval);
            blinking = data.isBlinking;
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        MustBeAfter = FindAnyObjectByType<PupilDialationCalibration>();
    }


    private void CollateCalibrationData()
    {
        float meanBlinkDuration = Statistics.ComputeMean(blinkDurations);
        float medianBlinkDuration = Statistics.ComputeMedian(blinkDurations);
        float stdDevBlinkDuration = Statistics.ComputeStandardDeviation(blinkDurations);

        float meanInterBlinkInterval = Statistics.ComputeMean(interBlinkIntervals);
        float medianInterBlinkInterval = Statistics.ComputeMedian(interBlinkIntervals);
        float stdDevInterBlinkInterval = Statistics.ComputeStandardDeviation(interBlinkIntervals);

        CalibrationManager.Instance.calibrationData.Calibration_BlinkRate = (float)(blinkDurations.Count) / calibrationTime_Total;
    }
}
