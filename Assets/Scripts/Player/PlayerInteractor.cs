using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家交互器 —— 扫描附近 IInteractable，E 键触发交互。
/// 使用 Unity New Input System，参考 PlayerMovement 的模式。
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("交互检测半径。")]
    [SerializeField] private float interactionRadius = 2.5f;
    [Tooltip("可交互物体所在层。")]
    [SerializeField] private LayerMask interactableMask = -1;

    [Header("References")]
    [Tooltip("玩家物品栏。留空自动查找。")]
    [SerializeField] private PlayerInventory inventory;

    private IInteractable currentTarget;
    private Collider[] overlapBuffer = new Collider[16];
    private InputAction interactAction;

    // ── 公开属性 ──────────────────────────

    /// <summary>当前可交互的目标（供 UI 脚本读取提示文字）。</summary>
    public IInteractable CurrentTarget => currentTarget;

    // ── 生命周期 ──────────────────────────

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
    }

    private void OnEnable()
    {
        interactAction.Enable();
        interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteractPerformed;
        interactAction.Disable();
    }

    private void Update()
    {
        ScanForInteractable();
    }

    // ── 输入回调 ──────────────────────────

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (currentTarget != null && currentTarget.CanInteract(inventory))
            currentTarget.OnInteract(inventory);
    }

    // ── 扫描 ──────────────────────────────

    private void ScanForInteractable()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, interactionRadius, overlapBuffer, interactableMask);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var interactable = overlapBuffer[i].GetComponent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, overlapBuffer[i].transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = interactable;
            }
        }

        // 始终显示最近物体的提示文本（即使不可交互），
        // E 键按下时才检查 CanInteract
        currentTarget = best;
    }

    // ── 编辑器可视化 ──────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
}
