using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IBikeService : BaseDevice
{
    internal BikeData latestBikeData;
    public BikeData GetLatestBikeData() { return latestBikeData; }

    internal override string FileHeader()
    {
        return "Bike_Speed_kmph,Bike_RPM,Bike_Power,";
    }

    internal override string GetData()
    {
        return $"{latestBikeData.speed_kmph},{latestBikeData.rpm},{latestBikeData.power},";
    }
}

public struct BikeData
{
    public float speed_kmph { get; set; }
    public float rpm { get; set; }
    public float power { get; set; }
}