using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FakeAOITagger : BaseDevice
{
    [SerializeField]
    private LayerMask _mask;

    [SerializeField]
    private string _lastTagged = "";

    // Update is called once per frame
    void FixedUpdate()
    {
        _lastTagged = DoTagging();
    }

    private RaycastHit[] _hits = new RaycastHit[10];

    private string DoTagging()
    {
        var ray = Camera.main.ScreenPointToRay(new Vector2(Screen.height / 2, Screen.width / 2));

        Debug.DrawRay(ray.origin, ray.direction*10.0f, Color.green);

        int count = Physics.RaycastNonAlloc(ray, _hits, Mathf.Infinity, _mask);

        _hits = _hits.OrderBy(x => x.distance).ToArray();

        for (int i = 0; i < count; i++)
        {
            Debug.Log($"Hit:   {_hits[i].transform.gameObject.tag}");
        }

        if (count > 0)
            return _hits[0].transform.gameObject.tag;

        return "Nothing In Focus";
    }

    internal override string FileHeader()
    {
        return ",AIOTagged";
    }

    internal override string GetData()
    {
        return $",{_lastTagged}";
    }

    public override string DeviceName()
    {
        return "AIO Tagger";
    }
}
