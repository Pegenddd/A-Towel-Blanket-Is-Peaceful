using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    public string localizationKey;
    public string prefix = "";
    public string suffix = "";

    [Header("Language Specific Font Size")]
    [Tooltip("Font size when language is English (0 = keep current/default)")]
    public float englishFontSize = 0f;
    [Tooltip("Font size when language is Thai (0 = keep current/default)")]
    public float thaiFontSize = 0f;

    [Header("Language Specific Line Spacing")]
    public float englishLineSpacing = 0f;
    public float thaiLineSpacing = 0f;

    [Header("Settings Scaling")]
    public bool scaleWithSettings = false;

    private TMP_Text textComponent;
    private TMP_FontAsset defaultFont;
    private static TMP_FontAsset cachedThaiFont;
    private static bool thaiFontLoaded = false;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            defaultFont = textComponent.font;
            if (englishFontSize <= 0) englishFontSize = textComponent.fontSize;
            if (thaiFontSize <= 0) thaiFontSize = textComponent.fontSize;
        }

        EnsureThaiFontLoaded();
    }

    private static void EnsureThaiFontLoaded()
    {
        if (!thaiFontLoaded)
        {
            cachedThaiFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/ThaiFont_SDF");
            if (cachedThaiFont == null)
            {
                cachedThaiFont = Resources.Load<TMP_FontAsset>("ThaiFont_SDF");
            }
            thaiFontLoaded = true;
        }
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateText;
        SettingsUI.OnFontScaleChanged += OnFontScaleChanged;
        UpdateText();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
        SettingsUI.OnFontScaleChanged -= OnFontScaleChanged;
    }

    private void OnFontScaleChanged(float scale)
    {
        if (scaleWithSettings) UpdateText();
    }

    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    public void UpdateText()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }

        if (textComponent == null) return;

        EnsureThaiFontLoaded();

        if (LocalizationManager.CurrentLanguage == Language.Thai)
        {
            if (cachedThaiFont != null)
            {
                textComponent.font = cachedThaiFont;
            }
            float scale = scaleWithSettings ? SettingsUI.CurrentFontScale : 1f;
            if (thaiFontSize > 0)
            {
                textComponent.fontSize = Mathf.Round(thaiFontSize * scale);
            }
            if (thaiLineSpacing != 0)
            {
                textComponent.lineSpacing = thaiLineSpacing;
            }
        }
        else
        {
            if (defaultFont != null)
            {
                textComponent.font = defaultFont;
            }
            float scale = scaleWithSettings ? SettingsUI.CurrentFontScale : 1f;
            if (englishFontSize > 0)
            {
                textComponent.fontSize = Mathf.Round(englishFontSize * scale);
            }
            if (englishLineSpacing != 0)
            {
                textComponent.lineSpacing = englishLineSpacing;
            }
        }

        if (!string.IsNullOrEmpty(localizationKey))
        {
            textComponent.text = prefix + LocalizationManager.Get(localizationKey) + suffix;
        }
    }
}
