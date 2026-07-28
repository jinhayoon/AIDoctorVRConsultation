using UnityEngine;
using AIDoctor.Flow;

public class SkinSwitcher : MonoBehaviour
{
    public SkinnedMeshRenderer leftHand, rightHand;
    public Material[] SkinMaterials;
    public string nextSceneName; // Set this in the Inspector

    private int idx = 0;

    private float inputDelayTimer = 0.5f;
    private bool canAcceptInput = false;

    void Start()
    {
        idx = PlayerPrefs.GetInt("SelectedSkinTone", 0);
        Debug.Log("Loaded SelectedSkinTone idx: " + idx);
        UpdateHandMaterials();

        inputDelayTimer = 0.5f;
        canAcceptInput = false;

        Debug.Log("[SkinSwitcher] Scene loaded");
        Debug.Log($"[SkinSwitcher] SceneFlowManager index: {SceneFlowManager.Instance?.CurrentSceneIndex}");
        Debug.Log($"[SkinSwitcher] SceneFlowManager scene: {SceneFlowManager.Instance?.CurrentSceneName}");
    }

    void Update()
    {
        if (!canAcceptInput)
        {
            inputDelayTimer -= Time.deltaTime;
            if (inputDelayTimer <= 0)
            {
                canAcceptInput = true;
                Debug.Log("[SkinSwitcher] now accepting input.");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            idx = (idx + 1) % SkinMaterials.Length;
            UpdateHandMaterials();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            idx = (idx - 1 + SkinMaterials.Length) % SkinMaterials.Length;
            UpdateHandMaterials();
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("===========================================");
            Debug.Log("[SkinSwitcher] Return key pressed!");
            Debug.Log($"[SkinSwitcher] Saving SelectedSkinTone idx: {idx}");

            PlayerPrefs.SetInt("SelectedSkinTone", idx);
            PlayerPrefs.SetInt("RashSkinTone", idx);
            PlayerPrefs.Save();
            //UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            //SceneFlowManager.Instance.LoadNextScene();

            Debug.Log($"[SkinSwitcher] Current index BEFORE: {SceneFlowManager.Instance?.CurrentSceneIndex}");
            SceneFlowManager.Instance.LoadNextScene();
        }
    }

    private void UpdateHandMaterials()
    {
        if (leftHand != null && SkinMaterials != null && idx < SkinMaterials.Length)
            leftHand.material = SkinMaterials[idx];
        if (rightHand != null && SkinMaterials != null && idx < SkinMaterials.Length)
            rightHand.material = SkinMaterials[idx];
    }
}
