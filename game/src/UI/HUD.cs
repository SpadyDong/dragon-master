using UnityEngine;
using TMPro;

/// <summary>
/// HUD 界面控制器
/// 显示时间、天气、金币等信息
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("文本组件")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI weatherText;

    [Header("天气图标")]
    [SerializeField] private GameObject[] weatherIcons;  // 索引 0=晴, 1=多云, ... 对应 GameManager.WeatherNames

    void Start()
    {
        if (timeText == null)
            Debug.LogWarning("HUD: timeText 未设置");
        if (goldText == null)
            Debug.LogWarning("HUD: goldText 未设置");
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // 更新时间
        if (timeText != null)
            timeText.text = GameManager.Instance.GetTimeString();

        // 更新金币
        if (goldText != null)
            goldText.text = GameManager.Instance.GetGoldString();

        // 更新天气
        if (weatherText != null)
            weatherText.text = GameManager.Instance.weather;

        UpdateWeatherIcon();
    }

    private void UpdateWeatherIcon()
    {
        if (weatherIcons == null || weatherIcons.Length == 0) return;

        int weatherIndex = System.Array.IndexOf(GameManager.WeatherNames, GameManager.Instance.weather);
        if (weatherIndex < 0) weatherIndex = 0;

        for (int i = 0; i < weatherIcons.Length; i++)
        {
            if (weatherIcons[i] != null)
                weatherIcons[i].SetActive(i == weatherIndex);
        }
    }
}
