using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class Tester : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] out_gazes);
        XrSingleEyeGazeDataHTC leftGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        if (leftGaze.isValid)
        {
            var x = leftGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;

            transform.position = leftGaze.gazePose.position.ToUnityVector();
            transform.rotation = leftGaze.gazePose.orientation.ToUnityQuaternion();
        }
    }
}
