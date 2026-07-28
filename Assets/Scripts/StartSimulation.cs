using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIDoctor.Flow;


public class StartSimulation : MonoBehaviour
{
    
    private SceneController sceneController;
    public string sceneName;
    public AudioSource firstAudioSource;
    public AudioSource secondAudioSource; 
    public GameObject firstGameObject;
    public GameObject secondGameObject;
    public GameObject bufferGameObject;
    public Button ContinueButton;
    public Button YesButton;
    public Button NoButton;
    public GameObject FirstPanel;
    public GameObject SecondPanel;

    public RecordScene recordScene;

    private bool secondSequenceStarted = false;
    private bool consentChosen = false;

    void Start()
    {
        if (sceneController == null)
        {
            sceneController = Object.FindFirstObjectByType<SceneController>();
        }
            

        ContinueButton.gameObject.SetActive(false); // Hide the button initially
        secondGameObject.SetActive(false); 
        YesButton.gameObject.SetActive(false);
        NoButton.gameObject.SetActive(false);
        bufferGameObject.SetActive(false);

        StartCoroutine(StartSceneAfterDelay());
        StartCoroutine(ShowButtonBeforeEnd(45.0f));
    }

    private void Update()
    {
        if (!secondSequenceStarted && ContinueButton.gameObject.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerContinue();
            }
        }

        if (!consentChosen && (YesButton != null && YesButton.gameObject.activeInHierarchy))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TriggerYes();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TriggerNo();
            }
        }
    }

    void PlayFirstAudio()
    {
        firstAudioSource.Play();
    }


    public IEnumerator StartSceneAfterDelay()
    {
        firstGameObject.SetActive(true); // Start the first GameObject
        yield return new WaitForSeconds(0.1f);
        PlayFirstAudio(); // Play the first audio
    }

    public IEnumerator ShowButtonBeforeEnd(float delay)
    {
        yield return new WaitForSeconds(delay);
        ContinueButton.gameObject.SetActive(true); // Show the button before the first GameObject ends

        ContinueButton.onClick.RemoveAllListeners();
        ContinueButton.onClick.AddListener(TriggerContinue);

    }


    private IEnumerator SecondSequence()
    {
        secondSequenceStarted = true;

        ContinueButton.gameObject.SetActive(false); // Hide the button after clicking
        FirstPanel.SetActive(false); 
        SecondPanel.SetActive(true); 

        firstGameObject.SetActive(false); // Stop the first GameObject
        secondGameObject.SetActive(true); // Start the second GameObject
        secondAudioSource.Play();
        yield return new WaitForSeconds(secondAudioSource.clip.length - .1f);

        secondGameObject.SetActive(false); 
        bufferGameObject.SetActive(true); //need to start playing sooner

        YesButton.gameObject.SetActive(true);
        NoButton.gameObject.SetActive(true);

        YesButton.onClick.RemoveAllListeners();
        NoButton.onClick.RemoveAllListeners();

        YesButton.onClick.AddListener(TriggerYes);
        NoButton.onClick.AddListener(TriggerNo);
    }

    bool Continuing = false;
    void FadeOut()
    {
        if (Continuing)
            return;
        Continuing = true;
        //sceneController.LoadScene(sceneName);

        var manager = AIDoctor.Flow.SceneFlowManager.Instance;
        if (manager != null)
        {
            // If the flow hasn't been initialized (no scenes), load by name directly.
            if (manager.TotalScenes == 0)
            {
                Debug.LogWarning("[StartSimulation] SceneFlowManager flow not initialized — loading scene directly by name");
                manager.LoadSceneByName(sceneName);
            }
            else if (manager.HasNextScene())
            {
                manager.LoadNextScene();
            }
            else
            {
                // No next scene in flow — load target by name as a fallback
                manager.LoadSceneByName(sceneName);
            }
        }
        else
        {
            if (sceneController != null)
                sceneController.LoadScene(sceneName);
            else
                Debug.LogError("[StartSimulation] No SceneController available to load scene: " + sceneName);
        }
    }

    /*private IEnumerator FadeToBlackAndLoadScene()
    {
        yield return sceneController.GetComponent<SceneFade>().FadeOutCoroutine(fadeDuration);
        sceneController.LoadScene("SceneEnd");
    }*/

    private void TriggerContinue()
    {
        if (secondSequenceStarted)
            return;

        secondSequenceStarted = true;
        if (recordScene != null && ContinueButton != null)
            recordScene.ButtonClicked(ContinueButton);

        StartCoroutine(SecondSequence());
    }

    private void TriggerYes()
    {
        if (consentChosen)
            return;

        consentChosen = true;
        if (recordScene != null && YesButton != null)
            recordScene.ButtonClicked(YesButton);

        FadeOut();
    }

    private void TriggerNo()
    {
        if (consentChosen)
            return;

        consentChosen = true;
        if (recordScene != null && NoButton != null)
            recordScene.ButtonClicked(NoButton);

        FadeOut();
    }

}