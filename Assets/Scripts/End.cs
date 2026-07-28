using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class End : MonoBehaviour
{
    public float waitTime = 4;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Wait());
    }

    public IEnumerator Wait()
    {
        float timer = 0;
        while (timer <= waitTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
