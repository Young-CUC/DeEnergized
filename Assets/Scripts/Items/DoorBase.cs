using UnityEngine;

/// <summary>
/// 门基类 —— 共享动画 + 碰撞 + 编辑器可视化。
///
/// 子类只需定义"谁来触发开/关"，调用 protected SetOpen(bool) 即可。
/// 不实现 IInteractable（门不直接被玩家操作，由插槽或编排器间接控制）。
///
/// 扩展方式：
///   PowerDoor   : DoorBase  —— BatterySlot 供电触发
///   ScriptedDoor : DoorBase  —— 外部脚本调用 Open()/Close()
///   (未来) KeyDoor, TimerDoor, TriggerDoor ...
/// </summary>
public abstract class DoorBase : MonoBehaviour
{
    [Header("Door Mesh")]
    [Tooltip("门体的 Transform（用于位移动画）。留空则使用自身。")]
    [SerializeField] protected Transform doorMesh;
    [Tooltip("开门后的本地位移（如 (0, 3, 0) = 向上滑开）。")]
    [SerializeField] protected Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("开/关门过渡速度。")]
    [SerializeField] protected float speed = 3f;

    [Header("Collision")]
    [Tooltip("门的碰撞体（开门后禁用，关门后启用）。留空则自动查找。")]
    [SerializeField] protected Collider doorCollider;

    // ── 运行时 ────────────────────────────

    protected Vector3 closedPos;
    protected Vector3 openPos;
    protected bool isOpen;

    // ── 生命周期 ──────────────────────────

    protected virtual void Awake()
    {
        if (doorMesh == null) doorMesh = transform;
        if (doorCollider == null) doorCollider = GetComponent<Collider>();

        closedPos = doorMesh.localPosition;
        openPos = closedPos + openOffset;
    }

    protected virtual void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;
        doorMesh.localPosition = Vector3.Lerp(
            doorMesh.localPosition, target, speed * Time.deltaTime);
    }

    // ── 子类 API ──────────────────────────

    /// <summary>
    /// 子类调用以改变门的状态。自动处理 Collider 切换。
    /// </summary>
    protected void SetOpen(bool open)
    {
        isOpen = open;
        if (doorCollider != null)
            doorCollider.enabled = !open;
    }

    // ── 编辑器可视化 ──────────────────────

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        // Awake 可能尚未执行，回退计算
        Vector3 cp = doorMesh != null
            ? doorMesh.localPosition
            : (Application.isPlaying ? closedPos : transform.localPosition);
        Vector3 op = cp + openOffset;

        Vector3 wc = transform.TransformPoint(isOpen ? op : cp);
        Vector3 wo = transform.TransformPoint(isOpen ? cp : op);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(wc, doorMesh != null ? doorMesh.localScale : Vector3.one);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(wo, doorMesh != null ? doorMesh.localScale : Vector3.one);
        Gizmos.DrawLine(wc, wo);
    }
#endif
}
