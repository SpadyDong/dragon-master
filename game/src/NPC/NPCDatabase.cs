using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NPC 数据库工厂
/// 运行时创建全部 40 名常驻 NPC 的完整数据
/// 在游戏初始化时调用 NPCDatabase.BuildAll()
/// </summary>
public static class NPCDatabase
{
    private static Dictionary<string, NPCDataContainer> _all = new();

    /// <summary>构建全部 41 名 NPC 数据</summary>
    public static Dictionary<string, NPCDataContainer> BuildAll()
    {
        _all = new();

        AddAlan(); AddPanshi(); AddLinmo(); AddYuntao(); AddQingye();
        AddHanchuan(); AddChixiao();
        AddMuqing(); AddChengxi(); AddXiaotang(); AddYeling(); AddQiuyue();
        AddTiemei(); AddXuelai(); AddLiuyan(); AddMaoShifu();
        AddLaocunzhang(); AddChenBlacksmith(); AddShenCouple(); AddZhouWood();
        AddSuYi(); AddHuaShu(); AddMafuLaoLi(); AddAkuang(); AddHuapo();
        AddPrincipal(); AddYupo(); AddAbao(); AddMaoDaniang();
        AddXiaoshi(); AddHanbai(); AddXiaotangKid(); AddAdan(); AddDoudou(); AddXiaoya();
        AddDayeye(); AddAma(); AddGeShu(); AddLaoxuezhe();

        return _all;
    }

    static void Add(string id, NPCDataContainer d) { d.npcId = id; _all[id] = d; }
    static NPCDataContainer Base(string id, string name, int age, string gender, string element, string weapon, string weaponName, bool marry)
        => new() { npcId=id, npcName=name, age=age, gender=gender, element=element, weaponType=weapon, weaponName=weaponName, isMarriageable=marry };

    // ==================== 青年可结婚 · 男（7） ====================

    static void AddAlan()
    {
        var d = Base("alan", "阿岚", 24, "男", "风", "单手剑", "月岚", true);
        d.personality = "沉稳刚毅、外硬内暖、对龙格外温柔；不擅长表达感情但在行动上最可靠";
        d.background = "父母是20年前虚空裂缝之战中牺牲的驯龙师。从小由村长和镇民轮流照顾，带着弟弟小石长大。白天在武器店打工，黄昏后独自在训练场练习驯龙，继承父母的遗志。";
        d.hobbies = "驯龙训练、研读战史、打磨武器、陪小石看星星";
        d.residence = "广场南旧驯龙师小屋（与小石同住）";
        d.birthMonth = 1; d.birthDay = 7;
        d.lovedItems = "野味烤肉,风灵石,龙鳞护符,羽毛笔,矿石"; d.likedItems = "矿石,生肉"; d.hatedItems = "花卉,甜食,生鱼";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="alan_n1", description="N1·训练场切磋", locationHint="驯龙训练场·黄昏", requiredHourRange=new[]{17,20} },
            new(){ eventIndex=2, requiredAffection=500, eventId="alan_n2", description="N2·陪小石看病", locationHint="诊所·白天", requiredHourRange=new[]{8,17} },
            new(){ eventIndex=3, requiredAffection=1000, eventId="alan_n3", description="N3·父母忌日扫墓", locationHint="遗忘墓园·春7", requiredHourRange=new[]{6,18} },
            new(){ eventIndex=4, requiredAffection=2500, eventId="alan_n4", description="N4·告白", locationHint="驯龙训练场·夜晚", requiredHourRange=new[]{20,24} }
        };
        d.relationships = new NPCRelationship[] {
            new(){ targetNpcId="xiaoshi", relationshipType="兄弟", description="相依为命的弟弟", baseAffection=90 },
            new(){ targetNpcId="panshi", relationshipType="兄弟般好友", description="常一起喝酒切磋", baseAffection=75 },
            new(){ targetNpcId="muqing", relationshipType="尊重", description="请教历史问题", baseAffection=50 },
            new(){ targetNpcId="chixiao", relationshipType="切磋对手", description="惺惺相惜", baseAffection=55 }
        };
        Add("alan", d);
    }

    static void AddPanshi()
    {
        var d = Base("panshi", "磐石", 27, "男", "土", "双手剑", "岩碎", true);
        d.personality = "粗犷豪爽、讲义气、嗓门大、对妹妹柳烟秒变温柔大哥";
        d.background = "少年时父母双亡，带着妹妹柳烟在矿场谋生。因发现龙饰矿石被前店主收为学徒，8年后盘下店铺。右臂有矿难烧痕，\"这是男人活过的证明\"。";
        d.hobbies = "矿洞探险、收集稀有矿石、酒吧大口喝酒";
        d.residence = "龙之商店二楼（与柳烟同住）";
        d.birthMonth = 2; d.birthDay = 3;
        d.lovedItems = "稀有矿石,土灵石,龙鞍设计图,烈酒,野味"; d.likedItems = "生肉,烈酒,锻造工具"; d.hatedItems = "花卉,生鱼,玩具";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="panshi_n1", description="N1·矿洞救援", locationHint="矿洞·任意时间" },
            new(){ eventIndex=2, requiredAffection=500, eventId="panshi_n2", description="N2·给柳烟准备嫁妆", locationHint="龙之商店·白天" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="panshi_n3", description="N3·醉酒吐真言", locationHint="酒吧·夜晚" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="panshi_n4", description="N4·新手打造龙鞍求婚", locationHint="龙之商店·黄昏" }
        };
        d.relationships = new NPCRelationship[] {
            new(){ targetNpcId="liuyan", relationshipType="兄妹", description="最疼爱的妹妹", baseAffection=85 },
            new(){ targetNpcId="alan", relationshipType="兄弟般好友", description="常一起喝酒", baseAffection=75 },
            new(){ targetNpcId="akuang", relationshipType="前同事", description="师父辈矿工", baseAffection=65 }
        };
        Add("panshi", d);
    }

    static void AddLinmo()
    {
        var d = Base("linmo", "林墨", 23, "男", "风", "弓箭", "灵雀弓", true);
        d.personality = "细腻文艺、手巧心静、不喜吵闹、说话轻声但有坚定的主见";
        d.background = "父亲周木匠是镇上唯一的木匠。母亲早逝后父子相依为命。16岁独立完成议事大厅木结构翻新一战成名。喜欢把木头雕成小动物送给镇上孩子。";
        d.hobbies = "木雕、家具设计、画建筑草图、雨天看书";
        d.residence = "木工坊后屋（与父亲周木匠同住）";
        d.birthMonth = 2; d.birthDay = 19;
        d.lovedItems = "高级木材,木雕,精美家具图纸,野花,果实"; d.hatedItems = "生肉,烈酒,矿石";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="linmo_n1", description="N1·木雕小猫", locationHint="木工坊·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="linmo_n2", description="N2·暴雨修屋顶", locationHint="主角家·雨天" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="linmo_n3", description="N3·父亲的首肯", locationHint="木工坊·黄昏" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="linmo_n4", description="N4·手工婚戒盒", locationHint="村口大树·夜晚" }
        };
        Add("linmo", d);
    }

    static void AddYuntao()
    {
        var d = Base("yuntao", "云涛", 25, "男", "水", "弓箭", "潮汐", true);
        d.personality = "自由散漫、开朗健谈、讨厌被束缚、有一说一绝不绕弯";
        d.background = "渔婆的独子，父亲7岁时出海未归。成年后接手渔店但一周有三天溜去钓鱼——店门口常年挂\"老板钓鱼去了，自助称斤\"木牌。";
        d.hobbies = "钓鱼、沙滩烤鱼、写鱼种图鉴（已60种）";
        d.residence = "渔店后屋（与渔婆同住）";
        d.birthMonth = 3; d.birthDay = 5;
        d.lovedItems = "金色帝王鱼,水灵石,新鱼竿,贝类,海藻"; d.hatedItems = "甜食,花卉,蔬菜";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="yuntao_n1", description="N1·教你钓帝王鱼", locationHint="沙滩·清晨", requiredHourRange=new[]{6,8} },
            new(){ eventIndex=2, requiredAffection=500, eventId="yuntao_n2", description="N2·祭奠父亲放花灯", locationHint="港口·夜晚" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="yuntao_n3", description="N3·被大鱼拖下水", locationHint="海滩·白天" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="yuntao_n4", description="N4·海上日出告白", locationHint="港口·清晨" }
        };
        Add("yuntao", d);
    }

    static void AddQingye()
    {
        var d = Base("qingye", "青野", 22, "男", "火", "单手剑", "灶火刀", true);
        d.personality = "阳光开朗、热爱生活、\"没有什么是一顿好饭解决不了的\"";
        d.background = "父母在他10岁时把他寄养在龙脊镇姨妈家后再也没回来。姨妈是面包房前店主，退休后把店交给他。带着师妹雪莱一起经营。";
        d.hobbies = "尝试新菜谱、给面包取名字、跟卯师傅比厨艺（每次输每次去）";
        d.residence = "面包房后屋";
        d.birthMonth = 3; d.birthDay = 22;
        d.lovedItems = "松露,顶级面粉,火焰糖,食材,牛奶,新鲜水果"; d.hatedItems = "生肉,酒,矿石";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="qingye_n1", description="N1·教烤面包", locationHint="面包房·清晨" },
            new(){ eventIndex=2, requiredAffection=500, eventId="qingye_n2", description="N2·童年往事", locationHint="面包房·夜晚" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="qingye_n3", description="N3·面包大赛挑战卯师傅", locationHint="饭馆·白天" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="qingye_n4", description="N4·深夜厨房告白", locationHint="面包房·深夜" }
        };
        Add("qingye", d);
    }

    static void AddHanchuan()
    {
        var d = Base("hanchuan", "寒川", 26, "男", "水", "单手剑", "青囊", true);
        d.personality = "外冷内热、极度理性、言简意赅极少废话，但诊断时极其耐心";
        d.background = "原城里大医院年轻主治医师，未婚妻在手术中去世后离开城市来龙脊镇开诊所。因为连夜上山采药救了老村长，全镇人对他的态度从\"太冷\"变为拥护。";
        d.hobbies = "翻阅医学典籍、爬山采药、溪边发一天呆、每周三免费给老人体检";
        d.residence = "诊所二楼（与寒柏同住）";
        d.birthMonth = 4; d.birthDay = 14;
        d.lovedItems = "稀有药草,医学典籍,水灵石,蔬菜,干净衣物"; d.hatedItems = "生肉,烈酒,烟草";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="hanchuan_n1", description="N1·连夜采药", locationHint="诊所·夜晚" },
            new(){ eventIndex=2, requiredAffection=500, eventId="hanchuan_n2", description="N2·寒柏生病", locationHint="诊所·白天" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="hanchuan_n3", description="N3·妻子的墓", locationHint="遗忘墓园·下午" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="hanchuan_n4", description="N4·星空告白", locationHint="溪边·夜晚" }
        };
        Add("hanchuan", d);
    }

    static void AddChixiao()
    {
        var d = Base("chixiao", "赤霄", 28, "男", "火", "双手剑", "燎原", true);
        d.personality = "热血好斗、正义感爆棚、看到不平事一定管到底、嗓门大笑容更大";
        d.background = "曾是苍莽大陆正规军前线战士，一次守城战以一己之力挡下三只攻城荒兽。战争结束后被推荐到龙脊镇冒险工会当副会长。";
        d.hobbies = "战斗训练、收集BOSS战利品、跟镇上青年比试剑术";
        d.residence = "冒险工会二楼";
        d.birthMonth = 1; d.birthDay = 20;
        d.lovedItems = "BOSS龙角,火灵石,高级双手剑,野味,烈酒"; d.hatedItems = "甜食,花卉,玩具";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="chixiao_n1", description="N1·挑战切磋", locationHint="训练场·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="chixiao_n2", description="N2·旧战伤复发", locationHint="诊所·任意时间" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="chixiao_n3", description="N3·退伍战友来信", locationHint="冒险工会·傍晚" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="chixiao_n4", description="N4·工会全体面前求婚", locationHint="冒险工会·白天" }
        };
        Add("chixiao", d);
    }

    // ==================== 青年可结婚 · 女（9） ====================

    static void AddMuqing()
    {
        var d = Base("muqing", "暮晴", 23, "女", "土", "弓箭", "考古者", true);
        d.personality = "知性温柔、博学但不卖弄、安静但不孤僻；聊起古文字停不下来";
        d.background = "5岁被老学者从废墟中捡回收养，左肩有一块类似龙鳞的胎记。镇上唯一古文字专家，正在翻译祖父留下的石碑铭文。";
        d.hobbies = "考古挖掘、翻译古文字、花园散步、深夜观星";
        d.residence = "博物馆二楼"; d.birthMonth = 1; d.birthDay = 4;
        d.lovedItems = "古币,灵石粉末,龙饰材料,书卷,矿石,茶叶"; d.hatedItems = "生肉,生鱼,脏物";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="muqing_n1", description="N1·博物馆夜话", locationHint="博物馆·夜晚" },
            new(){ eventIndex=2, requiredAffection=500, eventId="muqing_n2", description="N2·翻译祖父手稿", locationHint="图书馆·白天" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="muqing_n3", description="N3·胎记秘密", locationHint="溪边·黄昏" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="muqing_n4", description="N4·千年古碑前告白", locationHint="博物馆·夜晚" }
        };
        Add("muqing", d);
    }

    static void AddChengxi()
    {
        var d = Base("chengxi", "澄溪", 21, "女", "风", "弓箭", "花间矢", true);
        d.personality = "恬静优雅、对自然近乎通灵、\"每朵花都有自己的脾气\"";
        d.background = "由花婆带大的孙女，从未见过父母。花婆说\"你是在郁金香开得最盛那天被风送到花房的\"。凭一片花瓣颜色判断土壤养分。";
        d.hobbies = "培育新花种、给花起名字、做干花书签、雨天花房听雨";
        d.residence = "花房后屋（与花婆同住）"; d.birthMonth = 1; d.birthDay = 15;
        d.lovedItems = "传说花种,稀有鲜花,香氛,蔬果,花束,蚕丝"; d.hatedItems = "生肉,烈酒,矿石";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="chengxi_n1", description="N1·夜开花", locationHint="花房·夜晚" },
            new(){ eventIndex=2, requiredAffection=500, eventId="chengxi_n2", description="N2·花婆的祝福", locationHint="花房·白天" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="chengxi_n3", description="N3·培育新花色", locationHint="秘密花园·白天" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="chengxi_n4", description="N4·花海中告白", locationHint="秘密花园·黄昏" }
        };
        Add("chengxi", d);
    }

    static void AddXiaotang()
    {
        var d = Base("xiaotang", "小棠", 19, "女", "火", "单手剑", "缝针", true);
        d.personality = "活泼可爱、话多爱笑、手巧得令人惊讶、偶尔流露少女害羞";
        d.background = "裁缝铺老师傅关门弟子，师傅搬走后是镇上唯一裁缝。曾三天三夜为叶铃做出演出服。手上永远缠着彩色线头。";
        d.hobbies = "设计新衣服、收集布料、溪边染布、给孩子做布偶";
        d.residence = "裁缝铺后屋"; d.birthMonth = 2; d.birthDay = 8;
        d.lovedItems = "高级丝绸,糖果盒,头饰,彩色毛团,甜品"; d.hatedItems = "矿石,生鱼,工具";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="xiaotang_n1", description="N1·缝补旧衣", locationHint="裁缝铺·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="xiaotang_n2", description="N2·为节日赶制新衣", locationHint="裁缝铺·深夜" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="xiaotang_n3", description="N3·师父来信", locationHint="裁缝铺·傍晚" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="xiaotang_n4", description="N4·情侣装", locationHint="溪边·黄昏" }
        };
        Add("xiaotang", d);
    }

    static void AddYeling()
    {
        var d = Base("yeling", "叶铃", 24, "女", "水", "单手剑", "夜莺", true);
        d.personality = "热情开朗、酒量大到吓人、爽朗笑声是酒吧的背景音，独处时安静得判若两人";
        d.background = "8岁时父母在火山喷发中遇难，被华叔收养。14岁学吉他用音乐愈合伤口。唱的歌都是镇上的人和故事。";
        d.hobbies = "写歌、调鸡尾酒、收集乐器、周五开放麦克风之夜";
        d.residence = "酒吧二楼（与华叔同住）"; d.birthMonth = 2; d.birthDay = 25;
        d.lovedItems = "鸡尾酒配方,稀有水果,闪亮饰品,酒,海鲜,乐器"; d.hatedItems = "烟草,药,矿石";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="yeling_n1", description="N1·点歌之夜", locationHint="酒吧·夜晚" },
            new(){ eventIndex=2, requiredAffection=500, eventId="yeling_n2", description="N2·父母忌日扫墓", locationHint="遗忘墓园·白天" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="yeling_n3", description="N3·外地演出邀请", locationHint="酒吧·傍晚" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="yeling_n4", description="N4·全镇面前唱告白", locationHint="广场·夜晚" }
        };
        Add("yeling", d);
    }

    static void AddQiuyue()
    {
        var d = Base("qiuyue", "秋月", 25, "女", "冰", "弓箭", "冰晶弓", true);
        d.personality = "安静神秘、字字珠玑、冷幽默类型、有独特的反讽式日常";
        d.background = "身世成谜——秋月之夜被发现于图书馆门口的婴儿，篮子里只有一本冰晶封面装订的书。能凭记忆说出任意一本书的位置和内容。那本冰晶书至今打不开。";
        d.hobbies = "看书、整理书架、深夜散步、在书页边缘写四行诗批注";
        d.residence = "图书馆后屋"; d.birthMonth = 3; d.birthDay = 9;
        d.lovedItems = "古籍抄本,冰灵石,水晶花,书,果干,茶"; d.hatedItems = "烈酒,喧闹物,生肉";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="qiuyue_n1", description="N1·寻回禁书", locationHint="图书馆·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="qiuyue_n2", description="N2·冰晶书亮了", locationHint="图书馆·夜晚" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="qiuyue_n3", description="N3·批注诗集", locationHint="图书馆·黄昏" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="qiuyue_n4", description="N4·手写情书告白", locationHint="图书馆·深夜" }
        };
        Add("qiuyue", d);
    }

    static void AddTiemei()
    {
        var d = Base("tiemei", "铁梅", 26, "女", "火", "双手剑", "裂地锤刀", true);
        d.personality = "直爽有力、不扭捏、性格像锤子、18岁时捶穿3块铁板让外地商人闭嘴";
        d.background = "陈铁匠独女，从小在铁砧旁长大，14岁能打单手剑。镇上最好的武器和农具都出自她手。护臂系着父亲送的旧围裙一角。";
        d.hobbies = "锻造武器、敲钉子比赛、收集稀有金属、跟赤霄比举重";
        d.residence = "铁匠铺后屋（与陈铁匠同住）"; d.birthMonth = 3; d.birthDay = 25;
        d.lovedItems = "稀有金属,火灵石,新锤子,矿石,零件,烈酒"; d.hatedItems = "花卉,糖果,丝织品";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="tiemei_n1", description="N1·教你打铁", locationHint="铁匠铺·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="tiemei_n2", description="N2·父亲的旧围裙", locationHint="铁匠铺·黄昏" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="tiemei_n3", description="N3·外地商人再挑战", locationHint="铁匠铺·白天" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="tiemei_n4", description="N4·铁砧旁告白", locationHint="铁匠铺·夜晚" }
        };
        Add("tiemei", d);
    }

    static void AddXuelai()
    {
        var d = Base("xuelai", "雪莱", 20, "女", "风", "单手剑", "糖霜", true);
        d.personality = "天然呆、反射弧长2秒、做甜品时专注力惊人、任何配方一遍成功";
        d.background = "5岁被遗弃在面包房门口被收留。天赋在于味觉——尝过一次就能精确说出配方和克数。\"雪莱特制奶油蛋糕\"吃了会想起幸福的事。";
        d.hobbies = "研发新甜品、裱花（龙形图案）、收集奶油品牌、试吃甜食";
        d.residence = "面包房二楼"; d.birthMonth = 4; d.birthDay = 4;
        d.lovedItems = "甜点食谱,奶油,棉花糖,甜品,水果,糖"; d.hatedItems = "生鱼,生肉,苦物";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="xuelai_n1", description="N1·裱你最喜欢的花", locationHint="面包房·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="xuelai_n2", description="N2·遗弃那晚的回忆", locationHint="面包房门口·夜晚" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="xuelai_n3", description="N3·青野的认可", locationHint="面包房·白天" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="xuelai_n4", description="N4·第1001份甜品给你", locationHint="面包房·深夜" }
        };
        Add("xuelai", d);
    }

    static void AddLiuyan()
    {
        var d = Base("liuyan", "柳烟", 22, "女", "土", "双手剑", "算盘剑", true);
        d.personality = "精明能干、从不做亏本买卖、嘴上说\"不合算\"但私下帮了很多人";
        d.background = "父母双亡后跟着哥哥在矿场边长大。12岁自己学管账，16岁独立打理龙之商店账目。从不离身的账本其实是父母唯一遗物。";
        d.hobbies = "算账、收集宝石、讨价还价、给哥哥买衣服";
        d.residence = "龙之商店二楼（与磐石同住）"; d.birthMonth = 4; d.birthDay = 19;
        d.lovedItems = "金块,珍稀龙蛋,账本,宝石,海鲜"; d.hatedItems = "甜食,廉价物品,花";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="liuyan_n1", description="N1·教主角管账", locationHint="龙之商店·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="liuyan_n2", description="N2·全家福", locationHint="龙之商店二楼·夜晚" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="liuyan_n3", description="N3·哥哥的祝福", locationHint="龙之商店·白天" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="liuyan_n4", description="N4·两个人的新账本", locationHint="溪边·黄昏" }
        };
        Add("liuyan", d);
    }

    static void AddMaoShifu()
    {
        var d = Base("mao_shifu", "卯师傅", 24, "女", "火", "单手剑", "灶火", true);
        d.personality = "爽朗利落、讲究火候、厨房里像掌握时间的神、客人吃了什么她全记得";
        d.background = "自幼被名厨卯大娘收养，16岁独力接下养母饭馆。招牌菜\"云腾八珍锅\"为小镇传奇。每周日圆桌宴让玩家与NPC同桌互动。";
        d.hobbies = "研发新菜、清晨菜市场挑菜、周日圆桌宴（玩家+NPC同桌）";
        d.residence = "饭馆后屋"; d.birthMonth = 4; d.birthDay = 21;
        d.lovedItems = "松露,传说级鱼,完整兽骨,秘制香料,时令鲜蔬"; d.hatedItems = "垃圾食品,低品质原料,现成快餐";
        d.heartEvents = new NPCHeartEvent[] {
            new(){ eventIndex=1, requiredAffection=200, eventId="mao_shifu_n1", description="N1·试吃新菜", locationHint="饭馆·白天" },
            new(){ eventIndex=2, requiredAffection=500, eventId="mao_shifu_n2", description="N2·养母姜茶秘方", locationHint="饭馆·清晨" },
            new(){ eventIndex=3, requiredAffection=1000, eventId="mao_shifu_n3", description="N3·圆桌宴表白", locationHint="饭馆·夜晚" },
            new(){ eventIndex=4, requiredAffection=2500, eventId="mao_shifu_n4", description="N4·晨起厨房做好一桌", locationHint="主角家·清晨" }
        };
        Add("mao_shifu", d);
    }

    // ==================== 中年组（14） ====================
    static void AddLaocunzhang() { var d=Base("laocunzhang","老村长",62,"男","","","",false); d.personality="慈祥睿智，对每个镇民了如指掌"; d.background="第三任村长，祖父好友，保管那封信20年"; d.hobbies="老榆树下喝茶、写日记"; d.residence="议事大厅旁"; d.birthMonth=1;d.birthDay=1; Add("laocunzhang",d); }
    static void AddChenBlacksmith(){ var d=Base("chen_blacksmith","陈铁匠",54,"男","","","",false); d.personality="沉默寡言，对女儿骄傲得不得了但从来不当面说"; d.background="龙脊镇第三代铁匠，40年打铁。阿岚父母的武器是他打的"; d.hobbies="对铁砧自言自语，每季度免费检修全镇农具"; d.residence="铁匠铺后屋";d.birthMonth=2;d.birthDay=16; Add("chen_blacksmith",d); }
    static void AddShenCouple(){ var d1=Base("shen_daniang","沈大娘",48,"女","","","",false); d1.personality="精明世故";d1.hobbies="跟柳烟比算账";d1.residence="杂货铺后屋"; var d2=Base("shen_dashu","沈大叔",50,"男","","","",false); d2.personality="老实憨厚，话少但关键";d2.residence="杂货铺后屋"; Add("shen_daniang",d1);Add("shen_dashu",d2); }
    static void AddZhouWood(){ var d=Base("zhou_wood","周木匠",55,"男","","","",false); d.personality="固执但心底善良"; d.background="妻子早逝，独自抚养林墨。年轻时醉酒摔断左腿从此戒酒"; d.hobbies="榫卯不用钉子、收藏林墨设计图";d.residence="木工坊"; Add("zhou_wood",d); }
    static void AddSuYi(){ var d=Base("su_yi","苏姨",42,"女","","","",false); d.personality="热心肠到管闲事的程度，镇上信息中转站"; d.background="丈夫山洪遇难，独自带儿阿蛋至今";d.hobbies="给独居老人免费洗衣、给年轻人牵红线(成功率0)";d.residence="洗衣店后屋"; Add("su_yi",d); }
    static void AddHuaShu(){ var d=Base("hua_shu","华叔",51,"男","","","",false); d.personality="沉默温和，嘴比死还严"; d.background="曾是都城的首席调酒师，收养叶铃后把所有手艺传给她";d.hobbies="调制自己喝的特调、听叶铃唱歌";d.residence="酒吧二楼";d.birthMonth=3;d.birthDay=14; Add("hua_shu",d); }
    static void AddMafuLaoLi(){ var d=Base("mafu_laoli","马夫老李",58,"男","","","",false); d.personality="爱马如命"; d.background="管马厩30年，祖父的老马还活着";d.hobbies="刷毛、编马掌日记";d.residence="马厩小屋";d.birthMonth=2;d.birthDay=10; Add("mafu_laoli",d); }
    static void AddAkuang(){ var d=Base("akuang","老矿工阿土",57,"男","","","",false); d.personality="耿直憨厚，矿洞就是他的世界"; d.background="砸了40年矿，磐石的前同事";d.hobbies="每周泡一次澡";d.residence="矿洞入口小屋"; Add("akuang",d); }
    static void AddHuapo(){ var d=Base("huapo","花婆",68,"女","","","",false); d.personality="老顽童型慈祥奶奶、\"杀花比杀人更不可饶恕\""; d.background="曾是皇家花园匠人，周游过12国，收集稀有花种";d.hobbies="跟花说话、种花";d.residence="花房后屋";d.birthMonth=1;d.birthDay=28; Add("huapo",d); }
    static void AddPrincipal(){ var d=Base("principal","校长先生",56,"男","","","",false); d.personality="严厉中藏温柔"; d.background="原省城特级教师退休回镇办学";d.hobbies="背古诗、暑假沙漠采风";d.residence="学校旁";d.birthMonth=3;d.birthDay=30; Add("principal",d); }
    static void AddYupo(){ var d=Base("yupo","渔婆",60,"女","","","",false); d.personality="坚韧乐天，悲伤从不流露"; d.background="丈夫未归，独自拉扯云涛。凌晨四点半起床收网";d.hobbies="给云涛做\"父亲的菜\"、对海说话";d.residence="渔店后屋";d.birthMonth=2;d.birthDay=22; Add("yupo",d); }
    static void AddAbao(){ var d=Base("abao","猎人阿豹",43,"男","","","",false); d.personality="独来独往，林子里的声音比话有用"; d.background="管狩猎区15年，曾追捕过逃犯";d.hobbies="追踪大型猎物、冬天烤一整天肉";d.residence="森林小屋";d.birthMonth=3;d.birthDay=18; Add("abao",d); }
    static void AddMaoDaniang(){ var d=Base("mao_daniang","卯大娘",67,"女","","","",false); d.personality="退休后更爱说话，最享受看卯师傅忙碌的背影"; d.background="年轻时机是大陆最有名女厨之一，50岁回镇开饭馆收养卯师傅";d.hobbies="种有机菜、研制卯家酱油";d.residence="隐居山中";d.birthMonth=1;d.birthDay=12; Add("mao_daniang",d); }

    // ==================== 少年组（6） ====================
    static void AddXiaoshi(){ var d=Base("xiaoshi","小石",10,"男","","","",false); d.personality="勇敢好动，不服输"; d.background="阿岚之弟，梦想成为驯龙师";d.residence="旧驯龙师小屋";d.birthMonth=2;d.birthDay=1; Add("xiaoshi",d); }
    static void AddHanbai(){ var d=Base("hanbai","寒柏",8,"男","","","",false); d.personality="挑食第一名，崇拜医生哥哥，把旧听诊器挂脖子上"; Add("hanbai",d); }
    static void AddXiaotangKid(){ var d=Base("xiaotang","小糖",9,"女","","","",false); d.personality="脸蛋永远沾着糖霜，每天放学第一个冲到面包房"; Add("xiaotang",d); }
    static void AddAdan(){ var d=Base("adan","阿蛋",9,"男","","","",false); d.personality="调皮捣蛋，每天想新恶作剧(成功率30%)"; Add("adan",d); }
    static void AddDoudou(){ var d=Base("doudou","豆豆",7,"男","","","",false); d.personality="害羞内向，阿蛋的跟班但在门口看热闹不参与"; Add("doudou",d); }
    static void AddXiaoya(){ var d=Base("xiaoya","小丫",10,"女","","","",false); d.personality="温柔善良，镇上流浪猫狗的保护者，跟动物聊天能聊一天"; Add("xiaoya",d); }

    // ==================== 老年组（5） ====================
    static void AddDayeye(){ var d=Base("dayeye","大爷爷",110,"男","","","",false); d.personality="半神化长者，很少说话必说格言，110岁一口气爬上钟楼";d.residence="钟楼";d.birthMonth=3;d.birthDay=1; Add("dayeye",d); }
    static void AddAma(){ var d=Base("ama","阿嬷",72,"女","","","",false); d.personality="笑眯眯，\"再来一碗\"，从来不拒绝讨饭的人";d.residence="镇口小屋"; Add("ama",d); }
    static void AddGeShu(){ var d=Base("ge_shu","老兵葛叔",68,"男","","","",false); d.personality="满肚子故事(含大量杜撰)，赤霄最忠实听众";d.residence="镇边小屋";d.birthMonth=4;d.birthDay=28; Add("ge_shu",d); }
    static void AddLaoxuezhe(){ var d=Base("laoxuezhe","老学者",70,"男","","","",false); d.personality="退休后有强烈的求知欲，每天给暮晴留一封读书信";d.residence="图书馆隔壁";d.birthMonth=4;d.birthDay=2; Add("laoxuezhe",d); }
}
