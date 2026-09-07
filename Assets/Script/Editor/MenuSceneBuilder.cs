#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[InitializeOnLoad]
public static class MenuSceneBuilder
{
    private static readonly Color BgColor = new Color(0.06f, 0.08f, 0.12f, 1f);
    private static readonly Color CardBgColor = new Color(0.10f, 0.14f, 0.22f, 0.95f);
    private static readonly Color ButtonNormalColor = new Color(0.14f, 0.20f, 0.32f, 0.95f);
    private static readonly Color ButtonHighlightColor = new Color(0.22f, 0.32f, 0.50f, 1f);
    private static readonly Color ButtonPressedColor = new Color(0.10f, 0.15f, 0.25f, 1f);
    private static readonly Color BorderColor = new Color(0.25f, 0.38f, 0.55f, 0.5f);
    private static readonly Color TextPrimaryColor = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color TextSecondaryColor = new Color(0.65f, 0.74f, 0.88f, 0.9f);
    private static readonly Color AccentColor = new Color(0.28f, 0.52f, 0.86f, 1f);

    [MenuItem("Tools/Towel Blanket/1. Build Main Menu Scene", false, 1)]
    public static void BuildMainMenuScene()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";

        // Save active scene first if dirty
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.AddComponent<AudioListener>();
        camObj.tag = "MainCamera";

        // 2. Event System
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        EnsureInputModule(esObj);

        // 3. Audio Manager
        GameObject amObj = new GameObject("AudioManager");
        AudioManager am = amObj.AddComponent<AudioManager>();
        am.EnsureAudioSources();

        // 4. Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 5. Main Menu UI Root
        GameObject menuRoot = new GameObject("MainMenuRoot", typeof(RectTransform), typeof(MainMenuUI));
        menuRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        StretchFull(rootRect);
        MainMenuUI menuUI = menuRoot.GetComponent<MainMenuUI>();

        // 6. Background Image / Tint
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(menuRoot.transform, false);
        StretchFull(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.GetComponent<Image>();
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/picture/scense01.png");
        if (bgSprite == null) bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/picture/Colseeye.jpg");
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            bgImg.color = new Color(0.45f, 0.50f, 0.60f, 1f);
        }
        else
        {
            bgImg.color = BgColor;
        }

        // Vignette Overlay
        GameObject overlayObj = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(menuRoot.transform, false);
        StretchFull(overlayObj.GetComponent<RectTransform>());
        Image overlayImg = overlayObj.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.06f, 0.10f, 0.70f);

        // 7. Title Panel
        GameObject titleObj = new GameObject("TitleContainer", typeof(RectTransform));
        titleObj.transform.SetParent(menuRoot.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.75f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);
        titleRect.sizeDelta = new Vector2(900f, 160f);

        GameObject titleTextObj = CreateText(titleObj, "TitleText", "A Towel Blanket Is Peaceful", 48, TextAlignmentOptions.Center, TextPrimaryColor, true);
        RectTransform titleTRect = titleTextObj.GetComponent<RectTransform>();
        titleTRect.anchoredPosition = new Vector2(0f, 25f);
        titleTRect.sizeDelta = new Vector2(850f, 65f);
        LocalizedText titleLoc = titleTextObj.AddComponent<LocalizedText>();
        titleLoc.localizationKey = "menu_title";

        GameObject subTextObj = CreateText(titleObj, "SubtitleText", "An Interactive Visual Novel", 20, TextAlignmentOptions.Center, TextSecondaryColor, false);
        RectTransform subTRect = subTextObj.GetComponent<RectTransform>();
        subTRect.anchoredPosition = new Vector2(0f, -25f);
        subTRect.sizeDelta = new Vector2(700f, 35f);
        LocalizedText subLoc = subTextObj.AddComponent<LocalizedText>();
        subLoc.localizationKey = "menu_subtitle";

        // 8. Buttons Container
        GameObject buttonsObj = new GameObject("ButtonsContainer", typeof(RectTransform));
        buttonsObj.transform.SetParent(menuRoot.transform, false);
        RectTransform buttonsRect = buttonsObj.GetComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0.38f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.38f);
        buttonsRect.pivot = new Vector2(0.5f, 0.5f);
        buttonsRect.anchoredPosition = new Vector2(0f, 0f);
        buttonsRect.sizeDelta = new Vector2(420f, 340f);

        // Play Button
        Button playBtn = CreateStyledButton(buttonsObj, "PlayButton", "Play", "menu_play", new Vector2(0f, 105f), new Vector2(380f, 58f));
        menuUI.playButton = playBtn;

        // Continue Button
        GameObject contBtnObj = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        contBtnObj.transform.SetParent(buttonsObj.transform, false);
        RectTransform contRect = contBtnObj.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.5f, 0.5f);
        contRect.anchorMax = new Vector2(0.5f, 0.5f);
        contRect.pivot = new Vector2(0.5f, 0.5f);
        contRect.anchoredPosition = new Vector2(0f, 35f);
        contRect.sizeDelta = new Vector2(380f, 66f);

        Image contImg = contBtnObj.GetComponent<Image>();
        contImg.color = ButtonNormalColor;
        Button contBtn = contBtnObj.GetComponent<Button>();
        ConfigureButtonColors(contBtn, contImg);
        CanvasGroup contCg = contBtnObj.GetComponent<CanvasGroup>();

        GameObject contTitleObj = CreateText(contBtnObj, "Title", "Continue", 24, TextAlignmentOptions.Center, TextPrimaryColor, true);
        RectTransform cTitleRect = contTitleObj.GetComponent<RectTransform>();
        cTitleRect.anchoredPosition = new Vector2(0f, 11f);
        cTitleRect.sizeDelta = new Vector2(360f, 32f);
        LocalizedText contLoc = contTitleObj.AddComponent<LocalizedText>();
        contLoc.localizationKey = "menu_continue";

        GameObject contSubObj = CreateText(contBtnObj, "Subtitle", "No save data found", 13, TextAlignmentOptions.Center, TextSecondaryColor, false);
        RectTransform cSubRect = contSubObj.GetComponent<RectTransform>();
        cSubRect.anchoredPosition = new Vector2(0f, -14f);
        cSubRect.sizeDelta = new Vector2(360f, 22f);

        menuUI.continueButton = contBtn;
        menuUI.continueSubtitleText = contSubObj.GetComponent<TMP_Text>();
        menuUI.continueCanvasGroup = contCg;

        // Settings Button
        Button settingsBtn = CreateStyledButton(buttonsObj, "SettingsButton", "Settings", "menu_settings", new Vector2(0f, -42f), new Vector2(380f, 58f));
        menuUI.settingsButton = settingsBtn;

        // Quit Button
        Button quitBtn = CreateStyledButton(buttonsObj, "QuitButton", "Quit", "menu_quit", new Vector2(0f, -112f), new Vector2(380f, 58f));
        menuUI.quitButton = quitBtn;

        // 9. Settings Dialog Modal
        SettingsUI settingsModal = CreateSettingsModal(canvasObj);
        menuUI.settingsDialog = settingsModal;

        // 10. Toast Notification
        GameObject toastObj = CreateToastObject(menuRoot);
        menuUI.toastRoot = toastObj;
        menuUI.toastText = toastObj.GetComponentInChildren<TMP_Text>();

        // Set default play scene
        menuUI.defaultPlayScene = "First";

        // Assign menu BGM if found
        AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/SIGNALIS - 3000 Cycles (I missed you) [Extended].mp3");
        if (bgm != null)
        {
            menuUI.menuBGM = bgm;
        }

        // Save Scene
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("<color=green>[MenuSceneBuilder] MainMenu.unity created and saved successfully!</color>");

        UpdateBuildSettings();
    }

    [MenuItem("Tools/Towel Blanket/2. Add In-Game Menu to Current Scene", false, 2)]
    public static void AddInGameMenuToCurrentScene()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MenuSceneBuilder] No Canvas found in current scene! Please open a scene with a Canvas first.");
            return;
        }

        InGameMenuUI existing = UnityEngine.Object.FindAnyObjectByType<InGameMenuUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Debug.Log("[MenuSceneBuilder] InGameMenuUI already exists in scene.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject inGameRoot = new GameObject("InGameMenuRoot", typeof(RectTransform), typeof(InGameMenuUI));
        inGameRoot.transform.SetParent(canvas.transform, false);
        StretchFull(inGameRoot.GetComponent<RectTransform>());
        InGameMenuUI igm = inGameRoot.GetComponent<InGameMenuUI>();

        // 1. HUD Button (Top Right) - Setting Orb Button
        GameObject hudBtnObj = new GameObject("SettingHUDButton", typeof(RectTransform), typeof(Image), typeof(Button));
        hudBtnObj.transform.SetParent(inGameRoot.transform, false);
        RectTransform hudRect = hudBtnObj.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        hudRect.anchoredPosition = new Vector2(-28f, -28f);
        hudRect.sizeDelta = new Vector2(58f, 50f);

        Image hudImg = hudBtnObj.GetComponent<Image>();
        Sprite settingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SettingButton.png");
        if (settingSprite != null)
        {
            hudImg.sprite = settingSprite;
            hudImg.color = Color.white;
            hudImg.preserveAspect = true;
        }
        else
        {
            hudImg.color = ButtonNormalColor;
        }
        Button hudBtn = hudBtnObj.GetComponent<Button>();
        ColorBlock cb = hudBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.9f, 0.98f, 1f);
        cb.pressedColor = new Color(0.85f, 0.7f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        hudBtn.colors = cb;

        igm.hudMenuButton = hudBtn;

        // 2. Pause Overlay
        GameObject pausePanelObj = new GameObject("PausePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        pausePanelObj.transform.SetParent(inGameRoot.transform, false);
        StretchFull(pausePanelObj.GetComponent<RectTransform>());
        Image pauseBg = pausePanelObj.GetComponent<Image>();
        pauseBg.color = new Color(0.04f, 0.06f, 0.10f, 0.82f);
        CanvasGroup pauseCg = pausePanelObj.GetComponent<CanvasGroup>();

        // Modal Card
        GameObject cardObj = new GameObject("PauseCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(440f, 420f);
        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = CardBgColor;

        // Title
        GameObject pTitleObj = CreateText(cardObj, "Title", "Paused", 32, TextAlignmentOptions.Center, TextPrimaryColor, true);
        RectTransform pTitleRect = pTitleObj.GetComponent<RectTransform>();
        pTitleRect.anchorMin = new Vector2(0.5f, 1f);
        pTitleRect.anchorMax = new Vector2(0.5f, 1f);
        pTitleRect.pivot = new Vector2(0.5f, 1f);
        pTitleRect.anchoredPosition = new Vector2(0f, -24f);
        pTitleRect.sizeDelta = new Vector2(380f, 45f);
        LocalizedText pTitleLoc = pTitleObj.AddComponent<LocalizedText>();
        pTitleLoc.localizationKey = "pause_title";

        // Buttons in Pause Card
        Button resumeBtn = CreateStyledButton(cardObj, "ResumeButton", "Resume", "pause_resume", new Vector2(0f, 40f), new Vector2(340f, 52f));
        Button saveBtn = CreateStyledButton(cardObj, "SaveButton", "Save Game", "pause_save", new Vector2(0f, -22f), new Vector2(340f, 52f));
        Button settingsBtn = CreateStyledButton(cardObj, "SettingsButton", "Settings", "pause_settings", new Vector2(0f, -84f), new Vector2(340f, 52f));
        Button menuBtn = CreateStyledButton(cardObj, "MainMenuButton", "Main Menu", "pause_main_menu", new Vector2(0f, -146f), new Vector2(340f, 52f));

        igm.pausePanel = pausePanelObj;
        igm.pauseCanvasGroup = pauseCg;
        igm.resumeButton = resumeBtn;
        igm.saveButton = saveBtn;
        igm.settingsButton = settingsBtn;
        igm.mainMenuButton = menuBtn;

        // 3. Settings Modal
        SettingsUI settingsModal = CreateSettingsModal(canvas.gameObject);
        igm.settingsDialog = settingsModal;

        // 4. Toast Notification
        GameObject toastObj = CreateToastObject(inGameRoot);
        igm.toastRoot = toastObj;
        igm.toastText = toastObj.GetComponentInChildren<TMP_Text>();

        igm.dialogueManager = UnityEngine.Object.FindAnyObjectByType<DialogueManager>();
        igm.mainMenuSceneName = "MainMenu";

        pausePanelObj.SetActive(false);

        EditorUtility.SetDirty(inGameRoot);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green>[MenuSceneBuilder] In-Game Menu added to scene successfully!</color>");
    }

    [MenuItem("Tools/Towel Blanket/Setup In-Game Menu in First Scene", false, 4)]
    public static void SetupInGameMenuInFirstScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/First.unity");
        AddInGameMenuToCurrentScene();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("<color=green>[MenuSceneBuilder] First.unity updated with In-Game Menu and saved!</color>");
    }

    [MenuItem("Tools/Towel Blanket/Update Settings Modals In All Scenes", false, 15)]
    public static void UpdateSettingsModalsInAllScenes()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;

        UpdateSettingsModalInScene("Assets/Scenes/MainMenu.unity", isMainMenu: true);
        UpdateSettingsModalInScene("Assets/Scenes/First.unity", isMainMenu: false);

        if (!string.IsNullOrEmpty(currentScene) && File.Exists(currentScene))
        {
            EditorSceneManager.OpenScene(currentScene);
        }
        Debug.Log("<color=green>[MenuSceneBuilder] Settings modals updated in all scenes successfully!</color>");
    }

    private static void UpdateSettingsModalInScene(string scenePath, bool isMainMenu)
    {
        if (!File.Exists(scenePath)) return;

        EditorSceneManager.OpenScene(scenePath);

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        SettingsUI existingModal = UnityEngine.Object.FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (existingModal != null)
        {
            UnityEngine.Object.DestroyImmediate(existingModal.gameObject);
        }

        SettingsUI newModal = CreateSettingsModal(canvas.gameObject);

        if (isMainMenu)
        {
            MainMenuUI menuUI = UnityEngine.Object.FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (menuUI != null)
            {
                menuUI.settingsDialog = newModal;
                EditorUtility.SetDirty(menuUI);
            }
        }
        else
        {
            InGameMenuUI igm = UnityEngine.Object.FindAnyObjectByType<InGameMenuUI>(FindObjectsInactive.Include);
            if (igm != null)
            {
                igm.settingsDialog = newModal;
                EditorUtility.SetDirty(igm);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Tools/Towel Blanket/3. Update Build Settings", false, 3)]
    public static void UpdateBuildSettings()
    {
        string[] targetScenes = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/First.unity",
            "Assets/Scenes/SampleScene.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

        foreach (string path in targetScenes)
        {
            if (File.Exists(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"<color=green>[MenuSceneBuilder] Build Settings updated with {buildScenes.Count} scenes (MainMenu as Scene 0).</color>");
    }

    public static SettingsUI CreateSettingsModal(GameObject parentCanvas)
    {
        GameObject modalObj = new GameObject("SettingsModal", typeof(RectTransform), typeof(CanvasGroup), typeof(SettingsUI));
        modalObj.transform.SetParent(parentCanvas.transform, false);
        StretchFull(modalObj.GetComponent<RectTransform>());

        SettingsUI sui = modalObj.GetComponent<SettingsUI>();
        sui.panelRoot = modalObj;
        sui.canvasGroup = modalObj.GetComponent<CanvasGroup>();

        // Backdrop
        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(modalObj.transform, false);
        StretchFull(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.88f);

        // Modal Frame Card
        GameObject card = new GameObject("SettingsCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(modalObj.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(620f, 620f);
        Image cardImg = card.GetComponent<Image>();
        cardImg.color = CardBgColor;

        // Title
        GameObject title = CreateText(card, "Title", "Settings", 32, TextAlignmentOptions.Center, TextPrimaryColor, true);
        RectTransform tRect = title.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -22f);
        tRect.sizeDelta = new Vector2(400f, 40f);
        title.AddComponent<LocalizedText>().localizationKey = "settings_title";

        // Audio Header
        GameObject aHeader = CreateText(card, "AudioHeader", "Audio Settings", 20, TextAlignmentOptions.Left, AccentColor, true);
        RectTransform ahRect = aHeader.GetComponent<RectTransform>();
        ahRect.anchoredPosition = new Vector2(-40f, 205f);
        ahRect.sizeDelta = new Vector2(460f, 30f);
        aHeader.AddComponent<LocalizedText>().localizationKey = "settings_audio_header";

        // Master Slider Row
        var (masterSlider, masterVal) = CreateSliderRow(card, "MasterSliderRow", "Master Volume", "settings_master", 160f);
        sui.masterSlider = masterSlider;
        sui.masterValueText = masterVal;

        // BGM Slider Row
        var (bgmSlider, bgmVal) = CreateSliderRow(card, "BGMSliderRow", "Music (BGM)", "settings_bgm", 110f);
        sui.bgmSlider = bgmSlider;
        sui.bgmValueText = bgmVal;

        // SFX Slider Row
        var (sfxSlider, sfxVal) = CreateSliderRow(card, "SFXSliderRow", "Sound Effects (SFX)", "settings_sfx", 60f);
        sui.sfxSlider = sfxSlider;
        sui.sfxValueText = sfxVal;

        // Language & Text Header
        GameObject lHeader = CreateText(card, "LangHeader", "Language & Text", 20, TextAlignmentOptions.Left, AccentColor, true);
        RectTransform lhRect = lHeader.GetComponent<RectTransform>();
        lhRect.anchoredPosition = new Vector2(-40f, 5f);
        lhRect.sizeDelta = new Vector2(460f, 30f);
        lHeader.AddComponent<LocalizedText>().localizationKey = "settings_lang_header";

        // Language Buttons Row
        GameObject langRow = new GameObject("LanguageRow", typeof(RectTransform));
        langRow.transform.SetParent(card.transform, false);
        RectTransform lrRect = langRow.GetComponent<RectTransform>();
        lrRect.anchoredPosition = new Vector2(0f, -45f);
        lrRect.sizeDelta = new Vector2(500f, 44f);

        // English Button
        Button enBtn = CreateLangButton(langRow, "EnglishButton", "English", new Vector2(-125f, 0f));
        sui.englishButton = enBtn;
        sui.englishButtonBg = enBtn.GetComponent<Image>();
        sui.englishButtonText = enBtn.GetComponentInChildren<TMP_Text>();

        // Thai Button
        Button thBtn = CreateLangButton(langRow, "ThaiButton", "ภาษาไทย", new Vector2(125f, 0f));
        sui.thaiButton = thBtn;
        sui.thaiButtonBg = thBtn.GetComponent<Image>();
        sui.thaiButtonText = thBtn.GetComponentInChildren<TMP_Text>();

        // Font Size Slider Row
        var (fontSizeSlider, fontSizeVal) = CreateSliderRow(card, "FontSizeSliderRow", "Text Size", "settings_font_size", -105f);
        fontSizeSlider.minValue = SettingsUI.MIN_FONT_SIZE_SCALE;
        fontSizeSlider.maxValue = SettingsUI.MAX_FONT_SIZE_SCALE;
        fontSizeSlider.value = SettingsUI.CurrentFontScale;
        sui.fontSizeSlider = fontSizeSlider;
        sui.fontSizeValueText = fontSizeVal;

        // Close / Back Button
        Button closeBtn = CreateStyledButton(card, "CloseButton", "Back", "settings_back", new Vector2(0f, -235f), new Vector2(260f, 48f));
        sui.closeButton = closeBtn;

        modalObj.SetActive(false);
        return sui;
    }

    private static (Slider, TMP_Text) CreateSliderRow(GameObject parent, string name, string defaultLabel, string locKey, float posY)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0f, posY);
        rowRect.sizeDelta = new Vector2(500f, 40f);

        // Label
        GameObject labelObj = CreateText(row, "Label", defaultLabel, 16, TextAlignmentOptions.Left, TextPrimaryColor, false);
        RectTransform lRect = labelObj.GetComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0f, 0.5f);
        lRect.anchorMax = new Vector2(0f, 0.5f);
        lRect.pivot = new Vector2(0f, 0.5f);
        lRect.anchoredPosition = new Vector2(10f, 0f);
        lRect.sizeDelta = new Vector2(200f, 30f);
        labelObj.AddComponent<LocalizedText>().localizationKey = locKey;

        // Value text
        GameObject valObj = CreateText(row, "ValueText", "100%", 16, TextAlignmentOptions.Right, TextSecondaryColor, false);
        RectTransform vRect = valObj.GetComponent<RectTransform>();
        vRect.anchorMin = new Vector2(1f, 0.5f);
        vRect.anchorMax = new Vector2(1f, 0.5f);
        vRect.pivot = new Vector2(1f, 0.5f);
        vRect.anchoredPosition = new Vector2(-10f, 0f);
        vRect.sizeDelta = new Vector2(55f, 30f);
        TMP_Text valTMP = valObj.GetComponent<TMP_Text>();

        // Slider
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sRect = sliderObj.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.42f, 0.5f);
        sRect.anchorMax = new Vector2(0.86f, 0.5f);
        sRect.pivot = new Vector2(0.5f, 0.5f);
        sRect.anchoredPosition = Vector2.zero;
        sRect.sizeDelta = new Vector2(0f, 20f);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        // Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        StretchFull(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.18f, 0.24f, 0.35f, 0.9f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRect = fillArea.GetComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.25f);
        faRect.anchorMax = new Vector2(1f, 0.75f);
        faRect.sizeDelta = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRect = fill.GetComponent<RectTransform>();
        StretchFull(fRect);
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = AccentColor;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRect = handleArea.GetComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.sizeDelta = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRect = handle.GetComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(18f, 26f);
        Image handleImg = handle.GetComponent<Image>();
        handleImg.color = TextPrimaryColor;

        slider.targetGraphic = handleImg;
        slider.fillRect = fRect;
        slider.handleRect = hRect;
        slider.value = 1f;

        return (slider, valTMP);
    }

    private static Button CreateLangButton(GameObject parent, string name, string label, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform bRect = btnObj.GetComponent<RectTransform>();
        bRect.anchoredPosition = pos;
        bRect.sizeDelta = new Vector2(210f, 44f);

        Image img = btnObj.GetComponent<Image>();
        img.color = ButtonNormalColor;
        Button btn = btnObj.GetComponent<Button>();
        ConfigureButtonColors(btn, img);

        GameObject textObj = CreateText(btnObj, "Text", label, 18, TextAlignmentOptions.Center, TextPrimaryColor, true);
        StretchFull(textObj.GetComponent<RectTransform>());

        return btn;
    }

    private static Button CreateStyledButton(GameObject parent, string name, string defaultText, string locKey, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = ButtonNormalColor;
        Button btn = btnObj.GetComponent<Button>();
        ConfigureButtonColors(btn, img);

        GameObject textObj = CreateText(btnObj, "Text", defaultText, 22, TextAlignmentOptions.Center, TextPrimaryColor, true);
        StretchFull(textObj.GetComponent<RectTransform>());

        if (!string.IsNullOrEmpty(locKey))
        {
            LocalizedText loc = textObj.AddComponent<LocalizedText>();
            loc.localizationKey = locKey;
        }

        return btn;
    }

    private static void ConfigureButtonColors(Button btn, Image targetGraphic)
    {
        btn.targetGraphic = targetGraphic;
        var colors = btn.colors;
        colors.normalColor = ButtonNormalColor;
        colors.highlightedColor = ButtonHighlightColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonNormalColor;
        colors.disabledColor = new Color(0.12f, 0.15f, 0.20f, 0.45f);
        colors.fadeDuration = 0.15f;
        btn.colors = colors;
    }

    private static GameObject CreateText(GameObject parent, string name, string content, float fontSize, TextAlignmentOptions align, Color color, bool bold)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return obj;
    }

    private static GameObject CreateToastObject(GameObject parent)
    {
        GameObject toastObj = new GameObject("ToastNotification", typeof(RectTransform), typeof(Image));
        toastObj.transform.SetParent(parent.transform, false);
        RectTransform tRect = toastObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0f);
        tRect.anchorMax = new Vector2(0.5f, 0f);
        tRect.pivot = new Vector2(0.5f, 0f);
        tRect.anchoredPosition = new Vector2(0f, 40f);
        tRect.sizeDelta = new Vector2(460f, 48f);

        Image toastImg = toastObj.GetComponent<Image>();
        toastImg.color = new Color(0.15f, 0.22f, 0.35f, 0.95f);

        GameObject textObj = CreateText(toastObj, "ToastText", "Message", 17, TextAlignmentOptions.Center, TextPrimaryColor, true);
        StretchFull(textObj.GetComponent<RectTransform>());

        toastObj.SetActive(false);
        return toastObj;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void EnsureInputModule(GameObject esObj)
    {
        Type inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemType != null)
        {
            esObj.AddComponent(inputSystemType);
        }
        else
        {
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    public static string GetThaiAndAsciiCharacterSet()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        // ASCII printable
        for (int c = 32; c <= 126; c++)
        {
            sb.Append((char)c);
        }
        // Thai Unicode 0E01 - 0E5B
        for (int c = 0x0E01; c <= 0x0E5B; c++)
        {
            sb.Append((char)c);
        }
        return sb.ToString();
    }

    [MenuItem("Tools/Towel Blanket/5. Setup Thai Font Asset and Fallbacks", false, 5)]
    public static void SetupThaiFontSupport()
    {
        string fontPath = "Assets/Fonts/Thai_nuttatulipa.ttf";
        if (!File.Exists(fontPath))
        {
            fontPath = "Assets/Fonts/LeelawUI.ttf";
        }
        if (!File.Exists(fontPath))
        {
            fontPath = "Assets/Fonts/tahoma.ttf";
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.LogError("[MenuSceneBuilder] Font file not found at " + fontPath);
            return;
        }

        string assetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/ThaiFont_SDF.asset";

        // Delete existing corrupted or incomplete asset
        if (File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();
        }

        // Also delete old location if present
        if (File.Exists("Assets/Fonts/ThaiFont_SDF.asset"))
        {
            AssetDatabase.DeleteAsset("Assets/Fonts/ThaiFont_SDF.asset");
        }

        TMP_FontAsset thaiFontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            72,
            8,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true
        );

        if (thaiFontAsset == null)
        {
            Debug.LogError("[MenuSceneBuilder] Failed to create TMP_FontAsset from font!");
            return;
        }

        // Bake Thai + ASCII characters into the atlas
        string allChars = GetThaiAndAsciiCharacterSet();
        thaiFontAsset.TryAddCharacters(allChars, true);

        // Ensure Material uses TextMeshPro/Distance Field shader
        if (thaiFontAsset.material != null)
        {
            Shader dfShader = Shader.Find("TextMeshPro/Distance Field");
            if (dfShader != null)
            {
                thaiFontAsset.material.shader = dfShader;
            }
            thaiFontAsset.material.name = "ThaiFont_SDF Material";

            if (thaiFontAsset.atlasTexture != null)
            {
                thaiFontAsset.material.mainTexture = thaiFontAsset.atlasTexture;
                thaiFontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, thaiFontAsset.atlasTexture);
            }
        }

        // Create main asset file first
        AssetDatabase.CreateAsset(thaiFontAsset, assetPath);

        // CRITICAL: Persist atlas textures as sub-assets so they are not destroyed on reload
        if (thaiFontAsset.atlasTextures != null)
        {
            for (int i = 0; i < thaiFontAsset.atlasTextures.Length; i++)
            {
                if (thaiFontAsset.atlasTextures[i] != null)
                {
                    thaiFontAsset.atlasTextures[i].name = "ThaiFont Atlas " + i;
                    AssetDatabase.AddObjectToAsset(thaiFontAsset.atlasTextures[i], thaiFontAsset);
                }
            }
        }

        // Persist material as sub-asset
        if (thaiFontAsset.material != null)
        {
            AssetDatabase.AddObjectToAsset(thaiFontAsset.material, thaiFontAsset);
        }

        EditorUtility.SetDirty(thaiFontAsset);
        if (thaiFontAsset.material != null) EditorUtility.SetDirty(thaiFontAsset.material);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>[MenuSceneBuilder] Created ThaiFont_SDF.asset with baked glyphs and atlas texture!</color>");

        // Add to LiberationSans SDF fallback table
        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (defaultFont != null)
        {
            SerializedObject fontSo = new SerializedObject(defaultFont);
            SerializedProperty fontFallbacks = fontSo.FindProperty("m_FallbackFontAssetTable");
            if (fontFallbacks != null)
            {
                bool exists = false;
                for (int i = 0; i < fontFallbacks.arraySize; i++)
                {
                    if (fontFallbacks.GetArrayElementAtIndex(i).objectReferenceValue == thaiFontAsset)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    fontFallbacks.InsertArrayElementAtIndex(fontFallbacks.arraySize);
                    fontFallbacks.GetArrayElementAtIndex(fontFallbacks.arraySize - 1).objectReferenceValue = thaiFontAsset;
                    fontSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(defaultFont);
                    Debug.Log("<color=green>[MenuSceneBuilder] Added ThaiFont_SDF to LiberationSans SDF Fallback list!</color>");
                }
            }
        }

        // Add to TMP Settings fallbacks
        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty fallbacks = so.FindProperty("m_fallbackFontAssets");
            if (fallbacks != null)
            {
                bool exists = false;
                for (int i = 0; i < fallbacks.arraySize; i++)
                {
                    if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == thaiFontAsset)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    fallbacks.InsertArrayElementAtIndex(fallbacks.arraySize);
                    fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = thaiFontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    Debug.Log("<color=green>[MenuSceneBuilder] Added ThaiFont_SDF to TMP Settings fallbacks!</color>");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[MenuSceneBuilder] Thai Font support setup successfully!</color>");
    }

    [MenuItem("Tools/Towel Blanket/6. Setup Thai Dialogues in First Scene", false, 6)]
    public static void SetupThaiDialogueInFirstScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/First.unity");
        DialogueManager dm = UnityEngine.Object.FindAnyObjectByType<DialogueManager>();
        if (dm != null && dm.dialogues != null)
        {
            // Set Thai sample dialogue lines if empty
            if (dm.dialogues.Length > 0 && dm.dialogues[0] != null && string.IsNullOrEmpty(dm.dialogues[0].textThai))
                dm.dialogues[0].textThai = "คร่อก... ฟี้... (zzz)";

            if (dm.dialogues.Length > 1 && dm.dialogues[1] != null && string.IsNullOrEmpty(dm.dialogues[1].textThai))
                dm.dialogues[1].textThai = "เดี๋ยวนะ นี่ฉันตื่นแล้วเหรอ?";

            if (dm.dialogues.Length > 2 && dm.dialogues[2] != null && string.IsNullOrEmpty(dm.dialogues[2].textThai))
                dm.dialogues[2].textThai = "ง่วงนอนจังเลย...";

            if (dm.dialogues.Length > 3 && dm.dialogues[3] != null)
            {
                if (string.IsNullOrEmpty(dm.dialogues[3].textThai))
                    dm.dialogues[3].textThai = "เธอจะทำอย่างไรต่อไป?";
                dm.dialogues[3].choicePromptThai = "เลือกสิ่งที่จะทำ (คลิก หรือ กดค้าง)";
                dm.dialogues[3].tapChoiceTextThai = "กดธรรมดา (คลิก)";
                dm.dialogues[3].holdChoiceTextThai = "กดค้าง (Hold)";
            }

            EditorUtility.SetDirty(dm);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green>[MenuSceneBuilder] First.unity dialogues updated with Thai text and saved!</color>");
        }
    }

    [MenuItem("Tools/Towel Blanket/7. Setup Intro and Ending Screens in First Scene", false, 7)]
    public static void SetupIntroAndEndingInFirstScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/First.unity");
        DialogueManager dm = UnityEngine.Object.FindAnyObjectByType<DialogueManager>();
        if (dm != null)
        {
            dm.enableIntroQuote = true;
            dm.introQuoteThai = "ยามนั้นเมื่ออดัมและอีฟกัดผลแห่งปัญญา พวกเราก็ร่วงหล่นจากสวนอีเดนตลอดกาล -----------------------------------------------------------------";
            dm.introQuoteEnglish = "At that time, when Adam and Eve bit into the fruit of wisdom, we fell from the Garden of Eden forever -----------------------------------------------------------------";
            dm.introQuoteEnglishFontSize = 24f;
            dm.introQuoteThaiFontSize = 26f;
            dm.introQuoteEnglishHintFontSize = 15f;
            dm.introQuoteThaiHintFontSize = 16f;

            dm.enableEndingScreen = true;
            dm.endingTextThai = "ผ้าม่านยังคงปลิวไสวอยู่เหนือหัว";
            dm.endingTextEnglish = "The curtains still flutter overhead.";
            dm.endingEnglishFontSize = 32f;
            dm.endingThaiFontSize = 34f;
            dm.endingEnglishHintFontSize = 15f;
            dm.endingThaiHintFontSize = 16f;

            dm.overrideExistingMusic = false;
            dm.keepBGMContinuous = true;

            dm.EnsureIntroAndEndingUI();

            EditorUtility.SetDirty(dm);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green>[MenuSceneBuilder] First.unity updated with Intro Quote and Ending Screen!</color>");
        }
    }

    [MenuItem("Tools/Towel Blanket/Setup Everything (Menu, InGame, Thai Font, Build Settings)", false, 0)]
    public static void SetupEverything()
    {
        SetupThaiFontSupport();
        BuildMainMenuScene();
        SetupInGameMenuInFirstScene();
        SetupThaiDialogueInFirstScene();
        SetupIntroAndEndingInFirstScene();
        UpdateBuildSettings();
        Debug.Log("<color=green>[MenuSceneBuilder] ALL SYSTEMS SETUP COMPLETE!</color>");
    }

    [MenuItem("Tools/Towel Blanket/Open Save Folder", false, 20)]
    public static void OpenSaveFolder()
    {
        string dir = Application.persistentDataPath;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        EditorUtility.RevealInFinder(SaveSystem.SaveFilePath);
    }
}
#endif

