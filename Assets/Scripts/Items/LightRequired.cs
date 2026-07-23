using UnityEngine;

/// <summary>
/// 光源依赖标签 —— 挂载后，道具必须被手电筒照射才能交互。
///
/// 双通道检测（参照 EnemyPerception 的互补模式）：
///   P1: 光束节点扫描 —— OverlapSphere 查找 LightTrigger 层的 FlashlightCone 节点 + LOS
///       覆盖中远距离（光束节点每 2f 一个，节点 0 在命中点）
///   P2: 锥体数学检测 —— 直接计算角度 + 距离 + LOS
///       覆盖近距离（<2f 时无光束节点）
///
/// 不依赖物理触发器，不实现 IInteractable。
/// </summary>
public class LightRequired : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("OverlapSphere 扫描光束节点的半径。")]
    [SerializeField] private float beamNodeScanRadius = 2.5f;

    // ── 缓存 ────────────────────────────

    private FlashlightController flashlight;
    private Collider[] buffer = new Collider[32];
    private int lightTriggerMask;
    private int envMask;
    private int cachedFrame;
    private bool cachedIsLit;

    // ── 生命周期 ──────────────────────────

    private void Awake()
    {
        flashlight = FindAnyObjectByType<FlashlightController>();
        lightTriggerMask = LayerMask.GetMask("LightTrigger");
        envMask = LayerMask.GetMask("Environment");
    }

    // ── 公开属性 ──────────────────────────

    /// <summary>当前是否被手电筒照射。每帧缓存一次。</summary>
    public bool IsLit
    {
        get
        {
            if (cachedFrame != Time.frameCount)
            {
                cachedFrame = Time.frameCount;
                cachedIsLit = ComputeIsLit();
            }
            return cachedIsLit;
        }
    }

    // ── 检测逻辑 ──────────────────────────

    private bool ComputeIsLit()
    {
        if (flashlight == null || !flashlight.isLightOn)
            return false;

        // P1: 光束节点扫描 —— 对应 EnemyPerception.CheckVisualNodes
        if (ScanBeamNodes())
            return true;

        // P2: 锥体直接检测 —— 近距离兜底（< 2f 无光束节点）
        if (CheckConeDirect())
            return true;

        return false;
    }

    /// <summary>
    /// P1: 扫描道具周围是否有手电筒光束节点。
    /// 参照 EnemyPerception.CheckVisualNodes —— OverlapSphere + LightStimulus + LOS。
    /// </summary>
    private bool ScanBeamNodes()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, beamNodeScanRadius, buffer, lightTriggerMask);

        for (int i = 0; i < count; i++)
        {
            var stim = buffer[i].GetComponent<LightStimulus>();
            if (stim == null || stim.type != LightStimulus.StimulusType.FlashlightCone)
                continue;

            // 光束节点的 sourcePosition = 手电筒位置
            if (HasLineOfSight(stim.sourcePosition, transform.position))
                return true;
        }

        return false;
    }

    /// <summary>
    /// P2: 直接数学锥体检测 —— 角度 + 距离 + LOS。
    /// 处理近距离情况（手电筒锥体覆盖道具，但光束节点尚未生成）。
    /// </summary>
    private bool CheckConeDirect()
    {
        Vector3 toTarget = transform.position - flashlight.transform.position;
        float distance = toTarget.magnitude;

        // 距离检查
        if (distance > flashlight.maxDistance)
            return false;

        // 锥角检查
        float angle = Vector3.Angle(flashlight.transform.forward, toTarget.normalized);
        if (angle > flashlight.SpotHalfAngle)
            return false;

        // 视线检查
        return HasLineOfSight(flashlight.transform.position, transform.position);
    }

    /// <summary>
    /// 视线检测（参照 EnemyPerception.CheckLineOfSight）。
    /// </summary>
    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        return !Physics.Raycast(from, dir.normalized, dist, envMask);
    }

    // ── 编辑器可视化 ──────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, beamNodeScanRadius);
    }
#endif
}
