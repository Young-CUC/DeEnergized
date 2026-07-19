using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController controller;
    private Camera mainCamera;

    // 垂直速度（持久累积，避免每帧重建导致重力丢失）
    private float verticalVelocity;

    // �� �����ƶ����붯��
    private InputAction moveAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        // ��ʼ���ƶ����룺�� WASD Ϊ 2D ����
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    private void Update()
    {
        Move();
        AimAtMouse();
    }

    private void Move()
    {
        //  WASD 
        Vector2 input = moveAction.ReadValue<Vector2>();

        //3D 
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        Vector3 velocity = direction * moveSpeed;

        // 垂直速度：着地时维持一个微小的向下力确保持续贴地
        // 否则 CharacterController.Move(Vector3.zero) 可能不触发 isGrounded 重检测
        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -0.1f; // 贴地力，保证稳定着地
        }
        else
        {
            verticalVelocity -= 9.81f * Time.deltaTime; // 累积重力
        }

        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void AimAtMouse()
    {
        // ȷ������豸����
        if (Mouse.current == null) return;

        // ��ȡ��ǰ�������Ļ�ϵ�λ��
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            Vector3 lookDirection = new Vector3(point.x, transform.position.y, point.z);
            transform.LookAt(lookDirection);
        }
    }
}