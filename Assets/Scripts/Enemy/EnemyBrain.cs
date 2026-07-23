using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人决策层 —— 状态机 + 察觉度计算 + 导航控制。
///
/// 职责：
///   1. 从 EnemyPerception 读取感知结果
///   2. 管理察觉度（增长 / 衰减 / 速率配置）
///   3. 三状态 FSM (Patrol → Suspicious → Investigate)
///   4. NavMeshAgent 导航控制
///   5. 对外事件 (OnStateChanged, OnAwarenessChanged)
///
/// 依赖：EnemyPerception（感知层）, NavMeshAgent（导航层）。
/// 参照：Unreal Behavior Tree Controller + Blackboard 的分层模式。
/// </summary>
public class EnemyBrain : MonoBehaviour
{
    // ── 状态枚举 ──────────────────────────

    public enum E_State { Patrol, Suspicious, Investigate }

    // ── Inspector ──────────────────────────

    [Header("References")]
    [Tooltip("感知组件。留空则从同一 GameObject 自动获取。")]
    [SerializeField] private EnemyPerception perception;
    [Tooltip("NavMeshAgent。留空则从同一 GameObject 自动获取。")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Movement Speed")]
    [Tooltip("巡逻状态下的移动速度。")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [Tooltip("追击/调查状态下的移动速度。")]
    [SerializeField] private float investigateSpeed = 7f;

    [Header("Awareness")]
    [Tooltip("当前察觉度 (0-100)。")]
    public float currentAwareness = 0f;
    [Tooltip("未察觉时的衰减速率（每秒减少值）。")]
    public float decayRate = 100f;

    [Header("State (Read-Only)")]
    [SerializeField] private E_State _currentState = E_State.Patrol;
    public E_State currentState => _currentState;

    // ── 事件 ──────────────────────────────

    public event System.Action<E_State, E_State> OnStateChanged;
    public event System.Action<float> OnAwarenessChanged;

    // ── 内部 ──────────────────────────────

    private Vector3 lastKnownPosition;

    // ── 生命周期 ──────────────────────────

    private void Awake()
    {
        if (perception == null) perception = GetComponent<EnemyPerception>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        ApplySpeed(E_State.Patrol);
    }

    private void Update()
    {
        if (perception == null) return;

        // Step 1: 驱动感知层执行本帧扫描
        perception.Tick();

        // Step 2: 更新察觉度 + 状态机
        UpdateBrain();

        // Step 3: 重置感知层（为下一帧准备）
        perception.ResetFrameState();
    }

    // ── 主循环 ────────────────────────────

    private void UpdateBrain()
    {
        E_State previousState = _currentState;
        float previousAwareness = currentAwareness;

        // ── 察觉度变化 ──
        if (perception.IsDetectedThisFrame)
        {
            float rate = perception.GetRate(perception.DetectedSource);
            currentAwareness += rate * Time.deltaTime;
            lastKnownPosition = perception.DetectedPosition;
        }
        else
        {
            bool isMoving = _currentState == E_State.Investigate
                         && (agent != null)
                         && (agent.pathPending || agent.remainingDistance > 1f);

            if (!isMoving)
                currentAwareness -= decayRate * Time.deltaTime;
        }

        currentAwareness = Mathf.Clamp(currentAwareness, 0f, 100f);

        // ── 状态机 ──
        switch (_currentState)
        {
            case E_State.Patrol:
                if (perception.IsDetectedThisFrame)
                {
                    _currentState = E_State.Suspicious;
                    if (agent != null) agent.isStopped = true;
                }
                break;

            case E_State.Suspicious:
                if (currentAwareness >= 100f)
                {
                    _currentState = E_State.Investigate;
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        ApplySpeed(E_State.Investigate);
                    }
                }
                else if (currentAwareness <= 0f)
                {
                    _currentState = E_State.Patrol;
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        ApplySpeed(E_State.Patrol);
                    }
                }
                break;

            case E_State.Investigate:
                if (agent != null)
                {
                    agent.SetDestination(lastKnownPosition);

                    if (!perception.IsDetectedThisFrame
                        && !agent.pathPending
                        && agent.remainingDistance < 1f)
                    {
                        agent.isStopped = true;
                        if (currentAwareness < 100f)
                        {
                            _currentState = E_State.Suspicious;
                            ApplySpeed(E_State.Patrol);
                        }
                    }
                    else
                    {
                        agent.isStopped = false;
                    }
                }
                break;
        }

        // ── 触发事件 ──
        if (!Mathf.Approximately(previousAwareness, currentAwareness))
            OnAwarenessChanged?.Invoke(currentAwareness);

        if (previousState != _currentState)
            OnStateChanged?.Invoke(previousState, _currentState);
    }

    // ── 速度控制 ──────────────────────────

    private void ApplySpeed(E_State state)
    {
        if (agent == null) return;
        agent.speed = state == E_State.Investigate ? investigateSpeed : patrolSpeed;
    }

    // ── 强制察觉 API ──────────────────────

    /// <summary>
    /// 强制敌人进入警觉状态，锁定指定位置。
    /// 供电弧闪光、配电盘强光等"瞬间事件"调用。
    /// 绕过察觉度积累，直接将状态设为 Suspicious 并设置调查目标。
    /// </summary>
    public void ForceAlert(Vector3 targetPosition)
    {
        lastKnownPosition = targetPosition;
        currentAwareness = 50f; // 从中点开始，给敌人调查的空间

        E_State oldState = _currentState;
        _currentState = E_State.Suspicious;

        if (agent != null)
            agent.isStopped = true;

        OnAwarenessChanged?.Invoke(currentAwareness);
        if (oldState != _currentState)
            OnStateChanged?.Invoke(oldState, _currentState);
    }

    // ── 编辑器可视化 ──────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_currentState != E_State.Investigate) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, lastKnownPosition);
        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
    }
#endif
}
