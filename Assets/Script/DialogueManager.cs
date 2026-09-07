using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using JetBrains.Annotations;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public Image backgroundImage;
    public Image characterImage;
    public TMP_Text dialogueText;

    [Header("Font Size & Line Spacing Settings")]
    [Tooltip("Font size when language is English (default: 32)")]
    public float englishFontSize = 32f;
    [Tooltip("Font size when language is Thai (default: 32)")]
    public float thaiFontSize = 32f;
    [Tooltip("Line spacing for English dialogue")]
    public float englishLineSpacing = 0f;
    [Tooltip("Line spacing for Thai dialogue (extra room for Thai tone marks / vowels)")]
    public float thaiLineSpacing = 10f;

    [SerializeField] private bool testMode = false;
    [SerializeField] private int testStartElement = 10;

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
    [Tooltip("Default background music for this dialogue scene. If set, this music will play and stay throughout all dialogue elements (unless an element explicitly overrides or stops it).")]
    public AudioClip defaultBGM;
    [Tooltip("If true, starts defaultBGM even if a previous scene was already playing another music. If false, keeps already playing music.")]
    public bool overrideExistingMusic = false;
    [Tooltip("If true, background music continues playing seamlessly across all dialogues and scenes without stopping or restarting.")]
    public bool keepBGMContinuous = true;

    [Header("Intro Quote (Before Game Starts)")]
    [Tooltip("If true, displays the prologue quote before starting dialogue 0 for a new game.")]
    public bool enableIntroQuote = true;
    [TextArea(2, 4)]
    public string introQuoteEnglish = "At that time, when Adam and Eve bit into the fruit of wisdom, we fell from the Garden of Eden forever -----------------------------------------------------------------";
    [TextArea(2, 4)]
    public string introQuoteThai = "ยามนั้นเมื่ออดัมและอีฟกัดผลแห่งปัญญา พวกเราก็ร่วงหล่นจากสวนอีเดนตลอดกาล -----------------------------------------------------------------";
    [Tooltip("Font size for Intro Quote in English")]
    public float introQuoteEnglishFontSize = 24f;
    [Tooltip("Font size for Intro Quote in Thai")]
    public float introQuoteThaiFontSize = 26f;
    [Tooltip("Font size for Intro Quote hint in English")]
    public float introQuoteEnglishHintFontSize = 15f;
    [Tooltip("Font size for Intro Quote hint in Thai")]
    public float introQuoteThaiHintFontSize = 16f;
    [HideInInspector] public float introQuoteFontSize = 0f;
    public float introQuoteTextSpeed = 0.04f;
    public CanvasGroup introQuoteCanvasGroup;
    public TMP_Text introQuoteText;
    public TMP_Text introQuoteHintText;

    [Header("Ending Screen (Game Finish)")]
    [Tooltip("If true, displays the ending quote in the center of the screen when all dialogues finish.")]
    public bool enableEndingScreen = true;
    [TextArea(2, 4)]
    public string endingTextEnglish = "The curtains still flutter overhead.";
    [TextArea(2, 4)]
    public string endingTextThai = "ผ้าม่านยังคงปลิวไสวอยู่เหนือหัว";
    [Tooltip("Font size for Ending Text in English")]
    public float endingEnglishFontSize = 32f;
    [Tooltip("Font size for Ending Text in Thai")]
    public float endingThaiFontSize = 34f;
    [Tooltip("Font size for Ending hint in English")]
    public float endingEnglishHintFontSize = 15f;
    [Tooltip("Font size for Ending hint in Thai")]
    public float endingThaiHintFontSize = 16f;
    [HideInInspector] public float endingTextFontSize = 0f;
    public string endingReturnScene = "MainMenu";
    public CanvasGroup endingScreenCanvasGroup;
    public TMP_Text endingText;
    public TMP_Text endingHintText;

    private int currentDialogue = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private bool isChoiceActive = false;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private float currentHoldDuration = 1.0f;
    private float displayHoldProgress = 0f;
    private BranchDialogue activeBranch = null;

    private bool isIntroActive = false;
    private bool isIntroTyping = false;
    private Coroutine introTypingCoroutine;
    private bool isEndingActive = false;
    private float endingTimer = 0f;

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
        if (introQuoteFontSize > 0)
        {
            introQuoteThaiFontSize = introQuoteFontSize;
            introQuoteEnglishFontSize = introQuoteFontSize;
            introQuoteFontSize = 0;
        }
        if (endingTextFontSize > 0)
        {
            endingThaiFontSize = endingTextFontSize;
            endingEnglishFontSize = endingTextFontSize;
            endingTextFontSize = 0;
        }

        EnsureAudioManager();
        EnsureChoiceUI();
        EnsureIntroAndEndingUI();
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
        if (testMode)
        {
            currentDialogue = Mathf.Clamp(
                testStartElement,
                0,
                dialogues != null && dialogues.Length > 0 ? dialogues.Length - 1 : 0
            );

            Debug.Log($"[DialogueManager] TEST MODE - Starting at Element: {currentDialogue}");
        }
        else if (SaveSystem.HasPendingResume)
        {
            currentDialogue = Mathf.Clamp(
                SaveSystem.PendingDialogueIndex,
                0,
                dialogues != null && dialogues.Length > 0 ? dialogues.Length - 1 : 0
            );

            SaveSystem.ClearPendingResume();

            Debug.Log($"[DialogueManager] Resumed at dialogue index: {currentDialogue}");
        }

        EnsureAudioManager();
        InitializeBackgroundMusic();

        bool isNewGame = (currentDialogue == 0 && !SaveSystem.HasPendingResume && (!testMode || testStartElement == 0));
        if (enableIntroQuote && isNewGame)
        {
            ShowIntroQuote();
        }
        else
        {
            ShowDialogue();
        }
    }

    private void InitializeBackgroundMusic()
    {
        if (audioManager == null) return;

        if (audioManager.IsBGMPlaying() && !overrideExistingMusic)
        {
            return;
        }

        AudioClip resolvedBGM = ResolveActiveBGMForDialogue(currentDialogue);
        if (resolvedBGM != null)
        {
            if (!audioManager.IsBGMPlaying() || audioManager.GetCurrentBGM() != resolvedBGM)
            {
                audioManager.PlayBGM(resolvedBGM);
            }
        }
        else if (defaultBGM != null && !audioManager.IsBGMPlaying())
        {
            audioManager.PlayBGM(defaultBGM);
        }
    }

    public AudioClip ResolveActiveBGMForDialogue(int index)
    {
        if (dialogues != null && index >= 0 && index < dialogues.Length)
        {
            for (int i = index; i >= 0; i--)
            {
                if (dialogues[i] != null)
                {
                    if (dialogues[i].stopMusic && !keepBGMContinuous)
                    {
                        return null;
                    }
                    if (dialogues[i].backgroundMusic != null)
                    {
                        return dialogues[i].backgroundMusic;
                    }
                }
            }
        }
        return defaultBGM;
    }


    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        SettingsUI.OnFontScaleChanged += OnFontScaleChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        SettingsUI.OnFontScaleChanged -= OnFontScaleChanged;
    }

    private void OnFontScaleChanged(float scale)
    {
        UpdateDialogueFont();
        if (isIntroActive) ApplyIntroQuoteTypography();
        if (isEndingActive) ApplyEndingTypography();
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

    public void ApplyDialogueTypography(DialogueData dialogue = null, BranchDialogue branch = null)
    {
        if (dialogueText == null) return;

        if (defaultDialogueFont == null)
        {
            defaultDialogueFont = dialogueText.font;
        }

        EnsureThaiFontLoaded();

        bool isThai = LocalizationManager.CurrentLanguage == Language.Thai;
        float scale = SettingsUI.CurrentFontScale;

        if (isThai)
        {
            if (cachedThaiFont != null)
            {
                dialogueText.font = cachedThaiFont;
            }
            float size = thaiFontSize;
            if (branch != null && branch.thaiFontSize > 0) size = branch.thaiFontSize;
            else if (dialogue != null && dialogue.thaiFontSize > 0) size = dialogue.thaiFontSize;

            if (size > 0) dialogueText.fontSize = Mathf.Round(size * scale);
            dialogueText.lineSpacing = thaiLineSpacing;
        }
        else
        {
            if (defaultDialogueFont != null)
            {
                dialogueText.font = defaultDialogueFont;
            }
            float size = englishFontSize;
            if (branch != null && branch.englishFontSize > 0) size = branch.englishFontSize;
            else if (dialogue != null && dialogue.englishFontSize > 0) size = dialogue.englishFontSize;

            if (size > 0) dialogueText.fontSize = Mathf.Round(size * scale);
            dialogueText.lineSpacing = englishLineSpacing;
        }
    }

    private void UpdateDialogueFont()
    {
        DialogueData cur = (currentDialogue >= 0 && dialogues != null && currentDialogue < dialogues.Length) ? dialogues[currentDialogue] : null;
        ApplyDialogueTypography(activeBranch != null ? null : cur, activeBranch);
    }

    private void OnLanguageChanged()
    {
        UpdateDialogueFont();

        if (isIntroActive)
        {
            ApplyIntroQuoteTypography();
            if (isIntroTyping)
            {
                if (introTypingCoroutine != null) StopCoroutine(introTypingCoroutine);
                isIntroTyping = false;
            }
            if (introQuoteText != null)
            {
                introQuoteText.text = GetLocalizedIntroQuote();
            }
            if (introQuoteHintText != null)
            {
                introQuoteHintText.text = LocalizationManager.Get("story_intro_hint");
            }
        }

        if (isEndingActive)
        {
            ApplyEndingTypography();
            if (endingText != null)
            {
                endingText.text = GetLocalizedEndingText();
            }
            if (endingHintText != null)
            {
                endingHintText.text = LocalizationManager.Get("story_ending_return");
            }
        }

        if (dialogueText != null)
        {
            string newText = null;
            if (activeBranch != null)
            {
                newText = activeBranch.GetLocalizedText();
            }
            else if (dialogues != null && currentDialogue >= 0 && currentDialogue < dialogues.Length && dialogues[currentDialogue] != null)
            {
                newText = dialogues[currentDialogue].GetLocalizedText();
            }

            if (!string.IsNullOrEmpty(newText))
            {
                if (isTyping)
                {
                    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                    isTyping = false;
                    dialogueText.text = newText;
                    if (activeBranch == null)
                    {
                        OnTextFinished();
                    }
                }
                else
                {
                    dialogueText.text = newText;
                }
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
        if (Time.timeScale <= 0f || SettingsUI.IsOpen) return;

        if (isIntroActive)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnIntroClicked();
            }
            return;
        }

        if (isEndingActive)
        {
            endingTimer += Time.deltaTime;
            if (endingTimer >= 0.5f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                OnEndingClicked();
            }
            return;
        }

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
            if (enableEndingScreen)
            {
                ShowEndingScreen();
            }
            return;
        }

        DialogueData dialogue = dialogues[currentDialogue];
        ApplyDialogueTypography(dialogue);
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
            if (dialogue.stopMusic && !keepBGMContinuous)
            {
                audioManager.StopBGM();
            }
            else if (dialogue.backgroundMusic != null && dialogue.backgroundMusic != audioManager.GetCurrentBGM())
            {
                audioManager.PlayBGM(dialogue.backgroundMusic);
            }
            else if (dialogue.changeMusic && !keepBGMContinuous)
            {
                audioManager.StopBGM();
            }
            else if (!audioManager.IsBGMPlaying() && defaultBGM != null)
            {
                audioManager.PlayBGM(defaultBGM);
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
        ApplyDialogueTypography(null, branch);
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
            if (branch.stopMusic && !keepBGMContinuous)
            {
                audioManager.StopBGM();
            }
            else if (branch.backgroundMusic != null && branch.backgroundMusic != audioManager.GetCurrentBGM())
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
            if (enableEndingScreen)
            {
                ShowEndingScreen();
            }
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

    public void EnsureIntroAndEndingUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. Intro Screen
        if (introQuoteCanvasGroup == null)
        {
            Transform existingIntro = canvas.transform.Find("IntroQuoteScreen");
            if (existingIntro != null)
            {
                introQuoteCanvasGroup = existingIntro.GetComponent<CanvasGroup>();
                introQuoteText = existingIntro.Find("QuoteText")?.GetComponent<TMP_Text>();
                introQuoteHintText = existingIntro.Find("HintText")?.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject introObj = new GameObject("IntroQuoteScreen", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                introObj.transform.SetParent(canvas.transform, false);
                RectTransform rt = introObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                introObj.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 1f);
                introQuoteCanvasGroup = introObj.GetComponent<CanvasGroup>();

                // Quote Text
                GameObject textObj = new GameObject("QuoteText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(introObj.transform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0.08f, 0.25f);
                textRt.anchorMax = new Vector2(0.92f, 0.75f);
                textRt.sizeDelta = Vector2.zero;
                introQuoteText = textObj.GetComponent<TMP_Text>();
                introQuoteText.alignment = TextAlignmentOptions.Center;
                introQuoteText.enableWordWrapping = true;
                introQuoteText.color = new Color(0.95f, 0.95f, 0.95f, 1f);

                // Hint Text
                GameObject hintObj = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintObj.transform.SetParent(introObj.transform, false);
                RectTransform hintRt = hintObj.GetComponent<RectTransform>();
                hintRt.anchorMin = new Vector2(0.5f, 0.12f);
                hintRt.anchorMax = new Vector2(0.5f, 0.12f);
                hintRt.pivot = new Vector2(0.5f, 0.5f);
                hintRt.sizeDelta = new Vector2(500f, 35f);
                introQuoteHintText = hintObj.GetComponent<TMP_Text>();
                introQuoteHintText.alignment = TextAlignmentOptions.Center;
                introQuoteHintText.fontSize = 16f;
                introQuoteHintText.color = new Color(0.6f, 0.65f, 0.75f, 0.8f);

                introObj.SetActive(false);
            }
        }

        // 2. Ending Screen
        if (endingScreenCanvasGroup == null)
        {
            Transform existingEnding = canvas.transform.Find("EndingScreen");
            if (existingEnding != null)
            {
                endingScreenCanvasGroup = existingEnding.GetComponent<CanvasGroup>();
                endingText = existingEnding.Find("EndingText")?.GetComponent<TMP_Text>();
                endingHintText = existingEnding.Find("HintText")?.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject endingObj = new GameObject("EndingScreen", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                endingObj.transform.SetParent(canvas.transform, false);
                RectTransform rt = endingObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                endingObj.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 1f);
                endingScreenCanvasGroup = endingObj.GetComponent<CanvasGroup>();

                // Center Ending Text
                GameObject textObj = new GameObject("EndingText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(endingObj.transform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0.5f, 0.5f);
                textRt.anchorMax = new Vector2(0.5f, 0.5f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = Vector2.zero;
                textRt.sizeDelta = new Vector2(820f, 160f);
                endingText = textObj.GetComponent<TMP_Text>();
                endingText.alignment = TextAlignmentOptions.Center;
                endingText.enableWordWrapping = true;
                endingText.color = new Color(0.96f, 0.96f, 0.98f, 1f);

                // Return Hint Text
                GameObject hintObj = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintObj.transform.SetParent(endingObj.transform, false);
                RectTransform hintRt = hintObj.GetComponent<RectTransform>();
                hintRt.anchorMin = new Vector2(0.5f, 0.15f);
                hintRt.anchorMax = new Vector2(0.5f, 0.15f);
                hintRt.pivot = new Vector2(0.5f, 0.5f);
                hintRt.sizeDelta = new Vector2(500f, 35f);
                endingHintText = hintObj.GetComponent<TMP_Text>();
                endingHintText.alignment = TextAlignmentOptions.Center;
                endingHintText.fontSize = 16f;
                endingHintText.color = new Color(0.6f, 0.7f, 0.85f, 0.8f);

                endingObj.SetActive(false);
            }
        }

        // Keep SettingsModal on top of everything if it exists
        SettingsUI settingsModal = canvas.GetComponentInChildren<SettingsUI>(true);
        if (settingsModal != null)
        {
            settingsModal.transform.SetAsLastSibling();
        }
    }

    public void ShowIntroQuote()
    {
        EnsureIntroAndEndingUI();
        if (introQuoteCanvasGroup == null || introQuoteText == null)
        {
            ShowDialogue();
            return;
        }

        isIntroActive = true;
        isIntroTyping = false;
        introQuoteCanvasGroup.gameObject.SetActive(true);
        introQuoteCanvasGroup.alpha = 1f;

        if (dialogueText != null) dialogueText.text = "";
        if (characterImage != null) characterImage.enabled = false;

        if (introQuoteHintText != null)
        {
            introQuoteHintText.gameObject.SetActive(false);
            introQuoteHintText.text = LocalizationManager.Get("story_intro_hint");
        }

        ApplyIntroQuoteTypography();

        string fullQuote = GetLocalizedIntroQuote();
        if (introTypingCoroutine != null) StopCoroutine(introTypingCoroutine);
        introTypingCoroutine = StartCoroutine(TypeIntroQuote(fullQuote, introQuoteTextSpeed));
    }

    private void ApplyIntroQuoteTypography()
    {
        EnsureThaiFontLoaded();
        bool isThai = LocalizationManager.CurrentLanguage == Language.Thai;
        float scale = SettingsUI.CurrentFontScale;

        if (introQuoteText != null)
        {
            if (isThai && cachedThaiFont != null)
            {
                introQuoteText.font = cachedThaiFont;
                introQuoteText.lineSpacing = thaiLineSpacing;
            }
            else if (defaultDialogueFont != null)
            {
                introQuoteText.font = defaultDialogueFont;
                introQuoteText.lineSpacing = englishLineSpacing;
            }
            float quoteSize = isThai ? introQuoteThaiFontSize : introQuoteEnglishFontSize;
            introQuoteText.fontSize = Mathf.Round(quoteSize * scale);
        }

        if (introQuoteHintText != null)
        {
            if (isThai && cachedThaiFont != null)
            {
                introQuoteHintText.font = cachedThaiFont;
                introQuoteHintText.lineSpacing = thaiLineSpacing;
            }
            else if (defaultDialogueFont != null)
            {
                introQuoteHintText.font = defaultDialogueFont;
                introQuoteHintText.lineSpacing = englishLineSpacing;
            }
            float hintSize = isThai ? introQuoteThaiHintFontSize : introQuoteEnglishHintFontSize;
            introQuoteHintText.fontSize = Mathf.Round(hintSize * scale);
        }
    }

    private string GetLocalizedIntroQuote()
    {
        bool isThai = LocalizationManager.CurrentLanguage == Language.Thai;
        string quote = isThai ? introQuoteThai : introQuoteEnglish;
        if (string.IsNullOrEmpty(quote))
        {
            quote = LocalizationManager.Get("story_intro_quote");
        }
        return isThai ? ThaiFontAdjuster.Adjust(quote) : quote;
    }

    private IEnumerator TypeIntroQuote(string text, float speed)
    {
        isIntroTyping = true;
        introQuoteText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            introQuoteText.text += text[i];
            while (i + 1 < text.Length && ThaiFontAdjuster.IsThaiCombiningMark(text[i + 1]))
            {
                i++;
                introQuoteText.text += text[i];
            }
            yield return new WaitForSeconds(speed);
        }

        isIntroTyping = false;
        if (introQuoteHintText != null)
        {
            introQuoteHintText.gameObject.SetActive(true);
        }
    }

    private void OnIntroClicked()
    {
        if (isIntroTyping)
        {
            if (introTypingCoroutine != null) StopCoroutine(introTypingCoroutine);
            isIntroTyping = false;
            introQuoteText.text = GetLocalizedIntroQuote();
            if (introQuoteHintText != null) introQuoteHintText.gameObject.SetActive(true);
        }
        else
        {
            StartCoroutine(FadeOutIntroRoutine());
        }
    }

    private IEnumerator FadeOutIntroRoutine()
    {
        isIntroActive = false;
        if (introQuoteCanvasGroup != null)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                introQuoteCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            introQuoteCanvasGroup.alpha = 0f;
            introQuoteCanvasGroup.gameObject.SetActive(false);
        }

        ShowDialogue();
    }

    public void ShowEndingScreen()
    {
        EnsureIntroAndEndingUI();
        if (endingScreenCanvasGroup == null || endingText == null)
        {
            Debug.Log("[Dialogue] Ending screen not found, dialogue ended.");
            return;
        }

        isEndingActive = true;
        endingTimer = 0f;

        if (dialogueText != null) dialogueText.text = "";
        if (characterImage != null) characterImage.enabled = false;
        HideChoiceUI();

        ApplyEndingTypography();

        endingText.text = GetLocalizedEndingText();

        if (endingHintText != null)
        {
            endingHintText.text = LocalizationManager.Get("story_ending_return");
            endingHintText.gameObject.SetActive(false);
        }

        endingScreenCanvasGroup.gameObject.SetActive(true);
        StartCoroutine(FadeInEndingRoutine());
    }

    private void ApplyEndingTypography()
    {
        EnsureThaiFontLoaded();
        bool isThai = LocalizationManager.CurrentLanguage == Language.Thai;
        float scale = SettingsUI.CurrentFontScale;

        if (endingText != null)
        {
            if (isThai && cachedThaiFont != null)
            {
                endingText.font = cachedThaiFont;
                endingText.lineSpacing = thaiLineSpacing;
            }
            else if (defaultDialogueFont != null)
            {
                endingText.font = defaultDialogueFont;
                endingText.lineSpacing = englishLineSpacing;
            }
            float textSize = isThai ? endingThaiFontSize : endingEnglishFontSize;
            endingText.fontSize = Mathf.Round(textSize * scale);
        }

        if (endingHintText != null)
        {
            if (isThai && cachedThaiFont != null)
            {
                endingHintText.font = cachedThaiFont;
                endingHintText.lineSpacing = thaiLineSpacing;
            }
            else if (defaultDialogueFont != null)
            {
                endingHintText.font = defaultDialogueFont;
                endingHintText.lineSpacing = englishLineSpacing;
            }
            float hintSize = isThai ? endingThaiHintFontSize : endingEnglishHintFontSize;
            endingHintText.fontSize = Mathf.Round(hintSize * scale);
        }
    }

    private string GetLocalizedEndingText()
    {
        bool isThai = LocalizationManager.CurrentLanguage == Language.Thai;
        string text = isThai ? endingTextThai : endingTextEnglish;
        if (string.IsNullOrEmpty(text))
        {
            text = LocalizationManager.Get("story_ending_text");
        }
        return isThai ? ThaiFontAdjuster.Adjust(text) : text;
    }

    private IEnumerator FadeInEndingRoutine()
    {
        endingScreenCanvasGroup.alpha = 0f;
        float elapsed = 0f;
        float duration = 1.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            endingScreenCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        endingScreenCanvasGroup.alpha = 1f;

        if (endingHintText != null)
        {
            endingHintText.gameObject.SetActive(true);
        }
    }

    private void OnEndingClicked()
    {
        if (endingTimer < 0.5f) return;

        SaveSystem.ClearPendingResume();

        if (!string.IsNullOrEmpty(endingReturnScene))
        {
            SceneManager.LoadScene(endingReturnScene);
        }
    }
}