using UnityEngine;

/// <summary>
/// 音频管理器（骨架）
/// BGM + SFX 播放，音量控制，淡入淡出
/// M1 版本仅提供接口，音效资源后续补充
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("默认音量")]
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 自动创建 AudioSource（如果未设置）
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = bgmVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = sfxVolume;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>播放背景音乐</summary>
    public void PlayBGM(AudioClip clip, float fadeIn = 1f)
    {
        if (bgmSource == null || clip == null) return;

        if (bgmSource.isPlaying)
            bgmSource.Stop();

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>停止背景音乐</summary>
    public void StopBGM(float fadeOut = 1f)
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    /// <summary>播放音效</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }

    /// <summary>设置 BGM 音量</summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    /// <summary>设置 SFX 音量</summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
}
