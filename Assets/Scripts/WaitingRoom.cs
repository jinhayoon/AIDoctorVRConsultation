using AIDoctor.Flow;
using UnityEngine;
using UnityEngine.UI;


public class WaitingRoom : MonoBehaviour
{

    public GameObject text1;
    public GameObject text2;
    public GameObject text3;
    public string experimentCondition;

    private AudioSource audio1;
    private AudioSource audio2;


    private bool canContinue = false;
    private bool continuing = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        experimentCondition = PlayerPrefs.GetString("condition"); //load condition retrieved from PlayerPrefs set in SetCondition.cs

        audio1 = text1.GetComponent<AudioSource>();
        audio2 = text2.GetComponent<AudioSource>();

        text1.gameObject.SetActive(true);
        text2.gameObject.SetActive(false);
        text3.gameObject.SetActive(false);

        audio1.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (!audio1.isPlaying && text1.activeSelf)
        {
            text1.SetActive(false);
            text2.SetActive(true);
            text3.SetActive(false);
            audio2.Play();
        }

        if (!audio2.isPlaying && text2.activeSelf && !canContinue)
        {
            text2.gameObject.SetActive(false);
            text3.SetActive(true);
            canContinue = true;
            Debug.Log("Press ENTER to continue...");
        }

        // Listen for Return key
        if (canContinue && !continuing && Input.GetKeyDown(KeyCode.Return))
        {
            ContinueToNextScene();
        }
    }

    void ContinueToNextScene()
    {
        if (continuing) return;

        continuing = true;
        Debug.Log("Enter pressed. Advancing scene flow");

        //SceneFlowManager.Instance.LoadNextScene();

        if (AIDoctor.Flow.SceneFlowManager.Instance != null)
        {
            AIDoctor.Flow.SceneFlowManager.Instance.LoadNextScene();
        }
    }
}
