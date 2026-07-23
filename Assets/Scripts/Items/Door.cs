using UnityEngine;

/// <summary>
/// 供电门 —— 由 BatterySlot 供电控制开/关。
///
/// 触发逻辑：BatterySlot.OnPowerChanged UnityEvent → OnPowerChanged() → SetOpen()
/// 运动逻辑：继承自 DoorBase（Lerp 动画 + Collider 切换）。
///
/// 依赖：BatterySlot（OnPowerChanged UnityEvent）。
/// </summary>
public class Door : DoorBase
{
    [Header("Power Source")]
    [Tooltip("控制此门的电池插槽。")]
    [SerializeField] private BatterySlot slot;

    // ── 生命周期 ──────────────────────────

    private void Start()
    {
        if (slot != null)
            slot.OnPowerChanged.AddListener(OnPowerChanged);
    }

    private void OnDestroy()
    {
        if (slot != null)
            slot.OnPowerChanged.RemoveListener(OnPowerChanged);
    }

    // ── 电源回调 ──────────────────────────

    /// <summary>
    /// 电池插槽通电/断电时由 UnityEvent 调用。
    /// 通电 → 开门，断电 → 关门。
    /// </summary>
    public void OnPowerChanged(bool powered)
    {
        SetOpen(powered);
    }
}
