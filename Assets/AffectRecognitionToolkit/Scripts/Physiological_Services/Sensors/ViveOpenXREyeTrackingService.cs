using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;
using System;
using System.Runtime.InteropServices;

public class ViveOpenXREyeTrackingService : IEyeTrackingService
{
    public override string DeviceName()
    {
        return "Vive Elite Eye-Tracking";
    }

    private ViveEyeTracker _eyeTracker;

    // Start is called before the first frame update
    void Start()
    {

    }

    private XrSingleEyeGazeDataHTC[] _eyeGazeData;
    private XrSingleEyeGazeDataHTC _leftEyeGaze, _rightEyeGaze;
    private XrSingleEyePupilDataHTC[] _eyePupilData;
    private XrSingleEyePupilDataHTC _leftPupil, _rightPupil;
    private XrSingleEyeGeometricDataHTC[] _eyeShapeData;
    private XrSingleEyeGeometricDataHTC _leftShape, _rightShape;
    private static Vector3 invalidEyeVector = new Vector3(-1,-1,-1);

    private float current_IBI, current_blinkDuration;

    private float deltaTime;
    // Update is called once per frame
    void Update()
    {
        deltaTime = Time.deltaTime;
        if (XR_HTC_eye_tracker.Interop.GetEyeGazeData(out _eyeGazeData))
        {
            _leftEyeGaze = _eyeGazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            _rightEyeGaze = _eyeGazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            if (_leftEyeGaze.isValid)
            {
                latestEyeTrackingData.LeftEyeGazePosLocal = _leftEyeGaze.gazePose.position.ToUnityVector();
                latestEyeTrackingData.LeftEyeGazeDirLocal = _leftEyeGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;
            }
            else
            {
                latestEyeTrackingData.LeftEyeGazeDirLocal = invalidEyeVector; latestEyeTrackingData.LeftEyeGazePosLocal = invalidEyeVector;
                latestEyeTrackingData.LeftEyeGazeDirWorld = invalidEyeVector; latestEyeTrackingData.LeftEyeGazePosWorld = invalidEyeVector;
            }

            if (_rightEyeGaze.isValid)
            {
                latestEyeTrackingData.RightEyeGazePosLocal = _leftEyeGaze.gazePose.position.ToUnityVector();
                latestEyeTrackingData.RightEyeGazeDirLocal = _leftEyeGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;
            }
            else
            {
                latestEyeTrackingData.RightEyeGazeDirLocal = invalidEyeVector; latestEyeTrackingData.RightEyeGazePosLocal = invalidEyeVector;
                latestEyeTrackingData.RightEyeGazeDirWorld = invalidEyeVector; latestEyeTrackingData.RightEyeGazePosWorld = invalidEyeVector;
            }
        }else
        {
            latestEyeTrackingData.LeftEyeGazeDirLocal = invalidEyeVector; latestEyeTrackingData.LeftEyeGazePosLocal = invalidEyeVector;
            latestEyeTrackingData.LeftEyeGazeDirWorld = invalidEyeVector; latestEyeTrackingData.LeftEyeGazePosWorld = invalidEyeVector;
            latestEyeTrackingData.RightEyeGazeDirLocal = invalidEyeVector; latestEyeTrackingData.RightEyeGazePosLocal = invalidEyeVector;
            latestEyeTrackingData.RightEyeGazeDirWorld = invalidEyeVector; latestEyeTrackingData.RightEyeGazePosWorld = invalidEyeVector;
        }

        if(XR_HTC_eye_tracker.Interop.GetEyePupilData(out _eyePupilData))
        {
            _leftPupil = _eyePupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            _rightPupil = _eyePupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            
            if(_leftPupil.isDiameterValid)
                latestEyeTrackingData.pupilDilationLeft = _leftPupil.pupilDiameter;
            else
                latestEyeTrackingData.pupilDilationLeft = -1f;

            if (_rightPupil.isDiameterValid)
                latestEyeTrackingData.pupilDilationRight = _rightPupil.pupilDiameter;
            else
                latestEyeTrackingData.pupilDilationRight = -1f;
        }
        else
        {
            latestEyeTrackingData.pupilDilationLeft = -1f;
            latestEyeTrackingData.pupilDilationRight= -1f;
        }

        if (XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out _eyeShapeData))
        {
            _leftShape = _eyeShapeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            _rightShape = _eyeShapeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            latestEyeTrackingData.eyeClosedLeft = _leftShape.eyeOpenness <= 0.25;
            latestEyeTrackingData.eyeClosedRight = _rightShape.eyeOpenness <= 0.25;
        }

        if (latestEyeTrackingData.eyeClosedLeft && latestEyeTrackingData.eyeClosedRight) 
        {
            if (latestEyeTrackingData.isBlinking)
            {
                latestEyeTrackingData.current_blinkDuration += deltaTime;
            }
            else
            {
                latestEyeTrackingData.isBlinking = true;
                current_blinkDuration = deltaTime / 2;
                current_IBI += deltaTime / 2;
                latestEyeTrackingData.current_interBlinkInterval = current_IBI;
            }
        }
        else
        {
            if (latestEyeTrackingData.isBlinking)
            {
                latestEyeTrackingData.isBlinking = false;
                if (current_blinkDuration <= 0.7f)
                {
                    current_blinkDuration += deltaTime / 2;
                    latestEyeTrackingData.current_blinkDuration = current_blinkDuration;
                    current_IBI = deltaTime / 2;
                }
                else
                {
                    current_IBI = deltaTime + current_blinkDuration;
                }
            }
            else
                current_IBI += deltaTime;
        }
    }
}
