using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Scale")]
    public float hoverMultiplier = 1.05f;
    public float pressMultiplier = 0.95f;
    public float speed = 12f;

    [Header("Colors")]
    public Color normalColor = new Color(0.15f, 0.08f, 0.25f);
    public Color hoverColor = new Color(0.45f, 0.25f, 0.9f);

    [Header("Outline / Glow")]
    public float glowAlpha = 0.6f;           // базовый при hover
    public float pressedGlowAlpha = 0.9f;    // при клике
    public float pulseSpeed = 3f;            // скорость пульсации
    public float pulseAmount = 0.15f;        // амплитуда пульсации

    [Header("Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    RectTransform rect;
    Image img;
    AudioSource audioSource;
    Outline outline;
    Shadow glow;

    Vector3 baseScale;
    Vector3 targetScale;
    Color targetColor;

    float pulse;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        outline = GetComponent<Outline>();
        glow = GetComponent<Shadow>();

        baseScale = rect.localScale;
        targetScale = baseScale;
        targetColor = normalColor;
        img.color = normalColor;

        if (outline) outline.enabled = false;
        if (glow) glow.enabled = false;
    }

    void Update()
    {
        // Плавный scale и цвет кнопки
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.deltaTime * speed);
        img.color = Color.Lerp(img.color, targetColor, Time.deltaTime * speed);

        // Пульсация glow
        if (glow && glow.enabled)
        {
            pulse += Time.deltaTime * pulseSpeed;
            float alpha = glowAlpha + Mathf.Sin(pulse) * pulseAmount;
            var c = glow.effectColor;
            glow.effectColor = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }
        else if (glow)
        {
            var c = glow.effectColor;
            glow.effectColor = new Color(c.r, c.g, c.b, 0f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverMultiplier;
        targetColor = hoverColor;

        if (outline) outline.enabled = true;
        if (glow) glow.enabled = true;

        if (hoverSound)
            audioSource.PlayOneShot(hoverSound, 0.6f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
        targetColor = normalColor;

        if (outline) outline.enabled = false;
        if (glow) glow.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = baseScale * pressMultiplier;

        if (glow)
        {
            var c = glow.effectColor;
            glow.effectColor = new Color(c.r, c.g, c.b, pressedGlowAlpha);
        }

        if (clickSound)
            audioSource.PlayOneShot(clickSound, 0.8f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = baseScale * hoverMultiplier;
    }
}
