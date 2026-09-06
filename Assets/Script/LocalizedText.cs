using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    public string localizationKey;
    public string prefix = "";
    public string suffix = "";

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
        UpdateText();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
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

        if (LocalizationManager.CurrentLanguage == Language.Thai && cachedThaiFont != null)
        {
            textComponent.font = cachedThaiFont;
        }
        else if (defaultFont != null)
        {
            textComponent.font = defaultFont;
        }

        if (!string.IsNullOrEmpty(localizationKey))
        {
            textComponent.text = prefix + LocalizationManager.Get(localizationKey) + suffix;
        }
    }
}
