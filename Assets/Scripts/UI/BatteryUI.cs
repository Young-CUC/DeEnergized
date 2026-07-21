using UnityEngine;
using TMPro;

/// <summary>
/// 电池 HUD 显示组件 —— 事件驱动，将 PlayerBattery 的电量数据绑定到 UI。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BatteryUI : MonoBehaviour
{
    // ── 引用 ──────────────────────────────────

    [Header("Data Source")]
    [Tooltip("留空则自动查找场景中的 PlayerBattery。")]
    [SerializeField] private PlayerBattery battery;

    [Header("Numeric Display")]
    [Tooltip("显示电量数值的组件。")]
    [SerializeField] private TextMeshProUGUI chargeText;

    //[Header("Bar Display (Optional)")]
    //[Tooltip("电量条 Slider。留空则不显示。")]
    //[SerializeField] private Slider chargeSlider;
    //[Tooltip("Slider 的填充图 Image，用于颜色切换。")]
    //[SerializeField] private Image chargeFillImage;

    //[Header("Low Power Overlay (Optional)")]
    //[Tooltip("低电量时闪烁的提示 UI（红色边框/图标等），留空则无。")]
    //[SerializeField] private GameObject lowPowerOverlay;

    // ── 显示配置 ──────────────────────────────

    [Header("Numeric Format")]
    [Tooltip("数值格式化字符串。F1 = 一位小数，F0 = 整数，P0 = 百分比。")]
    [SerializeField] private string chargeFormat = "F1";

    [Header("Color States")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("电量低于此比例时切换为警告色。")]
    [SerializeField] private TextMeshProUGUI SavingModeText;
    [SerializeField] private float warningThreshold = 0.3f;   // 30%
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.2f);  // 金黄
    [SerializeField] private Color lowPowerColor = Color.red;

    [Header("Low Power Flash")]
    [Tooltip("低电量时是否闪烁文字。")]
    [SerializeField] private bool flashOnLowPower = true;
    [Tooltip("闪烁间隔（秒）。")]
    [SerializeField] private float flashInterval = 0.5f;

    // ── 内部状态 ──────────────────────────────

    private float flashTimer;
    private bool flashVisible = true;

    // ── 生命周期 ──────────────────────────────

    private void Awake()
    {
        if (battery == null)
        {
          Debug.LogWarning("[BatteryUI] 场景中没有找到 PlayerBattery，请在 Inspector 中手动拖入。", this);
        }
    }

    private void OnEnable()
    {
        if (battery != null)
        {
            battery.OnChargeChanged += OnChargeChanged;
            battery.OnLowPower += OnLowPowerEnter;
            battery.OnChargeRestored += OnChargeRestored;
        }

        // 初始刷新
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (battery != null)
        {
            battery.OnChargeChanged -= OnChargeChanged;
            battery.OnLowPower -= OnLowPowerEnter;
            battery.OnChargeRestored -= OnChargeRestored;
        }
    }

    private void Update()
    {
        // 仅在低电量闪烁时需要 Update
        if (!flashOnLowPower || battery == null || !battery.IsLowPower)
            return;

        SetSavingModeText();

        flashTimer += Time.deltaTime;
        if (flashTimer >= flashInterval)
        {
            flashTimer = 0f;
            flashVisible = !flashVisible;
            ApplyFlashState();
        }
    }

    // ── 事件回调 ──────────────────────────────

    private void OnChargeChanged(float current, float max)
    {
        RefreshDisplay();
    }

    private void OnLowPowerEnter()
    {
        flashTimer = 0f;
        flashVisible = true;

        //if (lowPowerOverlay != null)
        //    lowPowerOverlay.SetActive(true);

        RefreshDisplay();
    }

    private void OnChargeRestored()
    {
        flashVisible = true;

        //if (lowPowerOverlay != null)
        //    lowPowerOverlay.SetActive(false);

        RefreshDisplay();
    }

    // ── 显示刷新 ──────────────────────────────

    /// <summary>
    /// 根据当前电量刷新所有 UI 元素。也可从外部手动调用。
    /// </summary>
    public void RefreshDisplay()
    {
        if (battery == null) return;

        float current = battery.CurrentCharge;
        float max = battery.MaxCharge;
        float ratio = battery.ChargePercent;

        // ── 数值文本 ──
        if (chargeText != null)
        {
            string display = $"Current Battery : {current.ToString(chargeFormat)} / {max.ToString(chargeFormat)}";
            chargeText.text = display;
            chargeText.color = GetChargeColor(ratio);
        }

        // ── 电量条 ──
        //if (chargeSlider != null)
        //{
        //    chargeSlider.minValue = 0f;
        //    chargeSlider.maxValue = 1f;
        //    chargeSlider.value = ratio;

        //    if (chargeFillImage != null)
        //        chargeFillImage.color = GetChargeColor(ratio);
        //}

        // ── 低电量闪烁 ──
        if (flashOnLowPower && battery.IsLowPower)
        {
            ApplyFlashState();
        }
    }

    // ── 辅助方法 ──────────────────────────────

    private Color GetChargeColor(float ratio)
    {
        if (battery.IsLowPower) return lowPowerColor;
        if (ratio <= warningThreshold) return warningColor;
        return normalColor;
    }

    private void ApplyFlashState()
    {
        if (chargeText != null)
            chargeText.enabled = flashVisible;

        //if (lowPowerOverlay != null && battery.IsLowPower)
        //    lowPowerOverlay.SetActive(flashVisible);
    }

    // ── 公开配置方法（代码调用）─────────────────

    /// <summary>动态更换数据源。</summary>
    public void SetBattery(PlayerBattery newBattery)
    {
        if (battery == newBattery) return;

        // 取消旧订阅
        if (battery != null)
        {
            battery.OnChargeChanged -= OnChargeChanged;
            battery.OnLowPower -= OnLowPowerEnter;
            battery.OnChargeRestored -= OnChargeRestored;
        }

        battery = newBattery;

        // 注册新订阅
        if (battery != null)
        {
            battery.OnChargeChanged += OnChargeChanged;
            battery.OnLowPower += OnLowPowerEnter;
            battery.OnChargeRestored += OnChargeRestored;
        }

        RefreshDisplay();
    }

    public void SetSavingModeText()
    {        
        if (SavingModeText != null)
        {
            if(battery != null && battery.IsLowPower) SavingModeText.gameObject.SetActive(true);
            else SavingModeText.gameObject.SetActive(false);
        }
    }
    /// <summary>动态设置数值格式。</summary>
    public void SetFormat(string format)
    {
        chargeFormat = format;
        RefreshDisplay();
    }
}
