using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PupilDialationCalibration : ICalibrator
{
    [SerializeField]
    private Renderer _sphereMaterial;
    [SerializeField]
    private GameObject _sphere;

    public bool Calibrated = false;

    private float _blackScreenTime = 5.0f, _baselineTime = 2.0f, _calibrationScreenTime = 5.0f, timeLeft = 1.0f;
    //private float _blackScreenTime = 1.0f, _baselineTime = 2.0f, _calibrationScreenTime = 1.0f, timeLeft = 5.0f;


    int grayIndex;
    private Color32[] grayColors = new Color32[17]
    {
        new Color32(0, 0, 0, 255),       // Black
        new Color32(16, 16, 16, 255),   //0,16,32,48,64,80,96,112,128,144,160,176,192,208,224,240,255
        new Color32(32, 32, 32, 255),
        new Color32(48, 48, 48, 255),
        new Color32(64, 64, 64, 255),
        new Color32(80, 80, 80, 255),
        new Color32(96, 96, 96, 255),
        new Color32(112, 112, 112, 255),
        new Color32(128, 128, 128, 255),
        new Color32(144, 144, 144, 255),
        new Color32(160, 160, 160, 255),
        new Color32(176, 176, 176, 255),
        new Color32(192, 192, 192, 255),
        new Color32(208, 208, 208, 255),
        new Color32(224, 224, 224, 255),
        new Color32(240, 240, 240, 255),
        new Color32(255, 255, 255, 255)  // White
    };
    Color32 shownColour;

    enum Stage
    {
        Baseline,
        Light,
        Blacked
    }

    private Stage _stage;
    private IEyeTrackingService _eyeTracker;
    private List<EyeTrackingData> _calibrationData;

    internal override void BeginCalibration()
    {
        _sphere.SetActive(true);
        _eyeTracker = ART_Framework.Instance.eyeTrackerExport;
        _calibrationData = new List<EyeTrackingData>();
        calibrationStatus = CalibrationStatus.Calibrating;
        _sphereMaterial.material.color = grayColors[0];
    }

    /** The *Update* Method but for "currently calibrating" **/
    internal override void CalibrationUpdate()
    {

        timeLeft -= Time.deltaTime;

        if (timeLeft < 0f)
        {
            switch (_stage)
            {
                case Stage.Baseline:
                    _stage = Stage.Light;
                    timeLeft = _calibrationScreenTime;
                    break;
                case Stage.Light:
                    GetCalibrationValue();
                    _calibrationData.Clear();
                    _stage = Stage.Blacked;
                    timeLeft = _blackScreenTime;

                    if (grayIndex == grayColors.Length)
                    {
                        _sphere.SetActive(false);
                        calibrationStatus = CalibrationStatus.Calibrated;
                        return;
                    }

                    _sphereMaterial.material.color = grayColors[0];
                    break;
                case Stage.Blacked:

                    _sphereMaterial.material.color = grayColors[grayIndex];
                    shownColour = grayColors[grayIndex++];

                    _stage = Stage.Baseline;
                    timeLeft = _baselineTime;
                    break;
                default:
                    break;
            }
        }

        if (_stage == Stage.Light)
        {
            _calibrationData.Add(_eyeTracker.GetLatestEyeTrackingData());
        }

    }

    private void GetCalibrationValue()
    {
        try
        {
            CalibrationManager.Instance.calibrationData.Calibration_LeftEye_Dilation.Add(grayIndex, _calibrationData.Where(x => x.gazeValid).Select(x => x.pupilDilationLeft).Average());
            CalibrationManager.Instance.calibrationData.Calibration_RightEye_Dilation.Add(grayIndex, _calibrationData.Where(x => x.gazeValid).Select(x => x.pupilDilationRight).Average());
        }
        catch(Exception e) { Debug.LogException(e); }
    }


}
