using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    public string defaultPlayScene = "First";

    [Header("Menu Buttons")]
    public Button playButton;
    public Button continueButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Continue Button Details")]
    public TMP_Text continueSubtitleText;
    public CanvasGroup continueCanvasGroup;

    [Header("Settings Dialog")]
    public SettingsUI settingsDialog;

    [Header("Audio")]
    public AudioClip menuBGM;

    [Header("Feedback Toast")]
    public GameObject toastRoot;
    public TMP_Text toastText;
    private Coroutine toastCoroutine;

    void Awake()
    {
        EnsureAudioManager();
        SetupButtons();

        ChoicePanel cp = FindAnyObjectByType<ChoicePanel>(FindObjectsInactive.Include);
        if (cp != null)
        {
            cp.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        RefreshContinueButton();

        if (menuBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(menuBGM);
        }
        else if (AudioManager.Instance != null && AudioManager.Instance.defaultBGM != null && !AudioManager.Instance.IsBGMPlaying())
        {
            AudioManager.Instance.PlayBGM(AudioManager.Instance.defaultBGM);
        }

        if (toastRoot != null)
        {
            toastRoot.SetActive(false);
        }

        if (settingsDialog != null)
        {
            settingsDialog.Close();
        }
    }

    void OnEnable()
    {
        RefreshContinueButton();
    }

    private void EnsureAudioManager()
    {
        if (AudioManager.Instance == null)
        {
            AudioManager am = FindAnyObjectByType<AudioManager>();
            if (am == null)
            {
                GameObject amObj = new GameObject("AudioManager");
                am = amObj.AddComponent<AudioManager>();
            }
            am.EnsureAudioSources();
        }
    }

    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    public void RefreshContinueButton()
    {
        bool hasSave = SaveSystem.HasSaveData();

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
        }

        if (continueCanvasGroup != null)
        {
            continueCanvasGroup.alpha = hasSave ? 1.0f : 0.45f;
        }

        if (continueSubtitleText != null)
        {
            if (hasSave)
            {
                SaveData save = SaveSystem.LoadGame();
                if (save != null)
                {
                    string scenePreview = string.IsNullOrEmpty(save.sceneName) ? "Story" : save.sceneName;
                    continueSubtitleText.text = LocalizationManager.Get("menu_resume_hint", scenePreview, save.saveDateFormatted);
                }
                else
                {
                    continueSubtitleText.text = "";
                }
            }
            else
            {
                continueSubtitleText.text = LocalizationManager.Get("menu_no_save");
            }
        }
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuUI] Play clicked. Starting new story at scene: " + defaultPlayScene);
        SaveSystem.ClearPendingResume();
        SceneManager.LoadScene(defaultPlayScene);
    }

    private void OnContinueClicked()
    {
        if (!SaveSystem.HasSaveData())
        {
            ShowToast(LocalizationManager.Get("menu_no_save"));
            return;
        }

        SaveData save = SaveSystem.LoadGame();
        if (save != null)
        {
            Debug.Log($"[MainMenuUI] Continue clicked. Resuming {save.sceneName} at dialogue {save.dialogueIndex}...");
            SaveSystem.PrepareResume(save.dialogueIndex);
            SceneManager.LoadScene(save.sceneName);
        }
        else
        {
            ShowToast(LocalizationManager.Get("menu_no_save"));
        }
    }

    private void OnSettingsClicked()
    {
        if (settingsDialog != null)
        {
            settingsDialog.Open();
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quit clicked.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowToast(string message, float duration = 2.0f)
    {
        if (toastRoot == null || toastText == null) return;

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }
        toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        toastText.text = message;
        toastRoot.SetActive(true);
        yield return new WaitForSeconds(duration);
        toastRoot.SetActive(false);
    }
}
