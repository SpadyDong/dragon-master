/// <summary>
/// 游戏全局状态枚举
/// </summary>
public enum GameState
{
    Playing,    // 正常游戏运行
    Paused,     // Esc 暂停菜单
    Dialogue,   // 对话中
    Menu,       // 菜单界面（背包/装备/设置等）
    Transition, // 场景/地图过渡
    Combat      // 回合制战斗中
}
