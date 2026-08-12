using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置面板 — 音量 / 分辨率 / 操作提示
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("音量")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("画面")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("操作提示")]
    [SerializeField] private TextMeshProUGUI controlHintText;

    void Start()
    {
        // 初始化控件
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
        }
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // 操作提示
        if (controlHintText != null)
        {
            controlHintText.text =
                "<b>—— 操作说明 ——</b>\n\n" +
                "WASD / 方向键  —  移动（8方向）\n" +
                "Shift  —  奔跑\n" +
                "F  —  互动\n" +
                "Tab  —  打开菜单\n" +
                "B  —  打开背包\n" +
                "M  —  打开地图\n" +
                "Esc  —  关闭面板/暂停\n" +
                "Space  —  确认\n" +
                "Q / E  —  菜单内切换标签";
        }
    }

    /// <summary>刷新设置面板（从当前值同步）</summary>
    public void Refresh()
    {
        if (SettingsManager.Instance == null) return;

        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(SettingsManager.Instance.BGMVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SFXVolume);

        int pctBGM = Mathf.RoundToInt(SettingsManager.Instance.BGMVolume * 100);
        int pctSFX = Mathf.RoundToInt(SettingsManager.Instance.SFXVolume * 100);
        if (bgmValueText != null) bgmValueText.text = $"{pctBGM}%";
        if (sfxValueText != null) sfxValueText.text = $"{pctSFX}%";

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var options = SettingsManager.Instance.GetResolutionOptions();
            foreach (var opt in options)
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(opt));
            resolutionDropdown.SetValueWithoutNotify(SettingsManager.Instance.ResolutionIndex);
        }

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private void OnBGMChanged(float value)
    {
        int pct = Mathf.RoundToInt(value * 100);
        if (bgmValueText != null) bgmValueText.text = $"{pct}%";
        SettingsManager.Instance?.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        int pct = Mathf.RoundToInt(value * 100);
        if (sfxValueText != null) sfxValueText.text = $"{pct}%";
        SettingsManager.Instance?.SetSFXVolume(value);
    }

    private void OnResolutionChanged(int index)
    {
        SettingsManager.Instance?.SetResolution(index);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
