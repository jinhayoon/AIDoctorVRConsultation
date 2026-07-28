using UnityEngine;

public class LogPPTID : MonoBehaviour
{
    public static string PID { get; private set; }

    public void ReadInput(string ppt)
    {
        PID = ppt;
        Debug.Log($"PID set to: {PID}");
    }
}
