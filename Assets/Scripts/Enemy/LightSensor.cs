using UnityEngine;
using UnityEngine.AI;

public class LightSensor : MonoBehaviour
{
    public enum State { Patrol, Suspicious, Investigate }
    public State currentState = State.Patrol;

    [Header("Perception Settings")]
    public Transform eyeTransform;
    public float viewRadius = 15f;
    public float fovAngle = 120f;
    public LayerMask obstacleMask;

    [Header("Awareness Meter")]
    public float currentAwareness = 0f;
    public float buildUpRate = 200f;
    public float decayRate = 100f;

    private NavMeshAgent agent;
    private Vector3 lastKnownLightPos;
    private bool isReceivingLightThisFrame = false;
    private Vector3 currentFrameLightSourcePos;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("LightTrigger"))
        {
            LightStimulus stim = other.GetComponent<LightStimulus>();
            if (stim != null && CheckLineOfSight(stim.sourcePosition, eyeTransform.position))
            {
                isReceivingLightThisFrame = true;
                currentFrameLightSourcePos = stim.sourcePosition;
            }
        }
    }

    void Update()
    {
        CheckVisualNodes();
        UpdateStateMachine();
        isReceivingLightThisFrame = false;
    }

    void CheckVisualNodes()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius);
        foreach (Collider target in targetsInViewRadius)
        {
            if (target.TryGetComponent<LightSpotMarker>(out _))
            {
                Vector3 dirToTarget = (target.transform.position - eyeTransform.position).normalized;
                if (Vector3.Angle(eyeTransform.forward, dirToTarget) < fovAngle / 2)
                {
                    if (CheckLineOfSight(eyeTransform.position, target.transform.position))
                    {
                        isReceivingLightThisFrame = true;
                        currentFrameLightSourcePos = target.GetComponent<LightStimulus>().sourcePosition;
                        return;
                    }
                }
            }
        }
    }

    bool CheckLineOfSight(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (Physics.Raycast(start, dir.normalized, dist, obstacleMask))
        {
            return false;
        }
        return true;
    }

    void UpdateStateMachine()
    {
        // 1. 更新察觉条与残影坐标
        if (isReceivingLightThisFrame)
        {
            currentAwareness += buildUpRate * Time.deltaTime;
            lastKnownLightPos = currentFrameLightSourcePos;
        }
        else
        {
            bool isMovingToTarget = (currentState == State.Investigate && (agent.pathPending || agent.remainingDistance > 1f));

            // 只有在“非跑动追踪”的情况下，察觉条才允许衰减
            if (!isMovingToTarget)
            {
                currentAwareness -= decayRate * Time.deltaTime;
            }
        }
        currentAwareness = Mathf.Clamp(currentAwareness, 0, 100);

        // 2. 状态流转逻辑
        switch (currentState)
        {
            case State.Patrol:
                if (isReceivingLightThisFrame)
                {
                    currentState = State.Suspicious;
                    agent.isStopped = true;
                }
                break;

            case State.Suspicious:
                if (currentAwareness >= 100f)
                {
                    currentState = State.Investigate;
                    agent.isStopped = false;
                }
                else if (currentAwareness <= 0f)
                {
                    currentState = State.Patrol;
                    agent.isStopped = false;
                }
                break;

            case State.Investigate:
                agent.SetDestination(lastKnownLightPos);

                if (!isReceivingLightThisFrame && !agent.pathPending && agent.remainingDistance < 1f)
                {
                    agent.isStopped = true; // 到达残影位置，停下脚步四处张望

                    // 因为上面的衰减逻辑已经允许衰减，察觉条会开始下降
                    // 此时顺势切回“怀疑”状态，让它在原地把察觉条扣完
                    if (currentAwareness < 100f)
                    {
                        currentState = State.Suspicious;
                    }
                }
                else
                {
                    agent.isStopped = false; 
                }
                break;
        }
    }
    // 绘制怪物视野辅助线
    void OnDrawGizmosSelected()
    {
        if (eyeTransform == null) return;

        // 1. 画出最大视野半径 (黄色圆圈)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 2. 画出 FOV 视锥夹角 (红色射线)
        // 利用四元数旋转计算出左右边界的向量
        Gizmos.color = Color.red;
        Vector3 leftBoundary = Quaternion.Euler(0, -fovAngle / 2, 0) * eyeTransform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fovAngle / 2, 0) * eyeTransform.forward;

        Gizmos.DrawRay(eyeTransform.position, leftBoundary * viewRadius);
        Gizmos.DrawRay(eyeTransform.position, rightBoundary * viewRadius);

        // 3. 如果处于追踪状态，画出一条指向“残影坐标”的线 (绿色)
        if (currentState == State.Investigate)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, lastKnownLightPos);
            Gizmos.DrawWireSphere(lastKnownLightPos, 0.5f); // 在残影位置画个小球
        }
    }
}