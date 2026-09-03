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
    public float textSpeed = 0.03f;
    public AudioClip soundEffect;

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

    public float textSpeed = 0.03f;

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

    [Header("Click (Tap Option)")]
    public string tapChoiceText = "Tap (Click)";
    [DialogueElementSelector]
    [Tooltip("Target Element in Dialogues to jump to. Select -1 to proceed to next.")]
    public int tapTargetElement = -1;
    [Tooltip("Or put a dialogue element directly inside this Click option!")]
    public BranchDialogue tapElement = new BranchDialogue();
    public string tapSceneName = "";
    public AudioClip tapSound;

    [Header("Click & Hold (Hold Option)")]
    public string holdChoiceText = "Hold";
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
}