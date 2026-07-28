using UnityEngine;

public class RashSwitcher : MonoBehaviour
{
    public SkinnedMeshRenderer leftHand, rightHand;
    public Material[] LeftRashMaterials;
    public Material[] RightRashMaterials;

    void Start()
    {
        int rashIdx = PlayerPrefs.GetInt("RashSkinTone", 0);
        Debug.Log("Loaded RashSkinTone idx: " + rashIdx);

        if (LeftRashMaterials != null && LeftRashMaterials.Length > 0)
        {
            rashIdx = Mathf.Clamp(rashIdx, 0, LeftRashMaterials.Length - 1);
            leftHand.material = LeftRashMaterials[rashIdx];
        }

        if (RightRashMaterials != null && RightRashMaterials.Length > 0)
        {
            rashIdx = Mathf.Clamp(rashIdx, 0, RightRashMaterials.Length - 1);
            rightHand.material = RightRashMaterials[rashIdx];
        }
    }
}