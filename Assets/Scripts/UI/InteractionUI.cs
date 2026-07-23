using UnityEngine;
using TMPro;

/// <summary>
/// 统一交互 UI 管理器 —— 管理交互提示和库存状态显示。
///
/// 挂在玩家 Canvas 上。读取 PlayerInteractor 和 PlayerInventory 的状态，
/// 驱动 TMP_Text 显示。不包含任何游戏逻辑。
/// </summary>
public class InteractionUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("玩家交互器。")]
    [SerializeField] private PlayerInteractor interactor;
    [Tooltip("玩家物品栏。")]
    [SerializeField] private PlayerInventory inventory;

    [Header("Prompt Text")]
    [Tooltip("交互提示文字（如 '按 E 拾取'）。")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Battery Status")]
    [Tooltip("电池库存状态文字（如 '拥有备用电池'）。")]
    [SerializeField] private TextMeshProUGUI batteryStatusText;
    // ── 生命周期 ──────────────────────────

    private void Start()
    {
        if (interactor == null) interactor = GetComponentInParent<PlayerInteractor>();
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();

        if (inventory != null)
            inventory.OnCountChanged += OnBatteryCountChanged;

        // 初始状态
        OnBatteryCountChanged(inventory != null ? inventory.SpareBatteryCount : 0);
    }

    private void Update()
    {
        if (interactor == null || promptText == null) return;

        var target = interactor.CurrentTarget;
        if (target != null)
        {
            // ── UI 文字优先级（高 → 低）──
            // P1: 需要光照但未被照射 → "Need Light"（红）
            // P2: 不可交互（缺电池等） → 道具自身提示（红）
            // P3: 可交互 → 道具自身提示（白）

            // P1: LightRequired 未满足
            var lr = interactor.CurrentLightRequired;
            if (lr != null && !lr.IsLit)
            {
                promptText.text = InteractionTexts.NeedLight;
                promptText.color = InteractionTexts.WarnColor;
                promptText.enabled = true;
                return;
            }

            // P2 / P3: 道具自身提示 + 颜色区分可交互/不可交互
            promptText.text = target.GetPrompt();
            promptText.color = target.CanInteract(inventory)
                ? InteractionTexts.DefaultColor   // P3: 白
                : InteractionTexts.WarnColor;     // P2: 红
            promptText.enabled = true;
        }
        else
        {
            promptText.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnCountChanged -= OnBatteryCountChanged;
    }

    // ── 库存回调 ──────────────────────────

    private void OnBatteryCountChanged(int count)
    {
        if (batteryStatusText == null) return;

        batteryStatusText.text = count > 0
            ? InteractionTexts.HasBattery
            : InteractionTexts.NoBattery;
    }
}
