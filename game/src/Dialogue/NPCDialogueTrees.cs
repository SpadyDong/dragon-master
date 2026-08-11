using System.Collections.Generic;

/// <summary>
/// NPC 对话树数据库
/// 包含所有心事件对话 + 日常对话 + 新手引导对话
/// 运行时由 DialogueManager 加载
/// </summary>
public static class NPCDialogueTrees
{
    /// <summary>构建所有对话树</summary>
    public static List<DialogueTreeData> BuildAll()
    {
        var all = new List<DialogueTreeData>();

        // ===== 卯师傅 心事件 =====
        all.Add(new DialogueTreeData
        {
            dialogueId = "mao_shifu_n1", npcId = "mao_shifu",
            nodes = new()
            {
                new() { nodeId="start", speakerName="卯师傅", lines=new(){"今天试了一道新菜——火候比上次更准了。","你运气好，正好赶上第一锅。"},
                    choices=new(){ new(){ text="让我尝尝！", nextNodeId="taste", affectionChange=20 }, new(){ text="下次吧", nextNodeId="end" } } },
                new() { nodeId="taste", speakerName="卯师傅", lines=new(){"好吃吗？这道菜的秘诀是……啧，算了。","等你下次来，我再告诉你。"}, nextNodeId="end" },
                new() { nodeId="end", speakerName="卯师傅", lines=new(){"行，灶上还有活。有空再来。"} }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "mao_shifu_n2", npcId = "mao_shifu",
            nodes = new()
            {
                new() { nodeId="start", speakerName="卯师傅", lines=new(){"今天起得早，蒸了姜茶——养母的秘方。","你看起来有点疲惫，来一杯吧。"},
                    effects=new(){ new(){ type=DialogueEffectType.TriggerEvent, param="learn_ginger_tea_recipe", value=1 } },
                    choices=new(){ new(){ text="谢谢！真暖和", nextNodeId="secret", affectionChange=25 }, new(){ text="这是什么配方？", nextNodeId="recipe", affectionChange=20 } } },
                new() { nodeId="secret", speakerName="卯师傅", lines=new(){"姜要选三年的老姜，切的时候不能使劲——要顺着纹理。","茶底是野山蜂蜜，一年只采一季。"},
                    effects=new(){ new(){ type=DialogueEffectType.GiveItem, param="ginger_tea", value=1 } },
                    nextNodeId="end" },
                new() { nodeId="recipe", speakerName="卯师傅", lines=new(){"这个配方，除了我和养母，你是第三个知道的。","不是因为别的——是因为你会认真喝。"}, nextNodeId="end" },
                new() { nodeId="end", speakerName="卯师傅", lines=new(){"灶上还有一锅。喝完再说。"} }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "mao_shifu_n3", npcId = "mao_shifu",
            nodes = new()
            {
                new() { nodeId="start", speakerName="卯师傅", lines=new(){"今天是圆桌宴——人比平时多了不少。","你坐这里。这是你平时最喜欢的位子。"},
                    choices=new(){ new(){ text="你今天很开心嘛", nextNodeId="honest", affectionChange=30 }, new(){ text="是因为我吗？", nextNodeId="blush", affectionChange=40 } } },
                new() { nodeId="honest", speakerName="卯师傅", lines=new(){"……被你看出来了。","今天这道云腾八珍锅，比平时多加了一味。"},
                    effects=new(){ new(){ type=DialogueEffectType.ChangeAffection, value=30 } },
                    choices=new(){ new(){ text="什么？", nextNodeId="confess" } } },
                new() { nodeId="blush", speakerName="卯师傅", lines=new(){"……锅糊不了，你的嘴倒是挺会说的。","这味香料，本来只给云腾八珍锅用的。"}, nextNodeId="confess" },
                new() { nodeId="confess", speakerName="卯师傅", lines=new(){"从今天起——也给你一个人用。","火候到了，不坦白不行了。我这个人，认准了就是一辈子。"} }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "mao_shifu_n4", npcId = "mao_shifu",
            nodes = new()
            {
                new() { nodeId="start", speakerName="卯师傅", lines=new(){"你醒了？桌上已经做好了。","不是饭馆的菜——是专门给你做的。以后每天都是。"},
                    effects=new(){ new(){ type=DialogueEffectType.ChangeAffection, value=50 } },
                    choices=new(){ new(){ text="我愿意。", nextNodeId="married", affectionChange=100 } } },
                new() { nodeId="married", speakerName="卯师傅", lines=new(){"好。从今往后——灶上的火为两个人烧。"} }
            }
        });

        // ===== 阿岚 心事件 =====
        all.Add(new DialogueTreeData
        {
            dialogueId = "alan_n1", npcId = "alan",
            nodes = new()
            {
                new() { nodeId="start", speakerName="阿岚", lines=new(){"来训练场了？正好——跟我过两招。","不用保留实力，我看得出你最近进步很大。"},
                    choices=new(){ new(){ text="请赐教！", nextNodeId="spar", affectionChange=25 }, new(){ text="我怕伤着你", nextNodeId="spar" } } },
                new() { nodeId="spar", speakerName="阿岚", lines=new(){"很好——你的动作比上次干净多了。","你和你爷爷一样，有股不服输的劲。"},
                    effects=new(){ new(){ type=DialogueEffectType.ChangeAffection, value=25 } } }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "alan_n2", npcId = "alan",
            nodes = new()
            {
                new() { nodeId="start", speakerName="阿岚", lines=new(){"谢谢你来陪我们。小石又发烧了——寒川说问题不大，但我还是放心不下。","他睡着了还在说\"我要像哥哥一样\"——这孩子。"},
                    choices=new(){ new(){ text="他会好起来的", nextNodeId="end", affectionChange=20 } } },
                new() { nodeId="end", speakerName="阿岚", lines=new(){"嗯。谢谢你。有你在，我就不那么怕了。"} }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "alan_n3", npcId = "alan",
            nodes = new()
            {
                new() { nodeId="start", speakerName="阿岚", lines=new(){"今天是他们的忌日。每年我都会来。","小石还太小——有些事他不知道比较好。但我可以跟你说。"},
                    choices=new(){ new(){ text="我在这里", nextNodeId="cry", affectionChange=30 } } },
                new() { nodeId="cry", speakerName="阿岚", lines=new(){"他们说——只要龙还在天上飞，他们就在看着我们。","我不知道是不是真的。但每次训练的时候，我确实能感觉到。","就像有人在我身后推了一把。"},
                    effects=new(){ new(){ type=DialogueEffectType.ChangeAffection, value=40 } } }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "alan_n4", npcId = "alan",
            nodes = new()
            {
                new() { nodeId="start", speakerName="阿岚", lines=new(){"我知道现在说这个很奇怪……但我不想再等了。","小石说——\"哥哥，你要是喜欢她，就应该说出来。\"他被一个10岁的孩子教训了。"},
                    choices=new(){ new(){ text="我愿意", nextNodeId="yes", affectionChange=100 } } },
                new() { nodeId="yes", speakerName="阿岚", lines=new(){"我从未想过会再有家人。从今天起——你和翠翠，还有小石——我们是一家人。"},
                    effects=new(){ new(){ type=DialogueEffectType.ChangeAffection, value=100 } } }
            }
        });

        // ===== 新手引导对话 =====
        all.Add(new DialogueTreeData
        {
            dialogueId = "tutorial_day1_farm", npcId = "laocunzhang",
            nodes = new()
            {
                new() { nodeId="start", speakerName="老村长", lines=new(){"这是你爷爷的地。","拿着这把锄头，先开几块地。","种下芜菁种子，浇上水，明天就能发芽了。"},
                    effects=new(){ new(){ type=DialogueEffectType.GiveItem, param="rusty_hoe", value=1 }, new(){ type=DialogueEffectType.GiveItem, param="turnip_seed", value=5 } } }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "tutorial_day3_work", npcId = "alan",
            nodes = new()
            {
                new() { nodeId="start", speakerName="阿岚", lines=new(){"来打工吗？每天4小时，练练手艺还能赚钱。","打工还能提高熟练度——做多了自然熟练。武器这一行，没有捷径。"},
                    choices=new(){ new(){ text="开始打工（4小时）", nextNodeId="end", affectionChange=10 }, new(){ text="下次再说", nextNodeId="end" } } },
                new() { nodeId="end", speakerName="阿岚", lines=new(){"好，4小时后见。"} }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "tutorial_day4_market", npcId = "mao_shifu",
            nodes = new()
            {
                new() { nodeId="start", speakerName="卯师傅", lines=new(){"菜市场的价格每天不一样！","种多了同一种菜，价钱会跌——我每天去菜市场都先看牌子。","来，尝尝今日的烤鱼——免费。"},
                    effects=new(){ new(){ type=DialogueEffectType.TriggerEvent, param="restore_stamina_full", value=1 } } }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "tutorial_day5_tool", npcId = "tiemei",
            nodes = new()
            {
                new() { nodeId="start", speakerName="铁梅", lines=new(){"锈锄头？拿来，我给你换成铜的。","铜锭我收着也是收着——放在仓库里是铁，打成工具才是金。","按 E 可以打开背包——别丢了，这铜锄头比锈的好用多了。"} }
            }
        });

        all.Add(new DialogueTreeData
        {
            dialogueId = "tutorial_day6_equip", npcId = "tiemei",
            nodes = new()
            {
                new() { nodeId="start", speakerName="铁梅", lines=new(){"按 Q 打开装备栏。武器影响战斗伤害，防具减少受伤。","这个布衣给你——别嫌破，比光着强。等你厉害了，再回来找我打好的。"},
                    effects=new(){ new(){ type=DialogueEffectType.GiveItem, param="cloth_armor", value=1 } } }
            }
        });

        // ===== 日常对话（好感度分段） =====
        all.Add(BuildDailyDialogue("mao_shifu", "卯师傅",
            ("陌生", "欢迎光临卯时饭馆。今日菜单写在墙上了，自己看。"),
            ("熟悉", "又来啦。上次的烤鱼觉得怎么样？盐放多了？不会的，我从来不错盐。"),
            ("友好", "最近菜市场的香料涨价了——不过给你还是按老价钱。"),
            ("亲密", "你上次说喜欢麻一点的口味，我今天试了新配方。尝尝？")));

        all.Add(BuildDailyDialogue("alan", "阿岚",
            ("陌生", "你是新来的吧？我是阿岚，在武器店打工。有事可以找我。"),
            ("熟悉", "训练场上见你几次了——动作挺利落。你爷爷留下的功底还在。"),
            ("友好", "小石最近老提起你。他说你给了他一块龙鳞——他很当宝贝。"),
            ("亲密", "有时候我想，如果爸妈还在……算了，不说这个。今天要一起训练吗？")));

        all.Add(BuildDailyDialogue("tiemei", "铁梅",
            ("陌生", "要打什么？剑、农具、还是修东西？别站门口挡光。"),
            ("熟悉", "你的锄头还挺耐用——你用得不错。好好保养，能用两季。"),
            ("友好", "这把新锤子打好了，准备给磐石那家伙送去。你帮我看一眼——直不直？"),
            ("亲密", "我爹偷偷问我\"那孩子是不是对你有意思\"——被我拿锤子拍桌子了。拍的意思就是——他说得对。")));

        return all;
    }

    static DialogueTreeData BuildDailyDialogue(string npcId, string name,
        params (string affectionLevel, string line)[] dailyLines)
    {
        var nodes = new List<DialogueNode>();
        foreach (var (level, line) in dailyLines)
        {
            int requiredAffection = level switch
            {
                "亲密" => 1000, "友好" => 500, "熟悉" => 200, _ => 0
            };
            var conditions = requiredAffection > 0
                ? new DialogueCondition[] { new() { type = DialogueConditionType.AffectionAtLeast, value = requiredAffection } }
                : null;

            nodes.Add(new DialogueNode
            {
                nodeId = level,
                speakerName = name,
                lines = new() { line },
                conditions = conditions,
                nextNodeId = "end"
            });
        }
        nodes.Add(new DialogueNode { nodeId = "end", speakerName = name, lines = new() { "（对话结束）" } });

        return new DialogueTreeData { dialogueId = $"{npcId}_daily", npcId = npcId, nodes = nodes };
    }
}
