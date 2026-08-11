using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对话系统状态机
/// 管理对话流程：开始 → 单行打字 → 多行推进 → 选项分支 → 结束
/// 集成 NPC 数据库 + 新手引导对话
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("对话数据库")]
    [SerializeField] private List<DialogueTreeData> dialogueDatabase;

    /// <summary>当前是否在对话中</summary>
    public bool IsActive { get; private set; }

    private DialogueTreeData _currentTree;
    private DialogueNode _currentNode;
    private int _currentLineIndex;
    private string _currentNpcId;
    private bool _isTyping;
    private float _typeTimer;
    private int _typeCharIndex;

    private Dictionary<string, List<DialogueTreeData>> _dialogueMap; // npcId → trees
    private Dictionary<string, DialogueTreeData> _dialogueById;       // dialogueId → tree

    // 新手引导对话队列
    private Queue<(string npcId, string[] lines)> _tutorialQueue = new();

    void Awake()
    {
        Instance = this;
        _dialogueMap = new();
        _dialogueById = new();

        // 加载序列化数据库 + NPCDialogueTrees 工厂
        if (dialogueDatabase == null) dialogueDatabase = new();
        dialogueDatabase.AddRange(NPCDialogueTrees.BuildAll());

        foreach (var tree in dialogueDatabase)
        {
            if (!_dialogueMap.ContainsKey(tree.npcId))
                _dialogueMap[tree.npcId] = new List<DialogueTreeData>();
            _dialogueMap[tree.npcId].Add(tree);

            if (!string.IsNullOrEmpty(tree.dialogueId))
                _dialogueById[tree.dialogueId] = tree;
        }
    }

    /// <summary>开始对话（按 npcId）</summary>
    public void StartDialogue(string npcId)
    {
        StartDialogueById(npcId, null);
    }

    /// <summary>按 dialogueId 精确触发对话（心事件用）</summary>
    public bool StartDialogueById(string npcId, string dialogueId)
    {
        DialogueTreeData targetTree = null;

        if (!string.IsNullOrEmpty(dialogueId) && _dialogueById.TryGetValue(dialogueId, out targetTree))
        {
            // 找到了精确的心事件对话
        }
        else if (_dialogueMap.TryGetValue(npcId, out var trees))
        {
            // 日常对话：按好感度选择
            foreach (var tree in trees)
            {
                if (tree.dialogueId != null && tree.dialogueId.EndsWith("_daily"))
                {
                    // 找最适合好感度的日常对话节点
                    targetTree = tree;
                    break;
                }
            }

            // 没有日常对话 → 取第一个可用树
            if (targetTree == null && trees.Count > 0)
                targetTree = trees[0];
        }

        if (targetTree == null)
        {
            Debug.LogWarning($"未找到 NPC[{npcId}] 的对话数据");
            return false;
        }

        _currentTree = targetTree;
        _currentNpcId = npcId;
        IsActive = true;
        _currentNode = FindStartNode(_currentTree);
        _currentLineIndex = 0;

        if (_currentNode == null)
        {
            EndDialogue();
            return false;
        }

        InputManager.Instance.IsInputLocked = true;
        GameManager.Instance.SetState(GameState.Dialogue);
        EventBus.Publish(GameEvent.NPCDialogueStarted, npcId);
        ShowCurrentLine();
        return true;
    }

    /// <summary>新手引导对话（TutorialManager 调用）</summary>
    public void QueueTutorialDialogue(string npcId, string[] lines)
    {
        _tutorialQueue.Enqueue((npcId, lines));
    }

    void Update()
    {
        // 处理新手引导对话队列
        if (!IsActive && _tutorialQueue.Count > 0)
        {
            var (npcId, lines) = _tutorialQueue.Dequeue();
            PlayTutorialDialogue(npcId, lines);
            return;
        }

        if (!IsActive) return;

        // 打字机效果
        if (_isTyping)
        {
            _typeTimer += Time.deltaTime;
            if (_typeTimer >= 0.03f)
            {
                _typeTimer -= 0.03f;
                _typeCharIndex++;
                if (_typeCharIndex >= GetCurrentLine().Length)
                    _isTyping = false;
                DialogueUI.Instance?.UpdateTypewriter(GetCurrentLine(), _typeCharIndex);
            }
        }

        // 空格推进
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            Advance();
    }

    private void PlayTutorialDialogue(string npcId, string[] lines)
    {
        IsActive = true;
        _currentNpcId = npcId;

        // 获取 NPC 名字
        string npcName = npcId;
        if (NPCDatabase.BuildAll().TryGetValue(npcId, out var data))
            npcName = data.npcName;

        // 构建临时对话节点
        _currentTree = new DialogueTreeData
        {
            dialogueId = $"tutorial_{npcId}",
            npcId = npcId,
            nodes = new()
            {
                new DialogueNode
                {
                    nodeId = "start",
                    speakerName = npcName,
                    lines = new List<string>(lines),
                    nextNodeId = null
                }
            }
        };

        _currentNode = _currentTree.nodes[0];
        _currentLineIndex = 0;

        InputManager.Instance.IsInputLocked = true;
        GameManager.Instance.SetState(GameState.Dialogue);
        ShowCurrentLine();
    }

    private DialogueNode FindStartNode(DialogueTreeData tree)
    {
        foreach (var node in tree.nodes)
        {
            if (AreConditionsMet(node.conditions))
                return node;
        }
        return tree.nodes.Count > 0 ? tree.nodes[0] : null;
    }

    private string GetCurrentLine() => _currentNode.lines[_currentLineIndex];

    private void ShowCurrentLine()
    {
        _isTyping = true;
        _typeCharIndex = 0;
        _typeTimer = 0f;
        DialogueUI.Instance?.ShowLine(_currentNode.speakerName, GetCurrentLine());
    }

    public void Advance()
    {
        if (_isTyping)
        {
            _isTyping = false;
            _typeCharIndex = GetCurrentLine().Length;
            DialogueUI.Instance?.UpdateTypewriter(GetCurrentLine(), _typeCharIndex);
            return;
        }

        _currentLineIndex++;

        if (_currentLineIndex < _currentNode.lines.Count)
        {
            ShowCurrentLine();
        }
        else
        {
            ApplyNodeEffects();

            if (_currentNode.choices.Count > 0)
            {
                var availableChoices = new List<DialogueChoice>();
                foreach (var choice in _currentNode.choices)
                    if (AreConditionsMet(choice.conditions))
                        availableChoices.Add(choice);
                DialogueUI.Instance?.ShowChoices(availableChoices);
            }
            else if (!string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                GoToNode(_currentNode.nextNodeId);
            }
            else
            {
                EndDialogue();
            }
        }
    }

    public void SelectChoice(int index)
    {
        if (_currentNode == null || index < 0 || index >= _currentNode.choices.Count) return;

        var choice = _currentNode.choices[index];
        if (choice.effects != null)
            foreach (var effect in choice.effects) effect.Apply();

        if (!string.IsNullOrEmpty(choice.nextNodeId))
            GoToNode(choice.nextNodeId);
        else
            EndDialogue();
    }

    private void GoToNode(string nodeId)
    {
        DialogueNode nextNode = null;
        foreach (var node in _currentTree.nodes)
        {
            if (node.nodeId == nodeId && AreConditionsMet(node.conditions))
            { nextNode = node; break; }
        }

        if (nextNode != null)
        {
            _currentNode = nextNode;
            _currentLineIndex = 0;
            ApplyNodeEffects();
            ShowCurrentLine();
        }
        else EndDialogue();
    }

    private void ApplyNodeEffects()
    {
        if (_currentNode.effects == null) return;
        foreach (var e in _currentNode.effects) e.Apply();
    }

    private bool AreConditionsMet(DialogueCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0) return true;
        foreach (var c in conditions)
            if (!c.IsMet()) return false;
        return true;
    }

    public void EndDialogue()
    {
        // 记下对话前的 npcId
        string endedNpcId = _currentNpcId;

        IsActive = false;
        _currentNpcId = null;
        _currentTree = null;
        _currentNode = null;
        _isTyping = false;

        InputManager.Instance.IsInputLocked = false;
        GameManager.Instance.SetState(GameState.Playing);
        DialogueUI.Instance?.Hide();
        EventBus.Publish(GameEvent.NPCDialogueEnded, endedNpcId);

        // 通知 TutorialManager
        TutorialManager.Instance?.CompleteStep("tut_talk");
    }
}
