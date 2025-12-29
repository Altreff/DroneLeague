using UnityEngine;
using TMPro; 

public class UITextPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.2f;
    public Color normalColor = Color.white;
    public Color pulseColor = new Color(0.8f, 0.3f, 1f);

    TMP_Text txt; 
    float pulse;

    void Awake()
    {
        txt = GetComponent<TMP_Text>();
        if (txt == null)
        {
            Debug.LogError("UITextPulse: TMP_Text component not found on " + gameObject.name);
            return;
        }

        txt.color = normalColor;
    }

    void Update()
    {
        if (txt == null) return;

        pulse += Time.deltaTime * pulseSpeed;
        float t = Mathf.Sin(pulse) * pulseAmount; 
        txt.color = Color.Lerp(normalColor, pulseColor, Mathf.Abs(t));
    }

    public void HoverEnter()
    {
        pulseSpeed = 4f;
        pulseAmount = 0.3f;
    }

    public void HoverExit()
    {
        pulseSpeed = 2f;
        pulseAmount = 0.2f;
    }
}
