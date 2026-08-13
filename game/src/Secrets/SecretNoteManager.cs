using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 秘密纸条管理器 — GDD 7.14
/// 1-50 号秘密纸条收集 + 怪奇收藏家成就
/// 来源：矿洞 / 钓鱼 / 觅食概率获得
/// </summary>
public class SecretNoteManager : MonoBehaviour
{
    public static SecretNoteManager Instance { get; private set; }

    /// <summary>秘密纸条编号范围</summary>
    public const int NOTE_MIN = 1;
    public const int NOTE_MAX = 50;

    /// <summary>已收集的秘密纸条编号</summary>
    private HashSet<int> _foundNotes = new();

    /// <summary>发现概率（每次挖矿/钓鱼/觅食）</summary>
    [SerializeField] private float findChance = 0.05f;

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
        // 订阅挖矿/钓鱼/觅食事件
        EventBus.Subscribe<string>(GameEvent.OreMined, _ => TryFindNote());
        EventBus.Subscribe<string>(GameEvent.FishCaught, _ => TryFindNote());
        EventBus.Subscribe<string>(GameEvent.CropHarvested, _ => TryFindNote());
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<string>(GameEvent.OreMined, _ => TryFindNote());
        EventBus.Unsubscribe<string>(GameEvent.FishCaught, _ => TryFindNote());
        EventBus.Unsubscribe<string>(GameEvent.CropHarvested, _ => TryFindNote());
    }

    // ==================== 发现 ====================

    /// <summary>尝试发现秘密纸条（概率判定）</summary>
    public void TryFindNote()
    {
        if (Random.value > findChance) return;
        if (_foundNotes.Count >= NOTE_MAX) return; // 已集齐

        // 随机找到一个未收集的编号
        var available = new List<int>();
        for (int i = NOTE_MIN; i <= NOTE_MAX; i++)
            if (!_foundNotes.Contains(i)) available.Add(i);

        if (available.Count == 0) return;

        int noteId = available[Random.Range(0, available.Count)];
        _foundNotes.Add(noteId);

        EventBus.Publish(GameEvent.NoteFound, noteId);
        Debug.Log($"发现秘密纸条 #{noteId}");

        if (_foundNotes.Count >= NOTE_MAX)
        {
            Debug.Log("★ 集齐全部50张秘密纸条，解锁怪奇收藏家成就");
        }
    }

    // ==================== 查询 ====================

    /// <summary>是否已收集某纸条</summary>
    public bool HasNote(int noteId) => _foundNotes.Contains(noteId);

    /// <summary>已收集纸条数</summary>
    public int FoundNoteCount => _foundNotes.Count;

    /// <summary>是否集齐全部</summary>
    public bool IsComplete => _foundNotes.Count >= NOTE_MAX;

    /// <summary>获取所有已收集纸条编号（升序）</summary>
    public List<int> GetFoundNotes()
    {
        var list = new List<int>(_foundNotes);
        list.Sort();
        return list;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class SecretNoteSaveData
    {
        public List<int> foundNotes;
    }

    public SecretNoteSaveData GetSaveData()
    {
        return new SecretNoteSaveData { foundNotes = GetFoundNotes() };
    }

    public void LoadSaveData(SecretNoteSaveData data)
    {
        if (data?.foundNotes == null) return;
        _foundNotes = new HashSet<int>(data.foundNotes);
    }
}
