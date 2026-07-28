using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IEyeTrackingService : BaseDevice
{
    internal EyeTrackingData latestEyeTrackingData;

    public EyeTrackingData GetLatestEyeTrackingData() { return latestEyeTrackingData; }


    internal override string FileHeader()
    {
        return "EyeGaze_Local_Pos.x,EyeGaze_Local_Pos.y,EyeGaze_Local_Pos.z,EyeGaze_Local_Direction.x,EyeGaze_Local_Direction.y,EyeGaze_Local_Direction.z," +
            "LeftEyeGaze_Local_Pos.x,LeftEyeGaze_Local_Pos.y,LeftEyeGaze_Local_Pos.z,LeftEyeGaze_Local_Direction.x,LeftEyeGaze_Local_Direction.y,LeftEyeGaze_Local_Direction.z," +
            "RightEyeGaze_Local_Pos.x,RightEyeGaze_Local_Pos.y,RightEyeGaze_Local_Pos.z,RightEyeGaze_Local_Direction.x,RightEyeGaze_Local_Direction.y,RightEyeGaze_Local_Direction.z," + 
            "EyeGaze_World_Pos.x,EyeGaze_World_Pos.y,EyeGaze_World_Pos.z,EyeGaze_World_Direction.x,EyeGaze_World_Direction.y,EyeGaze_World_Direction.z," +
            "LeftEyeGaze_World_Pos.x,LeftEyeGaze_World_Pos.y,LeftEyeGaze_World_Pos.z,LeftEyeGaze_World_Direction.x,LeftEyeGaze_World_Direction.y,LeftEyeGaze_World_Direction.z," +
            "RightEyeGaze_World_Pos.x,RightEyeGaze_World_Pos.y,RightEyeGaze_World_Pos.z,RightEyeGaze_World_Direction.x,RightEyeGaze_World_Direction.y,RightEyeGaze_World_Direction.z," +
            "PupilDilation_Left,PupilDilation_Right,EyeClosed_Left,EyeClosed_Right,CurrentBlink_Duration,CurrentBlink_Interval,GazeValid,";
    }

    internal override string GetData()
    {
        return $"{latestEyeTrackingData.EyeGazePosLocal.x},{latestEyeTrackingData.EyeGazePosLocal.y},{latestEyeTrackingData.EyeGazePosLocal.z},{latestEyeTrackingData.EyeGazeDirLocal.x},{latestEyeTrackingData.EyeGazeDirLocal.y},{latestEyeTrackingData.EyeGazeDirLocal.z}," +
            $"{latestEyeTrackingData.LeftEyeGazePosLocal.x},{latestEyeTrackingData.LeftEyeGazePosLocal.y},{latestEyeTrackingData.LeftEyeGazePosLocal.z},{latestEyeTrackingData.LeftEyeGazeDirLocal.x},{latestEyeTrackingData.LeftEyeGazeDirLocal.y},{latestEyeTrackingData.LeftEyeGazeDirLocal.z}," +
            $"{latestEyeTrackingData.RightEyeGazePosLocal.x},{latestEyeTrackingData.RightEyeGazePosLocal.y},{latestEyeTrackingData.RightEyeGazePosLocal.z},{latestEyeTrackingData.RightEyeGazeDirLocal.x},{latestEyeTrackingData.RightEyeGazeDirLocal.y},{latestEyeTrackingData.RightEyeGazeDirLocal.z}," +
            $"{latestEyeTrackingData.EyeGazePosWorld.x},{latestEyeTrackingData.EyeGazePosWorld.y},{latestEyeTrackingData.EyeGazePosWorld.z},{latestEyeTrackingData.EyeGazeDirWorld.x},{latestEyeTrackingData.EyeGazeDirWorld.y},{latestEyeTrackingData.EyeGazeDirWorld.z}," +
            $"{latestEyeTrackingData.LeftEyeGazePosWorld.x},{latestEyeTrackingData.LeftEyeGazePosWorld.y},{latestEyeTrackingData.LeftEyeGazePosWorld.z},{latestEyeTrackingData.LeftEyeGazeDirWorld.x},{latestEyeTrackingData.LeftEyeGazeDirWorld.y},{latestEyeTrackingData.LeftEyeGazeDirWorld.z}," +
            $"{latestEyeTrackingData.RightEyeGazePosWorld.x},{latestEyeTrackingData.RightEyeGazePosWorld.y},{latestEyeTrackingData.RightEyeGazePosWorld.z},{latestEyeTrackingData.RightEyeGazeDirWorld.x},{latestEyeTrackingData.RightEyeGazeDirWorld.y},{latestEyeTrackingData.RightEyeGazeDirWorld.z}," +
            $"{latestEyeTrackingData.pupilDilationLeft},{latestEyeTrackingData.pupilDilationRight},{latestEyeTrackingData.eyeClosedLeft},{latestEyeTrackingData.eyeClosedRight},{latestEyeTrackingData.current_blinkDuration},{latestEyeTrackingData.current_interBlinkInterval},{latestEyeTrackingData.gazeValid},";
    }

    /// <summary>
    /// This should be used to call any external & device specific eye-tracking calibrations. (Such as follow the dots)
    /// </summary>
    internal virtual void EyeTrackerCalibration()
    {

    }
}


public struct EyeTrackingData
{
    public Vector3 LeftEyeGazePosWorld { get; set; }
    public Vector3 LeftEyeGazeDirWorld { get; set; }

    public Vector3 LeftEyeGazePosLocal { get; set; }
    public Vector3 LeftEyeGazeDirLocal { get; set; }


    public Vector3 RightEyeGazePosWorld { get; set; }
    public Vector3 RightEyeGazeDirWorld { get; set; }

    public Vector3 RightEyeGazePosLocal { get; set; }
    public Vector3 RightEyeGazeDirLocal { get; set; }


    public Vector3 EyeGazePosWorld { get; set; }
    public Vector3 EyeGazeDirWorld { get; set; }

    public Vector3 EyeGazePosLocal { get; set; }
    public Vector3 EyeGazeDirLocal { get; set; }


    public float pupilDilationLeft { get; set; }
    public float pupilDilationRight { get; set; }
    public bool eyeClosedLeft { get; set; }
    public bool eyeClosedRight { get; set; }
    public float current_blinkDuration { get; set; }
    public float current_interBlinkInterval { get; set; }

    public bool isBlinking { get; set; }

    public bool gazeValid { get; set; }
}