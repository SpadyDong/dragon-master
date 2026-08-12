using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 好感度查询面板 — 按分类查看所有 NPC 好感度
/// </summary>
public class RelationshipPanel : MonoBehaviour
{
    [Header("分类标签")]
    [SerializeField] private Button tabAll;
    [SerializeField] private Button tabBachelors;     // 可结婚男
    [SerializeField] private Button tabBachelorettes; // 可结婚女
    [SerializeField] private Button tabMiddleAged;    // 中年
    [SerializeField] private Button tabChildren;      // 少年
    [SerializeField] private Button tabElderly;       // 老年
    [SerializeField] private Button tabSpecial;       // 特殊NPC

    [Header("列表")]
    [SerializeField] private Transform npcListContainer;
    [SerializeField] private GameObject npcEntryPrefab;
    [SerializeField] private GameObject emptyHint;

    [Header("详情")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI npcAgeText;
    [SerializeField] private TextMeshProUGUI npcPersonalityText;
    [SerializeField] private TextMeshProUGUI affectionLevelText;
    [SerializeField] private TextMeshProUGUI affectionNumberText;
    [SerializeField] private Slider affectionBar;
    [SerializeField] private TextMeshProUGUI birthdayText;
    [SerializeField] private TextMeshProUGUI lovedGiftsText;
    [SerializeField] private TextMeshProUGUI likedGiftsText;
    [SerializeField] private TextMeshProUGUI hatedGiftsText;

    private enum NpcTab { All, Bachelors, Bachelorettes, MiddleAged, Children, Elderly, Special }
    private NpcTab _currentNpcTab = NpcTab.All;
    private List<GameObject> _entryObjects = new();
    private string _selectedNpcId;

    // NPC 分类数据
    private Dictionary<NpcTab, string[]> _npcGroups;
    private Dictionary<string, NPCDataContainer> _allNpcData;

    void Start()
    {
        // 注册按钮事件
        if (tabAll != null) tabAll.onClick.AddListener(() => SetTab(NpcTab.All));
        if (tabBachelors != null) tabBachelors.onClick.AddListener(() => SetTab(NpcTab.Bachelors));
        if (tabBachelorettes != null) tabBachelorettes.onClick.AddListener(() => SetTab(NpcTab.Bachelorettes));
        if (tabMiddleAged != null) tabMiddleAged.onClick.AddListener(() => SetTab(NpcTab.MiddleAged));
        if (tabChildren != null) tabChildren.onClick.AddListener(() => SetTab(NpcTab.Children));
        if (tabElderly != null) tabElderly.onClick.AddListener(() => SetTab(NpcTab.Elderly));
        if (tabSpecial != null) tabSpecial.onClick.AddListener(() => SetTab(NpcTab.Special));

        // 初始化 NPC 数据
        _allNpcData = NPCDatabase.BuildAll();
        BuildNpcGroups();
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void BuildNpcGroups()
    {
        _npcGroups = new Dictionary<NpcTab, string[]>
        {
            [NpcTab.Bachelors] = new[] { "alan", "panshi", "linmo", "yuntao", "qingye", "hanchuan", "chixiao" },
            [NpcTab.Bachelorettes] = new[] { "muqing", "chengxi", "xiaotang", "yeling", "qiuyue", "tiemei", "xuelai", "liuyan", "mao_shifu" },
            [NpcTab.MiddleAged] = new[] { "laocunzhang", "chen_blacksmith", "shen_daniang", "shen_dashu", "zhou_wood", "su_yi", "hua_shu", "mafu_laoli", "akuang", "huapo", "principal", "yupo", "abao", "mao_daniang" },
            [NpcTab.Children] = new[] { "xiaoshi", "hanbai", "xiaotang", "adan", "doudou", "xiaoya" },
            [NpcTab.Elderly] = new[] { "dayeye", "ama", "ge_shu", "laoxuezhe" },
            [NpcTab.Special] = new[] { "chi_yan", "bai_shuang", "mo_ya", "hasang", "hui_pao", "sailasi", "aier_wen", "luoqi", "luosha_nv", "erisi" }
        };
    }

    public void Refresh()
    {
        SetTab(_currentNpcTab);
    }

    private void SetTab(NpcTab tab)
    {
        _currentNpcTab = tab;

        // 清除旧条目
        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();
        if (detailPanel != null) detailPanel.SetActive(false);

        // 获取要显示的 NPC ID 列表
        List<string> npcIds;
        if (tab == NpcTab.All)
        {
            npcIds = new List<string>(_allNpcData.Keys);
        }
        else if (_npcGroups.TryGetValue(tab, out var group))
        {
            npcIds = new List<string>(group);
        }
        else
        {
            npcIds = new List<string>();
        }

        // 空提示
        if (emptyHint != null) emptyHint.SetActive(npcIds.Count == 0);

        // 按好感度排序
        npcIds.Sort((a, b) =>
        {
            int affA = GetPlayerAffection(a);
            int affB = GetPlayerAffection(b);
            return affB.CompareTo(affA);
        });

        // 创建条目
        foreach (var npcId in npcIds)
        {
            if (!_allNpcData.TryGetValue(npcId, out var data)) continue;
            if (npcEntryPrefab == null || npcListContainer == null) continue;

            var entry = Instantiate(npcEntryPrefab, npcListContainer);
            _entryObjects.Add(entry);

            // 设置文本
            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var heartText = entry.transform.Find("HeartText")?.GetComponent<TextMeshProUGUI>();
            var affBar = entry.transform.Find("AffectionBar")?.GetComponent<Slider>();
            var button = entry.GetComponent<Button>();

            if (nameText != null) nameText.text = data.npcName;
            if (heartText != null)
            {
                int aff = GetPlayerAffection(npcId);
                heartText.text = GetAffectionLevel(aff);
            }
            if (affBar != null)
            {
                int aff = GetPlayerAffection(npcId);
                affBar.value = Mathf.Clamp01((float)aff / 3000f);
            }

            if (button != null)
            {
                var id = npcId;
                button.onClick.AddListener(() => ShowNpcDetail(id));
            }
        }
    }

    private void ShowNpcDetail(string npcId)
    {
        if (detailPanel == null) return;
        if (!_allNpcData.TryGetValue(npcId, out var data)) return;

        _selectedNpcId = npcId;
        detailPanel.SetActive(true);

        if (npcNameText != null) npcNameText.text = data.npcName;
        if (npcAgeText != null) npcAgeText.text = $"{data.age}岁  {data.gender}";
        if (npcPersonalityText != null) npcPersonalityText.text = data.personality;

        int affection = GetPlayerAffection(npcId);
        if (affectionLevelText != null) affectionLevelText.text = GetAffectionLevel(affection);
        if (affectionNumberText != null) affectionNumberText.text = $"♥ {affection} / 3000";
        if (affectionBar != null) affectionBar.value = Mathf.Clamp01((float)affection / 3000f);

        if (birthdayText != null) birthdayText.text = $"生日: {data.birthMonth}月{data.birthDay}日";

        // 礼物偏好
        if (lovedGiftsText != null && data.lovedItems != null)
            lovedGiftsText.text = $"超爱: {string.Join(", ", data.lovedItems)}";
        if (likedGiftsText != null && data.likedItems != null)
            likedGiftsText.text = $"喜欢: {string.Join(", ", data.likedItems)}";
        if (hatedGiftsText != null && data.hatedItems != null)
            hatedGiftsText.text = $"讨厌: {string.Join(", ", data.hatedItems)}";
    }

    /// <summary>获取玩家对 NPC 的好感度</summary>
    private int GetPlayerAffection(string npcId)
    {
        // 使用 NPCRelationshipManager 获取好感度
        // 暂时返回模拟值（后续由真正的玩家好感系统提供）
        if (NPCRelationshipManager.Instance != null)
        {
            // 玩家好感存储在 NPCRelationshipManager 中（M6 实现）
        }
        // 模拟值：基于 NPC ID hash
        return Mathf.Abs(npcId.GetHashCode()) % 3000;
    }

    /// <summary>好感度等级描述</summary>
    private string GetAffectionLevel(int affection)
    {
        if (affection >= 3000) return "♥♥♥♥♥ 挚爱";
        if (affection >= 1000) return "♥♥♥♥ 亲密";
        if (affection >= 500) return "♥♥♥ 友好";
        if (affection >= 200) return "♥♥ 熟悉";
        return "♥ 陌生";
    }
}
