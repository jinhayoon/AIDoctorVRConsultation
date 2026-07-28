using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IHeartService : BaseDevice
{
    internal HeartData latestHeartData;
    public HeartData GetLatestHeartData() { return latestHeartData; }
    internal override string FileHeader()
    {
        return "HeartRate_BPM,HeartRate_RR_Interval,";
    }

    internal override string GetData()
    {
        return $"{latestHeartData.heartRateBPM},{latestHeartData.heartRate_RR_Interval},";
    }
}

public struct HeartData
{
    public float heartRateBPM { get; set; } // Heart Rate BPM
    public float heartRate_RR_Interval { get; set; } // Heart RR Interval
}