using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup settingsPanel;
    public float fadeDuration = 0.3f;

    public TMP_Dropdown difficultyDropdown;
    public Slider musicSlider;
    public Slider sfxSlider;

    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioMixer audioMixer;

    const string PREF_DIFFICULTY = "ai_difficulty";
    const string PREF_MUSIC = "music_volume";
    const string PREF_SFX = "sfx_volume";

    void Start()
    {
        // PANEL
        settingsPanel.alpha = 0f;
        settingsPanel.interactable = false;
        settingsPanel.blocksRaycasts = false;

        LoadSettings();
        BindUI();
    }

    // ---------------- UI ----------------

    void BindUI()
    {
        difficultyDropdown.onValueChanged.AddListener(SetDifficulty);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void OpenSettings()
    {
        StartCoroutine(FadeCanvasGroup(settingsPanel, settingsPanel.alpha, 1f));
    }

    public void CloseSettings()
    {
        StartCoroutine(FadeCanvasGroup(settingsPanel, settingsPanel.alpha, 0f));
        PlayerPrefs.Save(); // 💾 важно
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

    // ---------------- SETTINGS ----------------

    void LoadSettings()
    {
        int difficulty = PlayerPrefs.GetInt(PREF_DIFFICULTY, (int)AIDifficulty.Medium);
        float music = PlayerPrefs.GetFloat(PREF_MUSIC, 50f);
        float sfx = PlayerPrefs.GetFloat(PREF_SFX, 50f);

        difficultyDropdown.value = difficulty;
        musicSlider.value = music;
        sfxSlider.value = sfx;

        ApplyMusic(music);
        ApplySFX(sfx);

        UpdateText(musicValueText, music);
        UpdateText(sfxValueText, sfx);
    }

    public void SetDifficulty(int index)
    {
        PlayerPrefs.SetInt(PREF_DIFFICULTY, index);
    }

    public void SetMusicVolume(float percent)
    {
        ApplyMusic(percent);
        PlayerPrefs.SetFloat(PREF_MUSIC, percent);
        UpdateText(musicValueText, percent);
    }

    public void SetSFXVolume(float percent)
    {
        ApplySFX(percent);
        PlayerPrefs.SetFloat(PREF_SFX, percent);
        UpdateText(sfxValueText, percent);
    }

    // ---------------- APPLY ----------------

    void ApplyMusic(float percent)
    {
        float volume = Mathf.Lerp(0f, 0.02f, percent / 100f);
        musicSource.volume = volume;
    }

    void ApplySFX(float percent)
    {
        float db = Mathf.Log10(Mathf.Max(percent / 100f, 0.001f)) * 20f;
        audioMixer.SetFloat("SFXSounds", db);
    }

    void UpdateText(TMP_Text txt, float value)
    {
        if (txt != null)
            txt.text = $"{Mathf.RoundToInt(value)}%";
    }
}
