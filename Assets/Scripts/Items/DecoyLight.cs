using UnityEngine;

/// <summary>
/// 诱导灯 —— 由电池插槽供电的定时循环光源。
///
/// 亮灯时激活 LightStimulus + LightSpotMarker 子节点，
/// 由 EnemyPerception 的 CheckVisualNodes / OnTriggerStay 自然检测。
/// 不手动调用任何感知 API。
///
/// 依赖：BatterySlot（OnPowerChanged 事件）。
/// </summary>
[RequireComponent(typeof(Light))]
public class DecoyLight : MonoBehaviour
{
    [Header("Power Source")]
    [Tooltip("绑定的电池插槽。")]
    [SerializeField] private BatterySlot slot;

    [Header("Cycle")]
    [SerializeField] private float onDuration = 8f;
    [SerializeField] private float offDuration = 6f;

    [Header("Detection Node")]
    [Tooltip("子节点 —— 挂有 LightStimulus (Decoy) + LightSpotMarker + SphereCollider (LightTrigger 层)。")]
    [SerializeField] private GameObject detectionNode;

    // ── 运行时 ────────────────────────────

    private Light targetLight;
    private LightStimulus stimulus;
    private float timer;
    private bool isOn;
    private bool isActive;

    // ── 生命周期 ──────────────────────────

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        targetLight.enabled = false;

        if (detectionNode != null)
        {
            stimulus = detectionNode.GetComponent<LightStimulus>();
            detectionNode.SetActive(false);
        }
    }

    private void Start()
    {
        if (slot != null)
            slot.OnPowerChanged.AddListener(OnPowerChanged);
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            isOn = !isOn;
            timer = isOn ? onDuration : offDuration;
            targetLight.enabled = isOn;
            if (detectionNode != null) detectionNode.SetActive(isOn);
        }

        // 每帧更新光源位置，确保敌人锁定的是诱导灯而非原点
        if (isOn && stimulus != null)
            stimulus.sourcePosition = transform.position;
    }

    private void OnDestroy()
    {
        if (slot != null)
            slot.OnPowerChanged.RemoveListener(OnPowerChanged);
    }

    // ── 电源回调 ──────────────────────────

    public void OnPowerChanged(bool powered)
    {
        isActive = powered;
        if (!powered)
        {
            isOn = false;
            targetLight.enabled = false;
            if (detectionNode != null) detectionNode.SetActive(false);
        }
        else
        {
            isOn = true;
            timer = onDuration;
            targetLight.enabled = true;
            if (detectionNode != null) detectionNode.SetActive(true);
        }
    }
}
