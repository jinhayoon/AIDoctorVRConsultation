using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IGSRService : BaseDevice
{
    internal GSRData latestGSRData;
    public GSRData GetLatestGSRData() { return latestGSRData; }

    internal override string FileHeader()
    {
        return "GSR_SkinConductance,GSR_SkinResistance,";
    }

    internal override string GetData()
    {
        return $"{latestGSRData.gsrConductance},{latestGSRData.gsrResistance},";
    }
}

public struct GSRData
{
    public double gsrConductance { get; set; } //uS
    public double gsrResistance { get; set; } //kOhms
}