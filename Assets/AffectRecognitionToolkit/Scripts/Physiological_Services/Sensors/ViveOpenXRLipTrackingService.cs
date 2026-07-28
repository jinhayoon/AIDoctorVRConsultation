using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.FacialTracking;

public class ViveOpenXRLipTrackingService : ILipTrackingService
{
    public override string DeviceName()
    {
        return "Vive Elite Facial-Tracking";
    }

    private float[] eyeExps = new float[(int)XrEyeExpressionHTC.XR_EYE_EXPRESSION_MAX_ENUM_HTC];
    private float[] lipExps = new float[(int)XrLipExpressionHTC.XR_LIP_EXPRESSION_MAX_ENUM_HTC];

    private readonly string[] niceExpressionNames_Eye = { "Vive_Eye_Left_Blink", "Vive_Eye_Left_Wide", "Vive_Eye_Right_Blink", "Vive_Eye_Right_Wide", "Vive_Eye_Left_Squeeze", "Vive_Eye_Right_Squeeze",
        "Vive_Eye_Left_Down", "Vive_Eye_Right_Down", "Vive_Eye_Left_Out", "Vive_Eye_Right_In", "Vive_Eye_Left_In", "Vive_Eye_Right_Out", "Vive_Eye_Left_Up", "Vive_Eye_Right_Up" },
        niceExpressionNames_Lip = { "Vive_Face_Jaw_Right", "Vive_Face_Jaw_Left", "Vive_Face_Jaw_Forward", "Vive_Face_Jaw_Open", "Vive_Face_Mouth_Ape_Shape", "Vive_Face_Mouth_Upper_Right", "Vive_Face_Mouth_Upper_Left", "Vive_Face_Lower_Right", "Vive_Face_Lower_Left",
        "Vive_Face_Mouth_Upper_Overturn", "Vive_Face_Mouth_Lower_Overturn", "Vive_Face_Mouth_Pout", "Vive_Face_Mouth_Smile_Right", "Vive_Face_Mouth_Smile_Left", "Vive_Face_Mouth_Sad_Right", "Vive_Face_Mouth_Sad_Left", "Vive_Face_Mouth_Puff_Right", "Vive_Face_Mouth_Puff_left",
        "Vive_Face_Mouth_Suck", "Vive_Face_Mouth_Upper_UpRight", "Vive_Face_Mouth_Upper_UpLeft", "Vive_Face_Mouth_Lower_DownRight", "Vive_Face_Mouth_Lower_DownLeft", "Vive_Face_Mouth_Upper_Inside", "Vive_Face_Mouth_Lower_Inside", "Vive_Face_Tongue_Out_1", 
        "Vive_Face_Tongue_Left", "Vive_Face_Tongue_Right", "Vive_Face_Tongue_Up", "Vive_Face_Tongue_Down", "Vive_Face_Tongue_Roll", "Vive_Face_Tongue_Out_2", "Vive_Face_Tongue_Up_Right", "Vive_Face_Tongue_Up_Left", "Vive_Face_Tongue_Down_Right", "Vive_Face_Tongue_Down_Left" };

    // Start is called before the first frame update
    void Start()
    {
        latestLipTrackingData.currLipWeightings = new Dictionary<string, float>();
        for (int i = 0; i < niceExpressionNames_Eye.Length; i++)
            latestLipTrackingData.currLipWeightings.Add(niceExpressionNames_Eye[i], -1f);
        for (int i = 0; i < niceExpressionNames_Lip.Length; i++)
            latestLipTrackingData.currLipWeightings.Add(niceExpressionNames_Lip[i], -1f);
    }

    private float[] exps;

    // Update is called once per frame
    void Update()
    {
        var feature = OpenXRSettings.Instance.GetFeature<ViveFacialTracking>();
        if (feature != null)
        {
            // Eye expressions
            {
                if (feature.GetFacialExpressions(XrFacialTrackingTypeHTC.XR_FACIAL_TRACKING_TYPE_EYE_DEFAULT_HTC, out exps))
                {
                    eyeExps = exps;
                    for (int i = 0; i < niceExpressionNames_Eye.Length; i++)
                        latestLipTrackingData.currLipWeightings[niceExpressionNames_Eye[i]] = exps[i];
                }else
                {
                    for (int i = 0; i < niceExpressionNames_Eye.Length; i++)
                        latestLipTrackingData.currLipWeightings[niceExpressionNames_Eye[i]] = -1f;
                }
            }
            // Lip expressions
            {
                if (feature.GetFacialExpressions(XrFacialTrackingTypeHTC.XR_FACIAL_TRACKING_TYPE_LIP_DEFAULT_HTC, out exps))
                {
                    lipExps = exps;
                    for (int i = 0; i < niceExpressionNames_Lip.Length; i++)
                        latestLipTrackingData.currLipWeightings[niceExpressionNames_Lip[i]] = exps[i];
                }
                else
                {
                    for (int i = 0; i < niceExpressionNames_Lip.Length; i++)
                        latestLipTrackingData.currLipWeightings[niceExpressionNames_Lip[i]] = -1f;
                }
            }
        }
    }
}
