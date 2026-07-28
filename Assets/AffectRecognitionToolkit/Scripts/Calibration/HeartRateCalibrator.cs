using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeartRateCalibrator : ICalibrator
{
    public float TimeLeft = 120.0f;

    private List<float> BPM = new List<float>();
    private List<float> RR_Intervals = new List<float>();

    private IHeartService _heartService;

    internal override void BeginCalibration()
    {
        BPM.Clear();
        RR_Intervals.Clear();

        _heartService = ART_Framework.Instance.heartRateService;

        calibrationStatus = _heartService == null ? calibrationStatus : CalibrationStatus.Calibrating;
    }

    internal override void CalibrationUpdate()
    {
        TimeLeft -= Time.deltaTime;

        if(TimeLeft < 0)
        {
            CollateCalibrationData();
            calibrationStatus = CalibrationStatus.Calibrated;

            return;
        }

        BPM.Add(_heartService.latestHeartData.heartRateBPM);
        RR_Intervals.Add(_heartService.latestHeartData.heartRate_RR_Interval);
    }

    private void CollateCalibrationData()
    {
        CalibrationManager.Instance.calibrationData.Calibration_HeartRate_BPM = Statistics.ComputeMean(BPM);
        CalibrationManager.Instance.calibrationData.Calibration_HeartRate_RR = Statistics.ComputeMean(RR_Intervals);
        CalibrationManager.Instance.calibrationData.Calibration_HR_MAX = 207 - (0.7f * CalibrationManager.Instance.calibrationData.ParticipantAge);
        CalibrationManager.Instance.calibrationData.Calibration_HR_RESERVE = CalibrationManager.Instance.calibrationData.Calibration_HR_MAX - CalibrationManager.Instance.calibrationData.Calibration_HeartRate_BPM;
    }
}
