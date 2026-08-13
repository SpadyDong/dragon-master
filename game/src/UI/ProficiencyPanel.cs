using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 熟练度面板 — 6 大熟练度等级/进度 + 专精分支选择
/// </summary>
public class ProficiencyPanel : MonoBehaviour
{
    [Header("列表容器")]
    [SerializeField] private Transform skillListContainer;
    [SerializeField] private GameObject skillEntryPrefab;

    [Header("专精选择弹窗")]
    [SerializeField] private GameObject branchPopup;
    [SerializeField] private TextMeshProUGUI branchPopupTitle;
    [SerializeField] private TextMeshProUGUI branchADescription;
    [SerializeField] private TextMeshProUGUI branchBDescription;
    [SerializeField] private Button branchAButton;
    [SerializeField] private Button branchBButton;

    private List<GameObject> _entryObjects = new();
    private ProficiencySkill _pendingSkill;
    private int _pendingTier;

    void Start()
    {
        if (branchPopup != null) branchPopup.SetActive(false);
    }

    /// <summary>刷新熟练度面板</summary>
    public void Refresh()
    {
        if (ProficiencyManager.Instance == null) return;
        if (skillListContainer == null || skillEntryPrefab == null) return;

        // 清除旧条目
        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        foreach (ProficiencySkill skill in System.Enum.GetValues(typeof(ProficiencySkill)))
        {
            var entry = Instantiate(skillEntryPrefab, skillListContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var levelText = entry.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            var xpBar = entry.transform.Find("XPBar")?.GetComponent<Slider>();
            var branch1Text = entry.transform.Find("Branch1Text")?.GetComponent<TextMeshProUGUI>();
            var branch2Text = entry.transform.Find("Branch2Text")?.GetComponent<TextMeshProUGUI>();
            var branch1Button = entry.transform.Find("Branch1Button")?.GetComponent<Button>();
            var branch2Button = entry.transform.Find("Branch2Button")?.GetComponent<Button>();

            int level = ProficiencyManager.Instance.GetLevel(skill);
            float progress = ProficiencyManager.Instance.GetLevelProgress(skill);

            if (nameText != null)
                nameText.text = $"{ProficiencyUtils.GetSkillIcon(skill)} {ProficiencyUtils.GetSkillName(skill)}";
            if (levelText != null)
            {
                string title = ProficiencyManager.Instance.HasTitle(skill) ? $"  [称号:{ProficiencyUtils.GetTitle(skill)}]" : "";
                levelText.text = $"Lv.{level}{title}";
            }
            if (xpBar != null) xpBar.value = progress;

            // 专精分支显示
            SetupBranchUI(skill, 1, branch1Text, branch1Button);
            SetupBranchUI(skill, 2, branch2Text, branch2Button);
        }
    }

    private void SetupBranchUI(ProficiencySkill skill, int tier, TextMeshProUGUI text, Button button)
    {
        var branch = ProficiencyManager.Instance.GetBranch(skill, tier);
        if (text != null)
        {
            text.text = tier == 1 ? "Lv10 专精" : "Lv30 专精";
            if (branch != ProficiencyBranch.None)
                text.text = ProficiencyUtils.GetBranchName(skill, branch, tier);
        }

        if (button != null)
        {
            int requiredLevel = tier == 1 ? 10 : 30;
            bool canChoose = ProficiencyManager.Instance.GetLevel(skill) >= requiredLevel &&
                             ProficiencyManager.Instance.GetBranch(skill, tier) == ProficiencyBranch.None;

            button.gameObject.SetActive(canChoose);
            button.onClick.RemoveAllListeners();
            if (canChoose)
            {
                var capturedSkill = skill;
                var capturedTier = tier;
                button.onClick.AddListener(() => ShowBranchPopup(capturedSkill, capturedTier));
            }
        }
    }

    /// <summary>显示专精分支选择弹窗</summary>
    private void ShowBranchPopup(ProficiencySkill skill, int tier)
    {
        _pendingSkill = skill;
        _pendingTier = tier;

        if (branchPopupTitle != null)
            branchPopupTitle.text = $"{ProficiencyUtils.GetSkillName(skill)} Lv{(tier == 1 ? 10 : 30)} 专精选择";
        if (branchADescription != null)
            branchADescription.text = ProficiencyUtils.GetBranchName(skill, ProficiencyBranch.BranchA, tier);
        if (branchBDescription != null)
            branchBDescription.text = ProficiencyUtils.GetBranchName(skill, ProficiencyBranch.BranchB, tier);

        if (branchAButton != null)
        {
            branchAButton.onClick.RemoveAllListeners();
            branchAButton.onClick.AddListener(() => ChooseBranch(ProficiencyBranch.BranchA));
        }
        if (branchBButton != null)
        {
            branchBButton.onClick.RemoveAllListeners();
            branchBButton.onClick.AddListener(() => ChooseBranch(ProficiencyBranch.BranchB));
        }

        if (branchPopup != null) branchPopup.SetActive(true);
    }

    private void ChooseBranch(ProficiencyBranch branch)
    {
        if (ProficiencyManager.Instance != null)
            ProficiencyManager.Instance.ChooseBranch(_pendingSkill, _pendingTier, branch);

        if (branchPopup != null) branchPopup.SetActive(false);
        Refresh();
    }
}
