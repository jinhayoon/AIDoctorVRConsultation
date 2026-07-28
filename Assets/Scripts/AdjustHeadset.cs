using UnityEngine;
using AIDoctor.Flow;

public class AdjustHeadset : MonoBehaviour
{
    public SceneController sceneController; // Reference to SceneController
    public string sceneName; // Name of the next scene to load

    private float inputDelayTimer = 0.5f;  // Wait 0.5 seconds before accepting input
    private bool canAcceptInput = false;

    void Start()
    {
        // Reset the timer when scene loads
        inputDelayTimer = 0.5f;
        canAcceptInput = false;

        Debug.Log("[AdjustHeadset] Scene loaded");
        Debug.Log($"[AdjustHeadset] SceneFlowManager index: {SceneFlowManager.Instance?.CurrentSceneIndex}");
        Debug.Log($"[AdjustHeadset] SceneFlowManager scene: {SceneFlowManager.Instance?.CurrentSceneName}");
    }

    void Update()
    {
        // Wait for input delay before accepting any input
        if (!canAcceptInput)
        {
            inputDelayTimer -= Time.deltaTime;
            if (inputDelayTimer <= 0)
            {
                canAcceptInput = true;
                Debug.Log("[AdjustHeadset] Now accepting input");
            }
            return;  // Don't process input yet
        }

        // Now safe to check for Return key
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("[AdjustHeadset] Return key pressed - loading next scene");
            SceneFlowManager.Instance.LoadNextScene();
        }
    }
}