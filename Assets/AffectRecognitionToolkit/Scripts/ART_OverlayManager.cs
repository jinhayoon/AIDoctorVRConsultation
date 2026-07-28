using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ART_OverlayManager : MonoBehaviour
{
    public GameObject PhysDeviceConnection;
    public GameObject PhysData;
    public GameObject EmotionData;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))        
            PhysDeviceConnection.SetActive(!PhysDeviceConnection.activeInHierarchy);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            PhysData.SetActive(!PhysData.activeInHierarchy);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            EmotionData.SetActive(!EmotionData.activeInHierarchy);

    }
}
