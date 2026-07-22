using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 敌人巡逻脚本 —— 使用本地坐标定义巡逻路线，无需在场景中放置空物体。
/// 与 EnemyBrain 状态机集成：仅在 Patrol 状态下执行巡逻。
/// 通过 DefaultExecutionOrder 确保在 EnemyBrain 之后运行。
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("巡逻路线（本地坐标）")]
    [Tooltip("相对于此 Transform 的本地坐标。在编辑器中通过 Gizmos 可视化世界空间路径。")]
    [SerializeField] private Vector3[] localWaypoints;

    [Header("停留时长")]
    [Tooltip("每个巡逻点的默认停留时长（秒）。")]
    [SerializeField] private float defaultWaitTime = 1f;
    [Tooltip("每个巡逻点的独立停留时长覆盖。数组中对应索引的值若 > 0 则覆盖 defaultWaitTime。")]
    [SerializeField] private float[] waitTimes;

    [Header("巡逻模式")]
    [Tooltip("开启：循环遍历巡逻点。关闭：往返（到达终点后原路返回）。")]
    [SerializeField] private bool loopPatrol = true;

    [Header("到达判定")]
    [Tooltip("距离目标点多远时判定为已到达。")]
    [SerializeField] private float arrivalThreshold = 0.3f;

    [Header("引用")]
    [Tooltip("状态机引用，留空则从同一 GameObject 自动获取。")]
    [SerializeField] private EnemyBrain brain;

    private NavMeshAgent agent;
    private Vector3[] worldWaypoints;
    private int currentIndex = 0;
    private bool isReversing = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private EnemyBrain.E_State previousState;
    private bool hasStarted = false;

    //生命周期

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (brain == null)
            brain = GetComponent<EnemyBrain>();

        BuildWorldWaypoints();
    }

    private void Start()
    {
        if (brain != null)
            previousState = brain.currentState;

        if (worldWaypoints.Length > 0)
        {
            currentIndex = FindNearestWaypointIndex();
            StartMovingToCurrent();
        }

        hasStarted = true;
    }

    private void Update()
    {
        if (brain == null || worldWaypoints.Length == 0)
            return;

        // 检测是否刚刚进入 Patrol 状态
        bool justEnteredPatrol = hasStarted
            && previousState != EnemyBrain.E_State.Patrol
            && brain.currentState == EnemyBrain.E_State.Patrol;

        // 非 Patrol 状态下不做任何事
        if (brain.currentState != EnemyBrain.E_State.Patrol)
        {
            isWaiting = false;
            waitTimer = 0f;
            previousState = brain.currentState;
            return;
        }

        // 刚回到 Patrol：从最近巡逻点恢复
        if (justEnteredPatrol)
        {
            ResumePatrol();
        }

        ExecutePatrol();

        previousState = brain.currentState;
    }

    //巡逻逻辑

    private void ExecutePatrol()
    {
        // 只有一个巡逻点：原地等待
        if (worldWaypoints.Length == 1)
        {
            agent.isStopped = true;
            return;
        }

        if (isWaiting)
        {
            agent.isStopped = true;
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                AdvanceWaypoint();
                StartMovingToCurrent();
            }
        }
        else
        {
            // 确保 agent 启用
            if (agent.isStopped)
                agent.isStopped = false;

            // 到达判定
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalThreshold)
            {
                float wait = GetWaitTime(currentIndex);
                if (wait > 0f)
                {
                    isWaiting = true;
                    waitTimer = wait;
                }
                else
                {
                    AdvanceWaypoint();
                    StartMovingToCurrent();
                }
            }
        }
    }

    private void ResumePatrol()
    {
        currentIndex = FindNearestWaypointIndex();
        isWaiting = false;
        waitTimer = 0f;
        isReversing = false;
        StartMovingToCurrent();
    }

    //导航

    private void StartMovingToCurrent()
    {
        if (worldWaypoints.Length == 0) return;
        agent.isStopped = false;
        agent.SetDestination(worldWaypoints[currentIndex]);
    }

    private void AdvanceWaypoint()
    {
        if (worldWaypoints.Length <= 1) return;

        if (loopPatrol)
        {
            currentIndex = (currentIndex + 1) % worldWaypoints.Length;
        }
        else
        {
            // 往返模式
            if (isReversing)
            {
                currentIndex--;
                if (currentIndex < 0)
                {
                    currentIndex = 1;
                    isReversing = false;
                }
            }
            else
            {
                currentIndex++;
                if (currentIndex >= worldWaypoints.Length)
                {
                    currentIndex = Mathf.Max(0, worldWaypoints.Length - 2);
                    isReversing = true;
                }
            }
        }
    }

    //工具方法

    /// <summary>将本地坐标转换为世界空间坐标。</summary>
    private void BuildWorldWaypoints()
    {
        if (localWaypoints == null || localWaypoints.Length == 0)
        {
            worldWaypoints = new Vector3[0];
            return;
        }

        worldWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++)
        {
            worldWaypoints[i] = transform.TransformPoint(localWaypoints[i]);
        }
    }

    /// <summary>找到距离当前位置最近的巡逻点索引。</summary>
    private int FindNearestWaypointIndex()
    {
        int nearest = 0;
        float nearestSqrDist = float.MaxValue;
        Vector3 pos = transform.position;

        for (int i = 0; i < worldWaypoints.Length; i++)
        {
            float sqrDist = (worldWaypoints[i] - pos).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = i;
            }
        }

        return nearest;
    }

    /// <summary>获取指定索引巡逻点的等待时长。</summary>
    private float GetWaitTime(int index)
    {
        if (waitTimes != null && index < waitTimes.Length && waitTimes[index] > 0f)
            return waitTimes[index];
        return defaultWaitTime;
    }

    // ── 公开属性（供其他脚本查询）────────────────

    /// <summary>当前目标巡逻点索引（只读）。</summary>
    public int CurrentWaypointIndex => currentIndex;

    /// <summary>是否正在等待。</summary>
    public bool IsWaiting => isWaiting;

    /// <summary>当前世界空间巡逻点坐标。</summary>
    public Vector3 CurrentWaypointPosition =>
        worldWaypoints.Length > 0 ? worldWaypoints[currentIndex] : transform.position;




#if UNITY_EDITOR
    // ── 编辑器可视化 ──────────────────────────────

    private void OnValidate()
    {
        // 运行时 inspector 修改后即时刷新
        if (Application.isPlaying && hasStarted)
        {
            BuildWorldWaypoints();
        }
    }




    private void OnDrawGizmosSelected()
    {
        if (localWaypoints == null || localWaypoints.Length == 0)
            return;

        Vector3[] points;
        if (Application.isPlaying && worldWaypoints != null && worldWaypoints.Length == localWaypoints.Length)
        {
            points = worldWaypoints;
        }
        else
        {
            points = new Vector3[localWaypoints.Length];
            for (int i = 0; i < localWaypoints.Length; i++)
                points[i] = transform.TransformPoint(localWaypoints[i]);
        }

        // 连线 + 方向箭头
        for (int i = 0; i < points.Length; i++)
        {
            int next = (i + 1) % points.Length;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(points[i], points[next]);

            // 线段中点箭头
            Vector3 mid = (points[i] + points[next]) * 0.5f;
            Vector3 dir = (points[next] - points[i]).normalized;
            if (dir.sqrMagnitude > 0.001f)
                DrawArrowhead(mid, dir, 0.35f);
        }

        // 巡逻点球体
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.color = (i == 0) ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(points[i], 0.3f);
        }

        // 运行时高亮当前巡逻点
        if (Application.isPlaying && worldWaypoints != null && currentIndex < worldWaypoints.Length)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(worldWaypoints[currentIndex], 0.5f);
        }

        // 等待时长标签
        Handles.color = Color.white;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.LowerCenter,
        };
        for (int i = 0; i < points.Length; i++)
        {
            float wt = (waitTimes != null && i < waitTimes.Length && waitTimes[i] > 0f)
                ? waitTimes[i] : defaultWaitTime;
            Handles.Label(points[i] + Vector3.up * 0.55f, $"[{i}]  {wt:F1}s", labelStyle);
        }
    }

    private void DrawArrowhead(Vector3 pos, Vector3 direction, float size)
    {
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 150f, 0f) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 210f, 0f) * Vector3.forward;
        Gizmos.DrawRay(pos, right * size);
        Gizmos.DrawRay(pos, left * size);
    }
#endif
}
