using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MenuButtonSlideX : MonoBehaviour
{
    public float delay = 0f;
    public float duration = 0.45f;
    public float offscreenOffset = 300f; 

    CanvasGroup canvasGroup;
    RectTransform rect;
    Vector2 targetPos;
    Vector2 startPos;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = GetComponent<RectTransform>();

        targetPos = rect.anchoredPosition;
        startPos = targetPos + Vector2.left * offscreenOffset;

        rect.anchoredPosition = startPos;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void OnEnable()
    {
        StartCoroutine(AnimateIn());
    }

    IEnumerator AnimateIn()
    {
        yield return new WaitForSeconds(delay);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float ease = Mathf.SmoothStep(0, 1, t);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
            canvasGroup.alpha = ease;

            yield return null;
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}
