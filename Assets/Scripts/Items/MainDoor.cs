using UnityEngine;

/// <summary>
/// 脚本门 —— 由外部脚本调用 Open()/Close() 控制开/关。
///
/// 触发逻辑：DistributionPanel 等编排器调用 Open()/Close()
/// 运动逻辑：继承自 DoorBase（Lerp 动画 + Collider 切换）。
///
/// 不依赖 BatterySlot。
/// </summary>
public class MainDoor : DoorBase
{
    /// <summary>开启大门（禁用碰撞，允许通过）。</summary>
    public void Open()
    {
        SetOpen(true);
    }

    /// <summary>关闭大门（启用碰撞，阻挡通过）。</summary>
    public void Close()
    {
        SetOpen(false);
    }
}
