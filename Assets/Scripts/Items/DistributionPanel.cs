using UnityEngine;

/// <summary>
/// 配电盘 —— 游戏终局触发器。
///
/// 玩家按 E 交互 → 取出核心电池（电量归零）→ 开启最终大门 →
/// 启动逃脱倒计时（剩余电量 × 2 秒）→ 倒计时结束大门关闭。
///
/// 纯编排器：自己不持有计时/动画逻辑，只负责连线。
///
/// 依赖：
///   PlayerBattery  —— 读取电量 + 调用 Extract()
///   MainDoor        —— Open() / Close()
///   EscapeTimerUI   —— StartCountdown(duration, onComplete)
/// </summary>
[RequireComponent(typeof(Collider))]
public class DistributionPanel : MonoBehaviour, IInteractable
{
    [Header("References")]
    [Tooltip("玩家核心电池。")]
    [SerializeField] private PlayerBattery battery;
    [Tooltip("最终逃脱大门。")]
    [SerializeField] private MainDoor mainDoor;
    [Tooltip("逃脱倒计时 UI。")]
    [SerializeField] private EscapeTimerUI escapeTimerUI;

    [Header("Arc Flash (激活电弧)")]
    [Tooltip("电弧闪光预制件 —— 挂有 LightStimulus (ArcFlash) + LightSpotMarker + SphereCollider (LightTrigger 层)。")]
    [SerializeField] private GameObject arcFlashPrefab;
    [Tooltip("电弧闪光的感知半径（世界单位）。")]
    [SerializeField] private float arcFlashRadius = 30f;
    [Tooltip("闪光持续时间（秒）。")]
    [SerializeField] private float arcFlashDuration = 0.5f;

    [Header("State")]
    [Tooltip("是否已被激活（激活后不可再次交互）。")]
    [SerializeField] private bool used;

    // ── IInteractable ──────────────────────────

    public string GetPrompt()
    {
        if (used)                  return InteractionTexts.PanelUsed;
        if (battery == null)       return InteractionTexts.PanelNoBattery;
        if (battery.IsExtracted)   return InteractionTexts.PanelNoBattery;
        if (battery.CurrentCharge <= 0f) return InteractionTexts.PanelNoBattery;
        return InteractionTexts.PanelReady;
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        if (used) return false;
        if (battery == null) return false;
        if (battery.IsExtracted) return false;
        return battery.CurrentCharge > 0f;
    }

    public void OnInteract(PlayerInventory inventory)
    {
        if (used) return;
        if (battery == null) return;

        // 保存剩余电量（Extract 前读取）
        float remainingCharge = battery.CurrentCharge;

        // 取出核心电池 → 电量归零 + 手电筒强制关闭
        battery.Extract();

        used = true;

        // 电弧强光事件（与拔出电池相同效果）
        BroadcastArcFlash();

        // 开启最终大门
        if (mainDoor != null)
            mainDoor.Open();

        // 启动逃脱倒计时：剩余电量 × 2 秒
        float countdownSeconds = remainingCharge * 2f;
        if (escapeTimerUI != null)
            escapeTimerUI.StartCountdown(countdownSeconds, OnCountdownFinished);
    }

    // ── 电弧强光广播 ────────────────────────

    private void BroadcastArcFlash()
    {
        if (arcFlashPrefab == null)
        {
            Debug.LogWarning("[DistributionPanel] Arc Flash Prefab 未配置。");
            return;
        }

        var flash = Instantiate(arcFlashPrefab, transform.position, Quaternion.identity);
        flash.transform.localScale = Vector3.one * arcFlashRadius;

        var stim = flash.GetComponent<LightStimulus>();
        if (stim != null) stim.sourcePosition = transform.position;

        Destroy(flash, arcFlashDuration);
    }

    // ── 倒计时结束回调 ────────────────────────

    private void OnCountdownFinished()
    {
        if (mainDoor != null)
            mainDoor.Close();
    }

    // ── 编辑器 ────────────────────────────────

    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = used ? Color.gray : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
#endif
}
