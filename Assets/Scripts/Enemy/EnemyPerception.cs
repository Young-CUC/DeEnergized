using UnityEngine;

/// <summary>
/// 敌人感知层 —— 纯感知，不包含任何状态机或察觉度计算。
///
/// 职责：
///   1. 触发器检测 (LightTrigger 层的光照锥体)
///   2. 视觉节点扫描 (Physics.OverlapSphere 查找 LightSpotMarker)
///   3. 视线检测 (Raycast LOS)
///   4. 对外提供 ReportDetection() API（供 ProximityDetector 等调用）
///
/// 对外暴露的属性由 EnemyBrain 每帧读取并处理后调用 ResetFrameState()。
///
/// 参照：Unreal Engine AI Perception System (AIPerceptionComponent + AISense_Sight)
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    // ── 察觉来源 ──────────────────────────

    /// <summary>察觉来源类型 —— 扩展新来源时在此枚举中新增。</summary>
    public enum E_DetectionSource
    {
        Light,        // 手电筒光线 (优先级 1)
        Proximity,    // 近距离察觉
        DecoyLight,   // 诱导灯光线 (优先级 2)
        ArcFlash      // 电弧强光 (优先级 3，瞬间锁定)
    }

    [System.Serializable]
    public struct DetectionRate
    {
        [Tooltip("察觉来源")]
        public E_DetectionSource source;
        [Tooltip("该来源的察觉度增长速率（每秒增加值）")]
        public float rate;
    }

    // ── Inspector ──────────────────────────

    [Header("Sight Config")]
    public Transform eyeTransform;
    public float viewRadius = 15f;
    [Range(0f, 180f)]
    public float fovAngle = 120f;
    public LayerMask obstacleMask;

    [Header("Detection Rates")]
    [Tooltip("每种察觉来源的察觉度增长速率。未配置的来源默认 200/s。")]
    [SerializeField] private DetectionRate[] sourceRates = new DetectionRate[]
    {
        new DetectionRate { source = E_DetectionSource.Light, rate = 200f },
        new DetectionRate { source = E_DetectionSource.Proximity, rate = 50f },
        new DetectionRate { source = E_DetectionSource.DecoyLight, rate = 300f },
        new DetectionRate { source = E_DetectionSource.ArcFlash, rate = 100000f },
    };

    // ── 本帧感知结果（由 EnemyBrain 读取）──

    public bool IsDetectedThisFrame { get; private set; }
    public Vector3 DetectedPosition { get; private set; }
    public E_DetectionSource DetectedSource { get; private set; }

    // ── 内部状态 ──────────────────────────

    private bool triggerHit;
    private Vector3 triggerHitPos;
    private E_DetectionSource triggerHitSource;
    private bool externalReport;
    private Vector3 externalReportPos;
    private E_DetectionSource externalReportSource;
    private System.Collections.Generic.Dictionary<E_DetectionSource, float> rateLookup;

    // ── 生命周期 ──────────────────────────

    private void Awake()
    {
        rateLookup = new System.Collections.Generic.Dictionary<E_DetectionSource, float>();
        foreach (var c in sourceRates)
            rateLookup[c.source] = c.rate;
    }

    /// <summary>获取指定来源的察觉度增长速率。</summary>
    public float GetRate(E_DetectionSource source)
    {
        return rateLookup.TryGetValue(source, out float r) ? r : 200f;
    }

    /// <summary>将 LightStimulus.StimulusType 映射到 E_DetectionSource。</summary>
    private static E_DetectionSource StimulusToSource(LightStimulus.StimulusType type)
    {
        switch (type)
        {
            case LightStimulus.StimulusType.Decoy:     return E_DetectionSource.DecoyLight;
            case LightStimulus.StimulusType.ArcFlash:  return E_DetectionSource.ArcFlash;
            default:                                   return E_DetectionSource.Light;
        }
    }

    // ── Unity 触发器回调 ──────────────────

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("LightTrigger"))
        {
            LightStimulus stim = other.GetComponent<LightStimulus>();
            if (stim != null && CheckLineOfSight(stim.sourcePosition, eyeTransform.position))
            {
                triggerHit = true;
                triggerHitPos = stim.sourcePosition;
                triggerHitSource = StimulusToSource(stim.type);
            }
        }
    }

    // ── 对外 API ──────────────────────────

    /// <summary>
    /// 供外部检测器调用（如 ProximityDetector）。
    /// 调用后本帧 Tick() 会将其计入感知结果。
    /// </summary>
    public void ReportDetection(Vector3 sourcePosition, E_DetectionSource source)
    {
        externalReport = true;
        externalReportPos = sourcePosition;
        externalReportSource = source;
    }

    // ── 主逻辑（由 EnemyBrain.Update 驱动）──

    /// <summary>执行本帧视觉扫描，合并所有感知源结果。</summary>
    public void Tick()
    {
        // 视觉节点扫描（替换旧的 CheckVisualNodes）
        CheckVisualNodes();

        // 合并感知源：外部报告 (ReportDetection) > 视觉扫描 > 触发器
        if (externalReport)
        {
            IsDetectedThisFrame = true;
            DetectedPosition = externalReportPos;
            DetectedSource = externalReportSource;
        }
        else if (triggerHit)
        {
            IsDetectedThisFrame = true;
            DetectedPosition = triggerHitPos;
            DetectedSource = triggerHitSource;
        }
        else
        {
            IsDetectedThisFrame = false;
        }
    }

    /// <summary>在 EnemyBrain 处理完本帧逻辑后调用，重置感知状态。</summary>
    public void ResetFrameState()
    {
        triggerHit = false;
        externalReport = false;
        IsDetectedThisFrame = false;
    }

    // ── 视觉扫描 ──────────────────────────

    private void CheckVisualNodes()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius);
        foreach (Collider c in hits)
        {
            if (c.TryGetComponent<LightSpotMarker>(out _))
            {
                var stim = c.GetComponent<LightStimulus>();
                bool isDecoy = stim != null && stim.type == LightStimulus.StimulusType.Decoy;
                bool isArcFlash = stim != null && stim.type == LightStimulus.StimulusType.ArcFlash;

                // Decoy/ArcFlash 全向可见（跳过 FOV 检查）；手电筒光束仍需在视野锥内
                bool inFOV = isDecoy || isArcFlash || Vector3.Angle(
                    eyeTransform.forward,
                    (c.transform.position - eyeTransform.position).normalized) < fovAngle / 2f;

                // ArcFlash 无视障碍（闪光会照亮整个环境）；其余类型需视线无遮挡
                bool hasSight = isArcFlash || CheckLineOfSight(eyeTransform.position, c.transform.position);

                if (inFOV && hasSight)
                {
                    triggerHit = true;
                    triggerHitPos = stim.sourcePosition;
                    triggerHitSource = StimulusToSource(stim.type);
                    return;
                }
            }
        }
    }

    // ── 视线检测 ──────────────────────────

    private bool CheckLineOfSight(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        return !Physics.Raycast(start, dir.normalized, dist, obstacleMask);
    }

    // ── 编辑器可视化 ──────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (eyeTransform == null) return;

        // 视野范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // FOV 锥形线
        Gizmos.color = Color.red;
        Vector3 left = Quaternion.Euler(0, -fovAngle / 2, 0) * eyeTransform.forward;
        Vector3 right = Quaternion.Euler(0, fovAngle / 2, 0) * eyeTransform.forward;
        Gizmos.DrawRay(eyeTransform.position, left * viewRadius);
        Gizmos.DrawRay(eyeTransform.position, right * viewRadius);
    }
#endif
}
