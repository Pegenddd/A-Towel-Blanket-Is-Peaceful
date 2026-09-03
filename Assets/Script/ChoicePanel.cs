using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChoicePanel : MonoBehaviour
{
    [Header("Panel Containers")]
    public RectTransform panelRoot;
    public CanvasGroup canvasGroup;
    public Image panelBackground;

    [Header("Prompt / Question")]
    public TMP_Text promptText;
    public GameObject promptContainer;

    [Header("Tap Choice Card (Click / Tap)")]
    public RectTransform tapCardTransform;
    public Image tapCardBackground;
    public TMP_Text tapChoiceText;
    public TMP_Text tapHintText;
    public Button tapButton;

    [Header("Hold Choice Card (Hold)")]
    public RectTransform holdCardTransform;
    public Image holdCardBackground;
    public TMP_Text holdChoiceText;
    public TMP_Text holdHintText;
    public Image holdProgressBar;
    public Image holdProgressBackground;
    public Button holdButton;

    [Header("Visual Styling & Colors")]
    public Color panelColor = new Color(0.06f, 0.07f, 0.11f, 0.90f);
    public Color tapCardColor = new Color(0.16f, 0.22f, 0.32f, 0.95f);
    public Color holdCardColor = new Color(0.28f, 0.16f, 0.24f, 0.95f);
    public Color gaugeStartColor = new Color(0.35f, 0.75f, 1f, 0.9f);
    public Color gaugeFullColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color holdSuccessColor = Color.white;

    [Header("Animation Settings")]
    [Tooltip("Scale multiplier for Hold Card while charging")]
    public float holdScaleMultiplier = 1.08f;
    [Tooltip("Subtle card shake when hold progress is near full (> 75%)")]
    public bool enableShakeNearFull = true;
    public float shakeIntensity = 3.5f;
    [Tooltip("Fade transition speed")]
    public float fadeSpeed = 8f;

    [Header("Hint Labels")]
    public bool showKeyHints = true;
    public string defaultTapHint = "[ Click / Space ]";
    public string defaultHoldHint = "[ Hold Space / Click ]";

    [Header("Audio (Optional)")]
    public AudioClip openSound;
    public AudioClip tapClickSound;
    public AudioClip holdSuccessSound;

    public System.Action OnTapAction;
    public System.Action OnHoldAction;
    public System.Action OnHoldDownAction;
    public System.Action OnHoldUpAction;

    private bool isHoldingDirectly = false;
    private Vector3 initialHoldCardPos;
    private Coroutine fadeCoroutine;

    public bool IsHoldingDirectly => isHoldingDirectly;

    void Awake()
    {
        if (panelRoot == null) panelRoot = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (panelBackground == null) panelBackground = GetComponent<Image>();

        CacheInitialPosition();
        SetupButtonListeners();
        ApplyStyles();
    }

    void CacheInitialPosition()
    {
        if (holdCardTransform != null)
        {
            initialHoldCardPos = holdCardTransform.localPosition;
        }
    }

    void SetupButtonListeners()
    {
        if (tapButton != null)
        {
            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(() =>
            {
                if (tapClickSound != null) PlayAudio(tapClickSound);
                OnTapAction?.Invoke();
            });
        }

        if (holdButton != null)
        {
            EventTrigger trigger = holdButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = holdButton.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) =>
            {
                isHoldingDirectly = true;
                OnHoldDownAction?.Invoke();
            });
            trigger.triggers.Add(entryDown);

            EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) =>
            {
                isHoldingDirectly = false;
                OnHoldUpAction?.Invoke();
            });
            trigger.triggers.Add(entryUp);
        }
    }

    public void Setup(DialogueData dialogue, System.Action onTap, System.Action onHold, System.Action onHoldDown = null, System.Action onHoldUp = null)
    {
        OnTapAction = onTap;
        OnHoldAction = onHold;
        OnHoldDownAction = onHoldDown;
        OnHoldUpAction = onHoldUp;
        isHoldingDirectly = false;

        if (promptText != null)
        {
            promptText.text = string.IsNullOrEmpty(dialogue.choicePrompt)
                ? "Choose your path (Tap or Hold)"
                : dialogue.choicePrompt;
        }

        if (tapChoiceText != null)
        {
            tapChoiceText.text = dialogue.tapChoiceText;
        }
        if (tapHintText != null)
        {
            tapHintText.gameObject.SetActive(showKeyHints);
            tapHintText.text = defaultTapHint;
        }

        if (holdChoiceText != null)
        {
            holdChoiceText.text = dialogue.holdChoiceText;
        }
        if (holdHintText != null)
        {
            holdHintText.gameObject.SetActive(showKeyHints);
            holdHintText.text = defaultHoldHint;
        }

        ResetVisuals();
    }

    public void Show()
    {
        CacheInitialPosition();
        gameObject.SetActive(true);
        if (openSound != null) PlayAudio(openSound);

        if (canvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f));
        }
    }

    public void Hide()
    {
        isHoldingDirectly = false;
        if (canvasGroup != null && gameObject.activeInHierarchy)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, () => gameObject.SetActive(false)));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateHoldProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = progress;
            holdProgressBar.color = Color.Lerp(gaugeStartColor, gaugeFullColor, progress);
        }

        if (holdCardTransform != null)
        {
            float scale = 1f + (progress * (holdScaleMultiplier - 1f));
            holdCardTransform.localScale = new Vector3(scale, scale, 1f);

            if (enableShakeNearFull && progress >= 0.75f)
            {
                float shakeAmount = (progress - 0.75f) / 0.25f * shakeIntensity;
                Vector2 randomOffset = Random.insideUnitCircle * shakeAmount;
                holdCardTransform.localPosition = initialHoldCardPos + new Vector3(randomOffset.x, randomOffset.y, 0f);
            }
            else
            {
                holdCardTransform.localPosition = initialHoldCardPos;
            }
        }
    }

    public void ResetVisuals()
    {
        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = 0f;
            holdProgressBar.color = gaugeStartColor;
        }

        if (holdCardTransform != null)
        {
            holdCardTransform.localScale = Vector3.one;
            holdCardTransform.localPosition = initialHoldCardPos;
        }

        if (tapCardTransform != null)
        {
            tapCardTransform.localScale = Vector3.one;
        }
    }

    public IEnumerator PlayHoldSuccessAnimationRoutine(System.Action onComplete)
    {
        if (holdSuccessSound != null) PlayAudio(holdSuccessSound);

        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = 1f;
            holdProgressBar.color = holdSuccessColor;
        }

        if (holdCardTransform != null)
        {
            holdCardTransform.localScale = Vector3.one * (holdScaleMultiplier + 0.08f);
            holdCardTransform.localPosition = initialHoldCardPos;
        }

        yield return new WaitForSeconds(0.22f);

        ResetVisuals();
        onComplete?.Invoke();
    }

    IEnumerator FadeRoutine(float from, float to, System.Action onComplete = null)
    {
        canvasGroup.alpha = from;
        while (!Mathf.Approximately(canvasGroup.alpha, to))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, to, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    private void PlayAudio(AudioClip clip)
    {
        AudioManager am = FindFirstObjectByType<AudioManager>();
        if (am != null)
        {
            am.PlaySFX(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    [ContextMenu("Apply Styles to Children")]
    public void ApplyStyles()
    {
        if (panelBackground != null) panelBackground.color = panelColor;
        if (tapCardBackground != null) tapCardBackground.color = tapCardColor;
        if (holdCardBackground != null) holdCardBackground.color = holdCardColor;
        if (holdProgressBar != null) holdProgressBar.color = gaugeStartColor;
        if (tapHintText != null) tapHintText.gameObject.SetActive(showKeyHints);
        if (holdHintText != null) holdHintText.gameObject.SetActive(showKeyHints);
    }

#if UNITY_EDITOR
    [ContextMenu("Re-align Card Positions")]
    public void RealignCards()
    {
        if (tapCardTransform != null)
        {
            tapCardTransform.anchorMin = new Vector2(0.5f, 0.5f);
            tapCardTransform.anchorMax = new Vector2(0.5f, 0.5f);
            tapCardTransform.pivot = new Vector2(0.5f, 0.5f);
            tapCardTransform.anchoredPosition = new Vector2(-142f, -22f);
        }
        if (holdCardTransform != null)
        {
            holdCardTransform.anchorMin = new Vector2(0.5f, 0.5f);
            holdCardTransform.anchorMax = new Vector2(0.5f, 0.5f);
            holdCardTransform.pivot = new Vector2(0.5f, 0.5f);
            holdCardTransform.anchoredPosition = new Vector2(142f, -22f);
            initialHoldCardPos = holdCardTransform.localPosition;
        }
    }

    [MenuItem("GameObject/UI/Towel Blanket/Custom Choice Panel", false, 10)]
    public static ChoicePanel CreateChoicePanelHierarchy()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<EventSystem>();
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                                   ?? System.Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");
                if (inputModuleType != null) eventObj.AddComponent(inputModuleType);
            }
        }

        GameObject panelObj = new GameObject("ChoicePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(ChoicePanel));
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Choice Panel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 35f);
        panelRect.sizeDelta = new Vector2(600f, 230f);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.07f, 0.11f, 0.92f);

        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        ChoicePanel cp = panelObj.GetComponent<ChoicePanel>();
        cp.panelRoot = panelRect;
        cp.panelBackground = panelImg;
        cp.canvasGroup = cg;

        GameObject promptObj = new GameObject("ChoicePrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptObj.transform.SetParent(panelObj.transform, false);
        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 1f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.anchoredPosition = new Vector2(0f, -14f);
        promptRect.sizeDelta = new Vector2(-40f, 36f);

        TextMeshProUGUI promptText = promptObj.GetComponent<TextMeshProUGUI>();
        promptText.fontSize = 20;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = new Color(1f, 0.92f, 0.55f, 1f);
        promptText.text = "Choose Your Path";
        cp.promptText = promptText;
        cp.promptContainer = promptObj;

        GameObject tapCard = new GameObject("TapChoiceCard", typeof(RectTransform), typeof(Image), typeof(Button));
        tapCard.transform.SetParent(panelObj.transform, false);
        RectTransform tapRect = tapCard.GetComponent<RectTransform>();
        tapRect.anchorMin = new Vector2(0.5f, 0.5f);
        tapRect.anchorMax = new Vector2(0.5f, 0.5f);
        tapRect.pivot = new Vector2(0.5f, 0.5f);
        tapRect.anchoredPosition = new Vector2(-142f, -22f);
        tapRect.sizeDelta = new Vector2(255f, 120f);

        Image tapImg = tapCard.GetComponent<Image>();
        tapImg.color = new Color(0.16f, 0.22f, 0.32f, 0.95f);
        Button tapBtn = tapCard.GetComponent<Button>();
        tapBtn.targetGraphic = tapImg;

        GameObject tapHintObj = new GameObject("TapHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        tapHintObj.transform.SetParent(tapCard.transform, false);
        RectTransform tapHintRect = tapHintObj.GetComponent<RectTransform>();
        tapHintRect.anchorMin = new Vector2(0f, 1f);
        tapHintRect.anchorMax = new Vector2(1f, 1f);
        tapHintRect.pivot = new Vector2(0.5f, 1f);
        tapHintRect.anchoredPosition = new Vector2(0f, -8f);
        tapHintRect.sizeDelta = new Vector2(-20f, 22f);

        TextMeshProUGUI tapHintTMP = tapHintObj.GetComponent<TextMeshProUGUI>();
        tapHintTMP.fontSize = 13;
        tapHintTMP.alignment = TextAlignmentOptions.Center;
        tapHintTMP.color = new Color(0.55f, 0.8f, 1f, 0.9f);
        tapHintTMP.text = "[ Click / Space ]";

        GameObject tapTextObj = new GameObject("TapText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tapTextObj.transform.SetParent(tapCard.transform, false);
        RectTransform tapTextRect = tapTextObj.GetComponent<RectTransform>();
        tapTextRect.anchorMin = Vector2.zero;
        tapTextRect.anchorMax = Vector2.one;
        tapTextRect.sizeDelta = new Vector2(-20f, -36f);
        tapTextRect.anchoredPosition = new Vector2(0f, -10f);

        TextMeshProUGUI tapTextTMP = tapTextObj.GetComponent<TextMeshProUGUI>();
        tapTextTMP.fontSize = 17;
        tapTextTMP.alignment = TextAlignmentOptions.Center;
        tapTextTMP.color = Color.white;
        tapTextTMP.text = "Tap Choice";

        cp.tapCardTransform = tapRect;
        cp.tapCardBackground = tapImg;
        cp.tapChoiceText = tapTextTMP;
        cp.tapHintText = tapHintTMP;
        cp.tapButton = tapBtn;

        GameObject holdCard = new GameObject("HoldChoiceCard", typeof(RectTransform), typeof(Image), typeof(Button));
        holdCard.transform.SetParent(panelObj.transform, false);
        RectTransform holdRect = holdCard.GetComponent<RectTransform>();
        holdRect.anchorMin = new Vector2(0.5f, 0.5f);
        holdRect.anchorMax = new Vector2(0.5f, 0.5f);
        holdRect.pivot = new Vector2(0.5f, 0.5f);
        holdRect.anchoredPosition = new Vector2(142f, -22f);
        holdRect.sizeDelta = new Vector2(255f, 120f);

        Image holdImg = holdCard.GetComponent<Image>();
        holdImg.color = new Color(0.28f, 0.16f, 0.24f, 0.95f);
        Button holdBtn = holdCard.GetComponent<Button>();
        holdBtn.targetGraphic = holdImg;

        GameObject holdHintObj = new GameObject("HoldHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        holdHintObj.transform.SetParent(holdCard.transform, false);
        RectTransform holdHintRect = holdHintObj.GetComponent<RectTransform>();
        holdHintRect.anchorMin = new Vector2(0f, 1f);
        holdHintRect.anchorMax = new Vector2(1f, 1f);
        holdHintRect.pivot = new Vector2(0.5f, 1f);
        holdHintRect.anchoredPosition = new Vector2(0f, -8f);
        holdHintRect.sizeDelta = new Vector2(-20f, 22f);

        TextMeshProUGUI holdHintTMP = holdHintObj.GetComponent<TextMeshProUGUI>();
        holdHintTMP.fontSize = 13;
        holdHintTMP.alignment = TextAlignmentOptions.Center;
        holdHintTMP.color = new Color(1f, 0.65f, 0.65f, 0.9f);
        holdHintTMP.text = "[ Hold Space / Click ]";

        GameObject holdTextObj = new GameObject("HoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
        holdTextObj.transform.SetParent(holdCard.transform, false);
        RectTransform holdTextRect = holdTextObj.GetComponent<RectTransform>();
        holdTextRect.anchorMin = Vector2.zero;
        holdTextRect.anchorMax = Vector2.one;
        holdTextRect.sizeDelta = new Vector2(-20f, -48f);
        holdTextRect.anchoredPosition = new Vector2(0f, 6f);

        TextMeshProUGUI holdTextTMP = holdTextObj.GetComponent<TextMeshProUGUI>();
        holdTextTMP.fontSize = 17;
        holdTextTMP.alignment = TextAlignmentOptions.Center;
        holdTextTMP.color = Color.white;
        holdTextTMP.text = "Hold Choice";

        GameObject barBgObj = new GameObject("ProgressBar_BG", typeof(RectTransform), typeof(Image));
        barBgObj.transform.SetParent(holdCard.transform, false);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.08f, 0.10f);
        barBgRect.anchorMax = new Vector2(0.92f, 0.22f);
        barBgRect.sizeDelta = Vector2.zero;

        Image barBgImg = barBgObj.GetComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

        GameObject barFillObj = new GameObject("ProgressBar_Fill", typeof(RectTransform), typeof(Image));
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform barFillRect = barFillObj.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.sizeDelta = Vector2.zero;

        Image barFillImg = barFillObj.GetComponent<Image>();
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = 0.5f;
        barFillImg.color = cp.gaugeStartColor;

        cp.holdCardTransform = holdRect;
        cp.holdCardBackground = holdImg;
        cp.holdChoiceText = holdTextTMP;
        cp.holdHintText = holdHintTMP;
        cp.holdProgressBar = barFillImg;
        cp.holdProgressBackground = barBgImg;
        cp.holdButton = holdBtn;

        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm != null && dm.choicePanel == null)
        {
            Undo.RecordObject(dm, "Assign Choice Panel to DialogueManager");
            dm.choicePanel = cp;
            EditorUtility.SetDirty(dm);
            Debug.Log("<color=green>[ChoicePanel]</color> ChoicePanel connected to DialogueManager in Scene successfully!");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(panelObj, "Assets/Prefabs/ChoicePanel.prefab", InteractionMode.AutomatedAction);

        Selection.activeGameObject = panelObj;
        Debug.Log("<color=cyan>[ChoicePanel]</color> Custom Choice Panel created successfully and saved to Assets/Prefabs/ChoicePanel.prefab!");
        return cp;
    }
#endif
}
