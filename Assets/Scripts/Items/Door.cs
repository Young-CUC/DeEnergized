using UnityEngine;

/// <summary>
/// 通电门 —— 由电池插槽供电。通电常开，断电常闭，带平滑过渡。
/// 不实现 IInteractable（玩家不直接操作门，通过插槽间接控制）。
///
/// 依赖：BatterySlot（OnPowerChanged UnityEvent）。
/// </summary>
public class Door : MonoBehaviour
{
    [Header("Power Source")]
    [Tooltip("控制此门的电池插槽。")]
    [SerializeField] private BatterySlot slot;

    [Header("Door Mesh")]
    [Tooltip("门体的 Transform（用于位移动画）。留空则使用自身。")]
    [SerializeField] private Transform doorMesh;
    [Tooltip("开门后的本地位移（如 (0, 3, 0) = 向上滑开）。")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("开/关门过渡速度。")]
    [SerializeField] private float speed = 3f;

    [Header("Collision")]
    [Tooltip("门的碰撞体（通电后禁用，断电后启用）。留空则自动查找。")]
    [SerializeField] private Collider doorCollider;

    // ── 运行时 ────────────────────────────

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isPowered;

    // ── 生命周期 ──────────────────────────

    private void Awake()
    {
        if (doorMesh == null) doorMesh = transform;
        if (doorCollider == null) doorCollider = GetComponent<Collider>();

        closedPos = doorMesh.localPosition;
        openPos = closedPos + openOffset;
    }

    private void Start()
    {
        if (slot != null)
            slot.OnPowerChanged.AddListener(OnPowerChanged);
    }

    private void Update()
    {
        Vector3 target = isPowered ? openPos : closedPos;
        doorMesh.localPosition = Vector3.Lerp(
            doorMesh.localPosition, target, speed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (slot != null)
            slot.OnPowerChanged.RemoveListener(OnPowerChanged);
    }

    // ── 电源回调 ──────────────────────────

    public void OnPowerChanged(bool powered)
    {
        isPowered = powered;

        // 通电开门 → 禁用碰撞让玩家通过
        // 断电关门 → 启用碰撞阻挡玩家
        if (doorCollider != null)
            doorCollider.enabled = !powered;
    }

    // ── 编辑器可视化 ──────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 wc = transform.TransformPoint(closedPos);
        Vector3 wo = transform.TransformPoint(openPos);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(wc, doorMesh != null ? doorMesh.localScale : Vector3.one);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(wo, doorMesh != null ? doorMesh.localScale : Vector3.one);
        Gizmos.DrawLine(wc, wo);
    }
#endif
}
