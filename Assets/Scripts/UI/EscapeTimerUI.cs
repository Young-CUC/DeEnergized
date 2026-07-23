using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 逃脱倒计时 UI —— 独立管理最终大门开启后的倒计时显示。
///
/// 由 DistributionPanel 调用 StartCountdown(duration, onComplete)。
/// 自己管理 Update 倒计时 + TMP_Text 显示（一位小数）。
/// 不依赖任何其他脚本。
///
/// 使用方式：
///   1. 在 Canvas 下创建 TextMeshProUGUI，挂此脚本
///   2. 拖入 timerText
///   3. DistributionPanel 的 Inspector 中拖入此脚本引用
/// </summary>
public class EscapeTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("显示倒计时的 TMP 文本。")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Display")]
    [Tooltip("数值格式化字符串。F1 = 一位小数。")]
    [SerializeField] private string timeFormat = "F1";

    [Header("Visual")]
    [Tooltip("倒计时的正常颜色。")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("倒计时低于此秒数时切换警告色。")]
    [SerializeField] private float warningThreshold = 5f;
    [Tooltip("警告颜色。")]
    [SerializeField] private Color warningColor = Color.red;

    // ── 运行时 ────────────────────────────

    private float remaining;
    private bool active;
    private Action onComplete;

    // ── 公开属性 ──────────────────────────

    public bool IsActive => active;
    public float RemainingSeconds => remaining;

    // ── 生命周期 ──────────────────────────

    private void Start()
    {
        if (timerText != null)
            timerText.enabled = false;
    }

    private void Update()
    {
        if (!active) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            active = false;

            // 显示完成文字
            if (timerText != null)
            {
                timerText.text = InteractionTexts.EscapeTimerFinished;
                timerText.color = warningColor;
            }

            onComplete?.Invoke();
            return;
        }

        RefreshDisplay();
    }

    // ── 公开方法 ──────────────────────────

    /// <summary>
    /// 启动倒计时。
    /// </summary>
    /// <param name="durationSeconds">倒计时总时长（秒）。</param>
    /// <param name="onFinished">倒计时结束时回调（大门关闭等）。</param>
    public void StartCountdown(float durationSeconds, Action onFinished)
    {
        remaining = durationSeconds;
        active = true;
        onComplete = onFinished;

        if (timerText != null)
            timerText.enabled = true;

        RefreshDisplay();
    }

    /// <summary>立即停止倒计时（不触发回调）。</summary>
    public void Cancel()
    {
        active = false;
        onComplete = null;

        if (timerText != null)
            timerText.enabled = false;
    }

    // ── 显示刷新 ──────────────────────────

    private void RefreshDisplay()
    {
        if (timerText == null) return;

        timerText.text = $"{InteractionTexts.EscapeTimerPrefix}{remaining.ToString(timeFormat)}";
        timerText.color = remaining <= warningThreshold ? warningColor : normalColor;
    }
}
