using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    public static AudioManager Instance { get; private set; }

    public const string PREF_MASTER_VOLUME = "Audio_MasterVolume";
    public const string PREF_BGM_VOLUME = "Audio_BGMVolume";
    public const string PREF_SFX_VOLUME = "Audio_SFXVolume";

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
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureAudioSources();
        ApplyVolumes();
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