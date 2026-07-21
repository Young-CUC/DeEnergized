using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 玩家核心电池 —— 管理电量、持续消耗注册与瞬间消耗。
/// 遵循单一职责：只负责电量计算与事件通知，不关心谁在耗电。
/// </summary>
public class PlayerBattery : MonoBehaviour
{
    [Header("Battery Settings")]
    [SerializeField] private float maxCharge = 100f;
    [Tooltip("电量低于等于此值时触发省电模式，手电筒强制关闭且无法开启。")]
    [SerializeField] private float lowPowerThreshold = 10f;

    [Header("Runtime (read-only)")]
    [SerializeField] private float currentCharge;

    // 持续耗电源注册表
    private readonly Dictionary<string, float> drainSources = new Dictionary<string, float>();

    // 事件
    public event Action<float, float> OnChargeChanged;
    public event Action OnLowPower;
    public event Action OnChargeRestored;

    // 状态追踪
    private bool wasLowPower;

    // ── 公开属性 ────────────────────────────────

    public float CurrentCharge => currentCharge;
    public float MaxCharge => maxCharge;
    public float ChargePercent => maxCharge > 0f ? currentCharge / maxCharge : 0f;
    public bool IsLowPower => currentCharge <= lowPowerThreshold;
    public float LowPowerThreshold => lowPowerThreshold;

    // ── 生命周期 ────────────────────────────────

    private void Awake()
    {
        currentCharge = maxCharge;
        wasLowPower = IsLowPower;
        Debug.Log($"[PlayerBattery] 初始化完成: {currentCharge}/{maxCharge}, 省电阈值={lowPowerThreshold}");
    }

    private void Start()
    {
        // Start 在所有 Awake/OnEnable 之后执行，此时所有订阅者已就位
        // 推送初始电量，确保 UI 等订阅者正确显示
        OnChargeChanged?.Invoke(currentCharge, maxCharge);
    }

    private void Update()
    {
        // 没有活跃耗电源则不处理
        if (drainSources.Count == 0)
            return;

        // 汇总持续消耗
        float totalDrain = 0f;
        foreach (float rate in drainSources.Values)
            totalDrain += rate;

        if (totalDrain <= 0f)
            return;

        float previousCharge = currentCharge;
        currentCharge = Mathf.Max(0f, currentCharge - totalDrain * Time.deltaTime);

        if (Mathf.Approximately(currentCharge, previousCharge))
            return;

        OnChargeChanged?.Invoke(currentCharge, maxCharge);

        // 省电模式检测
        bool isLowNow = IsLowPower;
        if (isLowNow && !wasLowPower)
        {
            wasLowPower = true;
            OnLowPower?.Invoke();
        }
    }

    // ── 公开方法 ────────────────────────────────

    /// <summary>
    /// 注册一个持续耗电源。id 重复时覆盖速率。
    /// </summary>
    public void AddDrain(string sourceId, float ratePerSecond)
    {
        if (string.IsNullOrEmpty(sourceId))
        {
            Debug.LogWarning("PlayerBattery.AddDrain: sourceId 不能为空。");
            return;
        }
        drainSources[sourceId] = ratePerSecond;
    }

    /// <summary>
    /// 注销一个持续耗电源。
    /// </summary>
    public void RemoveDrain(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId))
            return;
        drainSources.Remove(sourceId);

        // 电量恢复检测（放在这里而不是 Update 中避免每帧轮询）
        if (wasLowPower && currentCharge > lowPowerThreshold)
        {
            wasLowPower = false;
            OnChargeRestored?.Invoke();
        }
    }

    /// <summary>
    /// 瞬间消耗指定电量。成功返回 true，不足返回 false。
    /// </summary>
    public bool Consume(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentCharge < amount)
            return false;

        currentCharge -= amount;
        OnChargeChanged?.Invoke(currentCharge, maxCharge);

        // 检测省电模式
        if (currentCharge <= lowPowerThreshold && !wasLowPower)
        {
            wasLowPower = true;
            OnLowPower?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// 是否有足够电量支付指定消耗。
    /// </summary>
    public bool CanAfford(float amount)
    {
        return currentCharge >= amount;
    }
}
