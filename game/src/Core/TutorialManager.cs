using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 七日新手引导管理器
/// 自然剧情推进，不弹窗，每完成一步自动推进
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [System.Serializable]
    public class TutorialStep
    {
        public int day;                              // 触发天数
        public string stepId;                        // 唯一标识
        public string description;                   // 描述
        public string triggerHint;                   // 玩家提示文字
        public TutorialTriggerType triggerType;       // 触发方式
        public string[] completionEvents;            // 完成时发布的事件
        public string[] rewardItemIds;               // 奖励物品 ID
        public int[] rewardItemCounts;               // 奖励物品数量
        public int rewardGold;                       // 奖励金币
        public string dialogueNpcId;                 // 引导对话的 NPC
        public string[] dialogueLines;               // 引导对话内容
    }

    public enum TutorialTriggerType
    {
        Auto,               // 自动触发（起床/进门）
        InteractWith,       // 与指定物体/NPC交互
        EnterArea,          // 进入指定区域
        UseItem,            // 使用指定物品
        CompleteAction      // 完成特定动作
    }

    public enum TutorialPhase
    {
        Done,
        Day1_Move,          // WASD移动
        Day1_Farm,          // 锄地+播种+浇水
        Day1_Sleep,         // 睡觉保存
        Day2_Map,           // 按M打开地图
        Day2_Crops,         // 看田里发芽
        Day2_Talk,          // NPC对话
        Day3_Work,          // 打工系统
        Day4_Market,        // 菜市场+烹饪
        Day5_ToolUpgrade,   // 工具升级+背包
        Day6_Equip,         // 装备系统
        Day7_Dragon         // 幼龙孵化+养龙
    }

    [Header("教程步骤")]
    [SerializeField] private List<TutorialStep> steps;

    [Header("UI")]
    [SerializeField] private GameObject tutorialHintUI;

    private TutorialPhase _currentPhase = TutorialPhase.Day1_Move;
    private int _currentStepIndex;
    private bool _waitingForCompletion;

    // 进度标记
    private bool _hasMoved, _hasFarmed, _hasSlept, _hasOpenedMap, _hasTalked, _hasWorked;
    private bool _hasVisitedMarket, _hasUpgradedTool, _hasOpenedInventory, _hasHatchedDragon;
    private bool _hasOpenedEquipment;

    void Awake()
    {
        Instance = this;
        if (steps == null) steps = new List<TutorialStep>();
        if (steps.Count == 0) BuildTutorialSteps();
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        ShowCurrentHint();
    }

    void Update()
    {
        CheckCompletionConditions();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    /// <summary>构建 7 天教程步骤</summary>
    private void BuildTutorialSteps()
    {
        steps = new List<TutorialStep>
        {
            // Day 1
            new() { day=1, stepId="tut_move", description="WASD 8方向移动", triggerHint="使用 WASD 键移动", triggerType=TutorialTriggerType.Auto },
            new() { day=1, stepId="tut_farm", description="锄地+播种+浇水", triggerHint="按 F 与锄头交互，锄地后播种浇水", triggerType=TutorialTriggerType.Auto,
                rewardItemIds=new[]{"rusty_hoe","watering_can","turnip_seed"}, rewardItemCounts=new[]{1,1,5}, dialogueNpcId="laocunzhang",
                dialogueLines=new[]{"这是你爷爷的地。","拿着这把锄头，先开几块地。","种下芜菁种子，浇上水，明天就能发芽了。"} },
            new() { day=1, stepId="tut_sleep", description="睡觉保存", triggerHint="回木屋，按 F 与床交互睡觉", triggerType=TutorialTriggerType.Auto },
            // Day 2
            new() { day=2, stepId="tut_map", description="按 M 打开地图", triggerHint="按 M 键查看全镇地图", triggerType=TutorialTriggerType.Auto, rewardGold=100 },
            new() { day=2, stepId="tut_crops", description="查看作物发芽", triggerHint="出门看看田里", triggerType=TutorialTriggerType.Auto },
            new() { day=2, stepId="tut_talk", description="与NPC对话", triggerHint="走向广场，靠近 NPC 按 F 交谈", triggerType=TutorialTriggerType.InteractWith,
                dialogueNpcId="alan", dialogueLines=new[]{"听说你是宗师的后人，来看看。","以后有需要去武器店找我。"} },
            // Day 3
            new() { day=3, stepId="tut_work", description="打工系统", triggerHint="去武器店找阿岚，按 F 选择打工", triggerType=TutorialTriggerType.InteractWith,
                rewardGold=80, dialogueNpcId="alan",
                dialogueLines=new[]{"来打工吗？每天4小时，练练手艺还能赚钱。","打工还能提高熟练度——做多了自然熟练。"} },
            // Day 4
            new() { day=4, stepId="tut_market", description="菜市场+食物", triggerHint="去饭馆找卯师傅，了解一下菜市场", triggerType=TutorialTriggerType.InteractWith,
                dialogueNpcId="mao_shifu",
                dialogueLines=new[]{"菜市场的价格每天不一样！","种多了同一种菜，价钱会跌。","来，尝尝今日的烤鱼——免费。"} },
            // Day 5
            new() { day=5, stepId="tut_tool", description="工具升级+背包", triggerHint="把锈锄头带去铁匠铺找铁梅", triggerType=TutorialTriggerType.Auto,
                rewardItemIds=new[]{"copper_ingot"}, rewardItemCounts=new[]{5},
                dialogueNpcId="tiemei", dialogueLines=new[]{"锈锄头？拿来，我给你换成铜的。","按 E 可以打开背包。"} },
            // Day 6
            new() { day=6, stepId="tut_equip", description="装备系统", triggerHint="去铁匠铺找铁梅了解装备", triggerType=TutorialTriggerType.InteractWith,
                rewardItemIds=new[]{"cloth_armor"}, rewardItemCounts=new[]{1},
                dialogueNpcId="tiemei", dialogueLines=new[]{"按 Q 打开装备栏。","武器影响战斗伤害，防具减少受伤。","这个布衣给你，别嫌破——比光着强。"} },
            // Day 7
            new() { day=7, stepId="tut_dragon", description="幼龙孵化+养龙", triggerHint="晚上去屋后枯井查看", triggerType=TutorialTriggerType.Auto,
                rewardItemIds=new[]{"dragon_food"}, rewardItemCounts=new[]{10},
                dialogueNpcId="alan", dialogueLines=new[]{"枯井下有声音？那是你爷爷留下的龙蛋！","去看看吧。"} },
        };
    }

    private void OnDayChanged(int day)
    {
        ShowCurrentHint();
    }

    private void ShowCurrentHint()
    {
        if (_currentStepIndex >= steps.Count) return;
        var step = steps[_currentStepIndex];
        int today = GameManager.Instance.day;

        if (today >= step.day)
        {
            // 显示提示
            if (tutorialHintUI != null)
                tutorialHintUI.SetActive(true);

            // 对话引导
            if (!string.IsNullOrEmpty(step.dialogueNpcId) && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.QueueTutorialDialogue(step.dialogueNpcId, step.dialogueLines);
            }
        }
    }

    private void CheckCompletionConditions()
    {
        if (_currentStepIndex >= steps.Count) return;

        var step = steps[_currentStepIndex];

        switch (step.stepId)
        {
            case "tut_move":
                if (!_hasMoved && InputManager.Instance.GetMoveInput().magnitude > 0.5f)
                    _hasMoved = true;
                if (_hasMoved) AdvanceStep();
                break;
            case "tut_farm":
                // 由 FarmingController 调用 CompleteStep 标记
                break;
            case "tut_sleep":
                // 由 DayTransition 调用
                break;
            case "tut_map":
                if (Input.GetKeyDown(KeyCode.M)) _hasOpenedMap = true;
                if (_hasOpenedMap) AdvanceStep();
                break;
            case "tut_talk":
                // 由 DialogueManager 在对话结束时标记
                break;
            case "tut_work":
                // 由打工系统标记
                break;
            case "tut_market":
                // 由菜市场交互标记
                break;
            case "tut_tool":
                if (Input.GetKeyDown(KeyCode.E)) _hasOpenedInventory = true;
                if (_hasOpenedInventory) AdvanceStep();
                break;
            case "tut_equip":
                if (Input.GetKeyDown(KeyCode.Q)) _hasOpenedEquipment = true;
                if (_hasOpenedEquipment) AdvanceStep();
                break;
            case "tut_dragon":
                // 由枯井剧情触发标记
                break;
        }
    }

    /// <summary>完成当前教程步骤</summary>
    public void CompleteStep(string stepId)
    {
        if (_currentStepIndex >= steps.Count) return;
        var step = steps[_currentStepIndex];
        if (step.stepId != stepId) return;

        AdvanceStep();
    }

    private void AdvanceStep()
    {
        var step = steps[_currentStepIndex];

        // 发放奖励
        if (step.rewardItemIds != null)
        {
            for (int i = 0; i < step.rewardItemIds.Length; i++)
                InventoryManager.Instance?.AddItem(step.rewardItemIds[i], step.rewardItemCounts[i]);
        }
        if (step.rewardGold > 0)
            GameManager.Instance.AddGold(step.rewardGold);

        // 发布完成事件
        if (step.completionEvents != null)
        {
            foreach (string evt in step.completionEvents)
                EventBus.Publish(GameEvent.PlayerInteracted, evt);
        }

        _currentStepIndex++;
        if (tutorialHintUI != null && _currentStepIndex >= steps.Count)
            tutorialHintUI.SetActive(false);

        ShowCurrentHint();
    }

    /// <summary>获取当前阶段（供外部查询）</summary>
    public TutorialPhase GetCurrentPhase() => _currentPhase;

    /// <summary>教程是否全部完成</summary>
    public bool IsComplete => _currentStepIndex >= steps.Count;
}
