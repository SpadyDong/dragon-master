using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 对话 UI 控制器
/// 底部对话框 + 头像 + 打字机效果 + 选项列表
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("主面板")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("说话人")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Image speakerPortrait;

    [Header("对话文字")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueIndicator; // ▼ 继续提示

    [Header("选项")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Color selectedChoiceColor = Color.yellow;
    [SerializeField] private Color normalChoiceColor = Color.white;

    [Header("动画")]
    [SerializeField] private float openSpeed = 8f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    private List<DialogueChoice> _currentChoices;
    private int _selectedIndex;
    private bool _showingChoices;

    void Awake()
    {
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    /// <summary>显示一行对话</summary>
    public void ShowLine(string speaker, string text)
    {
        dialoguePanel.SetActive(true);
        choicesPanel.SetActive(false);
        _showingChoices = false;

        if (speakerNameText != null) speakerNameText.text = speaker;
        if (dialogueText != null) dialogueText.text = "";
        // 后续加入头像绑定
    }

    /// <summary>更新打字机效果</summary>
    public void UpdateTypewriter(string fullText, int visibleChars)
    {
        if (dialogueText != null)
        {
            dialogueText.text = fullText.Substring(0, visibleChars);
        }

        bool isComplete = visibleChars >= fullText.Length;
        if (continueIndicator != null)
            continueIndicator.SetActive(isComplete && !_showingChoices);
    }

    /// <summary>显示选项</summary>
    public void ShowChoices(List<DialogueChoice> choices)
    {
        _currentChoices = choices;
        _selectedIndex = 0;
        _showingChoices = true;

        if (continueIndicator != null) continueIndicator.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(true);

        // 创建选项按钮
        if (choicesContainer != null && choiceButtonPrefab != null)
        {
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                    btnText.text = (i == 0 ? "> " : "  ") + choices[i].text;
            }
        }
    }

    /// <summary>刷新选项高亮</summary>
    private void RefreshChoiceHighlight()
    {
        if (choicesContainer == null) return;
        for (int i = 0; i < choicesContainer.childCount; i++)
        {
            var btnText = choicesContainer.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = (i == _selectedIndex ? "> " : "  ") + (_currentChoices[i].text);
                btnText.color = (i == _selectedIndex) ? selectedChoiceColor : normalChoiceColor;
            }
        }
    }

    void Update()
    {
        if (!_showingChoices) return;

        // ↑↓ 选择选项
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            _selectedIndex = (_selectedIndex - 1 + _currentChoices.Count) % _currentChoices.Count;
            RefreshChoiceHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            _selectedIndex = (_selectedIndex + 1) % _currentChoices.Count;
            RefreshChoiceHighlight();
        }

        // 空格确认
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            DialogueManager.Instance?.SelectChoice(_selectedIndex);
        }
    }

    /// <summary>隐藏对话框</summary>
    public void Hide()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(false);
        _showingChoices = false;
    }
}
