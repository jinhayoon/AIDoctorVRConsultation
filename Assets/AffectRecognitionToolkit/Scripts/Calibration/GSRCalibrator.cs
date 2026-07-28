using System.Collections.Generic;
using UnityEngine;

public class GSRCalibrator : ICalibrator
{
    public float TimeLeft = 120.0f;

    private List<float> GSR_Conductances = new List<float>();
    private List<float> GSR_Resistances = new List<float>();

    private IGSRService _gsrService;

    internal override void BeginCalibration()
    {
        GSR_Conductances.Clear();
        GSR_Resistances.Clear();

        _gsrService = ART_Framework.Instance.skinConductanceService;

        calibrationStatus = CalibrationStatus.Calibrating;
    }

    internal override void CalibrationUpdate()
    {
        TimeLeft -= Time.deltaTime;

        if (TimeLeft < 0)
        {
            CollateCalibrationData();
            calibrationStatus = CalibrationStatus.Calibrated;

            return;
        }

        GSR_Conductances.Add((float)_gsrService.latestGSRData.gsrConductance);
        GSR_Conductances.Add((float)_gsrService.latestGSRData.gsrResistance);
    }

    private void CollateCalibrationData()
    {
        CalibrationManager.Instance.calibrationData.Calibration_GSRConductance = Statistics.ComputeMean(GSR_Conductances);
        CalibrationManager.Instance.calibrationData.Calibration_GSRResistance = Statistics.ComputeMean(GSR_Resistances);
    }
}
