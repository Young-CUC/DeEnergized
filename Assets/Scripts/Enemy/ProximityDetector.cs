using UnityEngine;

/// <summary>
/// 接近察觉 —— 玩家进入一定范围后自动被敌人察觉，无需手电筒。
/// 在 EnemyPerception 之前运行，通过 ReportDetection() 注入到现有状态机中。
/// </summary>
[DefaultExecutionOrder(-50)]
public class ProximityDetector : MonoBehaviour
{
    [Header("Detection Range")]
    [Tooltip("察觉半径（世界单位）。")]
    [SerializeField] private float detectionRadius = 8f;
    [Tooltip("是否需要视线无遮挡。")]
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Target")]
    [Tooltip("玩家 Transform，留空则自动查找名为 Player 的 GameObject。")]
    [SerializeField] private Transform playerTransform;

    [Header("Obstacle Mask")]
    [Tooltip("视线遮挡层（如 Environment）。")]
    [SerializeField] private LayerMask obstacleMask = -1;

    [Header("References")]
    [Tooltip("状态机引用，留空则从同一 GameObject 自动获取。")]
    [SerializeField] private EnemyPerception lightSensor;

    // ── 生命周期 ──────────────────────────────

    private void Awake()
    {
        if (lightSensor == null)
            lightSensor = GetComponent<EnemyPerception>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (lightSensor == null || playerTransform == null)
            return;

        if (IsPlayerInRange())
        {
            lightSensor.ReportDetection(playerTransform.position, EnemyPerception.E_DetectionSource.Proximity);
        }
    }

    // ── 检测逻辑 ──────────────────────────────

    private bool IsPlayerInRange()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance > detectionRadius * detectionRadius)
            return false;

        if (requireLineOfSight)
        {
            if (Physics.Raycast(transform.position, toPlayer.normalized, Mathf.Sqrt(sqrDistance), obstacleMask))
                return false;
        }

        return true;
    }

    // ── 编辑器可视化 ──────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 半透明球体表示察觉范围
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        // 线框
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 范围标签
        UnityEditor.Handles.color = new Color(1f, 0.6f, 0.1f);
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (detectionRadius + 0.3f),
            $"Proximity: {detectionRadius:F1}m",
            new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter }
        );
    }
#endif
}
