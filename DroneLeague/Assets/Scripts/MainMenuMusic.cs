using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MainMenuMusic : MonoBehaviour
{
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 1.0f;
    public float targetVolume = 0.35f;

    AudioSource source;
    Coroutine fadeRoutine;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.volume = 0f;
        source.loop = true;
    }

    void Start()
    {
        source.Play();
        fadeRoutine = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            source.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
    }

    public void FadeOut()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        float startVolume = source.volume;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutDuration;
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        source.Stop();
    }
}
