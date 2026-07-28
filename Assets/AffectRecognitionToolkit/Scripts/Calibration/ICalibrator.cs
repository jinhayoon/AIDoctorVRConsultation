using UnityEngine;

public abstract class ICalibrator : MonoBehaviour
{
    public ICalibrator MustBeAfter = null;

    public CalibrationStatus calibrationStatus = CalibrationStatus.NotCalibrating;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (calibrationStatus == CalibrationStatus.Calibrating)
            CalibrationUpdate();
    }

    internal abstract void BeginCalibration();
    internal abstract void CalibrationUpdate();

    internal virtual string GetCalibrationHeader()
    {
        return "";
    }

    internal virtual string GetCalibrationStep()
    {
        return "";
    }
}
