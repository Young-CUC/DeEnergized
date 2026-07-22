using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 敌人察觉 UI —— 在敌人头顶显示察觉度进度条和状态图标。
///
/// 使用方式：
///   1. 在敌人 GameObject 下创建 World-Space Canvas 子物体。
///   2. Canvas 下放置一个 Image (Filled/Horizontal) 作为察觉条、一个 TextMeshProUGUI 作为图标。
///   3. Canvas 上添加 CanvasGroup 用于淡入淡出。
///   4. 将 EnemyBrain、CanvasGroup、Fill Image、图标 Text、Canvas Transform 拖入本组件的 Inspector 字段。
///
/// 事件驱动：订阅 EnemyBrain.OnStateChanged 和 OnAwarenessChanged。
/// 无需预制件，不依赖任何特定组件（无 [RequireComponent]）。
/// </summary>
public class EnemyAlertUI : MonoBehaviour
{
    // ── 引用（在 Inspector 中手动拖入）─────

    [Header("References")]
    [Tooltip("数据源 —— 场景中敌人身上的 EnemyBrain 组件。")]
    [SerializeField] private EnemyBrain sensor;
    [Tooltip("用于淡入淡出的 CanvasGroup。放在 Canvas 根节点上。")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("察觉条的填充 Image（需设置为 Image.Type.Filled, FillMethod.Horizontal, FillOrigin=Left）。")]
    [SerializeField] private Image fillImage;
    [Tooltip("察觉条的背景 Image —— 始终满格显示，作为\"最大值\"的视觉参考。")]
    [SerializeField] private Image barBackground;
    [Tooltip("状态图标文本（？/！）。放在 Canvas 下，使用 TextMeshProUGUI。")]
    [SerializeField] private TextMeshProUGUI stateIcon;
    [Tooltip("需要 Billboard 朝向摄像机的根节点（通常是 Canvas 的 Transform）。")]
    [SerializeField] private Transform uiRoot;
    [Tooltip("玩家摄像机（留空则自动使用 Camera.main）。")]
    [SerializeField] private Transform playerCamera;

    // ── 察觉条外观 ──────────────────────────

    [Header("Awareness Bar")]
    [Tooltip("察觉度 0% 时的颜色（刚被注意）。")]
    [SerializeField] private Color barNeutralColor = Color.white;
    [Tooltip("察觉度 50% 时的颜色（可疑）。")]
    [SerializeField] private Color barWarningColor = Color.yellow;
    [Tooltip("察觉度 100% 时的颜色（即将暴露）。")]
    [SerializeField] private Color barAlertColor = Color.red;
    [Tooltip("背景条颜色（始终满格，显示最大值）。")]
    [SerializeField] private Color barBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    // ── 状态图标 ────────────────────────────

    [Header("State Icon")]
    [Tooltip("Suspicious 状态下显示的字符。")]
    [SerializeField] private string suspiciousIcon = "?";
    [Tooltip("Investigate 状态下显示的字符。")]
    [SerializeField] private string investigateIcon = "!";
    [Tooltip("图标颜色 — Suspicious 状态下固定为此颜色。")]
    [SerializeField] private Color suspiciousIconColor = Color.yellow;
    [Tooltip("图标颜色 — Investigate 状态下固定为此颜色。")]
    [SerializeField] private Color investigateIconColor = Color.red;

    // ── 淡入淡出 ────────────────────────────

    [Header("Fade / Visibility")]
    [Tooltip("淡入速度（alpha/秒）。")]
    [SerializeField] private float fadeInSpeed = 6f;
    [Tooltip("淡出速度（alpha/秒）。")]
    [SerializeField] private float fadeOutSpeed = 3f;
    [Tooltip("回到 Patrol 状态后延迟多少秒再淡出。")]
    [SerializeField] private float patrolHideDelay = 1.5f;

    // ── 脉冲动画 ────────────────────────────

    [Header("Investigate Pulse")]
    [Tooltip("Investigate 状态下图标是否脉冲缩放。")]
    [SerializeField] private bool pulseOnInvestigate = true;
    [Tooltip("脉冲频率（Hz）。")]
    [SerializeField] private float pulseFrequency = 2f;
    [Tooltip("脉冲缩放幅度（0.3 = ±30%）。")]
    [SerializeField] private float pulseScaleAmount = 0.3f;

    // ── Billboard ───────────────────────────

    [Header("Billboard")]
    [Tooltip("是否每帧将 UI 旋转朝向摄像机。")]
    [SerializeField] private bool billboardToCamera = true;
    [Tooltip("UI 在敌人头顶的世界空间偏移。")]
    [SerializeField] private Vector3 uiWorldOffset = new Vector3(0f, 2.5f, 0f);

    // ── 运行时状态 ──────────────────────────

    private float targetAlpha;
    private float hideTimer;
    private EnemyBrain.E_State currentVisualState;
    private float baseIconScale = 1f;
    private bool hasIconScale;

    // ── 生命周期 ────────────────────────────

    private void Start()
    {
        // 自动查找摄像机
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        // 自动查找 EnemyBrain
        if (sensor == null)
            sensor = GetComponent<EnemyBrain>();

        // 记录图标初始缩放
        if (stateIcon != null)
        {
            baseIconScale = stateIcon.transform.localScale.x;
            hasIconScale = true;
        }

        // 背景条初始颜色
        if (barBackground != null)
            barBackground.color = barBackgroundColor;

        // 订阅事件
        if (sensor != null)
        {
            sensor.OnStateChanged += HandleStateChanged;
            sensor.OnAwarenessChanged += HandleAwarenessChanged;

            // 初始同步
            HandleStateChanged(sensor.currentState, sensor.currentState);
            HandleAwarenessChanged(sensor.currentAwareness);
        }

        // 初始隐藏
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        targetAlpha = 0f;
    }

    private void LateUpdate()
    {
        // ── Billboard ──
        if (billboardToCamera && uiRoot != null && playerCamera != null)
            uiRoot.rotation = playerCamera.rotation;

        // ── 世界位置偏移 ──
        if (uiRoot != null)
            uiRoot.position = transform.position + uiWorldOffset;

        // ── 淡入淡出 ──
        UpdateFade();

        // ── Investigate 脉冲 ──
        if (pulseOnInvestigate && stateIcon != null && hasIconScale
            && currentVisualState == EnemyBrain.E_State.Investigate)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * pulseScaleAmount;
            stateIcon.transform.localScale = Vector3.one * (baseIconScale * pulse);
        }
    }

    private void OnDestroy()
    {
        if (sensor != null)
        {
            sensor.OnStateChanged -= HandleStateChanged;
            sensor.OnAwarenessChanged -= HandleAwarenessChanged;
        }
    }

    // ── 事件处理 ────────────────────────────

    private void HandleStateChanged(EnemyBrain.E_State oldState, EnemyBrain.E_State newState)
    {
        currentVisualState = newState;

        switch (newState)
        {
            case EnemyBrain.E_State.Patrol:
                if (oldState == EnemyBrain.E_State.Suspicious || oldState == EnemyBrain.E_State.Investigate)
                    hideTimer = patrolHideDelay;  // 延迟后淡出
                else
                    targetAlpha = 0f;
                break;

            case EnemyBrain.E_State.Suspicious:
                targetAlpha = 1f;
                hideTimer = 0f;
                if (stateIcon != null)
                {
                    stateIcon.text = suspiciousIcon;
                    stateIcon.color = suspiciousIconColor;
                }
                // 显示察觉条
                SetBarVisible(true);
                if (fillImage != null)
                    fillImage.fillAmount = 0f;
                break;

            case EnemyBrain.E_State.Investigate:
                targetAlpha = 1f;
                hideTimer = 0f;
                if (stateIcon != null)
                {
                    stateIcon.text = investigateIcon;
                    stateIcon.color = investigateIconColor;
                }
                // 隐藏察觉条，只保留感叹号
                SetBarVisible(false);
                break;
        }
    }

    private void HandleAwarenessChanged(float awareness)
    {
        if (fillImage == null) return;

        float t = Mathf.Clamp01(awareness / 100f);

        // 填充条
        fillImage.fillAmount = t;

        // 三阶段颜色：0% 白 → 50% 黄 → 100% 红
        Color barColor;
        if (t < 0.5f)
            barColor = Color.Lerp(barNeutralColor, barWarningColor, t * 2f);
        else
            barColor = Color.Lerp(barWarningColor, barAlertColor, (t - 0.5f) * 2f);
        fillImage.color = barColor;
    }

    // ── 淡入淡出 ────────────────────────────

    private void UpdateFade()
    {
        if (canvasGroup == null) return;

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                targetAlpha = 0f;
        }

        float speed = targetAlpha > canvasGroup.alpha ? fadeInSpeed : fadeOutSpeed;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, speed * Time.deltaTime);
    }

    private void SetBarVisible(bool visible)
    {
        if (fillImage != null)
            fillImage.enabled = visible;
        if (barBackground != null)
            barBackground.enabled = visible;
    }

    // ── 公开 API ────────────────────────────

    /// <summary>强制显示或隐藏 UI。</summary>
    public void SetVisible(bool visible)
    {
        targetAlpha = visible ? 1f : 0f;
        hideTimer = 0f;
    }

    /// <summary>更换数据源（用于对象池或动态绑定）。</summary>
    public void BindSensor(EnemyBrain newSensor)
    {
        if (sensor == newSensor) return;

        if (sensor != null)
        {
            sensor.OnStateChanged -= HandleStateChanged;
            sensor.OnAwarenessChanged -= HandleAwarenessChanged;
        }

        sensor = newSensor;

        if (sensor != null)
        {
            sensor.OnStateChanged += HandleStateChanged;
            sensor.OnAwarenessChanged += HandleAwarenessChanged;
            HandleStateChanged(sensor.currentState, sensor.currentState);
            HandleAwarenessChanged(sensor.currentAwareness);
        }
    }
}
