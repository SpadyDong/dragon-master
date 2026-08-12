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
        // 版本标记
        public string version = "0.2.0";
    }

    void Awake()
    {
        Instance = this;
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
        }

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
