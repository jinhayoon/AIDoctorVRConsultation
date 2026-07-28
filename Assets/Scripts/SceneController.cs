using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

//code written by following a YouTube tutorial by Ketra Games https://youtu.be/vkOhefMbrFg?si=6OJ3os9gyjBtNftt

public class SceneController : MonoBehaviour
{
    [SerializeField]
    private float _sceneFadeDuration;

    private SceneFade _sceneFade;

    private void Awake()
    {
        _sceneFade = GetComponentInChildren<SceneFade>();
    }

    private IEnumerator Start()
    {
        yield return _sceneFade.FadeInCoroutine(_sceneFadeDuration);
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log("SceneController is trying to load: " + sceneName);
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;
        

        while (load.progress < 0.9f)
        {
            yield return null;
        }


        yield return _sceneFade.FadeOutCoroutine(_sceneFadeDuration);


        load.allowSceneActivation = true;
    }
}
