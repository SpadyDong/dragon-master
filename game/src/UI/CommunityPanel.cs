using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 社区中心面板 — 收集包列表 + 龙腾集团路线
/// </summary>
public class CommunityPanel : MonoBehaviour
{
    [Header("路线信息")]
    [SerializeField] private TextMeshProUGUI routeText;

    [Header("收集包列表")]
    [SerializeField] private CommunityBundle[] bundles;
    [SerializeField] private Transform bundleListContainer;
    [SerializeField] private GameObject bundleEntryPrefab;
    [SerializeField] private GameObject emptyHint;

    [Header("龙腾集团")]
    [SerializeField] private Button dragonTengButton;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    private List<GameObject> _entryObjects = new();

    void Start()
    {
        if (dragonTengButton != null) dragonTengButton.onClick.AddListener(OnJoinDragonTeng);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (CommunityCenterManager.Instance == null) return;

        if (routeText != null)
            routeText.text = CommunityCenterManager.Instance.JoinedDragonTeng
                ? "已加入龙腾集团（所有设施已解锁）"
                : "社区中心路线（收集包修复）";

        if (bundleListContainer == null || bundleEntryPrefab == null) return;

        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        if (emptyHint != null) emptyHint.SetActive(bundles == null || bundles.Length == 0);

        if (bundles == null) return;

        foreach (var bundle in bundles)
        {
            var entry = Instantiate(bundleEntryPrefab, bundleListContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var statusText = entry.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            var completeButton = entry.transform.Find("CompleteButton")?.GetComponent<Button>();

            bool completed = CommunityCenterManager.Instance.HasCompletedBundle(bundle.bundleId);

            if (nameText != null) nameText.text = bundle.bundleName;
            if (statusText != null)
            {
                statusText.text = completed
                    ? "<color=#88FF88>已完成</color>"
                    : (CommunityCenterManager.Instance.CanCompleteBundle(bundle) ? "可完成" : "材料不足");
            }

            if (completeButton != null)
            {
                var captured = bundle;
                completeButton.gameObject.SetActive(!completed);
                completeButton.interactable = CommunityCenterManager.Instance.CanCompleteBundle(bundle);
                completeButton.onClick.RemoveAllListeners();
                completeButton.onClick.AddListener(() =>
                {
                    CommunityCenterManager.Instance.CompleteBundle(captured);
                    Refresh();
                });
            }
        }

        if (dragonTengButton != null)
            dragonTengButton.interactable = !CommunityCenterManager.Instance.JoinedDragonTeng;
    }

    private void OnJoinDragonTeng()
    {
        CommunityCenterManager.Instance?.JoinDragonTengGroup();
        Refresh();
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
