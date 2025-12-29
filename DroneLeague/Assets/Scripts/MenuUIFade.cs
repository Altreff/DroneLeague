using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuUIFade : MonoBehaviour
{
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.6f;

    CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        FadeFromBlack();
    }

    public void FadeToBlack()
    {
        StartCoroutine(Fade(1f, fadeOutDuration));
    }

    public void FadeFromBlack()
    {
        StartCoroutine(Fade(0f, fadeInDuration));
    }

    IEnumerator Fade(float target, float duration)
    {
        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            canvasGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        canvasGroup.alpha = target;
    }

    // Новый метод для PlayGame с задержкой
    public void PlayGame(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        // Сначала fade
        yield return StartCoroutine(Fade(1f, fadeOutDuration));

        // Можно добавить дополнительную задержку, если нужно
        // yield return new WaitForSeconds(0.2f);

        // Загружаем сцену
        SceneManager.LoadScene(sceneName);
    }
}
