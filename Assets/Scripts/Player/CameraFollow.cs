using UnityEngine;

/// <summary>
/// 3D top-down camera — follows the player with configurable offset and smoothing.
/// Per design doc: "3D俯视角" (3D top-down perspective).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0, 15f, -8f);
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Rotation")]
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private float lookAngle = 55f; // tilt angle for perspective

    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }

        // Apply initial position immediately (no lerp on first frame)
        if (target != null)
        {
            transform.position = target.position + offset;
            if (lookAtTarget)
            {
                Quaternion rot = Quaternion.Euler(lookAngle, 0f, 0f);
                transform.rotation = rot;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / smoothSpeed);

        if (lookAtTarget)
        {
            // Keep a consistent angled top-down view
            Quaternion targetRotation = Quaternion.Euler(lookAngle, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
        }
    }
}
