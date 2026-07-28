using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ShowButton : MonoBehaviour
{
    public Button button;

    // Start is called before the first frame update
    void Start()
    {
        button.gameObject.SetActive(false); // Hide the button initially
        StartCoroutine(ShowButtonAfterDelay(48f)); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator ShowButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        button.gameObject.SetActive(true); // Show the button after the delay
        button.onClick.AddListener(OnButtonClick); // Add a listener to the button
    } 

    void OnButtonClick()
    {
        SceneManager.LoadScene("Scene2"); // Load the next scene
    }
}
