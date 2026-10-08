using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform head;

    [SerializeField]
    private Transform playerBody;

    [SerializeField]
    private Transform cameraTransform;


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

        // Player 本體保持直立
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


        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;


        moveInput = new Vector2(
            horizontal,
            vertical
        );

        moveInput = Vector2.ClampMagnitude(
            moveInput,
            1f
        );


        // Jump
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
        if (cameraTransform == null)
            return;


        // Camera 的前方
        Vector3 cameraForward =
            cameraTransform.forward;

        // Camera 的右方
        Vector3 cameraRight =
            cameraTransform.right;


        // RPG 移動只考慮水平面
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();


        // 根據 Camera 方向計算 WASD 移動方向
        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;


        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();


            // =====================
            // 移動 Player
            // =====================

            Vector3 movement =
                moveDirection *
                moveSpeed *
                Time.fixedDeltaTime;

            rb.MovePosition(
                rb.position + movement
            );


            // =====================
            // 下半身轉向
            // =====================

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection
                );


            if (playerBody != null)
            {
                playerBody.rotation =
                    Quaternion.Slerp(
                        playerBody.rotation,
                        targetRotation,
                        rotationSpeed *
                        Time.fixedDeltaTime
                    );
            }


            // =====================
            // 頭部轉向
            // 保留原本功能
            // =====================

            if (head != null)
            {
                head.rotation =
                    Quaternion.Slerp(
                        head.rotation,
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


        Vector3 velocity =
            rb.linearVelocity;

        velocity.y = 0f;

        rb.linearVelocity = velocity;


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