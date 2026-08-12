using UnityEngine;

/// <summary>
/// 畜禽种类
/// 对应 GDD 5.2.1：鸡/鸭/鹅/猪/牛/羊/山羊/家兔
/// </summary>
public enum AnimalType
{
    Chicken,   // 鸡：鸡蛋/羽毛/生鸡肉 → 腌蛋/熏鸡
    Duck,      // 鸭：鸭蛋/鸭毛/生鸭肉 → 咸鸭蛋/板鸭
    Goose,     // 鹅：鹅蛋/鹅毛/鹅肉 → 咸鹅蛋/风鹅
    Pig,       // 猪：松露/生猪肉 → 腊肉/火腿
    Cow,       // 牛：牛乳/牛皮/生牛肉 → 牛肉干
    Sheep,     // 羊：羊毛/生羊肉 → 羊肉干/毛线
    Goat,      // 山羊：山羊乳/山羊绒/山羊肉 → 山羊绒衫
    Rabbit     // 家兔：兔毛/兔肉 → 兔肉松/兔毛纱
}

/// <summary>
/// 畜禽数据 — Unity 中创建为 ScriptableObject
/// 用法：Assets → Create → Livestock → Animal Data
/// </summary>
[CreateAssetMenu(menuName = "Livestock/Animal Data", fileName = "NewAnimal")]
public class AnimalData : ScriptableObject
{
    [Header("基本信息")]
    public string animalId;
    public string animalName;
    [TextArea(1, 3)] public string description;
    public AnimalType type;
    public Sprite icon;

    [Header("产出")]
    [Tooltip("主产出物品ID（鸡蛋/牛乳/羊毛等）")]
    public string primaryProduct = "";
    [Tooltip("副产出物品ID（生肉/皮毛等，仅在出售/屠宰时获得）")]
    public string secondaryProduct = "";
    [Tooltip("主产出间隔天数")]
    public int productionInterval = 1;

    [Header("心情")]
    public int moodMax = 100;
    [Tooltip("每日心情衰减（不喂食/不抚摸时）")]
    public int dailyMoodDecay = 5;

    [Header("经济")]
    public int purchasePrice = 500;
    public int feedCost = 5;        // 每日饲料费用（文）

    [Header("放牧")]
    public bool canGraze = true;
    [Tooltip("放牧时心情加成")]
    public int grazingMoodBonus = 10;

    [Header("特殊")]
    [Tooltip("猪在秋冬野外放牧时 1% 概率找到松露")]
    public bool canFindTruffle = false;

    [Header("外观")]
    public Sprite idleSprite;
    public Sprite walkSprite1;
    public Sprite walkSprite2;
}
