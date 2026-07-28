using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class ReplayController : MonoBehaviour
{
    public string csvFileName = "Assets/EMOSENSE DATA/EMOSENSE_1.csv";
    public string targetSceneName = "Ella_Control";

    private List<Dictionary<string, float>> frameData = new List<Dictionary<string, float>>();
    private int currentFrame = 0;
    private bool readyToReplay = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        string filePath = Path.Combine(Application.dataPath, csvFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV file not found: {filePath}");
            return;
        }

        using (var reader = new StreamReader(filePath))
        {
            string headerLine = reader.ReadLine();
            if (string.IsNullOrEmpty(headerLine))
            {
                Debug.LogError("CSV file is empty or missing header.");
                return;
            }

            string[] headers = headerLine.Split(',');
            int sceneColIndex = System.Array.IndexOf(headers, "CurrentScene");
            if (sceneColIndex == -1)
            {
                Debug.LogError("CSV does not contain 'CurrentScene' column.");
                return;
            }

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] columns = line.Split(',');
                if (columns.Length > sceneColIndex && columns[sceneColIndex] == targetSceneName)
                {
                    // Found the first frame for the target scene, start collecting frames
                    do
                    {
                        var frameDict = new Dictionary<string, float>();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            float val;
                            if (float.TryParse(columns[i], out val))
                                frameDict[headers[i]] = val;
                        }
                        frameData.Add(frameDict);
                        line = reader.ReadLine();
                        if (line == null) break;
                        columns = line.Split(',');
                    } while (columns.Length > sceneColIndex && columns[sceneColIndex] == targetSceneName);

                    break; // Only collect frames for the first occurrence of the target scene
                }
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(targetSceneName);


        m = LayerMask.GetMask("Doc");
    }

    LayerMask m;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == targetSceneName)
        {
            readyToReplay = true;

            if (headSphere == null)
            {
                headSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                headSphere.transform.localScale = Vector3.one * 0.2f; // Adjust size as needed
                headSphere.GetComponent<Renderer>().material.color = Color.blue; // Optional: color
            }


            if (eyeSphere == null)
            {
                eyeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeSphere.transform.localScale = Vector3.one * 0.05f; // Adjust size as needed
                eyeSphere.GetComponent<Renderer>().material.color = Color.green; // Optional: color
            }
        }
    }

    private GameObject headSphere, eyeSphere;

    float HMD_pX, HMD_pY, HMD_pZ;
    float HMD_aqX, HMD_aqY, HMD_aqZ, HMD_aqW;
    float LeftEyeGaze_Pos_x, LeftEyeGaze_Pos_y, LeftEyeGaze_Pos_z;
    float LeftEyeGaze_Direction_x, LeftEyeGaze_Direction_y, LeftEyeGaze_Direction_z;

    void Update()
    {
        if (!readyToReplay || frameData.Count == 0) return;

        if (currentFrame < frameData.Count)
        {
            var frame = frameData[currentFrame];

            // Set values in Replay
            HMD_pX = frame.ContainsKey("HMD_pX") ? frame["HMD_pX"] : 0f;
            HMD_pY = frame.ContainsKey("HMD_pY") ? frame["HMD_pY"] - 1.36144f : 0f;
            HMD_pZ = frame.ContainsKey("HMD_pZ") ? frame["HMD_pZ"] : 0f;
            HMD_aqX = frame.ContainsKey("HMD_aqX") ? frame["HMD_aqX"] : 0f;
            HMD_aqY = frame.ContainsKey("HMD_aqY") ? frame["HMD_aqY"] : 0f;
            HMD_aqZ = frame.ContainsKey("HMD_aqZ") ? frame["HMD_aqZ"] : 0f;
            HMD_aqW = frame.ContainsKey("HMD_aqW") ? frame["HMD_aqW"] : 0f;

            LeftEyeGaze_Pos_x = frame.ContainsKey("LeftEyeGaze_Local_Pos.x") ? frame["LeftEyeGaze_Local_Pos.x"] : 0f;
            LeftEyeGaze_Pos_y = frame.ContainsKey("LeftEyeGaze_Local_Pos.y") ? frame["LeftEyeGaze_Local_Pos.y"] - 1.36144f : 0f;
            LeftEyeGaze_Pos_z = frame.ContainsKey("LeftEyeGaze_Local_Pos.z") ? frame["LeftEyeGaze_Local_Pos.z"] : 0f;

            LeftEyeGaze_Direction_x = frame.ContainsKey("LeftEyeGaze_Local_Direction.x") ? frame["LeftEyeGaze_Local_Direction.x"] : 0f;
            LeftEyeGaze_Direction_y = frame.ContainsKey("LeftEyeGaze_Local_Direction.y") ? frame["LeftEyeGaze_Local_Direction.y"] : 0f;
            LeftEyeGaze_Direction_z = frame.ContainsKey("LeftEyeGaze_Local_Direction.z") ? frame["LeftEyeGaze_Local_Direction.z"] : 0f;

            // Get main camera transform
            Transform camTrans = Camera.main.transform;

            // HMD local position and rotation
            Vector3 hmdLocalPos = new Vector3(HMD_pX, HMD_pY, HMD_pZ);
            Quaternion hmdLocalRot = new Quaternion(HMD_aqX, HMD_aqY, HMD_aqZ, HMD_aqW);

            // HMD global position and rotation (relative to main camera)
            Vector3 hmdGlobalPos = camTrans.TransformPoint(hmdLocalPos);
            Quaternion hmdGlobalRot = camTrans.rotation * hmdLocalRot;

            // Eye local position and direction
            Vector3 eyeLocalPos = new Vector3(LeftEyeGaze_Pos_x, LeftEyeGaze_Pos_y, LeftEyeGaze_Pos_z);
            Vector3 eyeLocalDir = new Vector3(LeftEyeGaze_Direction_x, LeftEyeGaze_Direction_y, LeftEyeGaze_Direction_z);

            // Eye global position and direction (relative to HMD)
            Vector3 eyeGlobalPos = hmdGlobalRot * eyeLocalPos + hmdGlobalPos;
            Vector3 eyeGlobalDir = hmdGlobalRot * eyeLocalDir;

            Ray r = new Ray(eyeLocalPos, eyeLocalDir.normalized);

            if (headSphere != null)
            {
                headSphere.transform.position = hmdGlobalPos;
                headSphere.transform.rotation = hmdGlobalRot;
            }

            if(eyeSphere != null)
            {
                eyeSphere.transform.position = eyeGlobalPos;
                // Optionally orient the eye sphere to face the gaze direction
                //eyeSphere.transform.rotation = Quaternion.LookRotation(eyeGlobalDir);
            }
            // Visualize the ray in the Scene view for debugging
            Debug.DrawRay(eyeGlobalPos, eyeGlobalDir.normalized * 20f, Color.red, 0.2f, false);


            if (Physics.Raycast(r, out RaycastHit hitInfo, 20f, m))
                Debug.Log("Currently Looking at Doc");


            // If you have a second gaze direction, set it similarly

            currentFrame++;
        }
    }
}