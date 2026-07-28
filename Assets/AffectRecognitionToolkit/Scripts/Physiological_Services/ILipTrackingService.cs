using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ILipTrackingService : BaseDevice
{
    internal LipTrackingData latestLipTrackingData;
    public LipTrackingData GetLatestLipTrackingData() { return latestLipTrackingData; }

    internal override string FileHeader()
    {
        return latestLipTrackingData.getLipWeightingHeaderAsCSVString();
    }

    internal override string GetData()
    {
        return latestLipTrackingData.getLipWeightingsAsCSVString();
    }
}


public struct LipTrackingData
{
    public Dictionary<string, float> currLipWeightings { get; set; }

    public string getLipWeightingHeaderAsCSVString()
    {
        string keys = "";

        foreach (var key in currLipWeightings.Keys)
        {
            keys += key + ",";
        }
        return keys;
    }

    public string getLipWeightingsAsCSVString()
    {
        string weightings = "";

        foreach (var pair in currLipWeightings)
        {
            weightings += pair.Value + ",";
        }
        return weightings;
    }
}