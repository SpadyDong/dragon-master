using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 主线管理器 — GDD 2.2
/// 三幕式主线状态机：幕推进 + 节点完成 + 四圣物收集 + 同行者选择 + 结局判定
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    /// <summary>当前主线幕</summary>
    private StoryAct _currentAct = StoryAct.Prologue;

    /// <summary>已完成的剧情节点</summary>
    private HashSet<StoryNode> _completedNodes = new();

    /// <summary>已收集的四圣物</summary>
    private HashSet<SacredRelic> _collectedRelics = new();

    /// <summary>第三幕选定的同行者（可结婚对象好感≥500 或任意NPC）</summary>
    private string _selectedCompanionId;

    /// <summary>已确定的结局（null=未确定）</summary>
    private StoryEnding? _ending;

    /// <summary>当前幕</summary>
    public StoryAct CurrentAct => _currentAct;

    /// <summary>当前结局</summary>
    public StoryEnding? Ending => _ending;

    /// <summary>选定的同行者</summary>
    public string SelectedCompanionId => _selectedCompanionId;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Subscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);

        // 初始幕
        UpdateActFromSeason();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Unsubscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);
    }

    // ==================== 幕推进 ====================

    void OnDayChanged(int day) => UpdateActFromSeason();
    void OnSeasonChanged(int season) => UpdateActFromSeason();

    /// <summary>根据季节自动推进幕（GDD 2.2）</summary>
    void UpdateActFromSeason()
    {
        if (GameManager.Instance == null) return;

        int season = GameManager.Instance.season;

        StoryAct target = season switch
        {
            0 => StoryAct.Act1,   // 春
            1 or 2 => StoryAct.Act2, // 夏/秋
            3 => StoryAct.Act3,   // 冬
            _ => StoryAct.Prologue
        };

        if (target != _currentAct)
        {
            _currentAct = target;
            EventBus.Publish(GameEvent.StoryActChanged, (int)_currentAct);
            Debug.Log($"主线推进至：{StoryUtils.GetActName(_currentAct)}");
        }
    }

    // ==================== 节点完成 ====================

    /// <summary>标记节点完成</summary>
    public void AdvanceTo(StoryNode node)
    {
        if (_completedNodes.Contains(node)) return;
        _completedNodes.Add(node);

        // 节点所属幕可能比当前幕更靠前（补完），但当前幕取最大
        var nodeAct = StoryUtils.GetActForNode(node);
        if ((int)nodeAct > (int)_currentAct)
            _currentAct = nodeAct;

        EventBus.Publish(GameEvent.StoryNodeCompleted, node.ToString());
        Debug.Log($"主线节点完成：{StoryUtils.GetNodeName(node)}");
    }

    /// <summary>节点是否已完成</summary>
    public bool IsNodeCompleted(StoryNode node) => _completedNodes.Contains(node);

    /// <summary>获取已完成节点</summary>
    public HashSet<StoryNode> GetCompletedNodes() => _completedNodes;

    // ==================== 四圣物 ====================

    /// <summary>收集圣物</summary>
    public void CollectRelic(SacredRelic relic)
    {
        if (_collectedRelics.Contains(relic)) return;
        _collectedRelics.Add(relic);

        EventBus.Publish(GameEvent.RelicCollected, relic.ToString());
        Debug.Log($"收集圣物：{StoryUtils.GetRelicName(relic)}");

        // 四圣物集齐
        if (HasAllRelics())
            Debug.Log("★ 四圣物集齐！");
    }

    /// <summary>是否已收集某圣物</summary>
    public bool HasRelic(SacredRelic relic) => _collectedRelics.Contains(relic);

    /// <summary>是否集齐四圣物</summary>
    public bool HasAllRelics() => _collectedRelics.Count >= 4;

    /// <summary>已收集圣物数量</summary>
    public int RelicCount => _collectedRelics.Count;

    // ==================== 同行者 ====================

    /// <summary>选择同行者（第三幕，好感≥500的可结婚对象或任意NPC）</summary>
    public void SelectCompanion(string npcId)
    {
        _selectedCompanionId = npcId;
        Debug.Log($"选定同行者：{npcId}");
    }

    // ==================== 结局判定 ====================

    /// <summary>判定结局（purify=净化 / destroy=消灭）</summary>
    public StoryEnding ResolveEnding(bool purify)
    {
        bool isMarried = MarriageManager.Instance != null && MarriageManager.Instance.IsMarried;
        bool hasCompanion = !string.IsNullOrEmpty(_selectedCompanionId);

        _ending = StoryEndingResolver.Resolve(purify, isMarried, hasCompanion);
        EventBus.Publish(GameEvent.EndingResolved, _ending.Value.ToString());
        Debug.Log($"结局：{StoryUtils.GetEndingName(_ending.Value)}");
        return _ending.Value;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class StorySaveData
    {
        public int currentAct;
        public List<int> completedNodes;
        public List<int> collectedRelics;
        public string selectedCompanionId;
        public int ending;  // -1=null
    }

    public StorySaveData GetSaveData()
    {
        var data = new StorySaveData
        {
            currentAct = (int)_currentAct,
            completedNodes = new List<int>(),
            collectedRelics = new List<int>(),
            selectedCompanionId = _selectedCompanionId ?? "",
            ending = _ending.HasValue ? (int)_ending.Value : -1
        };
        foreach (var n in _completedNodes) data.completedNodes.Add((int)n);
        foreach (var r in _collectedRelics) data.collectedRelics.Add((int)r);
        return data;
    }

    public void LoadSaveData(StorySaveData data)
    {
        if (data == null) return;

        _currentAct = (StoryAct)data.currentAct;
        _completedNodes.Clear();
        if (data.completedNodes != null)
            foreach (var n in data.completedNodes) _completedNodes.Add((StoryNode)n);

        _collectedRelics.Clear();
        if (data.collectedRelics != null)
            foreach (var r in data.collectedRelics) _collectedRelics.Add((SacredRelic)r);

        _selectedCompanionId = string.IsNullOrEmpty(data.selectedCompanionId) ? null : data.selectedCompanionId;
        _ending = data.ending >= 0 ? (StoryEnding)data.ending : null;
    }
}
