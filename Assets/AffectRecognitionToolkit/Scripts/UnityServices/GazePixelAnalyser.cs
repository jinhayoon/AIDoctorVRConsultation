using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GazePixelAnalyser : BaseDevice
{
    public Camera renderCamera; // Reference to the VR camera (output)
    public RenderTexture render;
    private GazePixelData gpd;

    public float fovealGrayScale;
    public float parafovealGrayScale;
    public float headGrayScale;
    public float imageGrayScale;

    public bool correctFoveal;
    public bool correctParafoveal;
    public bool correctHead;
    public bool correctImage;

    IEyeTrackingService _eyeTracker;

    public Vector3 GazeDirectionCombined;

    private void Start()
    {
        render = renderCamera.targetTexture;
        if (render == null)        
            Debug.LogError("Ensure GazePixelAnalyser camera has render texture set!");          
        RenderPipelineManager.endContextRendering += OnEndContextRendering;

        StartCoroutine(LookForEyeTracker());

        gpd = new GazePixelData();
    }

    private void OnDestroy()
    {
        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
    }

    private IEnumerator LookForEyeTracker()
    {
        while (true)
        {
            if (_eyeTracker == null || !_eyeTracker.isActiveAndEnabled)
            {
                _eyeTracker = FindAnyObjectByType<IEyeTrackingService>();
            }

            yield return new WaitForSeconds(3.0f);
        }
    }

    private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {        
        if (renderCamera.targetTexture == null) return;        
        if (!correctFoveal && !correctParafoveal && !correctHead && !correctImage) return;
       
        // Get the gaze direction from the headset's forward vector
        Vector3 GazeOriginCombinedLocal = _eyeTracker.latestEyeTrackingData.EyeGazePosLocal, GazeDirectionCombinedLocal = _eyeTracker.latestEyeTrackingData.EyeGazeDirLocal;

        GazeDirectionCombined = Camera.main.transform.TransformDirection(GazeDirectionCombinedLocal);

        // Create a temporary Texture2D to read pixel data from the RenderTexture
        RenderTexture prevRT = RenderTexture.active;
        RenderTexture.active = renderCamera.targetTexture;
        Texture2D tempTexture = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);                
        tempTexture.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
        tempTexture.Apply();       
        RenderTexture.active = prevRT;

        // Create a temporary Texture2D to read pixel data from the RenderTexture
        if (correctFoveal)
        {
            gpd.foveal_gray_scale_value = SamplePixelsInCircularRegion(tempTexture, GazeDirectionCombined, 2.0f); //foveal
            fovealGrayScale = gpd.foveal_gray_scale_value;
        } 
           
        if (correctParafoveal)
        {
            gpd.parafoveal_gray_scale_value = SamplePixelsInCircularRegion(tempTexture, GazeDirectionCombined, 10.0f); //parafoveal
            parafovealGrayScale = gpd.parafoveal_gray_scale_value;
        }

        if (correctHead)
        {
            gpd.headpoint_gray_scale_value = SamplePixelsInCircularRegion(tempTexture, Camera.main.transform.forward, 10.0f); //headpoint
            headGrayScale = gpd.headpoint_gray_scale_value;
        }

        if (correctImage)
        {
            gpd.image_gray_scale_value = SamplePixelsOfCameraImage(tempTexture); //image
            imageGrayScale = gpd.image_gray_scale_value;
        }
        
        Destroy(tempTexture);
    }

    private float SamplePixelsInCircularRegion(Texture2D r_texture, Vector3 gazeDirection, float visualAngle)
    {
        Vector2 uv = new Vector2(0.5f + gazeDirection.x * 0.5f, 0.5f + gazeDirection.y * 0.5f);

        // Convert UV coordinates to pixel coordinates
        int x = Mathf.RoundToInt(uv.x * r_texture.width);
        int y = Mathf.RoundToInt(uv.y * r_texture.height);

        // Calculate the number of pixels in each direction based on visual angle
        float radiusInRadians = Mathf.Deg2Rad * visualAngle;
        int numOfPixels = Mathf.RoundToInt(radiusInRadians * gazeDirection.magnitude * Mathf.Max(r_texture.width, r_texture.height));
        //float radiusInPixels = visualAngle * Mathf.Max(r_texture.width, r_texture.height) / 360f;

        // Calculate the average grayscale value
        float totalGrayScale = 0f;
        int pixelCount = 0;

        for (int i = -numOfPixels; i <= numOfPixels; i++)
        {
            for (int j = -numOfPixels; j <= numOfPixels; j++)
            {
                int sampleX = x + i;
                int sampleY = y + j;

                if (sampleX >= 0 && sampleX < r_texture.width && sampleY >= 0 && sampleY < r_texture.height)
                {
                    Color pixelColor = r_texture.GetPixel(sampleX, sampleY);
                    //float grayScale = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;
                    float grayScale = pixelColor.grayscale;
                    grayScale = Mathf.RoundToInt(grayScale * 255f);
                    totalGrayScale += grayScale;
                    pixelCount++;
                }
            }
        }

        float averageGrayScale = totalGrayScale / pixelCount;

        // Cleanup: Destroy the temporary Texture2D
        //Destroy(tempTexture);

        return averageGrayScale;
    }

    private float SamplePixelsOfCameraImage(Texture2D r_texture)
    {
        // Calculate the average grayscale value
        float sumGrayscale = 0f;
        Color[] pixels = r_texture.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float grayscaleValue = pixels[i].grayscale;
            grayscaleValue = Mathf.RoundToInt(grayscaleValue * 255f);
            sumGrayscale += grayscaleValue;
        }

        float averageGrayscale = sumGrayscale / pixels.Length;

        return averageGrayscale;
    }

    public GazePixelData GetLatestGazePixelData() { return gpd; }

    internal override string FileHeader()
    {
        return "Foveal_GrayScaleValue,Parafoveal_GrayScaleValue,Headpoint_GrayScaleValue,Image_GrayScaleValue,";
    }

    internal override string GetData()
    {
        return $"{gpd.foveal_gray_scale_value},{gpd.parafoveal_gray_scale_value},{gpd.headpoint_gray_scale_value},{gpd.image_gray_scale_value},";
    }

    public override string DeviceName() { return "GazePixelAnalyser"; }
}

public struct GazePixelData
{
    public float foveal_gray_scale_value { get; set; }
    public float parafoveal_gray_scale_value { get; set; }
    public float headpoint_gray_scale_value { get; set; }
    public float image_gray_scale_value { get; set; }
}
