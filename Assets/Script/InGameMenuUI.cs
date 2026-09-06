using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameMenuUI : MonoBehaviour
{
    [Header("HUD Trigger Button")]
    public Button hudMenuButton;

    [Header("Pause Overlay")]
    public GameObject pausePanel;
    public CanvasGroup pauseCanvasGroup;

    [Header("Pause Menu Buttons")]
    public Button resumeButton;
    public Button saveButton;
    public Button settingsButton;
    public Button mainMenuButton;

    [Header("Settings Dialog")]
    public SettingsUI settingsDialog;

    [Header("Toast Notification")]
    public GameObject toastRoot;
    public TMP_Text toastText;

    [Header("Target References")]
    public DialogueManager dialogueManager;
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private Coroutine toastCoroutine;

    void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (toastRoot != null) toastRoot.SetActive(false);
        if (settingsDialog != null) settingsDialog.Close();

        SetupButtons();
    }

    void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindAnyObjectByType<DialogueManager>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (settingsDialog != null && settingsDialog.gameObject.activeSelf)
            {
                settingsDialog.Close();
            }
            else
            {
                TogglePause();
            }
        }
    }

    private void SetupButtons()
    {
        if (hudMenuButton != null)
        {
            hudMenuButton.onClick.RemoveAllListeners();
            hudMenuButton.onClick.AddListener(OpenPauseMenu);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveCurrentGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsDialog != null) settingsDialog.Close();
        Time.timeScale = 1f;
    }

    public void SaveCurrentGame()
    {
        int currentIndex = 0;
        string preview = "";

        if (dialogueManager != null)
        {
            currentIndex = dialogueManager.GetCurrentDialogueIndex();
            preview = dialogueManager.GetCurrentDialogueText();
        }

        string currentScene = SceneManager.GetActiveScene().name;
        SaveSystem.SaveGame(currentScene, currentIndex, preview);

        ShowToast(LocalizationManager.Get("pause_saved_toast"));
    }

    public void OpenSettings()
    {
        if (settingsDialog != null)
        {
            settingsDialog.Open();
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowToast(string message, float duration = 2.2f)
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
        yield return new WaitForSecondsRealtime(duration);
        toastRoot.SetActive(false);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
