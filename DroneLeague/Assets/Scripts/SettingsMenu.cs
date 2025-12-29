using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup settingsPanel;
    public float fadeDuration = 0.3f;

    [Header("Difficulty")]
    public TMP_Dropdown difficultyDropdown;

    [Header("Audio Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Slider Value Text")]
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioMixer audioMixer; // параметр SFXSounds должен быть Exposed

    void Start()
    {
        // --- Panel init ---
        settingsPanel.alpha = 0f;
        settingsPanel.interactable = false;
        settingsPanel.blocksRaycasts = false;

        // --- Difficulty Dropdown ---
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Easy",
                "Medium",
                "Hard"
            });

            difficultyDropdown.value = (int)AIDifficulty.Medium;
            difficultyDropdown.RefreshShownValue();
            difficultyDropdown.onValueChanged.AddListener(SetDifficulty);
        }

        // --- Music Slider ---
        if (musicSlider != null)
        {
            musicSlider.minValue = 0;
            musicSlider.maxValue = 100;
            musicSlider.value = 50;
            UpdateMusicText(50);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        // --- SFX Slider ---
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0;
            sfxSlider.maxValue = 100;
            sfxSlider.value = 50;
            UpdateSFXText(50);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    // ================= PANEL =================

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

    // ================= SETTINGS =================

    public void SetDifficulty(int index)
    {
        AIDifficulty difficulty = (AIDifficulty)index;
        Debug.Log("Difficulty set to: " + difficulty);

        // Позже:
        // GameManager.SelectedDifficulty = difficulty;
    }

    public void SetMusicVolume(float percent)
    {
        UpdateMusicText(percent);

        // 50% = 0.15 → 100% = 0.3
        float volume = Mathf.Lerp(0f, 0.02f, percent / 100f);
        if (musicSource != null)
            musicSource.volume = volume;
    }

    public void SetSFXVolume(float percent)
    {
        UpdateSFXText(percent);

        // AudioMixer работает в децибелах
        float db = Mathf.Log10(Mathf.Max(percent / 100f, 0.001f)) * 20f;
        if (audioMixer != null)
            audioMixer.SetFloat("SFXSounds", db);
    }

    // ================= UI TEXT =================

    void UpdateMusicText(float value)
    {
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(value) + "%";
    }

    void UpdateSFXText(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value) + "%";
    }
}
