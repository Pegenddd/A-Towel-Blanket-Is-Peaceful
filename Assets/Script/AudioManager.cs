using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsureAudioSources();
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

        sfxSource.PlayOneShot(clip);
    }
}