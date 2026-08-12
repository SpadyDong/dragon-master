using UnityEngine;

/// <summary>
/// 设置管理器 — 持久化音量和画面设置
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("默认值")]
    public float defaultBGMVolume = 0.7f;
    public float defaultSFXVolume = 1.0f;
    public int defaultResolutionIndex = 2; // 1920x1080

    // 当前值
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public int ResolutionIndex { get; private set; }

    private const string KEY_BGM = "Settings_BGM";
    private const string KEY_SFX = "Settings_SFX";
    private const string KEY_RES = "Settings_Resolution";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>从 PlayerPrefs 加载设置</summary>
    public void Load()
    {
        BGMVolume = PlayerPrefs.GetFloat(KEY_BGM, defaultBGMVolume);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX, defaultSFXVolume);
        ResolutionIndex = PlayerPrefs.GetInt(KEY_RES, defaultResolutionIndex);
    }

    /// <summary>保存到 PlayerPrefs</summary>
    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_BGM, BGMVolume);
        PlayerPrefs.SetFloat(KEY_SFX, SFXVolume);
        PlayerPrefs.SetInt(KEY_RES, ResolutionIndex);
        PlayerPrefs.Save();
    }

    /// <summary>设置 BGM 音量 (0-1)</summary>
    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolume(BGMVolume);
        Save();
    }

    /// <summary>设置 SFX 音量 (0-1)</summary>
    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(SFXVolume);
        Save();
    }

    /// <summary>设置分辨率索引</summary>
    public void SetResolution(int index)
    {
        ResolutionIndex = index;
        ApplyResolution();
        Save();
    }

    private void ApplyResolution()
    {
        // 常用分辨率预设
        (int w, int h)[] presets = {
            (1280, 720),
            (1600, 900),
            (1920, 1080),
            (2560, 1440)
        };

        if (ResolutionIndex >= 0 && ResolutionIndex < presets.Length)
        {
            var (w, h) = presets[ResolutionIndex];
            Screen.SetResolution(w, h, Screen.fullScreen);
        }
    }

    /// <summary>获取分辨率选项名列表</summary>
    public string[] GetResolutionOptions()
    {
        return new[] { "1280×720", "1600×900", "1920×1080", "2560×1440" };
    }
}
