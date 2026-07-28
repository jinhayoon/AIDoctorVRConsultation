using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ART_Visualiser : MonoBehaviour
{
    public ART_Framework esf;
    public TextMeshProUGUI valence;
    public TextMeshProUGUI arousal;
    public TextMeshProUGUI fear;
    public TextMeshProUGUI stress;
    public TextMeshProUGUI happy;
    public TextMeshProUGUI sad;
    public TextMeshProUGUI bored;
    public TextMeshProUGUI excited;
    public TextMeshProUGUI content;
    public TextMeshProUGUI calm;

    public RectTransform valence_boxplot;
    public RectTransform arousal_boxplot;
    public RectTransform fear_boxplot;
    public RectTransform stress_boxplot;
    public RectTransform happy_boxplot;        
    public RectTransform sad_boxplot;
    public RectTransform bored_boxplot;
    public RectTransform excited_boxplot;
    public RectTransform content_boxplot;
    public RectTransform calm_boxplot;

    public RectTransform valence_marker;
    public RectTransform arousal_marker;
    public RectTransform fear_marker;
    public RectTransform stress_marker;
    public RectTransform happy_marker;
    public RectTransform sad_marker;
    public RectTransform bored_marker;
    public RectTransform excited_marker;
    public RectTransform content_marker;
    public RectTransform calm_marker;

    private float scaleImageMinX;
    private float scaleImageMaxX;
    private float scaleImageMaxX_Marker;

    // Start is called before the first frame update
    void Start()
    {
        scaleImageMinX = 257;
        scaleImageMaxX = 720;
        scaleImageMaxX_Marker = scaleImageMaxX;
    }

    // Update is called once per frame
    void Update()
    {
        valence.text = "Valence: " + esf.ValenceScore.ToString("F2");
        arousal.text = "Arousal: " + esf.ArousalScore.ToString("F2");
        fear.text = "Fear: " + esf.FearScore.ToString("F2");
        stress.text = "Stress: " + esf.StressScore.ToString("F2");
        happy.text = "Happy: " + esf.HappyScore.ToString("F2");
        sad.text = "Sad: " + esf.SadScore.ToString("F2");
        bored.text = "Bored: " + esf.BoredScore.ToString("F2");
        excited.text = "Excited: " + esf.ExcitedScore.ToString("F2");
        content.text = "Content: " + esf.ContentScore.ToString("F2");
        calm.text = "Calm: " + esf.CalmScore.ToString("F2");

        float minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.ValenceCI_Lower / 10.0f);
        float maxWidth = scaleImageMaxX - minX;
        float width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.ValenceCI_Upper / 10.0f), 0, maxWidth);
        valence_boxplot.anchoredPosition = new Vector2(minX, valence_boxplot.anchoredPosition.y);
        valence_boxplot.sizeDelta = new Vector2(width, valence_boxplot.sizeDelta.y);
        float markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.ValenceScore / 10.0f);        
        valence_marker.anchoredPosition = new Vector2(markerX, valence_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.ArousalCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.ArousalCI_Upper / 10.0f), 0, maxWidth);
        arousal_boxplot.anchoredPosition = new Vector2(minX, arousal_boxplot.anchoredPosition.y);
        arousal_boxplot.sizeDelta = new Vector2(width, arousal_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.ArousalScore / 10.0f);
        arousal_marker.anchoredPosition = new Vector2(markerX, arousal_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.FearCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.FearCI_Upper / 10.0f), 0, maxWidth);
        fear_boxplot.anchoredPosition = new Vector2(minX, fear_boxplot.anchoredPosition.y);
        fear_boxplot.sizeDelta = new Vector2(width, fear_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.FearScore / 10.0f);
        fear_marker.anchoredPosition = new Vector2(markerX, fear_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.StressCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.StressCI_Upper / 10.0f), 0, maxWidth);
        stress_boxplot.anchoredPosition = new Vector2(minX, stress_boxplot.anchoredPosition.y);
        stress_boxplot.sizeDelta = new Vector2(width, stress_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.StressScore / 10.0f);
        stress_marker.anchoredPosition = new Vector2(markerX, stress_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.HappyCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.HappyCI_Upper / 10.0f), 0, maxWidth);
        happy_boxplot.anchoredPosition = new Vector2(minX, happy_boxplot.anchoredPosition.y);
        happy_boxplot.sizeDelta = new Vector2(width, happy_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.HappyScore / 10.0f);
        happy_marker.anchoredPosition = new Vector2(markerX, happy_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.SadCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.SadCI_Upper / 10.0f), 0, maxWidth);
        sad_boxplot.anchoredPosition = new Vector2(minX, sad_boxplot.anchoredPosition.y);
        sad_boxplot.sizeDelta = new Vector2(width, sad_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.SadScore / 10.0f);
        sad_marker.anchoredPosition = new Vector2(markerX, sad_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.ExcitedCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.ExcitedCI_Upper / 10.0f), 0, maxWidth);
        excited_boxplot.anchoredPosition = new Vector2(minX, excited_boxplot.anchoredPosition.y);
        excited_boxplot.sizeDelta = new Vector2(width, excited_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.ExcitedScore / 10.0f);
        excited_marker.anchoredPosition = new Vector2(markerX, excited_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.BoredCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.BoredCI_Upper / 10.0f), 0, maxWidth);
        bored_boxplot.anchoredPosition = new Vector2(minX, bored_boxplot.anchoredPosition.y);
        bored_boxplot.sizeDelta = new Vector2(width, bored_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.BoredScore / 10.0f);
        bored_marker.anchoredPosition = new Vector2(markerX, bored_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.ContentCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.ContentCI_Upper / 10.0f), 0, maxWidth);
        content_boxplot.anchoredPosition = new Vector2(minX, content_boxplot.anchoredPosition.y);
        content_boxplot.sizeDelta = new Vector2(width, content_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.ContentScore / 10.0f);
        content_marker.anchoredPosition = new Vector2(markerX, content_marker.anchoredPosition.y);

        minX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX, esf.CalmCI_Lower / 10.0f);
        maxWidth = scaleImageMaxX - minX;
        width = Mathf.Clamp(Mathf.Lerp(0, maxWidth, esf.CalmCI_Upper / 10.0f), 0, maxWidth);
        calm_boxplot.anchoredPosition = new Vector2(minX, calm_boxplot.anchoredPosition.y);
        calm_boxplot.sizeDelta = new Vector2(width, calm_boxplot.sizeDelta.y);
        markerX = Mathf.Lerp(scaleImageMinX, scaleImageMaxX_Marker, esf.CalmScore / 10.0f);
        calm_marker.anchoredPosition = new Vector2(markerX, calm_marker.anchoredPosition.y);
    }
}
