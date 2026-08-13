using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 工匠设备面板 — 配方列表（开始加工）+ 加工任务（收取成品）
/// </summary>
public class ArtisanPanel : MonoBehaviour
{
    [Header("设备信息")]
    [SerializeField] private TextMeshProUGUI deviceNameText;

    [Header("配方列表")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeEntryPrefab;

    [Header("任务列表")]
    [SerializeField] private Transform jobListContainer;
    [SerializeField] private GameObject jobEntryPrefab;
    [SerializeField] private GameObject emptyHint;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    private ArtisanDeviceData _deviceData;
    private List<GameObject> _recipeObjects = new();
    private List<GameObject> _jobObjects = new();

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>设置设备数据并刷新</summary>
    public void Setup(ArtisanDeviceData deviceData)
    {
        _deviceData = deviceData;
        Refresh();
    }

    public void Refresh()
    {
        if (deviceNameText != null && _deviceData != null)
            deviceNameText.text = _deviceData.deviceName;

        RefreshRecipes();
        RefreshJobs();
    }

    /// <summary>刷新配方列表（开始加工）</summary>
    private void RefreshRecipes()
    {
        foreach (var obj in _recipeObjects) Destroy(obj);
        _recipeObjects.Clear();

        if (recipeListContainer == null || recipeEntryPrefab == null || _deviceData == null) return;
        if (_deviceData.recipes == null) return;

        for (int i = 0; i < _deviceData.recipes.Length; i++)
        {
            var recipe = _deviceData.recipes[i];
            var entry = Instantiate(recipeEntryPrefab, recipeListContainer);
            _recipeObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = entry.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            var processButton = entry.transform.Find("ProcessButton")?.GetComponent<Button>();

            if (nameText != null) nameText.text = recipe.recipeName;
            if (descText != null)
            {
                string input = $"{recipe.inputItemId}×{recipe.inputCount}";
                if (recipe.extraMaterials != null)
                {
                    foreach (var m in recipe.extraMaterials)
                        input += $" + {m.itemId}×{m.count}";
                }
                descText.text = $"{input} → {recipe.outputItemId}×{recipe.outputCount}（{recipe.processDays}天）";
            }

            if (processButton != null)
            {
                int idx = i; // 闭包捕获
                processButton.onClick.RemoveAllListeners();
                processButton.onClick.AddListener(() => StartProcess(idx));
            }
        }
    }

    /// <summary>刷新加工任务列表（收取成品）</summary>
    private void RefreshJobs()
    {
        foreach (var obj in _jobObjects) Destroy(obj);
        _jobObjects.Clear();

        if (ArtisanManager.Instance == null) return;
        if (jobListContainer == null || jobEntryPrefab == null) return;

        var jobs = ArtisanManager.Instance.GetPendingJobs();
        if (emptyHint != null) emptyHint.SetActive(jobs.Count == 0);

        for (int i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            var entry = Instantiate(jobEntryPrefab, jobListContainer);
            _jobObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var statusText = entry.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            var collectButton = entry.transform.Find("CollectButton")?.GetComponent<Button>();

            if (nameText != null) nameText.text = job.outputItemId;

            bool finished = job.IsFinished(GameManager.Instance != null ? GameManager.Instance.day : 1);
            if (statusText != null)
                statusText.text = finished ? "已完成" : $"加工中（剩{job.processDays - ((GameManager.Instance?.day ?? 1) - job.startDay)}天）";

            if (collectButton != null)
            {
                int idx = i; // 闭包捕获
                collectButton.gameObject.SetActive(finished);
                collectButton.onClick.RemoveAllListeners();
                collectButton.onClick.AddListener(() => Collect(idx));
            }
        }
    }

    /// <summary>开始加工（recipeIndex 对应设备配方索引）</summary>
    private void StartProcess(int recipeIndex)
    {
        if (_deviceData == null) return;
        if (ArtisanManager.Instance != null &&
            ArtisanManager.Instance.StartProcess(_deviceData, recipeIndex))
        {
            Refresh();
        }
    }

    /// <summary>收取成品</summary>
    private void Collect(int jobIndex)
    {
        if (ArtisanManager.Instance != null &&
            ArtisanManager.Instance.CollectJob(jobIndex, out _, out _))
        {
            Refresh();
        }
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
