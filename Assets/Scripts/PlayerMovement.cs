using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    #region 변수 (Variables)

    [Header("이동 설정 (Movement Settings)")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airMultiplier = 0.8f;
    [SerializeField] private float rotationSpeed = 15f;
    [Tooltip("캐릭터의 움직임이 목표 속도에 도달하는 데 걸리는 시간입니다. 작을수록 빠릿합니다.")]
    [SerializeField] private float moveSmoothTime = 0.05f;

    [Header("점프 설정 (Jump Settings)")]
    [Tooltip("점프 버튼을 누르는 즉시 적용될 초기 수직 속도입니다.")]
    [SerializeField] private float jumpInitialSpeed = 6f;
    [Tooltip("점프 버튼을 누르고 있는 동안 추가로 가해지는 부스터 힘입니다.")]
    [SerializeField] private float boosterForce = 20f;
    [Tooltip("부스터 힘이 최대로 지속될 수 있는 시간입니다.")]
    [SerializeField] private float maxBoostTime = 0.2f;
    [Tooltip("착지 직전 점프 입력을 기억해주는 시간입니다. (점프 버퍼링)")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [Tooltip("절벽에서 떨어진 직후 점프 입력을 허용하는 시간입니다. (코요테 타임)")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("지면 판정 (Ground Check)")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("머리 판정 (Head Check)")]
    [SerializeField] private Transform headCheck;
    [SerializeField] private float headRadius = 0.2f;

    // 내부 변수
    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private bool isGrounded;
    private float boostTimeCounter;
    private Transform cameraTransform;
    private bool isBoosting;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool isHittingHead;
    private Vector3 velocityRef = Vector3.zero;
    
    // 외부 제어용 변수
    public bool canRotate = true;
    public bool isAiming = false;

    #endregion


    #region 유니티 생명주기 함수 (Unity Lifecycle Methods)

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        isHittingHead = Physics.CheckSphere(headCheck.position, headRadius, groundMask);

        if (isGrounded && rb.linearVelocity.y < 0.1f)
        {
            isBoosting = false;
            boostTimeCounter = maxBoostTime;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isHittingHead)
        {
            isBoosting = false;
        }

        GetInput();

        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpInitialSpeed, rb.linearVelocity.z);
            isBoosting = true;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        MovePlayer();
        HandleJump();
    }

    #endregion


    #region 커스텀 함수 (Custom Methods)

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            isBoosting = false;
        }
    }

    private void HandleJump()
    {
        if (isBoosting && boostTimeCounter > 0)
        {
            rb.AddForce(Vector3.up * boosterForce, ForceMode.Force);
            boostTimeCounter -= Time.fixedDeltaTime;
        }
        else
        {
            isBoosting = false;
        }
    }
    
    private void MovePlayer()
    {
        // 1. 목표 이동 방향 계산 (카메라 기준)
        Vector3 camForward = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        Vector3 camRight = new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z).normalized;
        moveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;

        // 2. 회전 처리
        if (isAiming && canRotate)
        {
            // 조준 시에는 카메라 정면을 즉시 바라봄 (기존 로직 유지)
            if (camForward != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(camForward);
            }
        }
        // 이동 입력이 있고, 회전이 가능한 상태일 때 (조준 중이 아닐 때)
        else if (moveDirection.magnitude >= 0.1f && canRotate)
        {
            // ★★★★★★★★★★ 핵심 수정 ★★★★★★★★★★
            // 바라보는 방향을 실제 이동 방향(moveDirection)이 아닌,
            // 항상 카메라의 정면(camForward)으로 설정합니다.
            Quaternion targetRotation = Quaternion.LookRotation(camForward, Vector3.up);
            // ★★★★★★★★★★★★★★★★★★★★★★★★★

            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
        }

        // 3. 목표 속도 계산 (이하 기존 코드와 동일)
        float currentSpeed = isGrounded ? moveSpeed : moveSpeed * airMultiplier;
        Vector3 targetVelocity = moveDirection * currentSpeed;

        // 4. SmoothDamp를 사용하여 부드러운 속도 변경
        Vector3 smoothedVelocity = Vector3.SmoothDamp(
            new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z),
            new Vector3(targetVelocity.x, 0, targetVelocity.z),
            ref velocityRef,
            moveSmoothTime
        );

        // 5. 최종 속도를 Rigidbody에 적용
        rb.linearVelocity = new Vector3(smoothedVelocity.x, rb.linearVelocity.y, smoothedVelocity.z);
    }

    #endregion

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
        if (headCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(headCheck.position, headRadius);
        }
    }
    #endif
}