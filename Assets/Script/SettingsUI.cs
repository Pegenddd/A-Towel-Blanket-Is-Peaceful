using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Window Root")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;

    [Header("Volume Controls")]
    public Slider masterSlider;
    public TMP_Text masterValueText;
    public Slider bgmSlider;
    public TMP_Text bgmValueText;
    public Slider sfxSlider;
    public TMP_Text sfxValueText;

    [Header("Language Controls")]
    public Button englishButton;
    public Image englishButtonBg;
    public TMP_Text englishButtonText;
    public Button thaiButton;
    public Image thaiButtonBg;
    public TMP_Text thaiButtonText;

    [Header("Font Size Controls")]
    public Slider fontSizeSlider;
    public TMP_Text fontSizeValueText;

    public const string PREF_FONT_SIZE_SCALE = "Settings_FontSizeScale";
    public const float DEFAULT_FONT_SIZE_SCALE = 1.0f;
    public const float MIN_FONT_SIZE_SCALE = 0.75f;
    public const float MAX_FONT_SIZE_SCALE = 1.50f;

    public static bool IsOpen { get; private set; } = false;

    public static event Action<float> OnFontScaleChanged;

    public static float CurrentFontScale
    {
        get => PlayerPrefs.GetFloat(PREF_FONT_SIZE_SCALE, DEFAULT_FONT_SIZE_SCALE);
        set
        {
            float clamped = Mathf.Clamp(value, MIN_FONT_SIZE_SCALE, MAX_FONT_SIZE_SCALE);
            PlayerPrefs.SetFloat(PREF_FONT_SIZE_SCALE, clamped);
            PlayerPrefs.Save();
            OnFontScaleChanged?.Invoke(clamped);
        }
    }

    public static float GetScaledFontSize(float baseSize)
    {
        if (baseSize <= 0) return baseSize;
        return Mathf.Round(baseSize * CurrentFontScale);
    }

    [Header("Buttons")]
    public Button closeButton;

    public Action OnClosed;

    private readonly Color activeLangColor = new Color(0.24f, 0.45f, 0.72f, 1f);
    private readonly Color inactiveLangColor = new Color(0.12f, 0.16f, 0.24f, 0.85f);
    private readonly Color activeTextColor = Color.white;
    private readonly Color inactiveTextColor = new Color(0.7f, 0.75f, 0.85f, 0.8f);

    void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        SetupEventListeners();
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLanguageButtons;
        RefreshUI();
    }

    void OnDisable()
    {
        IsOpen = false;
        LocalizationManager.OnLanguageChanged -= RefreshLanguageButtons;
    }

    private void SetupEventListeners()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        if (fontSizeSlider != null)
        {
            fontSizeSlider.minValue = MIN_FONT_SIZE_SCALE;
            fontSizeSlider.maxValue = MAX_FONT_SIZE_SCALE;
            fontSizeSlider.onValueChanged.RemoveAllListeners();
            fontSizeSlider.onValueChanged.AddListener(OnFontSizeSliderChanged);
        }

        if (englishButton != null)
        {
            englishButton.onClick.RemoveAllListeners();
            englishButton.onClick.AddListener(() => SetLanguage(Language.English));
        }

        if (thaiButton != null)
        {
            thaiButton.onClick.RemoveAllListeners();
            thaiButton.onClick.AddListener(() => SetLanguage(Language.Thai));
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Open(Action onCloseCallback = null)
    {
        IsOpen = true;
        OnClosed = onCloseCallback;
        if (panelRoot != null) panelRoot.SetActive(true);
        gameObject.SetActive(true);
        RefreshUI();
    }

    public void Close()
    {
        if (!IsOpen && (panelRoot == null || !panelRoot.activeSelf) && !gameObject.activeSelf)
        {
            return;
        }

        IsOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
        gameObject.SetActive(false);

        Action callback = OnClosed;
        OnClosed = null;
        callback?.Invoke();
    }

    public void RefreshUI()
    {
        float master = PlayerPrefs.GetFloat(AudioManager.PREF_MASTER_VOLUME, 1f);
        float bgm = PlayerPrefs.GetFloat(AudioManager.PREF_BGM_VOLUME, 0.8f);
        float sfx = PlayerPrefs.GetFloat(AudioManager.PREF_SFX_VOLUME, 1f);

        if (AudioManager.Instance != null)
        {
            master = AudioManager.Instance.masterVolume;
            bgm = AudioManager.Instance.bgmVolume;
            sfx = AudioManager.Instance.sfxVolume;
        }

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(master);
            UpdateSliderLabel(masterValueText, master);
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(bgm);
            UpdateSliderLabel(bgmValueText, bgm);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfx);
            UpdateSliderLabel(sfxValueText, sfx);
        }

        float fontScale = CurrentFontScale;
        if (fontSizeSlider != null)
        {
            fontSizeSlider.minValue = MIN_FONT_SIZE_SCALE;
            fontSizeSlider.maxValue = MAX_FONT_SIZE_SCALE;
            fontSizeSlider.SetValueWithoutNotify(fontScale);
            UpdateSliderLabel(fontSizeValueText, fontScale);
        }

        RefreshLanguageButtons();
    }

    private void OnMasterSliderChanged(float val)
    {
        UpdateSliderLabel(masterValueText, val);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(val);
        }
        else
        {
            AudioListener.volume = val;
            PlayerPrefs.SetFloat(AudioManager.PREF_MASTER_VOLUME, val);
            PlayerPrefs.Save();
        }
    }

    private void OnBGMSliderChanged(float val)
    {
        UpdateSliderLabel(bgmValueText, val);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(val);
        }
        else
        {
            PlayerPrefs.SetFloat(AudioManager.PREF_BGM_VOLUME, val);
            PlayerPrefs.Save();
        }
    }

    private void OnSFXSliderChanged(float val)
    {
        UpdateSliderLabel(sfxValueText, val);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(val);
        }
        else
        {
            PlayerPrefs.SetFloat(AudioManager.PREF_SFX_VOLUME, val);
            PlayerPrefs.Save();
        }
    }

    private void OnFontSizeSliderChanged(float val)
    {
        float stepped = Mathf.Round(val * 20f) / 20f;
        UpdateSliderLabel(fontSizeValueText, stepped);
        CurrentFontScale = stepped;
    }

    private void UpdateSliderLabel(TMP_Text label, float value)
    {
        if (label != null)
        {
            label.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    private void SetLanguage(Language lang)
    {
        LocalizationManager.SetLanguage(lang);
        RefreshLanguageButtons();
    }

    private void RefreshLanguageButtons()
    {
        bool isEn = LocalizationManager.CurrentLanguage == Language.English;

        if (englishButtonBg != null)
        {
            englishButtonBg.color = isEn ? activeLangColor : inactiveLangColor;
        }
        if (englishButtonText != null)
        {
            englishButtonText.color = isEn ? activeTextColor : inactiveTextColor;
        }

        if (thaiButtonBg != null)
        {
            thaiButtonBg.color = !isEn ? activeLangColor : inactiveLangColor;
        }
        if (thaiButtonText != null)
        {
            thaiButtonText.color = !isEn ? activeTextColor : inactiveTextColor;
        }
    }
}
