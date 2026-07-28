using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PannelHider : MonoBehaviour
{
    [SerializeField]
    private GameObject pannelToHide;
    private Button _btn;
    private TMP_Text _btnText;

    private void Start()
    {
        _btn = GetComponentInChildren<Button>();
        _btnText = _btn.gameObject.GetComponentInChildren<TMP_Text>();
    }

    public void DoHide()
    {
        pannelToHide.SetActive(!pannelToHide.activeInHierarchy);

        _btnText.text = pannelToHide.activeInHierarchy ? "Hide" : "Show";
    }
}
 