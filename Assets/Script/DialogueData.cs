using UnityEngine;

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
    public AudioClip backgroundMusic;
    public bool changeMusic = false;
}