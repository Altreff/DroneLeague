using UnityEngine;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    public CanvasGroup settingsPanel;
    public float fadeDuration = 0.3f;

    void Start()
    {
        settingsPanel.alpha = 0f;
        settingsPanel.interactable = false;
        settingsPanel.blocksRaycasts = false;
    }

    public void OpenSettings()
    {
        StartCoroutine(FadeCanvasGroup(settingsPanel, settingsPanel.alpha, 1f));
    }

    public void CloseSettings()
    {
        StartCoroutine(FadeCanvasGroup(settingsPanel, settingsPanel.alpha, 0f));
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            cg.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        cg.alpha = end;
        cg.interactable = end > 0f;
        cg.blocksRaycasts = end > 0f;
    }
}
