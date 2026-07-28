using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseDevice : MonoBehaviour
{
    internal bool recording = true;

    internal abstract string FileHeader();

    internal abstract string GetData();

    public abstract string DeviceName();

    public bool IsConnected, IsStreaming;
}
