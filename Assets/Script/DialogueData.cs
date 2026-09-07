using UnityEngine;

public class DialogueElementSelectorAttribute : PropertyAttribute { }

[System.Serializable]
public class BranchDialogue
{
    [Tooltip("Check this to play this dialogue element directly when this option is chosen.")]
    public bool enabled = false;

    [Header("Scene")]
    public Sprite background;
    public Sprite character;

    [Header("Dialogue Content")]
    [TextArea(2, 5)]
    public string text;
    [TextArea(2, 5)]
    [Tooltip("Thai translation for this branch dialogue (optional).")]
    public string textThai = "";
    public float textSpeed = 0.03f;
    public AudioClip soundEffect;

    [Header("Font Size Override (Optional)")]
    [Tooltip("Override font size for English on this element (0 to use DialogueManager setting)")]
    public float englishFontSize = 0f;
    [Tooltip("Override font size for Thai on this element (0 to use DialogueManager setting)")]
    public float thaiFontSize = 0f;

    [Header("Background Music")]
    [Tooltip("Background music to play for this branch element. Leave empty to continue current music.")]
    public AudioClip backgroundMusic;
    [Tooltip("Check this to stop background music during this branch element.")]
    public bool stopMusic = false;

    [Header("After This Element Finishes")]
    [DialogueElementSelector]
    [Tooltip("Element in Dialogues array to jump to after this finishes. Select -1 to proceed to the next Element in the main list.")]
    public int nextElement = -1;
    [Tooltip("Scene to load after this finishes (optional).")]
    public string nextSceneName = "";

    public string GetLocalizedText()
    {
        if (LocalizationManager.CurrentLanguage == Language.Thai && !string.IsNullOrEmpty(textThai))
        {
            return ThaiFontAdjuster.Adjust(textThai);
        }
        return text;
    }
}

[System.Serializable]
public class DialogueData
{
    [Header("Scene")]
    public Sprite background;
    public Sprite character;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string text;
    [TextArea(2, 5)]
    [Tooltip("Thai translation for this dialogue (optional).")]
    public string textThai = "";

    public float textSpeed = 0.03f;

    [Header("Font Size Override (Optional)")]
    [Tooltip("Override font size for English on this element (0 to use DialogueManager setting)")]
    public float englishFontSize = 0f;
    [Tooltip("Override font size for Thai on this element (0 to use DialogueManager setting)")]
    public float thaiFontSize = 0f;

    [Header("Sound Effect")]
    public AudioClip soundEffect;

    [Header("Background Music")]
    [Tooltip("Background music to play during this dialogue element. Leave empty to continue playing current music.")]
    public AudioClip backgroundMusic;
    [Tooltip("Check this to stop background music on this element.")]
    public bool stopMusic = false;
    [Tooltip("Optional: toggle if you want to explicitly stop or change music.")]
    public bool changeMusic = false;

    [Header("Next Destination")]
    [DialogueElementSelector]
    [Tooltip("Which element to go to after this dialogue. Select -1 to proceed to the next Element in the list.")]
    public int nextElement = -1;
    [Tooltip("Load another scene after this dialogue finishes (optional).")]
    public string nextSceneName = "";

    [Header("Choice Panel Settings")]
    [Tooltip("If true, shows the ChoicePanel UI when text finishes typing.")]
    public bool hasChoice = false;
    public string choicePrompt = "Choose your path";
    public string choicePromptThai = "";

    [Header("Click (Tap Option)")]
    public string tapChoiceText = "Tap (Click)";
    public string tapChoiceTextThai = "";
    [DialogueElementSelector]
    [Tooltip("Target Element in Dialogues to jump to. Select -1 to proceed to next.")]
    public int tapTargetElement = -1;
    [Tooltip("Or put a dialogue element directly inside this Click option!")]
    public BranchDialogue tapElement = new BranchDialogue();
    public string tapSceneName = "";
    public AudioClip tapSound;

    [Header("Click & Hold (Hold Option)")]
    public string holdChoiceText = "Hold";
    public string holdChoiceTextThai = "";
    [DialogueElementSelector]
    [Tooltip("Target Element in Dialogues to jump to when holding. Select -1 to proceed to next.")]
    public int holdTargetElement = -1;
    [Tooltip("Or put a dialogue element directly inside this Hold option!")]
    public BranchDialogue holdElement = new BranchDialogue();
    public string holdSceneName = "";
    public AudioClip holdSound;
    [Tooltip("Required hold duration in seconds before triggering branch.")]
    public float holdDuration = 1.0f;

    public int tapTargetIndex
    {
        get => tapTargetElement;
        set => tapTargetElement = value;
    }

    public int holdTargetIndex
    {
        get => holdTargetElement;
        set => holdTargetElement = value;
    }

    public string GetLocalizedText()
    {
        if (LocalizationManager.CurrentLanguage == Language.Thai && !string.IsNullOrEmpty(textThai))
        {
            return ThaiFontAdjuster.Adjust(textThai);
        }
        return text;
    }

    public string GetLocalizedTapChoice()
    {
        if (LocalizationManager.CurrentLanguage == Language.Thai && !string.IsNullOrEmpty(tapChoiceTextThai))
        {
            return ThaiFontAdjuster.Adjust(tapChoiceTextThai);
        }
        return tapChoiceText;
    }

    public string GetLocalizedHoldChoice()
    {
        if (LocalizationManager.CurrentLanguage == Language.Thai && !string.IsNullOrEmpty(holdChoiceTextThai))
        {
            return ThaiFontAdjuster.Adjust(holdChoiceTextThai);
        }
        return holdChoiceText;
    }

    public string GetLocalizedChoicePrompt()
    {
        if (LocalizationManager.CurrentLanguage == Language.Thai && !string.IsNullOrEmpty(choicePromptThai))
        {
            return ThaiFontAdjuster.Adjust(choicePromptThai);
        }
        return choicePrompt;
    }
}