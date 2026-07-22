using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 电池插槽 —— 独立于被供电设备的中间层。
///
/// 状态：Empty（无电池）←→ Powered（有电池）。
/// 插入/拔出通过 IInteractable 接口触发。
/// 拔出时产生电弧强光事件（优先级 3），广播给敌人感知系统。
///
/// 依赖：无（不依赖任何被供电设备）。
/// 被依赖：DecoyLight, Door 等通过 OnPowerChanged 事件订阅。
/// </summary>
[RequireComponent(typeof(Collider))]
public class BatterySlot : MonoBehaviour, IInteractable
{
    [Header("State")]
    [Tooltip("当前是否已通电（有电池）。")]
    [SerializeField] private bool isPowered = false;

    // 提示文本统一由 InteractionTexts 管理

    [Header("Arc Flash (拔出电弧)")]
    [Tooltip("电弧闪光预制件 —— 挂有 LightStimulus (ArcFlash) + LightSpotMarker + SphereCollider (LightTrigger 层)。")]
    [SerializeField] private GameObject arcFlashPrefab;
    [Tooltip("电弧闪光的感知半径（世界单位）。")]
    [SerializeField] private float arcFlashRadius = 30f;
    [Tooltip("闪光持续时间（秒）。")]
    [SerializeField] private float arcFlashDuration = 0.5f;

    [Header("Events")]
    [Tooltip("通电状态变化时触发。")]
    public UnityEvent<bool> OnPowerChanged;

    // ── 公开属性 ──────────────────────────

    public bool IsPowered
    {
        get => isPowered;
        private set
        {
            if (isPowered == value) return;
            isPowered = value;
            OnPowerChanged?.Invoke(value);
        }
    }

    // ── IInteractable ──────────────────────

    public string GetPrompt()
    {
        if (IsPowered) return InteractionTexts.SlotRemove;
        var inv = FindAnyObjectByType<PlayerInventory>();
        return (inv != null && inv.HasSpareBattery)
            ? InteractionTexts.SlotInsert
            : InteractionTexts.SlotNeedBattery;
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        // 已通电 → 随时可拔出
        if (IsPowered) return true;
        // 未通电 → 需要有备用电池
        return inventory != null && inventory.HasSpareBattery;
    }

    public void OnInteract(PlayerInventory inventory)
    {
        if (IsPowered)
        {
            // 拔出：断电 + 归还电池 + 电弧事件
            IsPowered = false;
            inventory?.AddBattery();
            BroadcastArcFlash();
        }
        else
        {
            // 插入：通电
            if (inventory != null && inventory.TryUseBattery())
                IsPowered = true;
        }
    }

    // ── 电弧强光广播 ──────────────────────

    private void BroadcastArcFlash()
    {
        if (arcFlashPrefab == null)
        {
            Debug.LogWarning("[BatterySlot] Arc Flash Prefab 未配置。");
            return;
        }

        // 生成瞬间闪光物体，由 EnemyPerception 自动检测
        var flash = Instantiate(arcFlashPrefab, transform.position, Quaternion.identity);
        flash.transform.localScale = Vector3.one * arcFlashRadius;

        var stim = flash.GetComponent<LightStimulus>();
        if (stim != null) stim.sourcePosition = transform.position;

        Destroy(flash, arcFlashDuration);
    }

    // ── 编辑器 ────────────────────────────

    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsPowered ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        // 电弧范围
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, arcFlashRadius);
    }
#endif
}
