using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NPC 关系网管理器
/// 管理 NPC-NPC 之间的好感度、家庭关系、好友关系
/// 驱动跨 NPC 对话变化、送礼影响亲友、心事件前置条件
/// </summary>
public class NPCRelationshipManager : MonoBehaviour
{
    public static NPCRelationshipManager Instance { get; private set; }

    // NPC→NPC 好感度表（双向）
    private Dictionary<string, Dictionary<string, int>> _npcAffections = new();

    // 家庭关系组（用于"结婚后家人态度变化"等联动）
    private Dictionary<string, List<string>> _familyGroups = new();

    // 好友关系组
    private Dictionary<string, List<string>> _friendGroups = new();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeRelationshipDatabase();
    }

    void Start()
    {
        // 监听主角好感变化 → 影响 NPC 亲友态度
        EventBus.Subscribe<int>(GameEvent.NPCAffectionChanged, OnPlayerAffectionChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.NPCAffectionChanged, OnPlayerAffectionChanged);
    }

    /// <summary>初始化所有 NPC 关系数据</summary>
    private void InitializeRelationshipDatabase()
    {
        // === 家庭关系 ===
        // 兄弟
        AddFamilyRelation("alan", "xiaoshi", "兄弟", 90);
        AddFamilyRelation("panshi", "liuyan", "兄妹", 85);
        // 父子/父女
        AddFamilyRelation("chen_blacksmith", "tiemei", "父女", 80);
        AddFamilyRelation("zhou_wood", "linmo", "父子", 78);
        // 兄妹
        AddFamilyRelation("hanchuan", "hanbai", "兄弟", 88);
        // 表兄妹
        AddFamilyRelation("qingye", "xiaotang", "表兄妹", 70);
        // 母子
        AddFamilyRelation("su_yi", "adan", "母子", 82);
        AddFamilyRelation("yupo", "yuntao", "母子", 85);
        // 祖孙
        AddFamilyRelation("huapo", "chengxi", "祖孙", 80);
        AddFamilyRelation("huapo", "xiaoya", "祖孙", 78);
        // 继父女
        AddFamilyRelation("hua_shu", "yeling", "父女", 82);
        // 师徒/养母女
        AddFamilyRelation("mao_daniang", "mao_shifu", "师徒_养母女", 90);
        // 养父女
        AddFamilyRelation("laoxuezhe", "muqing", "师徒_养父女", 85);
        // 夫妻
        AddFamilyRelation("shen_daniang", "shen_dashu", "夫妻", 95);

        // === 好友关系 ===
        AddFriendRelation("alan", "panshi", "兄弟般好友", 75);
        AddFriendRelation("panshi", "chixiao", "酒友", 65);
        AddFriendRelation("panshi", "tiemei", "锻材伙伴", 60);
        AddFriendRelation("chixiao", "tiemei", "比试对手", 55);
        AddFriendRelation("chixiao", "ge_shu", "忘年交", 70);
        AddFriendRelation("yeling", "xiaotang", "闺蜜", 80);
        AddFriendRelation("hanchuan", "qiuyue", "安静之交", 65);
        AddFriendRelation("muqing", "chengxi", "好友", 70);
        AddFriendRelation("muqing", "qiuyue", "同事好友", 68);
        AddFriendRelation("chengxi", "qiuyue", "好友", 65);
        AddFriendRelation("mao_shifu", "qingye", "厨艺对手", 60);
        AddFriendRelation("qingye", "xuelai", "师兄妹", 80);
        AddFriendRelation("liuyan", "shen_daniang", "忘年交", 65);
        AddFriendRelation("linmo", "chengxi", "微妙好感", 55);
        AddFriendRelation("abao", "tiemei", "猎场搭档", 60);
        AddFriendRelation("ge_shu", "hua_shu", "老战友", 70);
        AddFriendRelation("ge_shu", "mafu_laoli", "老战友", 70);
    }

    private void AddFamilyRelation(string id1, string id2, string desc, int affection)
    {
        AddAffection(id1, id2, affection);
        if (!_familyGroups.ContainsKey(id1)) _familyGroups[id1] = new List<string>();
        if (!_familyGroups.ContainsKey(id2)) _familyGroups[id2] = new List<string>();
        _familyGroups[id1].Add(id2);
        _familyGroups[id2].Add(id1);
    }

    private void AddFriendRelation(string id1, string id2, string desc, int affection)
    {
        AddAffection(id1, id2, affection);
        if (!_friendGroups.ContainsKey(id1)) _friendGroups[id1] = new List<string>();
        if (!_friendGroups.ContainsKey(id2)) _friendGroups[id2] = new List<string>();
        _friendGroups[id1].Add(id2);
        _friendGroups[id2].Add(id1);
    }

    private void AddAffection(string id1, string id2, int value)
    {
        if (!_npcAffections.ContainsKey(id1))
            _npcAffections[id1] = new Dictionary<string, int>();
        _npcAffections[id1][id2] = value;

        if (!_npcAffections.ContainsKey(id2))
            _npcAffections[id2] = new Dictionary<string, int>();
        _npcAffections[id2][id1] = value;
    }

    /// <summary>获取两个 NPC 之间的好感度</summary>
    public int GetAffection(string npc1, string npc2)
    {
        if (_npcAffections.TryGetValue(npc1, out var dict) && dict.TryGetValue(npc2, out int val))
            return val;
        return 0;
    }

    /// <summary>获取某 NPC 的家人列表</summary>
    public List<string> GetFamily(string npcId)
    {
        return _familyGroups.TryGetValue(npcId, out var list) ? list : new List<string>();
    }

    /// <summary>获取某 NPC 的好友列表</summary>
    public List<string> GetFriends(string npcId)
    {
        return _friendGroups.TryGetValue(npcId, out var list) ? list : new List<string>();
    }

    /// <summary>主角对某 NPC 好感变化 → 影响其亲友对主角态度</summary>
    private void OnPlayerAffectionChanged(int change)
    {
        // 后续 M6 实现：结婚后配偶家人好感联动
        // 送礼给某NPC → 其好友/家人好感微调
    }
}
