using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 场景切换管理器
/// 处理多场景加载、淡入淡出转场、出生点定位
/// 类似星露谷：进入新区域 = 加载新场景 + 玩家出现在对应入口
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("转场动画")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("场景配置")]
    [SerializeField] private SceneConfig[] sceneConfigs;

    /// <summary>当前所在场景名</summary>
    public string CurrentScene { get; private set; } = "Town";

    /// <summary>是否正在切换场景</summary>
    public bool IsTransitioning { get; private set; }

    private string _returnPortalName;
    private string _returnScene;
    private string _returnSpawnId;
    private Dictionary<string, SceneConfig> _configMap = new();

    [System.Serializable]
    public class SceneConfig
    {
        public string sceneName;
        public string displayName;           // "龙脊镇" / "雪山" / "密林"
        public bool isPeaceZone;             // 是否安全区
        public SpawnPoint[] spawnPoints;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public string id;                    // 出生点标识（"default"/"from_village"/"from_forest"等）
        public Vector2 position;             // 世界坐标
        public string facingDirection;       // 面向方向 "down"/"up"/"left"/"right"
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 构建配置索引
        if (sceneConfigs != null)
        {
            foreach (var cfg in sceneConfigs)
                _configMap[cfg.sceneName] = cfg;
        }
    }

    void Start()
    {
        // 初始化当前场景
        CurrentScene = SceneManager.GetActiveScene().name;
        EventBus.Subscribe<string>(GameEvent.PlayerInteracted, OnPortalTriggered);

        // 首次加载时定位出生点
        StartCoroutine(WaitAndSpawn("default"));
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<string>(GameEvent.PlayerInteracted, OnPortalTriggered);
    }

    private void OnPortalTriggered(string evt)
    {
        // Portal 交互在 ScenePortal 中处理
    }

    /// <summary>设置返回传送门信息（从A场景到B场景时记录，方便回程）</summary>
    public void SetReturnPortal(string portalName, string fromScene, string toSpawnId)
    {
        _returnPortalName = portalName;
        _returnScene = fromScene;
        _returnSpawnId = toSpawnId;
    }

    /// <summary>切换到目标场景</summary>
    public void TransitionToScene(string sceneName, string spawnPointId)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, spawnPointId));
    }

    private IEnumerator TransitionRoutine(string sceneName, string spawnPointId)
    {
        IsTransitioning = true;

        // 1. 锁定玩家
        GameManager.Instance.SetState(GameState.Transition);

        // 2. 淡出
        yield return StartCoroutine(FadeOut());

        // 3. 加载场景
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        CurrentScene = sceneName;

        // 4. 定位玩家到出生点
        yield return StartCoroutine(WaitAndSpawn(spawnPointId));

        // 5. 淡入
        yield return StartCoroutine(FadeIn());

        // 6. 解锁
        GameManager.Instance.SetState(GameState.Playing);
        IsTransitioning = false;
    }

    private IEnumerator WaitAndSpawn(string spawnPointId)
    {
        yield return null; // 等待一帧让新场景初始化

        PlayerController player = PlayerController.Instance;
        if (player == null) yield break;

        // 查找出生点
        if (_configMap.TryGetValue(CurrentScene, out SceneConfig config))
        {
            foreach (var sp in config.spawnPoints)
            {
                if (sp.id == spawnPointId)
                {
                    player.Teleport(new Vector3(sp.position.x, sp.position.y, 0));

                    // 设置朝向
                    if (sp.facingDirection == "up") player.SetFacingDirection(1);
                    else if (sp.facingDirection == "left") player.SetFacingDirection(2);
                    else if (sp.facingDirection == "right") player.SetFacingDirection(3);
                    else player.SetFacingDirection(0); // default: down

                    // 相机立即跟随
                    CameraFollow cf = Camera.main?.GetComponent<CameraFollow>();
                    if (cf != null) cf.SnapToTarget();

                    return;
                }
            }
        }

        // 没有找到出生点 → 停在原地或 (0,0)
        Debug.LogWarning($"场景 [{CurrentScene}] 未找到出生点 [{spawnPointId}]");
    }

    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null) yield break;
        fadeOverlay.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, a);
            yield return null;
        }
        fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
    }

    private IEnumerator FadeIn()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, a);
            yield return null;
        }
        fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>获取当前场景配置</summary>
    public SceneConfig GetCurrentSceneConfig()
    {
        return _configMap.TryGetValue(CurrentScene, out var cfg) ? cfg : null;
    }

    /// <summary>当前是否为安全区</summary>
    public bool IsCurrentScenePeaceZone()
    {
        if (_configMap.TryGetValue(CurrentScene, out var cfg))
            return cfg.isPeaceZone;
        return true;
    }
}
