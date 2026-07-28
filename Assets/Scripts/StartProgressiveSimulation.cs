using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;


public class StartProgressiveSimulation : MonoBehaviour
{
    public SceneController sceneController;
    public string sceneName;

    public AudioSource introAudioSource;
    public AudioSource controlAudioSource;
    public AudioSource explainMedicalAudioSource;
    public AudioSource explainMedicalDecisionAudioSource;
    public AudioSource treatmentAudioSource;
    public AudioSource explainTreatmentAudioSource;
    public AudioSource explainTreatmentDecisionAudioSource;
    public AudioSource consentAudioSource;

    public GameObject intro;
    public GameObject dataCollectionAndDiagnosis;
    public GameObject bufferGameObject;
    public GameObject explainMedical;
    public GameObject explainMedicalDecision;
    public GameObject treatmentGameObject;
    public GameObject explainTreatment;
    public GameObject explainTreatmentDecision;
    public GameObject consentGameObject;


    //UI params
    public Button firstContinueButton;
    public Button secondContinueButton;
    public Button explainMedicalButton;
    public Button explainMedicalDecisionButton;
    public Button explainTreatmentButton;
    public Button explainTreatmentDecisionButton;
    public Button thirdContinueButton; 

    public Button YesButton;
    public Button NoButton;

    public GameObject FirstPanel; // data privacy intro scene
    public GameObject SecondPanel; //data collection + diagnosis scene
    public GameObject ThirdPanel; //treatment scene
    public GameObject FourthPanel; //consent scene


    public RecordScene recordScene;

    //public float sceneDelay = 20f;
    //public float audioDelay = 80f; // Time in seconds to delay
    //public float buttonDelay = 5f;
    //blic float secondGameObjectDuration = 124f;c:\Users\Jeff\AI Tele 2\Assets\Scripts\StartProgressiveSimulation.cs
    //public float fadeDuration = 3.0f;  

    void Start()
    {
        firstContinueButton.gameObject.SetActive(false);

        secondContinueButton.gameObject.SetActive(false);
        explainMedicalButton.gameObject.SetActive(false);
        explainMedicalDecisionButton.gameObject.SetActive(false);

        explainTreatmentButton.gameObject.SetActive(false);
        explainTreatmentDecisionButton.gameObject.SetActive(false);
        thirdContinueButton.gameObject.SetActive(false);

        YesButton.gameObject.SetActive(false);
        NoButton.gameObject.SetActive(false);

        FirstPanel.gameObject.SetActive(false);
        SecondPanel.gameObject.SetActive(false);
        ThirdPanel.gameObject.SetActive(false);


        StartCoroutine(PlayIntroSequence());
    }


    private IEnumerator PlayIntroSequence()
    {
        intro.SetActive(true);
        introAudioSource.Play();
        yield return new WaitForSeconds(introAudioSource.clip.length - 3f);

        FirstPanel.SetActive(true);
        firstContinueButton.gameObject.SetActive(true);
        firstContinueButton.onClick.AddListener(() => {
            if (recordScene != null)
                recordScene.ButtonClicked(firstContinueButton);
            StartCoroutine(PlayDataCollectionSequence());
        });
    }

    private IEnumerator PlayDataCollectionSequence()
    {
        intro.SetActive(false);
        FirstPanel.SetActive(false);
        firstContinueButton.gameObject.SetActive(false);

        dataCollectionAndDiagnosis.SetActive(true);
        controlAudioSource.Play();

        yield return new WaitForSeconds(controlAudioSource.clip.length);


        // Now deactivate the audio/scene objects
        dataCollectionAndDiagnosis.SetActive(false);

        // Show buttons and panel while audio is still playing
        bufferGameObject.SetActive(true);
        SecondPanel.SetActive(true);
        ActivateSecondPanelButtons();

    }

    private void ActivateSecondPanelButtons()
    {
        // Remove all previous listeners to prevent stacking
        secondContinueButton.onClick.RemoveAllListeners();
        explainMedicalButton.onClick.RemoveAllListeners();
        explainMedicalDecisionButton.onClick.RemoveAllListeners();
        
        secondContinueButton.gameObject.SetActive(true);
        explainMedicalButton.gameObject.SetActive(true);
        explainMedicalDecisionButton.gameObject.SetActive(true);

        explainMedicalButton.onClick.AddListener(() => {
            if (recordScene != null)
                recordScene.ButtonClicked(explainMedicalButton);
            StartCoroutine(PlayExplainMedicalSequence());
        });

        explainMedicalDecisionButton.onClick.AddListener(() =>
        {
            if (recordScene != null)
                recordScene.ButtonClicked(explainMedicalDecisionButton);
            StartCoroutine(PlayExplainMedicalDecisionSequence());
        });

        secondContinueButton.onClick.AddListener(() =>
        {
            if (recordScene != null)
                recordScene.ButtonClicked(secondContinueButton);
            StartCoroutine(PlayTreatmentSequence());
        });
    }

    private IEnumerator PlayExplainMedicalSequence()
    {
        SecondPanel.SetActive(false);
        bufferGameObject.SetActive(false);
        explainMedical.SetActive(true);
        explainMedicalAudioSource.Play();
        yield return new WaitWhile(() => explainMedicalAudioSource.isPlaying);

        explainMedical.SetActive(false);
        bufferGameObject.SetActive(true);
        SecondPanel.SetActive(true);
    }

    private IEnumerator PlayExplainMedicalDecisionSequence()
    {
        SecondPanel.SetActive(false);
        bufferGameObject.SetActive(false);
        explainMedicalDecision.SetActive(true);
        explainMedicalDecisionAudioSource.Play();
        yield return new WaitWhile(() => explainMedicalDecisionAudioSource.isPlaying);


        explainMedicalDecision.SetActive(false);
        bufferGameObject.SetActive(true);
        SecondPanel.SetActive(true);
    }


    private IEnumerator PlayTreatmentSequence()
    {
        SecondPanel.SetActive(false);
        bufferGameObject.SetActive(false);

        treatmentGameObject.SetActive(true);
        treatmentAudioSource.Play();
        yield return new WaitForSeconds(treatmentAudioSource.clip.length - .05f);
        
        treatmentGameObject.SetActive(false);
        bufferGameObject.SetActive(true);
        ThirdPanel.SetActive(true);

        ActivateThirdPanelButtons();
    }

    private void ActivateThirdPanelButtons()
    {
        explainTreatmentButton.onClick.RemoveAllListeners();
        explainTreatmentDecisionButton.onClick.RemoveAllListeners();
        thirdContinueButton.onClick.RemoveAllListeners();

        explainTreatmentButton.gameObject.SetActive(true);
        explainTreatmentDecisionButton.gameObject.SetActive(true);
        thirdContinueButton.gameObject.SetActive(true);

        explainTreatmentButton.onClick.AddListener(() =>
        {
            if (recordScene != null)
                recordScene.ButtonClicked(explainTreatmentButton);
            StartCoroutine(PlayExplainTreatmentSequence());
        });
        explainTreatmentDecisionButton.onClick.AddListener(() =>
        {
            if (recordScene != null)
                recordScene.ButtonClicked(explainTreatmentDecisionButton);
            StartCoroutine(PlayTreatmentDecisionSequence());
        });
        thirdContinueButton.onClick.AddListener(() =>
        {
            if (recordScene != null)
                recordScene.ButtonClicked(thirdContinueButton);
            StartCoroutine(PlayConsentSequence());
        });
    }

    private IEnumerator PlayExplainTreatmentSequence()
    {
        ThirdPanel.SetActive(false);
        bufferGameObject.SetActive(false);
        explainTreatment.SetActive(true);
        explainTreatmentAudioSource.Play();
        yield return new WaitWhile(() => explainTreatmentAudioSource.isPlaying);
        
        explainTreatment.SetActive(false);
        bufferGameObject.SetActive(true);
        ThirdPanel.SetActive(true);
        ActivateThirdPanelButtons(); 
    }

    private IEnumerator PlayTreatmentDecisionSequence()
    {
        ThirdPanel.SetActive(false);
        bufferGameObject.SetActive(false);
        explainTreatmentDecision.SetActive(true);
        explainTreatmentDecisionAudioSource.Play();
        yield return new WaitWhile(() => explainTreatmentDecisionAudioSource.isPlaying);

        explainTreatmentDecision.SetActive(false);
        bufferGameObject.SetActive(true);
        ThirdPanel.SetActive(true);
        ActivateThirdPanelButtons();
    }

    private IEnumerator PlayConsentSequence()
    {
        ThirdPanel.SetActive(false); 
        bufferGameObject.SetActive(false);

        consentGameObject.SetActive(true);
        consentAudioSource.Play();
        yield return new WaitForSeconds(consentAudioSource.clip.length); 

        consentGameObject.SetActive(false);
        bufferGameObject.SetActive(true);
        FourthPanel.SetActive(true);

        YesButton.gameObject.SetActive(true);
        NoButton.gameObject.SetActive(true);

        YesButton.onClick.AddListener(() => {
            if (recordScene != null)
                recordScene.ButtonClicked(YesButton);
            FadeOut();
        });
        NoButton.onClick.AddListener(() => {
            if (recordScene != null)
                recordScene.ButtonClicked(NoButton);
            FadeOut();
        });
    }

    void FadeOut()
    {
        sceneController.LoadScene(sceneName);
    }



    /*private IEnumerator FadeToBlackAndLoadScene()
    {
        yield return sceneController.GetComponent<SceneFade>().FadeOutCoroutine(fadeDuration); // Adjust duration as needed
        sceneController.LoadScene(sceneName);
    }*/

}