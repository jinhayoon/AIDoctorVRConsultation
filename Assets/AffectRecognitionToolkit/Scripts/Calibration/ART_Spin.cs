using UnityEngine;

public class ART_Spin : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(20 * Time.deltaTime, 12 * Time.deltaTime, 30 * Time.deltaTime);
    }
}
