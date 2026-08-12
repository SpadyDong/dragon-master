using System.Collections.Generic;

/// <summary>
/// 41 个 NPC 支线任务数据库
/// 每个 NPC 至少 1 个支线任务，完成后解锁不影响主线的奖励
/// </summary>
public static class NPCQuests
{
    public static List<QuestData> BuildAll()
    {
        var all = new List<QuestData>();

        // =================== 可结婚男 (7) ===================
        all.Add(new QuestData
        {
            questId="q_alan_dragon_train", questName="月下驯龙", npcId="alan", questType=QuestType.Side,
            description="阿岚说夜色下龙的反应速度最快，邀请你连续3个夜晚去训练场。",
            completionText="你爷爷也会在月光下训龙。这是他的龙鞍图纸——现在它是你的了。",
            requiredAffection=300, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="training_ground",requiredCount=3,description="夜晚在训练场训练 3 次"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="dragon_saddle_recipe",amount=1,description="龙鞍配方"}}, rewardGold=200, rewardAffection=50
        });

        all.Add(new QuestData
        {
            questId="q_panshi_mineral", questName="矿洞深处", npcId="panshi", questType=QuestType.Side,
            description="磐石说矿洞深处有他要找的「岩心矿」，但岔路太多一个人不安全。",
            completionText="找到了！这块岩心矿够打好几副龙鞍了。这个给你——矿工都知道的秘诀。",
            requiredAffection=300, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="mine_b3",requiredCount=1,description="陪磐石到矿洞第3层"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="miner_helmet",amount=1,description="矿工头盔(永久照明)"}}, rewardGold=300, rewardAffection=50
        });

        all.Add(new QuestData
        {
            questId="q_linmo_wood_carving", questName="木雕之心", npcId="linmo", questType=QuestType.Side,
            description="林墨需要 5 种不同木材来做一件特别的作品，但他不肯说是给谁做的。",
            completionText="用松木做底、檀木做框、橡木做脚、榉木做面、桦木做纹……这是给你的。放在家里吧。",
            requiredAffection=250, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="pine_wood",requiredCount=1,description="收集松木"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="oak_wood",requiredCount=1,description="收集橡木"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="birch_wood",requiredCount=1,description="收集桦木"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="maple_wood",requiredCount=1,description="收集枫木"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="cherry_wood",requiredCount=1,description="收集樱桃木"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="wood_carving_shelf",amount=1,description="手工木雕置物架"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_yuntao_legendary_fish", questName="大鱼传说", npcId="yuntao", questType=QuestType.Side,
            description="云涛说暴风雨天是钓传说鱼「龙鳞鲷」的唯一时机。他需要帮手拉网。",
            completionText="我钓了十年没钓上来——你一来就上钩了。这把竿子拿去吧，比起陪我钓鱼，它更衬你。",
            requiredAffection=350, requiredSeason=-1, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Fish,targetId="dragon_scale_bream",requiredCount=1,description="在暴风雨天钓上龙鳞鲷"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="golden_fishing_rod",amount=1,description="金色鱼竿(钓鱼难度-20%)"}}, rewardGold=500, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_qingye_bread_contest", questName="面包大赛", npcId="qingye", questType=QuestType.Side,
            description="青野要挑战卯师傅的面包大赛，但他缺三种顶级食材。这是他自拜师以来磨的最认真的仗。",
            completionText="卯师傅说：'这次你赢了。'然后她笑了——我从来没见她笑成那样。这个火焰面包配方是我的战利品——也是你的。",
            requiredAffection=400, prerequisiteQuests=new[]{"q_mao_shifu_secret_soy"}, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="truffle",requiredCount=1,description="收集松露"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="golden_wheat",requiredCount=3,description="收集金色小麦"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="volcano_salt",requiredCount=1,description="收集火山盐"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Recipe,rewardId="flame_bread_recipe",amount=1,description="火焰面包配方"}}, rewardGold=400, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_hanchuan_herb", questName="药到病除", npcId="hanchuan", questType=QuestType.Side,
            description="寒川收治了一位高烧不退的老人，需要雪山顶上的稀有药材「冰灵芝」。他一个人没法在暴风雪中上山。",
            completionText="药效起作用了。你知道为什么我在城里待不下去吗？因为在城里，没有人会在暴风雪中陪我上山。这个给你——泡水喝，毒虫不近身。",
            requiredAffection=300, requiredSeason=3, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="ice_lingzhi",requiredCount=1,description="在雪山采集冰灵芝"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Perk,rewardId="antidote_pendant",amount=1,description="避毒坠(免疫中毒)"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_chixiao_veteran", questName="老兵之证", npcId="chixiao", questType=QuestType.Side,
            description="赤霄收到退伍战友来信，约他回旧战场见面。他想带一个信得过的人一起去。",
            completionText="十年了——那面城墙还在。这枚勋章是我当年守城得来的，给你。不是我不要了——是我希望有人替我戴着它继续走下去。",
            requiredAffection=400, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="old_battlefield",requiredCount=1,description="陪赤霄去旧战场"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="veteran_medal",amount=1,description="老兵勋章(攻击+5)"}}, rewardGold=300, rewardAffection=60
        });

        // =================== 可结婚女 (9) ===================
        all.Add(new QuestData
        {
            questId="q_muqing_ancient_stele", questName="古碑之谜", npcId="muqing", questType=QuestType.Side,
            description="暮晴在图书馆地下室发现一块未破译的古碑，上面的灵纹与她左肩的胎记一模一样。",
            completionText="碑文翻译出来了——这是古代驯龙师留给后人的守护灵纹。它和我的胎记相同，不是巧合。这个护符是用碑文残片做的，送给你。",
            requiredAffection=350, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="spirit_powder",requiredCount=5,description="收集灵石粉末"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="ancient_coin",requiredCount=3,description="收集古币"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="spirit_amulet",amount=1,description="灵纹护符(灵力恢复+20%)"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_chengxi_night_bloom", questName="夜开花", npcId="chengxi", questType=QuestType.Side,
            description="澄溪说奶奶花婆曾培育过一种「只在满月夜绽放」的花，但种子失传了。她找到了最后的种子——需要你帮忙。",
            completionText="开了……真的开了。奶奶说这花叫'月下雪'，一年只开一朵。我把它做成喷泉了——放在花园里，晚上会发光。",
            requiredAffection=300, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="moon_water",requiredCount=7,description="连续7天夜晚浇月亮井水"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="garden_fountain",amount=1,description="花园喷泉装饰"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_xiaotang_tailor_contest", questName="裁缝大赛", npcId="xiaotang", questType=QuestType.Side,
            description="三年一度的裁缝大赛在邻镇举行，小棠需要三种稀有布料才能做出参赛作品。",
            completionText="第一名！他们说龙脊镇出了个天才裁缝——其实只是你帮我找到了最好的料子。这套衣服是用剩下的料做的，你的尺寸。",
            requiredAffection=250, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="silk_bolt",requiredCount=3,description="收集丝绸"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="spider_silk",requiredCount=5,description="收集蛛丝"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="rainbow_dye",requiredCount=1,description="收集彩虹染料"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="custom_outfit",amount=1,description="专属时装"}}, rewardGold=300, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_yeling_lost_reed", questName="写给远方的歌", npcId="yeling", questType=QuestType.Side,
            description="叶铃的手风琴坏了一个簧片，声音怎么也调不回来。那簧片是父亲留下的最后一件东西。",
            completionText="谢谢你。这个簧片不值钱，但它是我能唱歌的原因。我修好了手风琴，录了一首歌给你——以后在家想听的时候，放它就行。",
            requiredAffection=350, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="crystal_reed",requiredCount=1,description="在矿洞中找到水晶簧片"},new QuestObjective{type=QuestObjectiveType.Deliver,targetId="crystal_reed",requiredCount=1,description="交给叶铃"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="music_box",amount=1,description="音乐盒(可在家播放BGM)"}}, rewardAffection=70
        });

        all.Add(new QuestData
        {
            questId="q_qiuyue_ice_book", questName="冰晶书", npcId="qiuyue", questType=QuestType.Side,
            description="秋月的那本冰晶书——在某天夜晚突然发光了。只有一页出现了文字：「冰之碎片，隐于极北之巅。」",
            completionText="书里掉出来这个——冰晶碎片。不知道为什么，握着它的时候我不觉得冷了。也许这本书一直在等这一天——等一个愿意陪我走到这里的人。",
            requiredAffection=400, requiredSeason=3, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="north_peak",requiredCount=1,description="去雪山北峰寻找冰之碎片"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="ice_shard",amount=1,description="冰之碎片(附魔武器+冰属性)"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_tiemei_ultimate_forge", questName="终极锻造", npcId="tiemei", questType=QuestType.Side,
            description="铁梅说她这辈子想打一把「传说的武器」——但需要矿洞深处的火灵石和稀有金属。",
            completionText="给你。这把武器的名字叫'熔岩之心'——是我这辈子打的最好的东西。比我爹打的所有剑都强。拿着它的时候想着我。",
            requiredAffection=450, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="fire_spirit_stone",requiredCount=1,description="收集火灵石"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="mithril_ingot",requiredCount=3,description="收集秘银锭"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="dragon_scale",requiredCount=2,description="收集龙鳞"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="lava_heart_sword",amount=1,description="传说武器·熔岩之心"}}, rewardAffection=80
        });

        all.Add(new QuestData
        {
            questId="q_xuelai_lost_sweet", questName="失落的甜味", npcId="xuelai", questType=QuestType.Side,
            description="雪莱说她能尝出四种甜——糖是甜的、蜂蜜是甜的、花蜜是甜的、果实是甜的。但据说还有第五种甜：「石头里的甜」。",
            completionText="找到了！是石蜜——矿洞里的矿石上结的糖霜。你尝一下——甜不甜？这个蛋糕是用了五甜做的新品。所有吃到的人都会笑。我把配方给你。",
            requiredAffection=300, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="rock_sugar",requiredCount=3,description="在矿洞中找到石蜜(矿壁上的糖霜)"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Recipe,rewardId="happiness_cake_recipe",amount=1,description="幸福蛋糕配方"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_liuyan_ledger_page", questName="家传账本", npcId="liuyan", questType=QuestType.Side,
            description="柳烟的账本缺了一页——恰好是记录父母当年从矿场赎身的那笔账。她翻遍了整个龙之商店都没找到。",
            completionText="你找到了矿场旧档案室的记录……上面有父亲按的手印。原来他不是矿奴——他是自由人。这张卡给你，以后在我店里买东西，不打折——打心。",
            requiredAffection=350, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="mine_archive",requiredCount=1,description="去矿场旧档案室找那一页"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Perk,rewardId="discount_card",amount=1,description="永久折扣卡(商店9折)"}}, rewardAffection=60
        });

        all.Add(new QuestData
        {
            questId="q_mao_shifu_secret_soy", questName="秘制酱油", npcId="mao_shifu", questType=QuestType.Side,
            description="卯师傅说养母的酱油秘方里有一味「只有卯家人知道的料」。她只知道大概方向——在雪山的某个山洞里。",
            completionText="就是这个——花椒树皮发酵的酱。养母说'卯家的饭和别人家不一样，不在手艺，在这滴酱油。'现在云腾八珍锅的配方才算完整了。给你一份。",
            requiredAffection=400, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="fermented_pepper_bark",requiredCount=1,description="在雪山山洞找到花椒树皮酿酱"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Recipe,rewardId="eight_treasure_pot_recipe",amount=1,description="云腾八珍锅配方"}}, rewardAffection=60
        });

        // =================== 中年组 (14) ===================
        all.Add(new QuestData
        {
            questId="q_laocunzhang_history", questName="镇史补遗", npcId="laocunzhang", questType=QuestType.Side,
            description="老村长正在编纂龙脊镇的镇史，缺三样东西：二十年前的驯龙师合影、第一任村长的信、火山喷发的目击记录。",
            completionText="齐了！这本镇史会放在博物馆的——你爷爷的照片就在那一页。这个古钟挂饰是钟楼上的残件，你留着。",
            requiredDay=7, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="old_photo",requiredCount=1,description="找到旧合影"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="founder_letter",requiredCount=1,description="找到第一任村长信"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="eruption_record",requiredCount=1,description="找到火山记录"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="bell_ornament",amount=1,description="古钟挂饰"}}, rewardGold=200, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_chen_smith_furnace", questName="炉火不熄", npcId="chen_blacksmith", questType=QuestType.Side,
            description="陈铁匠的熔炉出问题了——修了三十年的老炉，缺关键部件。这炉子见证了阿岚父母的武器诞生，不能熄。",
            completionText="活了。这炉子比我还老——但它还不能死。这把锤子是当年我爹传给我的，现在我传给你。下次升级工具拿着它，敲铁少花两分力气。",
            requiredDay=10, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="fire_brick",requiredCount=5,description="收集耐火砖"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="copper_pipe",requiredCount=2,description="收集铜管"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Perk,rewardId="smith_hammer",amount=1,description="铁匠之锤(工具升级费-20%)"}}, rewardGold=150, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_shen_missing_goods", questName="失踪的货物", npcId="shen_daniang", questType=QuestType.Side,
            description="沈大娘的一批杂货被野怪叼走了——在密林小径附近。不是值钱的东西，但有几卷布料是小棠订的。",
            completionText="找到了！谢谢你——这些不值什么钱，但大娘的杂货铺不能失信于人。这个背包是她年轻时用剩的好皮子做的，多装几样东西没问题。",
            requiredDay=5, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Kill,targetId="wild_wolf",requiredCount=3,description="在密林小径击败叼货的野狼"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Perk,rewardId="backpack_expansion",amount=4,description="背包扩容 +4格"}}, rewardGold=100, rewardAffection=20
        });

        all.Add(new QuestData
        {
            questId="q_shen_dashu_wine", questName="老友的酒", npcId="shen_dashu", questType=QuestType.Side,
            description="沈大叔说华叔年轻时酿过一种酒，名叫'不语'。但华叔当年赌气把配方撕了。沈大叔想再喝一次。",
            completionText="华叔从柜子底层翻出了配方——他说'我以为再也没有人记得这酒了。'这坛给你。送礼的时候说这是不语——没人会拒绝。",
            requiredDay=15, objectives=new[]{new QuestObjective{type=QuestObjectiveType.TalkTo,targetId="hua_shu",requiredCount=1,description="跟华叔聊聊'不语'这款酒"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="aged_wine",amount=3,description="不语·陈年佳酿(送礼万能·+60好感)"}}, rewardAffection=25
        });

        all.Add(new QuestData
        {
            questId="q_zhou_final_work", questName="最后的作品", npcId="zhou_wood", questType=QuestType.Side,
            description="周木匠说自己老了，想做最后一件东西就退休。需要最顶级的木材。",
            completionText="这件东西叫'承'——底座是我打的，面是林墨磨的，现在放进你的屋子里。我们周家三代木匠，这件是最好的。",
            requiredDay=20, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="ancient_oak",requiredCount=1,description="在密林深处找到千年橡木"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="master_woodwork",amount=1,description="大师木雕·承"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_suyi_laundry_secret", questName="洗衣店的秘密", npcId="su_yi", questType=QuestType.Side,
            description="苏姨的洗衣店有个传闻——她的皂角配方能让衣服越洗越新。但她说不小心把配方扔进河里了。",
            completionText="捞上来了！这配方是我奶奶传给我的——用苍莽河里的水藻煮皂角。这个香囊给你，洗完澡挂上，一整天不沾灰。",
            requiredDay=8, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Fish,targetId="river_algae",requiredCount=3,description="在苍莽河钓起被冲走的皂角包裹"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="clean_sachet",amount=1,description="洁净香囊(永久·衣服不会脏)"}}, rewardAffection=25
        });

        all.Add(new QuestData
        {
            questId="q_huashu_silent_cocktail", questName="沉默的调酒师", npcId="hua_shu", questType=QuestType.Side,
            description="华叔说他调了三十年酒，还剩一杯没调出来的：给「一个不能喝酒的人」喝的酒。",
            completionText="这杯是用草莓、薄荷和月光泉水调的——没有一滴酒精，但有所有说不出口的话。这是VIP卡——以后来酒吧坐角落那个位子，它是你的。",
            requiredDay=12, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="strawberry",requiredCount=5,description="收集草莓"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="mint",requiredCount=3,description="收集薄荷"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="moon_spring_water",requiredCount=1,description="取月光泉水"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Unlock,rewardId="vip_seat",amount=1,description="酒吧VIP座位(每周五免费1杯)"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_mafu_horseshoe", questName="老马识途", npcId="mafu_laoli", questType=QuestType.Side,
            description="马夫老李的老马丢了一只马蹄铁——它在沙滩上跑的时候脱落的。老李说那只马蹄铁是祖父打的。",
            completionText="我的老天爷——你找到了！在马肚子里——不是，在沙子里埋着——总之谢谢！这块东西跟着老马走了一辈子，现在它跟着你走。",
            requiredDay=6, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="old_horseshoe",requiredCount=1,description="在沙滩上找到旧马蹄铁"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Perk,rewardId="lucky_horseshoe",amount=1,description="幸运马蹄铁(坐骑移动速度+15%)"}}, rewardGold=100, rewardAffection=20
        });

        all.Add(new QuestData
        {
            questId="q_akuang_lantern", questName="矿灯", npcId="akuang", questType=QuestType.Side,
            description="老矿工阿土的矿灯掉进矿洞深处了——不是值钱的东西，但那盏灯跟了他四十年。",
            completionText="小子——哦不，恩人。这灯是我进矿洞第一年我爹给的。能再亮起来真是太好了。这把探矿镐给你——敲矿石的时候，有时候会多蹦一颗出来。",
            requiredDay=6, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="mine_b2",requiredCount=1,description="去矿洞第2层找回阿土的矿灯"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="prospector_pick",amount=1,description="探矿镐(挖矿双倍概率+5%)"}}, rewardGold=100, rewardAffection=20
        });

        all.Add(new QuestData
        {
            questId="q_huapo_secret_garden", questName="秘密花园", npcId="huapo", questType=QuestType.Side,
            description="花婆说她年轻时在森林深处藏了一个秘密花园，种着这片大陆上最后一批稀有花种。她老了走不动了——希望有人能去照看。",
            completionText="你浇水了？好孩子。那花园里有三十七种花，最里面那棵——叫星辰兰。种子在桌上，拿回去种。花婆的传承不能断。",
            requiredDay=15, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="secret_garden",requiredCount=1,description="去森林深处找到秘密花园"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="water",requiredCount=5,description="浇5次水"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="star_orchid_seed",amount=1,description="星辰兰种子(全季节开花·稀有)"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_principal_graduation", questName="毕业典礼", npcId="principal", questType=QuestType.Side,
            description="校长要为六年级的四个孩子准备毕业礼物——全镇每人出一份，他要凑齐「镇上的四样特产」。",
            completionText="四个孩子的礼物齐了——铁匠家的锤子(铁梅打的)、花房的压花(澄溪做的)、面包房的姜饼(青野烤的)、龙鳞书签(你自己做的)。这个镇章代表着：你的毕业典礼，全校都记住了。",
            requiredDay=25, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="tiny_hammer",requiredCount=1,description="找铁梅做小锤子"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="pressed_flower",requiredCount=1,description="找澄溪做压花"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="gingerbread",requiredCount=1,description="找青野做姜饼"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="dragon_scale_bookmark",requiredCount=1,description="自制龙鳞书签"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="town_badge",amount=1,description="镇章(使用后全体NPC好感+15)"}}, rewardAffection=40
        });

        all.Add(new QuestData
        {
            questId="q_yupo_lantern", questName="等一个人", npcId="yupo", questType=QuestType.Side,
            description="渔婆每年这个时候都会往海里放一盏灯——给「没回来的人」。今年她手受伤了做不了灯。",
            completionText="放出去了。谢谢你……我知道他不会回来了——但灯应该还有人放。这根珊瑚手链是我年轻时他送我的。它们是一对的——现在给你一根。希望你不会用上它的另一半。",
            requiredDay=20, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="paper",requiredCount=3,description="收集纸"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="candle",requiredCount=1,description="收集蜡烛"},new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="port",requiredCount=1,description="去港口放灯"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="tide_charm",amount=1,description="潮汐护符(钓鱼成功率+10%)"}}, rewardAffection=35
        });

        all.Add(new QuestData
        {
            questId="q_abao_track", questName="追踪", npcId="abao", questType=QuestType.Side,
            description="猎人阿豹说密林里有一只罕见的白鹿——他追踪了两年没追上。他说自己的腿脚不如年轻时候了。",
            completionText="你追上了——我两年没追上，你七天就追上了。这只鹿叫'林间雪'，我不打算打它了——有些东西追上了才知道不该放倒。这件披风给你——穿上在密林里走路，野兽会少一半。",
            requiredDay=14, requiredSeason=-1, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="deep_forest",requiredCount=7,description="连续7天去密林深处追踪白鹿"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="hunter_cloak",amount=1,description="猎人披风(密林遇怪率-50%)"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_mao_daniang_soy_brew", questName="老酱油", npcId="mao_daniang", questType=QuestType.Side,
            description="卯大娘说她有一坛酱油要酿——需要整整一个秋天。这坛酱油叫'卯家老酱油'，是卯家菜的灵魂。",
            completionText="丫头说你来过了——她每天跑来搅一次。她说的时候眼睛里有光，就像我当年看她学切菜的时候。这坛好了，带回去吧——以后卯时饭馆的味道，在家里也能有了。",
            requiredDay=20, requiredSeason=2, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Wait,targetId="autumn",requiredCount=28,description="在秋季每天去搅一次酱油（共28天）"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="mao_soy_sauce",amount=1,description="卯家老酱油(顶级料理原料·品质+1档)"}}, rewardAffection=40
        });

        // =================== 少年组 (6) ===================
        all.Add(new QuestData
        {
            questId="q_xiaoshi_dragon", questName="小驯龙师", npcId="xiaoshi", questType=QuestType.Side,
            description="小石说他想看一次真正的龙——不是画片上的，是会呼吸会吼叫的龙。",
            completionText="好大——比我哥说的还大。它看了我一眼！我画下来了，虽然画得不好但这是我最满意的一张……送给你！",
            requiredDay=3, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="dragon_training_ground",requiredCount=1,description="带小石去驯龙训练场看一次龙"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="child_drawing",amount=1,description="小石的龙画作(墙壁装饰)"}}, rewardAffection=25
        });

        all.Add(new QuestData
        {
            questId="q_hanbai_medicine", questName="不爱吃药", npcId="hanbai", questType=QuestType.Side,
            description="寒柏感冒了不肯吃药——苦。寒川试了所有办法都没用。",
            completionText="甜的！你放的不是苦的——是甜的！哥哥说蜂蜜和药草混在一起就不苦了。这张卡片我画了一个星期——给你，这是谢礼。",
            requiredDay=5, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="honey",requiredCount=2,description="收集蜂蜜给寒川做甜药"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="thank_you_card",amount=1,description="寒柏画的感谢卡片(墙壁装饰)"}}, rewardAffection=20
        });

        all.Add(new QuestData
        {
            questId="q_xiaotang_candy", questName="糖果大冒险", npcId="xiaotang", questType=QuestType.Side,
            description="小糖说她把青野给她的五颗手工糖弄丢了——分别掉在广场、河边、花房、铁匠铺和图书馆。",
            completionText="全部找到了！你最好啦！青野哥说这五颗糖本来就是要给'帮小糖找糖的人'种的糖果树种子——给你！明年就能长糖了！",
            requiredDay=3, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="lost_candy",requiredCount=5,description="在镇上5个地点找到小糖丢的糖果"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="candy_tree_seed",amount=1,description="糖果树种子(种出能收获糖果的树)"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_adan_prank", questName="恶作剧之王", npcId="adan", questType=QuestType.Side,
            description="阿蛋说他策划了史上最伟大的恶作剧——需要你在村长讲话的时候在他背后放一个放屁垫。",
            completionText="哈哈哈哈你看到村长的脸没有！他说'这是谁干的——'然后没绷住自己笑了。你是我见过最好的恶作剧搭档——这个给你，下次一起用。",
            requiredDay=7, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="square",requiredCount=1,description="在村长讲话时放置恶作剧道具"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="prank_kit",amount=1,description="恶作剧道具包(可在任何NPC身上使用·不会降低好感)"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_doudou_spider", questName="勇敢的心", npcId="doudou", questType=QuestType.Side,
            description="豆豆怕蜘蛛怕到不敢去图书馆还书——因为图书馆后院有一只大蜘蛛。秋月说他再不来还书就要'温柔地训话'了。",
            completionText="我……我闭上眼睛走过去的！她没关我——她还说'书可以慢慢还，但勇敢不能慢慢练'。这个护身符你帮我保管——哪天我不怕了，你再还给我。",
            requiredDay=8, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Kill,targetId="giant_spider",requiredCount=1,description="清除图书馆后院的蜘蛛"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="courage_charm",amount=1,description="勇气护符(持有者无视小怪的恐惧效果)"}}, rewardAffection=25
        });

        all.Add(new QuestData
        {
            questId="q_xiaoya_cat_house", questName="流浪猫之家", npcId="xiaoya", questType=QuestType.Side,
            description="小丫说冬天要来了，她想给镇上三只流浪猫做个窝。但她手太小了，切不动木头。",
            completionText="做好了！咪咪、小灰和大橘——一人一栋！你看它们钻进去了——我就知道你会帮我。这个小猫屋是照你的样子做的——放在家里吧，说不定哪天会有猫来找你。",
            requiredDay=10, requiredSeason=3, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="wood_plank",requiredCount=5,description="收集木板"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="wool",requiredCount=3,description="收集羊毛"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Furniture,rewardId="cat_house",amount=1,description="猫猫小屋(家里会出现流浪猫来住)"}}, rewardAffection=30
        });

        // =================== 老年组 (4) ===================
        all.Add(new QuestData
        {
            questId="q_dayeye_bell", questName="古钟之音", npcId="dayeye", questType=QuestType.Side,
            description="大爷爷说古钟上的锈太厚了，已经敲不出当年那个音了。",
            completionText="你擦了一整天。现在你敲一下。听到那个回音了吗？那是龙脊镇一百一十年的呼吸。你是个好孩子——今天全镇都能听到它响。",
            requiredDay=14, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="polishing_cloth",requiredCount=3,description="收集抛光布"},new QuestObjective{type=QuestObjectiveType.Wait,targetId="bell",requiredCount=1,description="花一整天擦拭古钟"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="bell_blessing",amount=1,description="古钟余音(使用后1天内全体好感+5)"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_ama_home_taste", questName="家的味道", npcId="ama", questType=QuestType.Side,
            description="阿嬷说她想给镇上三位独居老人做一顿饭——但她一个人拿不了那么多食材。",
            completionText="他们都吃完了——老兵葛叔说这顿饭让他想起小时候。不是菜有多好……是有人坐下来陪他吃。这条围裙跟了我半辈子，现在给你——做饭的时候记得：最好的调料是时间。",
            requiredDay=8, objectives=new[]{new QuestObjective{type=QuestObjectiveType.Collect,targetId="rice",requiredCount=3,description="收集大米"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="egg",requiredCount=3,description="收集鸡蛋"},new QuestObjective{type=QuestObjectiveType.Collect,targetId="cabbage",requiredCount=2,description="收集白菜"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Equipment,rewardId="grandma_apron",amount=1,description="阿嬷的围裙(烹饪成功率+10%)"}}, rewardAffection=35
        });

        all.Add(new QuestData
        {
            questId="q_geshu_medal", questName="勋章", npcId="ge_shu", questType=QuestType.Side,
            description="老兵葛叔说他的军功章丢了——不是现在丢的，是二十年前虚空裂缝之战结束后就再没见过。他不确定是掉在旧战场了，还是压根没领。",
            completionText="是这个——我找了二十年。我以为他们忘了给我发。谢谢。这本书是我写的——里面有一些真的故事，有一些是老了之后编的。你分不清哪个是哪个——但我希望有人读。",
            requiredDay=20, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="old_battlefield",requiredCount=1,description="去旧战场档案室查军功章记录"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="war_story_book",amount=1,description="葛叔战争故事集"}}, rewardAffection=30
        });

        all.Add(new QuestData
        {
            questId="q_laoxuezhe_fossil", questName="未完成的研究", npcId="laoxuezhe", questType=QuestType.Side,
            description="老学者说他退休前最后一项研究没有完成——沙漠里有一块龙类先祖的化石，年代比苍莽大陆任何记录都要早。他走不动了。",
            completionText="你带回来了。这是始祖龙的翼骨化石——如果把它拼回大陆的考古时间线……整个龙族起源的历史要往前推两千年。这本图鉴是我研究了一辈子的成果，天还没亮，笔还不应该停。送给你。",
            requiredDay=25, objectives=new[]{new QuestObjective{type=QuestObjectiveType.ReachPlace,targetId="desert_dig_site",requiredCount=1,description="去沙漠挖掘现场带回化石"}},
            rewards=new[]{new QuestReward{type=QuestRewardType.Item,rewardId="fossil_collection",amount=1,description="古生物图鉴(博物馆可捐赠)"}}, rewardGold=500, rewardAffection=30
        });

        return all;
    }
}
