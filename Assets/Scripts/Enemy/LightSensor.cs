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

    /// <summary>
    /// 供外部检测器（如 ProximityDetector）报告"玩家被察觉"。
    /// 调用后，本帧 UpdateStateMachine 会将此视为检测到光源。
    /// </summary>
    public void ReportDetection(Vector3 sourcePosition)
    {
        isReceivingLightThisFrame = true;
        currentFrameLightSourcePos = sourcePosition;
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
        // 1. ���²�������Ӱ����
        if (isReceivingLightThisFrame)
        {
            currentAwareness += buildUpRate * Time.deltaTime;
            lastKnownLightPos = currentFrameLightSourcePos;
        }
        else
        {
            bool isMovingToTarget = (currentState == State.Investigate && (agent.pathPending || agent.remainingDistance > 1f));

            // ֻ���ڡ����ܶ�׷�١�������£������������˥��
            if (!isMovingToTarget)
            {
                currentAwareness -= decayRate * Time.deltaTime;
            }
        }
        currentAwareness = Mathf.Clamp(currentAwareness, 0, 100);

        // 2. ״̬��ת�߼�
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
                    agent.isStopped = true; 

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




    void OnDrawGizmosSelected()
    {
        if (eyeTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Gizmos.color = Color.red;
        Vector3 leftBoundary = Quaternion.Euler(0, -fovAngle / 2, 0) * eyeTransform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fovAngle / 2, 0) * eyeTransform.forward;

        Gizmos.DrawRay(eyeTransform.position, leftBoundary * viewRadius);
        Gizmos.DrawRay(eyeTransform.position, rightBoundary * viewRadius);

        if (currentState == State.Investigate)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, lastKnownLightPos);
            Gizmos.DrawWireSphere(lastKnownLightPos, 0.5f); 
        }
    }
}