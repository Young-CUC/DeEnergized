using UnityEngine;
/// <summary>
/// 玩家物品栏 —— 管理备用电池的持有数量。
/// 单一职责：仅追踪持有物数量。UI 由独立脚本管理。
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Battery")]
    [Tooltip("当前持有的备用电池数量。")]
    [SerializeField] private int spareBatteryCount = 0;

    // ── 事件 ──────────────────────────────

    public event System.Action<int> OnCountChanged;

    // ── 公开属性 ──────────────────────────

    public int SpareBatteryCount => spareBatteryCount;
    public bool HasSpareBattery => spareBatteryCount > 0;

    // ── 公开方法 ──────────────────────────

    /// <summary>添加一枚备用电池。</summary>
    public void AddBattery()
    {
        spareBatteryCount++;
        OnCountChanged?.Invoke(spareBatteryCount);
    }

    /// <summary>尝试消耗一枚备用电池。成功返回 true。</summary>
    public bool TryUseBattery()
    {
        if (spareBatteryCount <= 0) return false;
        spareBatteryCount--;
        OnCountChanged?.Invoke(spareBatteryCount);
        return true;
    }
}
