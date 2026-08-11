using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对话系统状态机
/// 管理对话流程：开始 → 单行打字 → 多行推进 → 选项分支 → 结束
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

    private Dictionary<string, DialogueTreeData> _dialogueMap;

    void Awake()
    {
        Instance = this;
        _dialogueMap = new();
        foreach (var tree in dialogueDatabase)
        {
            _dialogueMap[tree.npcId] = tree;
        }
    }

    /// <summary>开始对话</summary>
    public void StartDialogue(string npcId)
    {
        if (!_dialogueMap.TryGetValue(npcId, out _currentTree))
        {
            Debug.LogWarning($"未找到 NPC[{npcId}] 的对话数据");
            return;
        }

        IsActive = true;
        _currentNpcId = npcId;
        _currentNode = FindStartNode(_currentTree);
        _currentLineIndex = 0;

        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        // 锁定输入和游戏状态
        InputManager.Instance.IsInputLocked = true;
        GameManager.Instance.SetState(GameState.Dialogue);
        EventBus.Publish(GameEvent.NPCDialogueStarted, npcId);

        // 显示第一行
        ShowCurrentLine();
    }

    /// <summary>找到最佳起始节点（根据条件过滤）</summary>
    private DialogueNode FindStartNode(DialogueTreeData tree)
    {
        foreach (var node in tree.nodes)
        {
            if (AreConditionsMet(node.conditions))
                return node;
        }
        return tree.nodes.Count > 0 ? tree.nodes[0] : null;
    }

    void Update()
    {
        if (!IsActive) return;

        // 打字机效果
        if (_isTyping)
        {
            _typeTimer += Time.deltaTime;
            if (_typeTimer >= 0.03f) // ~33 chars/sec
            {
                _typeTimer -= 0.03f;
                _typeCharIndex++;
                if (_typeCharIndex >= GetCurrentLine().Length)
                {
                    _isTyping = false;
                }
                DialogueUI.Instance?.UpdateTypewriter(GetCurrentLine(), _typeCharIndex);
            }
        }

        // 空格推进对话
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            Advance();
    }

    private string GetCurrentLine()
    {
        return _currentNode.lines[_currentLineIndex];
    }

    private void ShowCurrentLine()
    {
        _isTyping = true;
        _typeCharIndex = 0;
        _typeTimer = 0f;

        DialogueUI.Instance?.ShowLine(
            _currentNode.speakerName,
            GetCurrentLine()
        );
    }

    /// <summary>推进对话</summary>
    public void Advance()
    {
        if (_isTyping)
        {
            // 打字中 → 跳过打字，立即完成
            _isTyping = false;
            _typeCharIndex = GetCurrentLine().Length;
            DialogueUI.Instance?.UpdateTypewriter(GetCurrentLine(), _typeCharIndex);
            return;
        }

        // 当前行完成
        _currentLineIndex++;

        if (_currentLineIndex < _currentNode.lines.Count)
        {
            // 还有更多行
            ShowCurrentLine();
        }
        else
        {
            // 节点结束
            ApplyNodeEffects();

            if (_currentNode.choices.Count > 0)
            {
                // 有选项 → 显示选项
                List<DialogueChoice> availableChoices = new();
                foreach (var choice in _currentNode.choices)
                {
                    if (AreConditionsMet(choice.conditions))
                        availableChoices.Add(choice);
                }
                DialogueUI.Instance?.ShowChoices(availableChoices);
            }
            else if (!string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                // 无选项，跳转到下一节点
                GoToNode(_currentNode.nextNodeId);
            }
            else
            {
                // 对话结束
                EndDialogue();
            }
        }
    }

    /// <summary>选择选项</summary>
    public void SelectChoice(int index)
    {
        if (_currentNode == null || index < 0 || index >= _currentNode.choices.Count)
            return;

        DialogueChoice choice = _currentNode.choices[index];

        // 应用效果
        foreach (var effect in choice.effects)
            effect.Apply();

        if (!string.IsNullOrEmpty(choice.nextNodeId))
        {
            GoToNode(choice.nextNodeId);
        }
        else
        {
            EndDialogue();
        }
    }

    private void GoToNode(string nodeId)
    {
        DialogueNode nextNode = null;
        foreach (var node in _currentTree.nodes)
        {
            if (node.nodeId == nodeId && AreConditionsMet(node.conditions))
            {
                nextNode = node;
                break;
            }
        }

        if (nextNode != null)
        {
            _currentNode = nextNode;
            _currentLineIndex = 0;
            ApplyNodeEffects();
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void ApplyNodeEffects()
    {
        if (_currentNode.effects == null) return;
        foreach (var effect in _currentNode.effects)
            effect.Apply();
    }

    private bool AreConditionsMet(DialogueCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0) return true;
        foreach (var c in conditions)
            if (!c.IsMet()) return false;
        return true;
    }

    /// <summary>结束对话</summary>
    public void EndDialogue()
    {
        IsActive = false;
        _currentNpcId = null;
        _currentTree = null;
        _currentNode = null;
        _isTyping = false;

        InputManager.Instance.IsInputLocked = false;
        GameManager.Instance.SetState(GameState.Playing);
        DialogueUI.Instance?.Hide();
        EventBus.Publish(GameEvent.NPCDialogueEnded, _currentNpcId);
    }
}
