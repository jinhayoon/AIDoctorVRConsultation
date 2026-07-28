using UnityEngine;

public class HMDPositionService : BaseDevice
{

    private string[] c_names = { "pX", "pY", "pZ", "rX", "rY", "rZ", "arX", "arY", "arZ", "aqX", "aqY", "aqZ", "aqW" };
    private string[] c_units = { "meters", "meters", "meters", "degrees", "degrees", "degrees", "degrees", "degrees", "degrees", "q", "q", "q", "q" };
    private float[] sample = new float[13];

    private double _createdAt;

    private Quaternion _previousQuart;


    // Update is called once per frame
    void Update()
    {
        if (!recording) return;


        doSample();
    }

    void doSample()
    {
        var pVec = transform.localPosition;
        var qVec = transform.localRotation;

        var euVecAbsolute = qVec.eulerAngles;

        var euVecRelative = (qVec * _previousQuart).eulerAngles;
        _previousQuart = Quaternion.Inverse(qVec);

        sample[0] = pVec.x; sample[1] = pVec.y; sample[2] = pVec.z;
        sample[3] = euVecRelative.x; sample[4] = euVecRelative.y; sample[5] = euVecRelative.z;
        sample[6] = euVecAbsolute.x; sample[7] = euVecAbsolute.y; sample[8] = euVecAbsolute.z;
        sample[9] = qVec.x; sample[10] = qVec.y; sample[11] = qVec.z; sample[12] = qVec.w;
    }

    internal override string FileHeader()
    {
        string header = "";
        foreach (string name in c_names)
        {
            header += $"HMD_{name},";
        }

        return header;
    }

    internal override string GetData()
    {
        string data = "";
        foreach (float s in sample)
        {
            data += $"{s},";
        }

        return data;
    }

    public override string DeviceName()
    {
        return "HMD";
    }
}
