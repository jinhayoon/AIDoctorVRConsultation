using UnityEngine;
using UnityEngine.UI;


public class ButtonController : MonoBehaviour
{
    public Button button1;
    public Button button2;
    public Button button3;
    public GameObject Panel1;
    public GameObject Panel2;
    public string sceneName; 
    public SceneController sceneController;

    void Start()
    {
        Panel1.gameObject.SetActive(true);
        button1.gameObject.SetActive(true);
        button2.gameObject.SetActive(false);
        Panel2.gameObject.SetActive(false);

        button1.onClick.AddListener(OnButton1Click);
        button2.onClick.AddListener(OnButton2Click);
        button3.onClick.AddListener(OnButton3Click);
    }

    void OnButton1Click()
    {
        button1.gameObject.SetActive(false);
        button2.gameObject.SetActive(true);
    }

    void OnButton2Click()
    {
        Panel1.gameObject.SetActive(false);
        button2.gameObject.SetActive(false);
        Panel2.gameObject.SetActive(true);
        button3.gameObject.SetActive(true);
    }

    bool Continuing = false;
    void OnButton3Click()
    {
        if (Continuing)
            return;
        sceneController.LoadScene(sceneName);
        Continuing = true;
    }
}
