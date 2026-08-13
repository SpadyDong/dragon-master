using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 存档管理器
/// 5 手动槽位 + 1 自动槽位，JSON 格式
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const int MANUAL_SLOTS = 5;
    private const float AUTO_SAVE_INTERVAL = 600f; // 10 分钟
    private const string SAVE_DIR = "Saves";
    private float _autoSaveTimer;

    [System.Serializable]
    public class SaveData
    {
        public string saveName;
        public string saveDate;
        // 时间
        public int year, season, day, hour, minute;
        public string weather;
        public int weatherIndex;
        // 经济
        public int gold;
        // 场景
        public string sceneName;
        public float playerPosX, playerPosY;
        // 玩家属性
        public int currentHP, currentStamina, currentHunger, currentMP;
        public int strength, agility, vitality, intelligence;
        public int playerElement; // -1=null, 0-6=DragonElement
        public int playerGender;  // 0=Male, 1=Female
        // 背包
        public InventoryManager.InventorySaveData inventory;
        // 装备
        public EquipmentManager.EquipmentSaveData equipment;
        // 任务
        public QuestSaveData quests;
        // 种植
        public List<FarmTile.FarmTileSaveData> farmTiles;
        public List<FruitTree.FruitTreeSaveData> fruitTrees;
        public List<Beehive.BeehiveSaveData> beehives;
        // 畜牧
        public List<AnimalController.AnimalSaveData> animals;
        // 储物箱
        public List<StorageChest.StorageSaveData> storageChests;
        // 驯龙
        public List<DragonController.DragonSaveData> dragons;
        // 钓鱼
        public FishingManager.FishingSaveData fishing;
        public List<FishPond.FishPondSaveData> fishPonds;
        // 地形
        public List<TerrainTile.TerrainSaveData> terrainTiles;
        // 熟练度 — M10
        public ProficiencyManager.ProficiencySaveData proficiency;
        // 烹饪 — M10
        public CookingManager.CookingSaveData cooking;
        // 矿洞 — M10
        public MiningManager.MiningSaveData mining;
        // 成就 — M10
        public AchievementManager.AchievementSaveData achievements;
        // 菜市场 — M10.5
        public MarketManager.MarketSaveData market;
        // 工匠设备 — M10.5
        public ArtisanManager.ArtisanSaveData artisan;
        // 玩家→NPC 好感度 — M6
        public PlayerAffectionManager.PlayerAffectionSaveData playerAffection;
        // 结婚 — M6
        public MarriageManager.MarriageSaveData marriage;
        // 节日 — M10
        public FestivalManager.FestivalSaveData festival;
        // 社区中心 — M10
        public CommunityCenterManager.CommunityCenterSaveData communityCenter;
        // 博物馆 — M10
        public MuseumManager.MuseumSaveData museum;
        // 传送 — M10
        public TransportManager.TransportSaveData transport;
        // 秘密纸条 — M10
        public SecretNoteManager.SecretNoteSaveData secretNotes;
        // 主线剧情 — M11
        public StoryManager.StorySaveData story;
        // 版本标记
        public string version = "0.12.0";
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, SAVE_DIR));
    }

    void Update()
    {
        _autoSaveTimer += Time.unscaledDeltaTime;
        if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            _autoSaveTimer = 0f;
            AutoSave();
        }
    }

    /// <summary>保存到指定槽位</summary>
    public void SaveToSlot(int slot)
    {
        if (slot < 0 || slot >= MANUAL_SLOTS) return;

        SaveData data = BuildSaveData();
        data.saveName = $"存档 {slot + 1}";
        data.saveDate = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        string path = GetSlotPath(slot);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log($"已保存到槽位 {slot + 1}: {path}");
    }

    /// <summary>从指定槽位加载</summary>
    public void LoadFromSlot(int slot)
    {
        if (slot < 0 || slot >= MANUAL_SLOTS) return;

        string path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"槽位 {slot + 1} 无存档");
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        ApplySaveData(data);

        Debug.Log($"已加载槽位 {slot + 1}");
    }

    /// <summary>自动存档</summary>
    public void AutoSave()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        SaveData data = BuildSaveData();
        data.saveName = "自动存档";
        data.saveDate = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        string path = GetAutoSavePath();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    /// <summary>获取所有存档信息</summary>
    public List<SaveSlotInfo> ListSaves()
    {
        List<SaveSlotInfo> list = new();
        for (int i = 0; i < MANUAL_SLOTS; i++)
        {
            string path = GetSlotPath(i);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                list.Add(new SaveSlotInfo { slot = i, saveName = data.saveName, saveDate = data.saveDate });
            }
        }
        return list;
    }

    private SaveData BuildSaveData()
    {
        GameManager gm = GameManager.Instance;
        PlayerController pc = PlayerController.Instance;
        PlayerStats ps = PlayerStats.Instance;

        SaveData data = new()
        {
            year = gm.year, season = gm.season, day = gm.day,
            hour = gm.hour, minute = gm.minute,
            weather = gm.weather, weatherIndex = gm.weatherIndex,
            gold = gm.gold,
        };

        if (pc != null)
        {
            data.playerPosX = pc.transform.position.x;
            data.playerPosY = pc.transform.position.y;
        }

        if (ps != null)
        {
            data.currentHP = ps.CurrentHP;
            data.currentStamina = ps.CurrentStamina;
            data.currentHunger = ps.CurrentHunger;
            data.currentMP = ps.CurrentMP;
            data.strength = ps.Strength;
            data.agility = ps.Agility;
            data.vitality = ps.Vitality;
            data.intelligence = ps.Intelligence;
            data.playerElement = ps.PlayerElement.HasValue ? (int)ps.PlayerElement.Value : -1;
        }

        data.playerGender = (int)gm.playerGender;

        data.inventory = InventoryManager.Instance?.GetSaveData();
        data.equipment = EquipmentManager.Instance?.GetSaveData();
        data.quests = QuestManager.Instance?.GetSaveData();

        // 种植数据
        if (FarmingManager.Instance != null)
        {
            data.farmTiles = FarmingManager.Instance.GetAllTiles()
                .ConvertAll(t => t.GetSaveData());
            data.fruitTrees = new List<FruitTree.FruitTreeSaveData>();
            data.beehives = new List<Beehive.BeehiveSaveData>();
        }

        // 畜牧数据
        if (LivestockManager.Instance != null)
            data.animals = LivestockManager.Instance.GetAllSaveData();

        // 驯龙数据
        if (DragonManager.Instance != null)
            data.dragons = DragonManager.Instance.GetAllSaveData();

        // 钓鱼数据
        if (FishingManager.Instance != null)
        {
            data.fishing = FishingManager.Instance.GetSaveData();
            data.fishPonds = new List<FishPond.FishPondSaveData>();
            foreach (var pond in FindObjectsByType<FishPond>(FindObjectsSortMode.None))
                data.fishPonds.Add(pond.GetSaveData());
        }

        // 地形数据
        if (TerrainManager.Instance != null)
            data.terrainTiles = TerrainManager.Instance.GetAllSaveData();

        // M10 数据
        if (ProficiencyManager.Instance != null)
            data.proficiency = ProficiencyManager.Instance.GetSaveData();
        if (CookingManager.Instance != null)
            data.cooking = CookingManager.Instance.GetSaveData();
        if (MiningManager.Instance != null)
            data.mining = MiningManager.Instance.GetSaveData();
        if (AchievementManager.Instance != null)
            data.achievements = AchievementManager.Instance.GetSaveData();

        // M10.5 数据
        if (MarketManager.Instance != null)
            data.market = MarketManager.Instance.GetSaveData();
        if (ArtisanManager.Instance != null)
            data.artisan = ArtisanManager.Instance.GetSaveData();

        // M6 玩家好感度数据
        if (PlayerAffectionManager.Instance != null)
            data.playerAffection = PlayerAffectionManager.Instance.GetSaveData();

        // M6 结婚数据
        if (MarriageManager.Instance != null)
            data.marriage = MarriageManager.Instance.GetSaveData();

        // M10 剩余数据
        if (FestivalManager.Instance != null)
            data.festival = FestivalManager.Instance.GetSaveData();
        if (CommunityCenterManager.Instance != null)
            data.communityCenter = CommunityCenterManager.Instance.GetSaveData();
        if (MuseumManager.Instance != null)
            data.museum = MuseumManager.Instance.GetSaveData();
        if (TransportManager.Instance != null)
            data.transport = TransportManager.Instance.GetSaveData();
        if (SecretNoteManager.Instance != null)
            data.secretNotes = SecretNoteManager.Instance.GetSaveData();

        // M11 主线数据
        if (StoryManager.Instance != null)
            data.story = StoryManager.Instance.GetSaveData();

        return data;
    }

    private void ApplySaveData(SaveData data)
    {
        GameManager gm = GameManager.Instance;
        gm.year = data.year;
        gm.season = data.season;
        gm.day = data.day;
        gm.hour = data.hour;
        gm.minute = data.minute;
        gm.SetWeather(data.weatherIndex);
        gm.AddGold(data.gold - gm.gold);

        PlayerController pc = PlayerController.Instance;
        if (pc != null)
            pc.Teleport(new Vector3(data.playerPosX, data.playerPosY, 0));

        InventoryManager.Instance?.LoadSaveData(data.inventory);
        EquipmentManager.Instance?.LoadSaveData(data.equipment);
        QuestManager.Instance?.LoadSaveData(data.quests);

        // 恢复玩家主元素（兼容旧存档：<0.8.0 无此字段）
        if (PlayerStats.Instance != null && IsVersionAtLeast(data.version, "0.8.0") && data.playerElement >= 0)
        {
            PlayerStats.Instance.SetPlayerElement((DragonElement)data.playerElement);
        }

        // 恢复玩家性别
        if (data.playerGender >= 0)
            gm.SetPlayerGender((PlayerGender)data.playerGender);

        // 恢复种植数据
        if (data.farmTiles != null && FarmingManager.Instance != null)
        {
            var tiles = FarmingManager.Instance.GetAllTiles();
            for (int i = 0; i < Mathf.Min(tiles.Count, data.farmTiles.Count); i++)
                tiles[i].LoadSaveData(data.farmTiles[i]);
        }

        // 恢复畜牧数据
        if (data.animals != null && LivestockManager.Instance != null)
            LivestockManager.Instance.LoadAllSaveData(data.animals);

        // 恢复驯龙数据
        if (data.dragons != null && DragonManager.Instance != null)
            DragonManager.Instance.LoadAllSaveData(data.dragons);

        // 恢复钓鱼数据
        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.LoadSaveData(data.fishing);

            if (data.fishPonds != null)
            {
                var ponds = FindObjectsByType<FishPond>(FindObjectsSortMode.None);
                for (int i = 0; i < Mathf.Min(ponds.Length, data.fishPonds.Count); i++)
                    ponds[i].LoadSaveData(data.fishPonds[i]);
            }
        }

        // 恢复地形数据
        if (data.terrainTiles != null && TerrainManager.Instance != null)
            TerrainManager.Instance.LoadAllSaveData(data.terrainTiles);

        // 恢复 M10 数据
        if (data.proficiency != null && ProficiencyManager.Instance != null)
            ProficiencyManager.Instance.LoadSaveData(data.proficiency);
        if (data.cooking != null && CookingManager.Instance != null)
            CookingManager.Instance.LoadSaveData(data.cooking);
        if (data.mining != null && MiningManager.Instance != null)
            MiningManager.Instance.LoadSaveData(data.mining);
        if (data.achievements != null && AchievementManager.Instance != null)
            AchievementManager.Instance.LoadSaveData(data.achievements);

        // 恢复 M10.5 数据
        if (data.market != null && MarketManager.Instance != null)
            MarketManager.Instance.LoadSaveData(data.market);
        if (data.artisan != null && ArtisanManager.Instance != null)
            ArtisanManager.Instance.LoadSaveData(data.artisan);

        // 恢复 M6 玩家好感度数据
        if (data.playerAffection != null && PlayerAffectionManager.Instance != null)
            PlayerAffectionManager.Instance.LoadSaveData(data.playerAffection);

        // 恢复 M6 结婚数据
        if (data.marriage != null && MarriageManager.Instance != null)
            MarriageManager.Instance.LoadSaveData(data.marriage);

        // 恢复 M10 剩余数据
        if (data.festival != null && FestivalManager.Instance != null)
            FestivalManager.Instance.LoadSaveData(data.festival);
        if (data.communityCenter != null && CommunityCenterManager.Instance != null)
            CommunityCenterManager.Instance.LoadSaveData(data.communityCenter);
        if (data.museum != null && MuseumManager.Instance != null)
            MuseumManager.Instance.LoadSaveData(data.museum);
        if (data.transport != null && TransportManager.Instance != null)
            TransportManager.Instance.LoadSaveData(data.transport);
        if (data.secretNotes != null && SecretNoteManager.Instance != null)
            SecretNoteManager.Instance.LoadSaveData(data.secretNotes);

        // 恢复 M11 主线数据
        if (data.story != null && StoryManager.Instance != null)
            StoryManager.Instance.LoadSaveData(data.story);
    }

    /// <summary>版本号比较（数值逐段比较，兼容 x.y.z 格式）</summary>
    private static bool IsVersionAtLeast(string current, string threshold)
    {
        if (string.IsNullOrEmpty(current)) return false;
        if (System.Version.TryParse(current, out var cv) && System.Version.TryParse(threshold, out var tv))
            return cv >= tv;
        // 解析失败时回退字典序（尽力而为）
        return string.CompareOrdinal(current, threshold) >= 0;
    }

    private string GetSlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, SAVE_DIR, $"save_{slot}.json");
    private string GetAutoSavePath() =>
        Path.Combine(Application.persistentDataPath, SAVE_DIR, "autosave.json");

    [System.Serializable]
    public struct SaveSlotInfo
    {
        public int slot;
        public string saveName;
        public string saveDate;
    }
}
