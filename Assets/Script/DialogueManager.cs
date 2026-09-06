using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public Image backgroundImage;
    public Image characterImage;
    public TMP_Text dialogueText;

    [Header("Choice UI")]
    [Tooltip("Reference to the ChoicePanel component in the Canvas.")]
    public ChoicePanel choicePanel;

    [Header("Choice Timing Settings")]
    [Tooltip("Maximum press duration (seconds) considered as a tap.")]
    public float tapThreshold = 0.35f;

    [Header("Dialogue")]
    public DialogueData[] dialogues;

    [Header("Audio")]
    public AudioManager audioManager;

    private int currentDialogue = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private bool isChoiceActive = false;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private float currentHoldDuration = 1.0f;
    private float displayHoldProgress = 0f;
    private BranchDialogue activeBranch = null;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (choicePanel == null)
        {
            choicePanel = FindFirstObjectByType<ChoicePanel>(FindObjectsInactive.Include);
        }
        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        }
    }
#endif

    void Awake()
    {
        EnsureAudioManager();
        EnsureChoiceUI();
    }

    public void EnsureAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        }

        if (audioManager == null)
        {
            GameObject amObj = new GameObject("AudioManager");
            audioManager = amObj.AddComponent<AudioManager>();
            audioManager.EnsureAudioSources();
        }
        else
        {
            audioManager.EnsureAudioSources();
        }
    }

    void Start()
    {
        if (SaveSystem.HasPendingResume)
        {
            currentDialogue = Mathf.Clamp(SaveSystem.PendingDialogueIndex, 0, dialogues != null && dialogues.Length > 0 ? dialogues.Length - 1 : 0);
            SaveSystem.ClearPendingResume();
            Debug.Log($"[DialogueManager] Resumed at dialogue index: {currentDialogue}");
        }

        ShowDialogue();
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private TMP_FontAsset defaultDialogueFont;
    private static TMP_FontAsset cachedThaiFont;
    private static bool thaiFontLoaded = false;

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

    private void UpdateDialogueFont()
    {
        if (dialogueText == null) return;

        if (defaultDialogueFont == null)
        {
            defaultDialogueFont = dialogueText.font;
        }

        EnsureThaiFontLoaded();

        if (LocalizationManager.CurrentLanguage == Language.Thai && cachedThaiFont != null)
        {
            dialogueText.font = cachedThaiFont;
        }
        else if (defaultDialogueFont != null)
        {
            dialogueText.font = defaultDialogueFont;
        }
    }

    private void OnLanguageChanged()
    {
        UpdateDialogueFont();

        if (!isTyping && dialogueText != null)
        {
            if (activeBranch != null)
            {
                dialogueText.text = activeBranch.GetLocalizedText();
            }
            else if (currentDialogue < dialogues.Length && dialogues[currentDialogue] != null)
            {
                dialogueText.text = dialogues[currentDialogue].GetLocalizedText();
            }
        }
    }

    public int GetCurrentDialogueIndex()
    {
        return currentDialogue;
    }

    public string GetCurrentDialogueText()
    {
        if (dialogues != null && currentDialogue >= 0 && currentDialogue < dialogues.Length)
        {
            return dialogues[currentDialogue] != null ? dialogues[currentDialogue].GetLocalizedText() : "";
        }
        return "";
    }

    public void SetCurrentDialogueIndex(int index)
    {
        currentDialogue = Mathf.Clamp(index, 0, dialogues != null && dialogues.Length > 0 ? dialogues.Length - 1 : 0);
        ShowDialogue();
    }

    void Update()
    {
        if (currentDialogue >= dialogues.Length) return;
        DialogueData dialogue = dialogues[currentDialogue];

        bool hasHoldBranch = (dialogue != null && (
            dialogue.holdTargetElement >= 0 ||
            !string.IsNullOrEmpty(dialogue.holdSceneName) ||
            (dialogue.holdElement != null && dialogue.holdElement.enabled && !string.IsNullOrEmpty(dialogue.holdElement.text))
        ));
        bool canHold = isChoiceActive || (hasHoldBranch && !isTyping);

        if (canHold)
        {
            UpdateChoiceInput();
        }
        else
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                OnClick();
            }
        }
    }

    void OnClick()
    {
        if (isTyping)
        {
            FinishText();
            return;
        }

        if (activeBranch != null)
        {
            BranchDialogue finishedBranch = activeBranch;
            activeBranch = null;

            if (!string.IsNullOrEmpty(finishedBranch.nextSceneName))
            {
                SceneManager.LoadScene(finishedBranch.nextSceneName);
                return;
            }

            if (finishedBranch.nextElement >= 0)
            {
                currentDialogue = finishedBranch.nextElement;
            }
            else
            {
                currentDialogue++;
            }

            ShowDialogue();
            return;
        }

        NextDialogue();
    }

    void ShowDialogue()
    {
        UpdateDialogueFont();
        HideChoiceUI();
        isChoiceActive = false;
        isHolding = false;
        holdTimer = 0f;
        displayHoldProgress = 0f;

        if (currentDialogue >= dialogues.Length)
        {
            Debug.Log("[Dialogue] Dialogue finished.");
            return;
        }

        DialogueData dialogue = dialogues[currentDialogue];
        currentHoldDuration = Mathf.Max(0.2f, dialogue.holdDuration);

        if (dialogue.background != null && backgroundImage != null)
        {
            backgroundImage.sprite = dialogue.background;
        }

        if (characterImage != null)
        {
            if (dialogue.character != null)
            {
                characterImage.sprite = dialogue.character;
                characterImage.enabled = true;
            }
            else
            {
                characterImage.enabled = false;
            }
        }

        EnsureAudioManager();

        if (dialogue.soundEffect != null && audioManager != null)
        {
            audioManager.PlaySFX(dialogue.soundEffect);
        }

        // Background Music
        if (audioManager != null)
        {
            if (dialogue.stopMusic)
            {
                audioManager.StopBGM();
            }
            else if (dialogue.backgroundMusic != null)
            {
                audioManager.PlayBGM(dialogue.backgroundMusic);
            }
            else if (dialogue.changeMusic)
            {
                audioManager.StopBGM();
            }
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(
            TypeText(dialogue.GetLocalizedText(), dialogue.textSpeed)
        );
    }

    IEnumerator TypeText(string text, float speed)
    {
        isTyping = true;
        if (dialogueText != null)
        {
            dialogueText.text = "";

            for (int i = 0; i < text.Length; i++)
            {
                dialogueText.text += text[i];

                // Append any combining vowel or tone mark immediately without delay
                while (i + 1 < text.Length && ThaiFontAdjuster.IsThaiCombiningMark(text[i + 1]))
                {
                    i++;
                    dialogueText.text += text[i];
                }

                yield return new WaitForSeconds(speed);
            }
        }

        isTyping = false;
        OnTextFinished();
    }

    void FinishText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        if (dialogueText != null)
        {
            if (activeBranch != null)
            {
                dialogueText.text = activeBranch.GetLocalizedText();
            }
            else if (currentDialogue < dialogues.Length)
            {
                dialogueText.text = dialogues[currentDialogue].GetLocalizedText();
            }
        }

        isTyping = false;
        if (activeBranch == null)
        {
            OnTextFinished();
        }
    }

    void OnTextFinished()
    {
        if (currentDialogue < dialogues.Length)
        {
            DialogueData current = dialogues[currentDialogue];
            if (current.hasChoice)
            {
                ActivateChoices(current);
            }
        }
    }

    void ActivateChoices(DialogueData dialogue)
    {
        isChoiceActive = true;
        isHolding = false;
        holdTimer = 0f;
        displayHoldProgress = 0f;
        currentHoldDuration = Mathf.Max(0.2f, dialogue.holdDuration);

        EnsureChoiceUI();
        if (choicePanel != null)
        {
            choicePanel.Setup(dialogue, ExecuteTapChoice, ExecuteHoldChoice);
            choicePanel.Show();
        }
    }

    void UpdateChoiceInput()
    {
        bool pointerDownDirect = (choicePanel != null && choicePanel.IsHoldingDirectly);

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || pointerDownDirect)
        {
            if (!isHolding)
            {
                isHolding = true;
                holdTimer = 0f;
            }
        }

        if (isHolding && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || pointerDownDirect))
        {
            holdTimer += Time.deltaTime;
            float targetProgress = Mathf.Clamp01(holdTimer / currentHoldDuration);
            displayHoldProgress = Mathf.Lerp(displayHoldProgress, targetProgress, Time.deltaTime * 20f);

            UpdateHoldVisuals(targetProgress);

            if (holdTimer >= currentHoldDuration)
            {
                isHolding = false;
                ExecuteHoldChoice();
                return;
            }
        }

        if (isHolding && (!Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Space) && !pointerDownDirect))
        {
            if (holdTimer < tapThreshold)
            {
                isHolding = false;
                ExecuteTapChoice();
                return;
            }
            else
            {
                isHolding = false;
            }
        }

        if (!isHolding && displayHoldProgress > 0f)
        {
            displayHoldProgress = Mathf.MoveTowards(displayHoldProgress, 0f, Time.deltaTime * 3.5f);
            UpdateHoldVisuals(displayHoldProgress);
        }
    }

    void UpdateHoldVisuals(float progress)
    {
        if (choicePanel != null)
        {
            choicePanel.UpdateHoldProgress(progress);
        }
    }

    void ExecuteTapChoice()
    {
        if (currentDialogue >= dialogues.Length) return;
        DialogueData dialogue = dialogues[currentDialogue];

        Debug.Log("[Dialogue] Choice selected: Tap -> " + dialogue.tapChoiceText);

        if (dialogue.tapSound != null && audioManager != null)
        {
            audioManager.PlaySFX(dialogue.tapSound);
        }

        isChoiceActive = false;
        HideChoiceUI();

        if (dialogue.tapElement != null && dialogue.tapElement.enabled && !string.IsNullOrEmpty(dialogue.tapElement.text))
        {
            Debug.Log("[Dialogue] Playing inline Tap element directly!");
            PlayBranchElement(dialogue.tapElement);
            return;
        }

        if (!string.IsNullOrEmpty(dialogue.tapSceneName))
        {
            SceneManager.LoadScene(dialogue.tapSceneName);
            return;
        }

        if (dialogue.tapTargetElement >= 0)
        {
            if (dialogue.tapTargetElement < dialogues.Length)
            {
                Debug.Log($"[Dialogue] Tap action -> Jumping to Element {dialogue.tapTargetElement}: \"{dialogues[dialogue.tapTargetElement].text}\"");
                currentDialogue = dialogue.tapTargetElement;
            }
            else
            {
                Debug.LogWarning($"[Dialogue] tapTargetElement ({dialogue.tapTargetElement}) exceeds dialogues array size ({dialogues.Length})!");
                currentDialogue = dialogues.Length;
            }
        }
        else
        {
            currentDialogue++;
        }

        ShowDialogue();
    }

    void ExecuteHoldChoice()
    {
        if (currentDialogue >= dialogues.Length) return;
        DialogueData dialogue = dialogues[currentDialogue];

        Debug.Log("[Dialogue] Choice selected: Hold -> " + dialogue.holdChoiceText);

        if (dialogue.holdSound != null && audioManager != null)
        {
            audioManager.PlaySFX(dialogue.holdSound);
        }

        isChoiceActive = false;

        if (choicePanel != null)
        {
            StartCoroutine(choicePanel.PlayHoldSuccessAnimationRoutine(() => ProceedAfterHold(dialogue)));
        }
        else
        {
            ProceedAfterHold(dialogue);
        }
    }

    void ProceedAfterHold(DialogueData dialogue)
    {
        HideChoiceUI();

        if (dialogue.holdElement != null && dialogue.holdElement.enabled && !string.IsNullOrEmpty(dialogue.holdElement.text))
        {
            Debug.Log("[Dialogue] Playing inline Hold element directly!");
            PlayBranchElement(dialogue.holdElement);
            return;
        }

        if (!string.IsNullOrEmpty(dialogue.holdSceneName))
        {
            SceneManager.LoadScene(dialogue.holdSceneName);
            return;
        }

        if (dialogue.holdTargetElement >= 0)
        {
            if (dialogue.holdTargetElement < dialogues.Length)
            {
                Debug.Log($"[Dialogue] Hold action -> Jumping to Element {dialogue.holdTargetElement}: \"{dialogues[dialogue.holdTargetElement].text}\"");
                currentDialogue = dialogue.holdTargetElement;
            }
            else
            {
                Debug.LogWarning($"[Dialogue] holdTargetElement ({dialogue.holdTargetElement}) exceeds dialogues array size ({dialogues.Length})!");
                currentDialogue = dialogues.Length;
            }
        }
        else
        {
            currentDialogue++;
        }

        ShowDialogue();
    }

    void PlayBranchElement(BranchDialogue branch)
    {
        UpdateDialogueFont();
        activeBranch = branch;
        isChoiceActive = false;
        isHolding = false;
        holdTimer = 0f;
        displayHoldProgress = 0f;

        if (branch.background != null && backgroundImage != null)
        {
            backgroundImage.sprite = branch.background;
        }

        if (characterImage != null)
        {
            if (branch.character != null)
            {
                characterImage.sprite = branch.character;
                characterImage.enabled = true;
            }
            else
            {
                characterImage.enabled = false;
            }
        }

        EnsureAudioManager();

        if (branch.soundEffect != null && audioManager != null)
        {
            audioManager.PlaySFX(branch.soundEffect);
        }

        // Background Music for branch element
        if (audioManager != null)
        {
            if (branch.stopMusic)
            {
                audioManager.StopBGM();
            }
            else if (branch.backgroundMusic != null)
            {
                audioManager.PlayBGM(branch.backgroundMusic);
            }
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(
            TypeText(branch.GetLocalizedText(), branch.textSpeed > 0 ? branch.textSpeed : 0.03f)
        );
    }

    void NextDialogue()
    {
        if (currentDialogue < dialogues.Length)
        {
            DialogueData current = dialogues[currentDialogue];

            if (!string.IsNullOrEmpty(current.nextSceneName))
            {
                SceneManager.LoadScene(current.nextSceneName);
                return;
            }

            if (current.nextElement >= 0)
            {
                if (current.nextElement < dialogues.Length)
                {
                    Debug.Log($"[Dialogue] Jumping to Element {current.nextElement}: \"{dialogues[current.nextElement].text}\"");
                    currentDialogue = current.nextElement;
                }
                else
                {
                    Debug.LogWarning($"[Dialogue] nextElement ({current.nextElement}) exceeds dialogues array size ({dialogues.Length})!");
                    currentDialogue = dialogues.Length;
                }
            }
            else
            {
                currentDialogue++;
            }
        }
        else
        {
            currentDialogue++;
        }

        if (currentDialogue >= dialogues.Length)
        {
            Debug.Log("[Dialogue] Dialogue finished.");
            return;
        }

        ShowDialogue();
    }

    void EnsureChoiceUI()
    {
        if (choicePanel != null) return;

        choicePanel = FindFirstObjectByType<ChoicePanel>(FindObjectsInactive.Include);
        if (choicePanel != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

#if UNITY_EDITOR
        ChoicePanel.CreateChoicePanelHierarchy();
        choicePanel = FindFirstObjectByType<ChoicePanel>(FindObjectsInactive.Include);
#else
        GameObject panelObj = new GameObject("ChoicePanel_Runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(ChoicePanel));
        panelObj.transform.SetParent(canvas.transform, false);
        choicePanel = panelObj.GetComponent<ChoicePanel>();
        choicePanel.panelRoot = panelObj.GetComponent<RectTransform>();
        choicePanel.panelBackground = panelObj.GetComponent<Image>();
        choicePanel.canvasGroup = panelObj.GetComponent<CanvasGroup>();
        choicePanel.gameObject.SetActive(false);
#endif
    }

    void HideChoiceUI()
    {
        if (choicePanel != null)
        {
            choicePanel.Hide();
        }
    }
}