using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ART_Framework : BaseDevice
{
    public static ART_Framework Instance;

    internal IBikeService bikeControlService;
    internal IGSRService skinConductanceService;
    internal IHeartService heartRateService;
    internal IEyeTrackingService eyeTrackerExport;
    internal ILipTrackingService lipTrackingExport;

    internal GazePixelAnalyser gazePixelAnalyser;

    // need this for data cleaning
    private ART_CalibrationData cd;

    public float ValenceScore;
    public float ValenceCI_Upper;
    public float ValenceCI_Lower;

    public float ArousalScore;
    public float ArousalCI_Upper;
    public float ArousalCI_Lower;

    public float FearScore;
    public float FearCI_Upper;
    public float FearCI_Lower;

    public float StressScore;
    public float StressCI_Upper;
    public float StressCI_Lower;

    public float HappyScore;
    public float HappyCI_Upper;
    public float HappyCI_Lower;

    public float SadScore;
    public float SadCI_Upper;
    public float SadCI_Lower;

    public float BoredScore;
    public float BoredCI_Upper;
    public float BoredCI_Lower;

    public float ExcitedScore;
    public float ExcitedCI_Upper;
    public float ExcitedCI_Lower;

    public float ContentScore;
    public float ContentCI_Upper;
    public float ContentCI_Lower;

    public float CalmScore;
    public float CalmCI_Upper;
    public float CalmCI_Lower;

    private HeartData hr_data;
    private GSRData sc_data;
    private EyeTrackingData eye_data;
    private LipTrackingData lip_data;
    private SmileData smile_data;
    private BikeData bike_data;
    private GazePixelData gaze_PixelData;

    public float pupil_dilation_level;
    public float pupil_dilation_response;
    public float skin_conductance_level;
    public float heart_rate;
    public float heart_rate_variability;
    public float smile;
    public float power;

    public float corrected_pupil_dilation_left;
    public float corrected_pupil_dilation_right;

    private RollingWindow<HeartData> hr_window;
    private RollingWindow<GSRData> sc_window;
    private RollingWindow<EyeTrackingData> eye_window;
    private RollingWindow<SmileData> smile_window;
    private RollingWindow<BikeData> bike_window;
    private RollingWindow<EyeTrackingData> eye_pixel_window;

    private bool waitingForDevices = true;
    private bool enoughDevices = true;

    private string Left_Smile = "", Right_Smile = "";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        smile_data = new SmileData();
        hr_window = new RollingWindow<HeartData>();
        sc_window = new RollingWindow<GSRData>();
        eye_window = new RollingWindow<EyeTrackingData>();
        smile_window = new RollingWindow<SmileData>();
        bike_window = new RollingWindow<BikeData>();
        eye_pixel_window = new RollingWindow<EyeTrackingData>();

        corrected_pupil_dilation_left = 0.0f;
        corrected_pupil_dilation_right = 0.0f;

        //Find Eye and Lip Tracking service
        AssignDeviceServices(ref lipTrackingExport);
        if (lipTrackingExport != null)
            Debug.Log("[EmoSense]: Lip Tracker Service Found");
        else
            Debug.Log("[EmoSense]: No Valid Lip Tracker Service Found");

        AssignDeviceServices(ref eyeTrackerExport);
        if (eyeTrackerExport != null)
            Debug.Log("[EmoSense]: Eye Tracker Service Found");
        else
            Debug.Log("[EmoSense]: No Valid Eye Tracker Service Found");

        AssignDeviceServices(ref gazePixelAnalyser);
        if (gazePixelAnalyser != null)
            Debug.Log("[EmoSense]: Gaze Analysis Service Found");
        else
            Debug.Log("[EmoSense]: No Valid Gaze Analysis Service Found");

        //Find HR Service
        AssignDeviceServices(ref heartRateService);
        if (heartRateService != null)
            Debug.Log("[EmoSense]: Heart Rate Service Found");
        else
            Debug.Log("[EmoSense]: No Valid Heart Rate Service Found");

        //Find SC service
        AssignDeviceServices(ref skinConductanceService);
        if (skinConductanceService != null)
            Debug.Log("[EmoSense]: Shimmer Service Found");
        else
            Debug.Log("[EmoSense]: No Valid Shimmer Service Found");

        //Find Bike Service
        AssignDeviceServices(ref bikeControlService);
        if (bikeControlService != null)
            Debug.Log("[EmoSense]: Bike Service Found");
        else
            Debug.Log("[EmoSense]: No Valid Bike Service Found");

        //CalibrationDataLoader.OnCalibrationDataReady += CalibrationDataLoader_OnCalibrationDataReady;
        StartCoroutine(SuspendUpdateCoroutine());

        if (Instance == null)
            Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingForDevices)
            return;

        if (bikeControlService != null)
        {
            if (bikeControlService.IsStreaming)
            {
                bike_data = bikeControlService.GetLatestBikeData();
                bike_window.AddData(bike_data, Time.time);
                //Power Avg
                power = bike_window.RollingAverage("power");
            }
        }

        if (skinConductanceService != null)
        {
            if (skinConductanceService.IsStreaming)
            {
                sc_data = skinConductanceService.GetLatestGSRData();
                sc_window.AddData(sc_data, Time.time);
                //SCL Avg
                skin_conductance_level = sc_window.RollingAverage("gsrConductance");
            }
        }

        if (heartRateService != null)
        {
            if (heartRateService.IsStreaming)
            {
                hr_data = heartRateService.GetLatestHeartData();
                hr_window.AddData(hr_data, Time.time);

                //BPM Avg
                heart_rate = hr_window.RollingAverage("heartRateBPM");

                List<float> RR_intervals = hr_window.GetMetricValues("heartRate_RR_Interval");

                if (RR_intervals.Count > 1)
                    // RMSSD
                    heart_rate_variability = CalculateRMSSD(RR_intervals);

            }

        }

        if (eyeTrackerExport != null)
        {
            if (eyeTrackerExport.isActiveAndEnabled)
            {
                eye_data = eyeTrackerExport.GetLatestEyeTrackingData();

                eye_window.AddData(eye_data, Time.time);
                //PDL
                pupil_dilation_level = (eye_window.RollingAverage("pupilDilationLeft") + eye_window.RollingAverage("pupilDilationRight")) / 2;
                //PDR
                pupil_dilation_response = (eye_window.RollingStdDev("pupilDilationLeft") + eye_window.RollingStdDev("pupilDilationRight")) / 2;
            }
        }

        if (gazePixelAnalyser != null && cd != null)
        {

            if (gazePixelAnalyser.isActiveAndEnabled)
            {
                gaze_PixelData = gazePixelAnalyser.GetLatestGazePixelData();
                //calculate corrected values from calibration data and current gaze.

                KeyValuePair<int, float> foveal_Left_KeyValue = cd.FindClosestKeyValueLeft(gaze_PixelData.foveal_gray_scale_value);
                KeyValuePair<int, float> foveal_Right_KeyValue = cd.FindClosestKeyValueRight(gaze_PixelData.foveal_gray_scale_value);

                float foveal_participant_calibration_pupil_dilation_left = foveal_Left_KeyValue.Value;
                float foveal_participant_calibration_pupil_dilation_right = foveal_Right_KeyValue.Value;
                float foveal_corrected_dilation_left = eye_data.pupilDilationLeft - foveal_participant_calibration_pupil_dilation_left;
                float foveal_corrected_dilation_right = eye_data.pupilDilationRight - foveal_participant_calibration_pupil_dilation_right;

                corrected_pupil_dilation_left = foveal_corrected_dilation_left;
                corrected_pupil_dilation_right = foveal_corrected_dilation_right;

                eye_data.pupilDilationLeft = foveal_corrected_dilation_left;
                eye_data.pupilDilationRight = foveal_corrected_dilation_right;
                eye_pixel_window.AddData(eye_data, Time.time);

                // Foveal Corrected PDL
                pupil_dilation_level = (eye_pixel_window.RollingAverage("pupilDilationLeft") + eye_pixel_window.RollingAverage("pupilDilationRight")) / 2;
                // Foveal Corrected PDR
                pupil_dilation_response = (eye_pixel_window.RollingStdDev("pupilDilationLeft") + eye_pixel_window.RollingStdDev("pupilDilationRight")) / 2;
            }

        }

        if (lipTrackingExport != null)
        {
            if (lipTrackingExport.isActiveAndEnabled)
            {
#if DEBUG
                Debug.Log($"Keys Found: {has_smile_keys}, Left: {Left_Smile}, Right: {Right_Smile}");
#endif
                lip_data = lipTrackingExport.GetLatestLipTrackingData();
                if (!(has_smile_keys && lip_data.currLipWeightings.ContainsKey(Left_Smile) && lip_data.currLipWeightings.ContainsKey(Right_Smile)))
                    has_smile_keys = GetSmileKeys(lip_data.currLipWeightings.Keys.ToArray());

                if (!lip_data.IsUnityNull() && has_smile_keys)
                {
                    smile_data.Mouth_Smile_Left = lip_data.currLipWeightings[Left_Smile];
                    smile_data.Mouth_Smile_Right = lip_data.currLipWeightings[Right_Smile];
                    smile_window.AddData(smile_data, Time.time);
                }
                else
                    print("NULL lip data");

                // Smile Avg
                smile = (smile_window.RollingAverage("Mouth_Smile_Left") + smile_window.RollingAverage("Mouth_Smile_Right")) / 2;
            }
        }

        if (enoughDevices)
            UpdateEmotions();
    }

    private bool has_smile_keys = false;

    private bool GetSmileKeys(string[] all_facial_keys)
    {
        var key_set = all_facial_keys.Where(x => x.ToLower().Contains("smile"));
        if (key_set.Count() > 0)
        {
            Left_Smile = key_set.First();
            Right_Smile = key_set.Last();
            return true;
        }
        Left_Smile = "";
        Right_Smile = "";
        return false;
    }

    private IEnumerator SuspendUpdateCoroutine()
    {
        // Print to the debug log when it starts waiting
        Debug.Log("[EmoSense]: Waiting for device services to come online...");

        // Suspend for 1.5 seconds
        yield return new WaitForSeconds(1.5f);

        // After 1.5 seconds, allow Update to run and print to the debug log
        waitingForDevices = false;
        Debug.Log("[EmoSense]: Running EmoSense...");
    }

    private void CalibrationDataLoader_OnCalibrationDataReady(ART_CalibrationData calData)
    {
        cd = calData;
    }

    private void AssignDeviceServices<T>(ref T component) where T : Behaviour
    {
        // Find all components of type T in the children of this game object
        T[] components = GetComponentsInChildren<T>(true);

        // Check if there is more than one component of type T
        if (components.Length > 1)
        {
            throw new System.Exception($"There are multiple components of type {typeof(T).Name} in the children.");
        }

        // Assign the component if it's the only one and is active
        if (components.Length == 1 && components[0].gameObject.activeInHierarchy && components[0].enabled)
        {
            component = components[0];
        }
    }

    public float CalculateRMSSD(List<float> rrIntervals)
    {
        if (rrIntervals.Count < 2)
        {
            throw new ArgumentException("There must be at least two RR intervals to calculate RMSSD.");
        }

        List<float> differences = new List<float>();
        for (int i = 1; i < rrIntervals.Count; i++)
        {
            differences.Add(rrIntervals[i] - rrIntervals[i - 1]);
        }

        List<float> squaredDifferences = differences.Select(diff => diff * diff).ToList();
        float meanOfSquaredDifferences = squaredDifferences.Average();

        float rmssd = Mathf.Sqrt(meanOfSquaredDifferences);

        return rmssd;
    }

    private void UpdateEmotions()
    {
        if (eyeTrackerExport == null || lipTrackingExport == null)
        {
            enoughDevices = false;
            Debug.Log("[EmoSense]: Not enough Sensors available,  please ensure atleast lip and eye tracking is enabled");
            return;
        }

        if (!eyeTrackerExport.isActiveAndEnabled || !lipTrackingExport.isActiveAndEnabled)
        {
            enoughDevices = false;
            Debug.Log("[EmoSense]: Not enough Sensors available, please ensure atleast lip and eye tracking is enabled");
            return;
        }

        if (skinConductanceService != null && bikeControlService != null)
        {
            if (skinConductanceService.IsStreaming && bikeControlService.IsStreaming)
                // we have everything - PDL + PDR + SCL + SMILE + POWER
                EmotionPrediction_All();
            else if (skinConductanceService.IsStreaming && !bikeControlService.IsStreaming)
                // we dont have power - PDL + PDR + SCL + SMILE
                EmotionPrediction_Eye_Lip_SCL();
            else if (bikeControlService.IsStreaming && !skinConductanceService.IsStreaming)
                //we have power but not scl - PDL + PDR + SMILE + power
                EmotionPrediction_Eye_Lip_Bike();
        }
        else
            //Only Vive Sensors available -  PDL + PDR  + SMILE
            EmotionPrediction_Eye_Lip();

        CheckBounds();
    }

    void CheckBounds()
    {
        if (ValenceScore > 10) ValenceScore = 10.0f;
        else if (ValenceScore < 0) ValenceScore = 10.0f;
        if (ValenceCI_Lower > 10) ValenceCI_Lower = 10;
        else if (ValenceCI_Lower < 0) ValenceCI_Lower = 0;
        if (ValenceCI_Upper > 10) ValenceCI_Upper = 10;
        else if (ValenceCI_Upper < 0) ValenceCI_Upper = 0;

        if (ArousalScore > 10) ArousalScore = 10;
        else if (ArousalScore < 0) ArousalScore = 0;
        if (ArousalCI_Lower > 10) ArousalCI_Lower = 10;
        else if (ArousalCI_Lower < 0) ArousalCI_Lower = 0;
        if (ArousalCI_Upper > 10) ArousalCI_Upper = 10;
        else if (ArousalCI_Upper < 0) ArousalCI_Upper = 0;

        if (FearScore > 10) FearScore = 10;
        else if (FearScore < 0) FearScore = 0;
        if (FearCI_Lower > 10) FearCI_Lower = 10;
        else if (FearCI_Lower < 0) FearCI_Lower = 0;
        if (FearCI_Upper > 10) FearCI_Upper = 10;
        else if (FearCI_Upper < 0) FearCI_Upper = 0;

        if (StressScore > 10) StressScore = 10;
        else if (StressScore < 0) StressScore = 0;
        if (StressCI_Lower > 10) StressCI_Lower = 10;
        else if (StressCI_Lower < 0) StressCI_Lower = 0;
        if (StressCI_Upper > 10) StressCI_Upper = 10;
        else if (StressCI_Upper < 0) StressCI_Upper = 0;

        if (HappyScore > 10) HappyScore = 10;
        else if (HappyScore < 0) HappyScore = 0;
        if (HappyCI_Lower > 10) HappyCI_Lower = 10;
        else if (HappyCI_Lower < 0) HappyCI_Lower = 0;
        if (HappyCI_Upper > 10) HappyCI_Upper = 10;
        else if (HappyCI_Upper < 0) HappyCI_Upper = 0;

        if (SadScore > 10) SadScore = 10;
        else if (SadScore < 0) SadScore = 0;
        if (SadCI_Lower > 10) SadCI_Lower = 10;
        else if (SadCI_Lower < 0) SadCI_Lower = 0;
        if (SadCI_Upper > 10) SadCI_Upper = 10;
        else if (SadCI_Upper < 0) SadCI_Upper = 0;

        if (BoredScore > 10) BoredScore = 10;
        else if (BoredScore < 0) BoredScore = 0;
        if (BoredCI_Lower > 10) BoredCI_Lower = 10;
        else if (BoredCI_Lower < 0) BoredCI_Lower = 0;
        if (BoredCI_Upper > 10) BoredCI_Upper = 10;
        else if (BoredCI_Upper < 0) BoredCI_Upper = 0;

        if (ExcitedScore > 10) ExcitedScore = 10;
        else if (ExcitedScore < 0) ExcitedScore = 0;
        if (ExcitedCI_Lower > 10) ExcitedCI_Lower = 10;
        else if (ExcitedCI_Lower < 0) ExcitedCI_Lower = 0;
        if (ExcitedCI_Upper > 10) ExcitedCI_Upper = 10;
        else if (ExcitedCI_Upper < 0) ExcitedCI_Upper = 0;

        if (ContentScore > 10) ContentScore = 10;
        else if (ContentScore < 0) ContentScore = 0;
        if (ContentCI_Lower > 10) ContentCI_Lower = 10;
        else if (ContentCI_Lower < 0) ContentCI_Lower = 0;
        if (ContentCI_Upper > 10) ContentCI_Upper = 10;
        else if (ContentCI_Upper < 0) ContentCI_Upper = 0;

        if (CalmScore > 10) CalmScore = 10;
        else if (CalmScore < 0) CalmScore = 0;
        if (CalmCI_Lower > 10) CalmCI_Lower = 10;
        else if (CalmCI_Lower < 0) CalmCI_Lower = 0;
        if (CalmCI_Upper > 10) CalmCI_Upper = 10;
        else if (CalmCI_Upper < 0) CalmCI_Upper = 0;
    }

    void EmotionPrediction_Eye_Lip()
    {
        ValenceScore = 6.220574f + -0.724043f * pupil_dilation_level + -0.756042f * pupil_dilation_response;
        ValenceCI_Lower = 5.6832891f + -0.9810378f * pupil_dilation_level + -1.3752451f * pupil_dilation_response;
        ValenceCI_Upper = 6.7578582f + -0.4670486f * pupil_dilation_level + -0.1368394f * pupil_dilation_response;

        ArousalScore = 3.636520f + 0.744951f * pupil_dilation_level + 1.284904f * pupil_dilation_response + 2.301723f * smile;
        ArousalCI_Lower = 3.095197f + 0.5070226f * pupil_dilation_level + 0.7086200f * pupil_dilation_response + 0.9980774f * smile;
        ArousalCI_Upper = 4.1745199f + 0.9828787f * pupil_dilation_level + 1.8611872f * pupil_dilation_response + 3.6053693f * smile;

        FearScore = -0.4068040f + 0.9881671f * pupil_dilation_level + 1.4599630f * pupil_dilation_response;
        FearCI_Lower = -0.8582726f + 0.7704441f * pupil_dilation_level + 0.9353196f * pupil_dilation_response;
        FearCI_Upper = 0.04456357f + 1.20589011f * pupil_dilation_level + 1.98460636f * pupil_dilation_response;

        StressScore = -0.543894f + 0.558724f * pupil_dilation_level + 3.173707f * pupil_dilation_response;
        StressCI_Lower = -1.145473f + 1.261356f * pupil_dilation_level + 2.456854f * pupil_dilation_response;
        StressCI_Upper = 0.05768446f + 1.85609184f * pupil_dilation_level + 3.89056012f * pupil_dilation_response;

        HappyScore = 6.125446f + -0.797134f * pupil_dilation_level + -0.972358f * pupil_dilation_response + 1.405508f * smile;
        HappyCI_Lower = 5.55352565f + -1.06567202f * pupil_dilation_level + -1.62313760f * pupil_dilation_response + -0.05763539f * smile;
        HappyCI_Upper = 6.6973754f + -0.5285969f * pupil_dilation_level + -0.3215783f * pupil_dilation_response + 2.8686512f * smile;

        SadScore = 1.129233f + 0.536379f * pupil_dilation_level;
        SadCI_Lower = 0.7774934f + 0.2945224f * pupil_dilation_level;
        SadCI_Upper = 1.4809726f + 0.7782356f * pupil_dilation_level;

        BoredScore = 3.621246f + -0.480268f * pupil_dilation_level + -0.443435f * pupil_dilation_response + -3.305203f * smile;
        BoredCI_Lower = 3.0188181f + -0.7574308f * pupil_dilation_level + -1.1149728f * pupil_dilation_response + -4.8186793f * smile;
        BoredCI_Upper = 4.2236744f + -0.2031050f * pupil_dilation_level + 0.2281028f * pupil_dilation_response + -1.7917269f * smile;

        ExcitedScore = 3.760693f + 0.822826f * pupil_dilation_response + 3.188371f * smile;
        ExcitedCI_Lower = 3.760693f + 0.822826f * pupil_dilation_response + 1.6659706f * smile;
        ExcitedCI_Upper = 3.760693f + 0.822826f * pupil_dilation_response + 4.710772f * smile;

        ContentScore = 6.379589f + -0.961420f * pupil_dilation_level + -1.349947f * pupil_dilation_response + 0.113614f * smile;
        ContentCI_Lower = 5.840549f + -1.216864f * pupil_dilation_level + -1.969066f * pupil_dilation_response + -1.276606f * smile;
        ContentCI_Upper = 6.9186288f + -0.7059765f * pupil_dilation_level + -0.7308283f * pupil_dilation_response + 1.5038331f * smile;

        CalmScore = 8.030915f + -1.886216f * pupil_dilation_level + -2.696539f * pupil_dilation_response;
        CalmCI_Lower = 7.417447f + -2.171746f * pupil_dilation_level + -3.384272f * pupil_dilation_response;
        CalmCI_Upper = 8.644382f + -1.600687f * pupil_dilation_level + -2.008807f * pupil_dilation_response;
    }

    void EmotionPrediction_Eye_Lip_SCL()
    {
        ValenceScore = 6.220574f + -0.724043f * pupil_dilation_level + -0.756042f * pupil_dilation_response;
        ValenceCI_Lower = 5.6832891f + -0.9810378f * pupil_dilation_level + -1.3752451f * pupil_dilation_response;
        ValenceCI_Upper = 6.9186288f + -0.7059765f * pupil_dilation_level - 0.1368394f * pupil_dilation_response;

        ArousalScore = 3.662561f + 0.730707f * pupil_dilation_level + 1.269076f * pupil_dilation_response + 0.160197f * skin_conductance_level + 2.239803f * smile;
        ArousalCI_Lower = 3.1220610f + 0.4897801f * pupil_dilation_level + 0.6909891f * pupil_dilation_response + -0.1674801f * skin_conductance_level + 0.9286398f * smile;
        ArousalCI_Upper = 4.2030603f + 0.9716340f * pupil_dilation_level + 1.8471621f * pupil_dilation_response + 0.4878736f * skin_conductance_level + 3.5509655f * smile;

        FearScore = -0.4068040f + 0.9881671f * pupil_dilation_level + 1.4599630f * pupil_dilation_response;
        FearCI_Lower = -0.8582726f + 0.7704441f * pupil_dilation_level + 0.9353196f * pupil_dilation_response;
        FearCI_Upper = 0.04456357f + 1.20589011f * pupil_dilation_level + 1.98460636f * pupil_dilation_response;

        StressScore = -0.5176318f + 1.5449337f * pupil_dilation_level + 3.1486740f * pupil_dilation_response + 0.5189392f * skin_conductance_level;
        StressCI_Lower = -1.12175650f + 1.24497522f * pupil_dilation_level + 2.43196900f * pupil_dilation_response + 0.09779477f * skin_conductance_level;
        StressCI_Upper = 0.08649293f + 1.84489219f * pupil_dilation_level + 3.86537895f * pupil_dilation_response + 0.94008366f * skin_conductance_level;

        HappyScore = 6.125446f + -0.797134f * pupil_dilation_level + -0.972358f * pupil_dilation_response + 1.405508f * smile;
        HappyCI_Lower = 5.55352565f + -1.06567202f * pupil_dilation_level + -1.62313760f * pupil_dilation_response + -0.05763539f * smile;
        HappyCI_Upper = 6.6973754f + -0.5285969f * pupil_dilation_level + -0.3215783f * pupil_dilation_response + 2.8686512f * smile;

        SadScore = 1.129233f + 0.536379f * pupil_dilation_level;
        SadCI_Lower = 6.9186288f + -0.7059765f * pupil_dilation_level;
        SadCI_Upper = 1.4809726f + 0.7782356f * pupil_dilation_level;

        BoredScore = 3.621246f + -0.480268f * pupil_dilation_level + -0.443435f * pupil_dilation_response + -3.305203f * smile;
        BoredCI_Lower = 3.0188181f + -0.7574308f * pupil_dilation_level + -1.1149728f * pupil_dilation_response + -4.8186793f * smile;
        BoredCI_Upper = 4.2236744f + -0.2031050f * pupil_dilation_level + 0.2281028f * pupil_dilation_response + -1.7917269f * smile;

        ExcitedScore = 3.760693f + 0.822826f * pupil_dilation_response + 3.188371f * smile;
        ExcitedCI_Lower = 3.1757221f + 0.1484375f * pupil_dilation_response + 1.6659706f * smile;
        ExcitedCI_Upper = 4.345664f + 1.497214f * pupil_dilation_response + 4.710772f * smile;

        ContentScore = 6.379589f + -0.961420f * pupil_dilation_level + -1.349947f * pupil_dilation_response + 0.113614f * smile;
        ContentCI_Lower = 5.840549f + -1.216864f * pupil_dilation_level + -1.969066f * pupil_dilation_response + -1.276606f * smile;
        ContentCI_Upper = 6.9186288f + -0.7059765f * pupil_dilation_level + -0.7308283f * pupil_dilation_response + 1.5038331f * smile;

        CalmScore = 8.030915f + -1.886216f * pupil_dilation_level + -2.696539f * pupil_dilation_response;
        CalmCI_Lower = 7.417447f + -2.171746f * pupil_dilation_level + -3.384272f * pupil_dilation_response;
        CalmCI_Upper = 8.644382f + -1.600687f * pupil_dilation_level + -2.008807f * pupil_dilation_response;
    }

    void EmotionPrediction_Eye_Lip_Bike()
    {
        ValenceScore = 6.424391f + -0.615025f * pupil_dilation_level + -0.623880f * pupil_dilation_response + -0.003629f * power;
        ValenceCI_Lower = 5.828419445f + -0.892815737f * pupil_dilation_level + -1.245901201f * pupil_dilation_response + -0.007131843f * power;
        ValenceCI_Upper = 7.020361885f + -0.337234341f * pupil_dilation_level + -0.001859515f * pupil_dilation_response + -0.000126324f * power;

        ArousalScore = 3.0546916f + 0.4494893f * pupil_dilation_level + 1.3183087f * pupil_dilation_response + 1.8576008f * smile + 0.0094298f * power;
        ArousalCI_Lower = 2.49400993f + 0.19977552f * pupil_dilation_level + 0.75725008f * pupil_dilation_response + 0.59781472f * smile + 0.00626962f * power;
        ArousalCI_Upper = 3.61537334f + 0.69920314f * pupil_dilation_level + 1.87936736f * pupil_dilation_response + 3.11738685f * smile + 0.01258991f * power;

        FearScore = -0.6439730f + 0.8810232f * pupil_dilation_level + 1.4071612f * pupil_dilation_response + 0.0039833f * power;
        FearCI_Lower = -1.1606400615f + 0.6408746984f * pupil_dilation_level + 0.8695801624f * pupil_dilation_response + 0.0009548402f * power;
        FearCI_Upper = -0.12730590f + 1.12117171f * pupil_dilation_level + 1.94474230f * pupil_dilation_response + 0.00701186f * power;

        StressScore = -1.4098111f + 1.2721993f * pupil_dilation_level + 3.1554589f * pupil_dilation_response + 0.0121392f * power;
        StressCI_Lower = -2.071521506f + 0.958393403f * pupil_dilation_level + 2.451234604f * pupil_dilation_response + 0.008186086f * power;
        StressCI_Upper = -0.74810063f + 1.58600514f * pupil_dilation_level + 3.85968313f * pupil_dilation_response + 0.01609223f * power;

        HappyScore = 6.125446f + -0.797134f * pupil_dilation_level + -0.972358f * pupil_dilation_response + 1.405508f * smile;
        HappyCI_Lower = 5.55352565f + -1.06567202f * pupil_dilation_level + -1.62313760f * pupil_dilation_response + -0.05763539f * smile;
        HappyCI_Upper = 6.6973754f + -0.5285969f * pupil_dilation_level + -0.3215783f * pupil_dilation_response + 2.8686512f * smile;

        SadScore = 1.129233f + 0.536379f * pupil_dilation_level;
        SadCI_Lower = 0.7774934f + 0.2945224f * pupil_dilation_level;
        SadCI_Upper = 1.4809726f + 0.7782356f * pupil_dilation_level;

        BoredScore = 3.621246f + -0.480268f * pupil_dilation_level + -0.443435f * pupil_dilation_response + -3.305203f * smile;
        BoredCI_Lower = 3.0188181f + -0.7574308f * pupil_dilation_level + -1.1149728f * pupil_dilation_response + -4.8186793f * smile;
        BoredCI_Upper = 4.2236744f + -0.2031050f * pupil_dilation_level + 0.2281028f * pupil_dilation_response + -1.7917269f * smile;

        ExcitedScore = 3.1285325f + 0.7907470f * pupil_dilation_response + 2.8110059f * smile + 0.0075217f * power;
        ExcitedCI_Lower = 2.449680758f + 0.107116795f * pupil_dilation_response + 1.275632808f * smile + 0.003883935f * power;
        ExcitedCI_Upper = 3.80738424f + 1.47437712f * pupil_dilation_response + 4.34637901f * smile + 0.01115944f * power;

        ContentScore = 6.379589f + -0.961420f * pupil_dilation_level + -1.349947f * pupil_dilation_response + 0.113614f * smile;
        ContentCI_Lower = 5.840549f + -1.216864f * pupil_dilation_level + -1.969066f * pupil_dilation_response + -1.276606f * smile;
        ContentCI_Upper = 6.9186288f + -0.7059765f * pupil_dilation_level + -0.7308283f * pupil_dilation_response + 1.5038331f * smile;

        CalmScore = 8.954761f + -1.545054f * pupil_dilation_level + -2.573313f * pupil_dilation_response + -0.013396f * power;
        CalmCI_Lower = 8.30732510f + -1.84702013f * pupil_dilation_level + -3.24951022f * pupil_dilation_response + -0.01720299f * power;
        CalmCI_Upper = 9.60219777f + -1.23208815f * pupil_dilation_level + -1.89711611f * pupil_dilation_response + -0.00958803f * power;
    }

    void EmotionPrediction_All()
    {

        ValenceScore = 6.424391f + -0.615025f * pupil_dilation_level + -0.623880f * pupil_dilation_response + -0.003629f * power;
        ValenceCI_Lower = 5.828419445f + -0.892815737f * pupil_dilation_level + -1.245901201f * pupil_dilation_response + -0.007131843f * power;
        ValenceCI_Upper = 7.020361885f + -0.337234341f * pupil_dilation_level + -0.001859515f * pupil_dilation_response + -0.000126324f * power;

        ArousalScore = 3.0652430f + 0.4491480f * pupil_dilation_level + 1.3144163f * pupil_dilation_response + 0.0795995f * skin_conductance_level + 1.8313020f * smile + 0.0093683f * power;
        ArousalCI_Lower = 2.50187158f + 0.19845352f * pupil_dilation_level + 0.75268850f * pupil_dilation_response + -0.23732914f * skin_conductance_level + 0.56701064f * smile + 0.00619514f * power;
        ArousalCI_Upper = 3.62861446f + 0.69984252f * pupil_dilation_level + 1.87614414f * pupil_dilation_response + 0.39652822f * skin_conductance_level + 3.09559332f * smile + 0.01254154f * power;

        FearScore = -0.6439730f + 0.8810232f * pupil_dilation_level + 1.4071612f * pupil_dilation_response + 0.0039833f * power;
        FearCI_Lower = -1.1606400615f + 0.6408746984f * pupil_dilation_level + 0.8695801624f * pupil_dilation_response + 0.0009548402f * power;
        FearCI_Upper = -0.12730590f + 1.12117171f * pupil_dilation_level + 1.94474230f * pupil_dilation_response + 0.00701186f * power;

        StressScore = -1.3623915f + 1.2627884f * pupil_dilation_level + 3.1304176f * pupil_dilation_response + 0.3914037f * skin_conductance_level + 0.0118472f * power;
        StressCI_Lower = -2.026042653f + 0.948557809f * pupil_dilation_level + 2.426994946f * pupil_dilation_response + -0.020314661f * skin_conductance_level + 0.007886725f * power;
        StressCI_Upper = -0.69874042f + 1.57701889f * pupil_dilation_level + 3.83384018f * pupil_dilation_response + 0.80312212f * skin_conductance_level + 0.01580763f * power;

        HappyScore = 6.125446f + -0.797134f * pupil_dilation_level + -0.972358f * pupil_dilation_response + 1.405508f * smile;
        HappyCI_Lower = 5.55352565f + -1.06567202f * pupil_dilation_level + -1.62313760f * pupil_dilation_response + -0.05763539f * smile;
        HappyCI_Upper = 6.6973754f + -0.5285969f * pupil_dilation_level + -0.3215783f * pupil_dilation_response + 2.8686512f * smile;

        SadScore = 1.129233f + 0.536379f * pupil_dilation_level;
        SadCI_Lower = 0.7774934f + 0.2945224f * pupil_dilation_level;
        SadCI_Upper = 1.4809726f + 0.7782356f * pupil_dilation_level;

        BoredScore = 3.621246f + -0.480268f * pupil_dilation_level + -0.443435f * pupil_dilation_response + -3.305203f * smile;
        BoredCI_Lower = 3.0188181f + -0.7574308f * pupil_dilation_level + -1.1149728f * pupil_dilation_response + -4.8186793f * smile;
        BoredCI_Upper = 4.2236744f + -0.2031050f * pupil_dilation_level + 0.2281028f * pupil_dilation_response + -1.7917269f * smile;

        ExcitedScore = 3.1285325f + 0.7907470f * pupil_dilation_response + 2.8110059f * smile + 0.0075217f * power;
        ExcitedCI_Lower = 2.449680758f + 0.107116795f * pupil_dilation_response + 1.275632808f * smile + 0.003883935f * power;
        ExcitedCI_Upper = 3.80738424f + 1.47437712f * pupil_dilation_response + 4.34637901f * smile + 0.01115944f * power;

        ContentScore = 6.379589f + -0.961420f * pupil_dilation_level + -1.349947f * pupil_dilation_response + 0.113614f * smile;
        ContentCI_Lower = 5.840549f + -1.216864f * pupil_dilation_level + -1.969066f * pupil_dilation_response + -1.276606f * smile;
        ContentCI_Upper = 6.9186288f + -0.7059765f * pupil_dilation_level + -0.7308283f * pupil_dilation_response + 1.5038331f * smile;

        CalmScore = 8.954761f + -1.545054f * pupil_dilation_level + -2.573313f * pupil_dilation_response + -0.013396f * power;
        CalmCI_Lower = 8.30732510f + -1.84702013f * pupil_dilation_level + -3.24951022f * pupil_dilation_response + -0.01720299f * power;
        CalmCI_Upper = 9.60219777f + -1.23208815f * pupil_dilation_level + -1.89711611f * pupil_dilation_response + -0.00958803f * power;
    }

    internal override string FileHeader()
    {
        return "Emo_Valence_Score,Emo_Valence_CI_Upper,Emo_Valence_CI_Lower," +
            "Emo_Arousal_Score,Emo_Arousal_CI_Upper,Emo_Arousal_CI_Lower," +
            "Emo_Fear_Score,Emo_Fear_CI_Upper,Emo_Fear_CI_Lower," +
            "Emo_Stress_Score,Emo_Stress_CI_Upper,Emo_Stress_CI_Lower," +
            "Emo_Happy_Score,Emo_Happy_CI_Upper,Emo_Happy_CI_Lower," +
            "Emo_Sad_Score,Emo_Sad_CI_Upper,Emo_Sad_CI_Lower," +
            "Emo_Bored_Score,Emo_Bored_CI_Upper,Emo_Bored_CI_Lower," +
            "Emo_Excited_Score,Emo_Excited_CI_Upper,Emo_Excited_CI_Lower," +
            "Emo_Content_Score,Emo_Content_CI_Upper,Emo_Content_CI_Lower," +
            "Emo_Calm_Score,Emo_Calm_CI_Upper,Emo_Calm_CI_Lower,";
    }

    internal override string GetData()
    {
        return $"{ValenceScore},{ValenceCI_Upper},{ValenceCI_Lower},{ArousalScore},{ArousalCI_Upper},{ArousalCI_Lower},{FearScore},{FearCI_Upper},{FearCI_Lower},{StressScore},{StressCI_Upper},{StressCI_Lower},{HappyScore},{HappyCI_Upper},{HappyCI_Lower},{SadScore},{SadCI_Upper},{SadCI_Lower},{BoredScore},{BoredCI_Upper},{BoredCI_Lower},{ExcitedScore},{ExcitedCI_Upper},{ExcitedCI_Lower},{ContentScore},{ContentCI_Upper},{ContentCI_Lower},{CalmScore},{CalmCI_Upper},{CalmCI_Lower},";
    }

    public override string DeviceName()
    {
        return "EmoSense Framework";
    }
}

public struct RollingWindowData<T>
{
    public T Data;
    public float Timestamp;

    public RollingWindowData(T data, float timestamp)
    {
        Data = data;
        Timestamp = timestamp;
    }
}

public struct SmileData
{
    public float Mouth_Smile_Right { get; set; }
    public float Mouth_Smile_Left { get; set; }
}

public class RollingWindow<T>
{
    private List<RollingWindowData<T>> dataQueue = new List<RollingWindowData<T>>();
    private Dictionary<string, List<float>> metricValues = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> sqDiffQueues = new Dictionary<string, List<float>>();
    private float windowDuration = 60.0f; // 60 seconds

    public Dictionary<string, float> sumDictionary = new Dictionary<string, float>();
    public Dictionary<string, float> sumOfSqDiffDictionary = new Dictionary<string, float>();

    public void AddData(T newData, float timestamp)
    {
        foreach (var property in newData.GetType().GetProperties())
        {
            var propertyType = property.PropertyType;

            if (propertyType == typeof(float) || propertyType == typeof(double) || propertyType == typeof(int))
            {
                //Debug.Log("Property:" + property.Name);
                var metricValue = 0.0f;

                if (property.GetValue(newData) is float)
                {
                    metricValue = (float)property.GetValue(newData);
                }
                else if (property.GetValue(newData) is double doubleValue)
                {
                    metricValue = (float)doubleValue;

                }
                else if (property.GetValue(newData) is int intValue)
                {
                    //int
                    metricValue = (float)intValue;
                }

                var metricName = property.Name;

                // Calculate squared difference and update sums for each metric
                float diff = metricValue - (sumDictionary.ContainsKey(metricName) ? sumDictionary[metricName] / dataQueue.Count : 0);
                float squaredDiff = diff * diff;

                // Initialize metric-specific lists if they don't exist
                if (!metricValues.ContainsKey(metricName))
                {
                    metricValues[metricName] = new List<float>();
                    sqDiffQueues[metricName] = new List<float>();
                }

                metricValues[metricName].Add(metricValue);
                sqDiffQueues[metricName].Add(squaredDiff);

                sumOfSqDiffDictionary[metricName] = sumOfSqDiffDictionary.ContainsKey(metricName) ?
                    sumOfSqDiffDictionary[metricName] + squaredDiff :
                    squaredDiff;

                sumDictionary[metricName] = sumDictionary.ContainsKey(metricName) ?
                    sumDictionary[metricName] + metricValue :
                    metricValue;
            }
        }

        var rollingData = new RollingWindowData<T>(newData, timestamp);
        dataQueue.Add(rollingData);

        // Remove data older than the window duration
        float currentTime = Time.time;
        while (dataQueue.Count > 0 && currentTime - dataQueue[0].Timestamp > windowDuration)
        {
            var removedData = dataQueue[0].Data;
            foreach (var property in removedData.GetType().GetProperties())
            {
                var propertyType = property.PropertyType;

                if (propertyType == typeof(float) || propertyType == typeof(double) || propertyType == typeof(int))
                {
                    var metricName = property.Name;
                    float removedValue = 0.0f;

                    if (property.GetValue(newData) is float)
                    {
                        removedValue = (float)property.GetValue(newData);
                    }
                    else if (property.GetValue(newData) is double doubleValue)
                    {
                        removedValue = (float)doubleValue;

                    }
                    else if (property.GetValue(newData) is int intValue)
                    {
                        //int
                        removedValue = (float)intValue;
                    }

                    metricValues[metricName].RemoveAt(0);
                    sqDiffQueues[metricName].RemoveAt(0);
                    sumDictionary[metricName] -= removedValue;
                    sumOfSqDiffDictionary[metricName] -= sqDiffQueues[metricName][0];
                }
            }

            dataQueue.RemoveAt(0);
        }
    }


    // Retrieve a list of values for a specific metric
    public List<float> GetMetricValues(string metricName)
    {
        if (metricValues.ContainsKey(metricName))
        {
            return metricValues[metricName];
        }
        return new List<float>(); // Metric not found
    }

    public List<RollingWindowData<T>> GetDataInWindow()
    {
        return dataQueue;
    }

    // Calculate the rolling average of a specific metric
    public float RollingAverage(string metricName)
    {
        if (sumDictionary.ContainsKey(metricName))
        {
            return sumDictionary[metricName] / dataQueue.Count;
        }
        return 0; // Metric not found
    }

    // Calculate the rolling standard deviation of a specific metric
    public float RollingStdDev(string metricName)
    {
        if (sumOfSqDiffDictionary.ContainsKey(metricName))
        {
            float meanSquaredDiff = sumOfSqDiffDictionary[metricName] / dataQueue.Count;
            return (float)Math.Sqrt(meanSquaredDiff);
        }
        return 0; // Metric not found
    }
}