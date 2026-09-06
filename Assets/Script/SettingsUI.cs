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
        OnClosed = onCloseCallback;
        if (panelRoot != null) panelRoot.SetActive(true);
        gameObject.SetActive(true);
        RefreshUI();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        gameObject.SetActive(false);
        OnClosed?.Invoke();
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
