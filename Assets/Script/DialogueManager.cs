using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public Image backgroundImage;
    public Image characterImage;
    public TMP_Text dialogueText;

    [Header("Dialogue")]
    public DialogueData[] dialogues;

    [Header("Audio")]
    public AudioManager audioManager;

    private int currentDialogue = 0;

    private Coroutine typingCoroutine;

    private bool isTyping = false;

    void Start()
    {
        ShowDialogue();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }
    }

    void OnClick()
    {
        // ถ้าข้อความกำลังพิมพ์
        if (isTyping)
        {
            FinishText();
            return;
        }

        // ถ้าข้อความพิมพ์จบแล้ว
        NextDialogue();
    }

    void ShowDialogue()
    {
        if (currentDialogue >= dialogues.Length)
        {
            Debug.Log("Dialogue จบแล้ว");
            return;
        }

        DialogueData dialogue = dialogues[currentDialogue];

        // Background
        if (dialogue.background != null)
        {
            backgroundImage.sprite = dialogue.background;
        }

        // Character
        if (dialogue.character != null)
        {
            characterImage.sprite = dialogue.character;
            characterImage.enabled = true;
        }
        else
        {
            characterImage.enabled = false;
        }

        // Sound Effect
        if (dialogue.soundEffect != null)
        {
            audioManager.PlaySFX(dialogue.soundEffect);
        }

        // Background Music
        if (dialogue.changeMusic)
        {
            audioManager.PlayBGM(dialogue.backgroundMusic);
        }

        // Text
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(
            TypeText(dialogue.text, dialogue.textSpeed)
        );
    }

    IEnumerator TypeText(string text, float speed)
    {
        isTyping = true;

        dialogueText.text = "";

        foreach (char letter in text)
        {
            dialogueText.text += letter;

            yield return new WaitForSeconds(speed);
        }

        isTyping = false;
    }

    void FinishText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = dialogues[currentDialogue].text;

        isTyping = false;
    }

    void NextDialogue()
    {
        currentDialogue++;

        if (currentDialogue >= dialogues.Length)
        {
            Debug.Log("Dialogue จบแล้ว");
            return;
        }

        ShowDialogue();
    }
}