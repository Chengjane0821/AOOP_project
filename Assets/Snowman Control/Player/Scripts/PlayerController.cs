using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SnowmanController : MonoBehaviour
{
    [Header("References")]
    public GameObject head;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float rotationSpeed = 10f;

    [Header("Jump")]
    [SerializeField]
    private float jumpForce = 6f;

    [Header("Ground Check")]
    [SerializeField]
    private float groundCheckDistance = 0.25f;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector2 moveInput;

    private bool canJump;
    private bool jumpRequested;


    // =========================
    // Start
    // =========================

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // 不讓雪人倒下
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        canJump = false;
    }


    // =========================
    // Update
    // =========================

    private void Update()
    {
        ReadInput();
        CheckGround();
    }


    // =========================
    // FixedUpdate
    // =========================

    private void FixedUpdate()
    {
        Move();

        if (jumpRequested && canJump)
        {
            Jump();
        }

        jumpRequested = false;
    }


    // =========================
    // Input
    // =========================

    private void ReadInput()
    {
        if (Keyboard.current == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        // W / S
        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        // A / D
        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;


        moveInput = new Vector2(
            horizontal,
            vertical
        );

        // 防止斜走速度變快
        moveInput = Vector2.ClampMagnitude(
            moveInput,
            1f
        );


        // Space
        if (
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            canJump
        )
        {
            jumpRequested = true;
        }
    }


    // =========================
    // Movement
    // =========================

    private void Move()
    {
        if (Camera.main == null)
            return;


        // Camera 的前方
        Vector3 cameraForward =
            Camera.main.transform.forward;

        // Camera 的右方
        Vector3 cameraRight =
            Camera.main.transform.right;


        // 不考慮 Camera 上下角度
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();


        // 計算移動方向
        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;


        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();


            // =====================
            // 移動
            // =====================

            Vector3 movement =
                moveDirection *
                moveSpeed *
                Time.fixedDeltaTime;

            rb.MovePosition(
                rb.position + movement
            );


            // =====================
            // 角色轉向
            // =====================

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection
                );

            Quaternion newRotation =
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.fixedDeltaTime
                );

            rb.MoveRotation(newRotation);


            // 如果 Head 有指定
            if (head != null)
            {
                head.transform.rotation =
                    Quaternion.Slerp(
                        head.transform.rotation,
                        targetRotation,
                        rotationSpeed *
                        Time.fixedDeltaTime
                    );
            }
        }
    }


    // =========================
    // Jump
    // =========================

    private void Jump()
    {
        canJump = false;


        // 先把目前垂直速度清掉
        Vector3 velocity =
            rb.linearVelocity;

        velocity.y = 0f;

        rb.linearVelocity = velocity;


        // 往上跳
        rb.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse
        );
    }


    // =========================
    // Ground Check
    // =========================

    private void CheckGround()
    {
        Vector3 center =
            transform.TransformPoint(
                capsule.center
            );


        float bottom =
            capsule.height / 2f -
            capsule.radius;


        Vector3 origin =
            center +
            Vector3.down * bottom;


        canJump = Physics.SphereCast(
            origin,
            capsule.radius * 0.8f,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );
    }
}