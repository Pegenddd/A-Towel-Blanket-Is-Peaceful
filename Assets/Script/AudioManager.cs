using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    [Tooltip("Default background music to play automatically. Loops and persists across all scenes via DontDestroyOnLoad.")]
    public AudioClip defaultBGM;
    [Tooltip("Play defaultBGM automatically when AudioManager starts if no music is playing.")]
    public bool playOnAwake = true;
    [Tooltip("If another AudioManager in a new scene has a defaultBGM, override current music?")]
    public bool overrideExistingBGM = false;

    public static AudioManager Instance { get; private set; }

    public const string PREF_MASTER_VOLUME = "Audio_MasterVolume";
    public const string PREF_BGM_VOLUME = "Audio_BGMVolume";
    public const string PREF_SFX_VOLUME = "Audio_SFXVolume";

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
            EnsureAudioSources();
            ApplyVolumes();

            if (playOnAwake && defaultBGM != null)
            {
                PlayBGM(defaultBGM);
            }
        }
        else if (Instance != this)
        {
            if (defaultBGM != null && (!Instance.IsBGMPlaying() || overrideExistingBGM))
            {
                if (Instance.GetCurrentBGM() != defaultBGM || !Instance.IsBGMPlaying())
                {
                    Instance.PlayBGM(defaultBGM);
                }
            }
            Destroy(gameObject);
            return;
        }
    }

    public void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolume);
        PlayerPrefs.SetFloat(PREF_BGM_VOLUME, bgmVolume);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        ApplyVolumes();
        SaveVolumeSettings();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
        SaveVolumeSettings();
    }

    public void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;

        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (bgmSource == null)
        {
            if (sources.Length > 0)
            {
                bgmSource = sources[0];
            }
            else
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
        }
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        if (sfxSource == null)
        {
            if (sources.Length > 1 && sources[1] != bgmSource)
            {
                sfxSource = sources[1];
            }
            else
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        ApplyVolumes();
    }

    public void PlayBGM(AudioClip clip)
    {
        EnsureAudioSources();

        if (clip == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        EnsureAudioSources();
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    public bool IsBGMPlaying()
    {
        return bgmSource != null && bgmSource.isPlaying;
    }

    public AudioClip GetCurrentBGM()
    {
        return bgmSource != null ? bgmSource.clip : null;
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        EnsureAudioSources();

        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}