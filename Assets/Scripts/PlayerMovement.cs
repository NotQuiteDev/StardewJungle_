using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    #region 변수 (Variables)

    [Header("이동 설정 (Movement Settings)")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airMultiplier = 0.8f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float moveSmoothTime = 0.05f;

    [Header("점프 설정 (Jump Settings)")]
    [SerializeField] private float jumpInitialSpeed = 6f;
    [SerializeField] private float boosterForce = 20f;
    [SerializeField] private float maxBoostTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("조준 시 투명화 (Aim Transparency)")]
    [SerializeField][Range(0f, 1f)] private float transparentAlpha = 0.3f;
    [SerializeField] private float fadeSpeed = 10f;

    [Header("지면 판정 (Ground Check)")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("머리 판정 (Head Check)")]
    [SerializeField] private Transform headCheck;
    [SerializeField] private float headRadius = 0.2f;

    // --- 내부 변수 ---
    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private bool isGrounded;
    private float boostTimeCounter;
    private Transform cameraTransform;
    private bool isBoosting;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool isHittingHead;
    private Vector3 velocityRef = Vector3.zero;
    private Dictionary<Material, Color> originalMaterials = new Dictionary<Material, Color>();

    private InputSystem_Actions playerControls;

    private InventoryManager inventoryManager;
    private InputAction attackAction;

    // --- 외부 제어용 변수 ---
    public bool canRotate = true;
    public bool isAiming { get; private set; }

    #endregion


    #region 유니티 생명주기 및 입력 시스템 이벤트

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraTransform = Camera.main.transform;
        playerControls = new InputSystem_Actions();

        inventoryManager = GetComponent<InventoryManager>();
        attackAction = playerControls.Player.Attack; // Attack 액션 찾아오기

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (!originalMaterials.ContainsKey(mat))
                {
                    originalMaterials.Add(mat, mat.color);
                }
            }
        }
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
        // ▼▼▼ 수정된 부분: Jump 액션의 performed와 canceled 이벤트를 모두 구독 ▼▼▼
        playerControls.Player.Jump.performed += OnJumpPerformed;
        playerControls.Player.Jump.canceled += OnJumpCanceled;
        // ▲▲▲ 수정된 부분 ▲▲▲
        // ★ 여기 추가: 홀드형 시작/종료
        attackAction.started += OnAttackStarted;
        attackAction.canceled += OnAttackCanceled;
        attackAction.performed += OnAttackPerformed;
        playerControls.Player.Focusing.started += OnFocusingStarted;
        playerControls.Player.Focusing.canceled += OnFocusingCanceled;
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
        // ▼▼▼ 수정된 부분: Jump 액션의 구독 해제 ▼▼▼
        playerControls.Player.Jump.performed -= OnJumpPerformed;
        playerControls.Player.Jump.canceled -= OnJumpCanceled;
        // ▲▲▲ 수정된 부분 ▲▲▲
        // ★ 여기 추가: 구독 해제
        attackAction.started -= OnAttackStarted;
        attackAction.canceled -= OnAttackCanceled;

        // (선택)
        attackAction.performed -= OnAttackPerformed;

        playerControls.Player.Focusing.started -= OnFocusingStarted;
        playerControls.Player.Focusing.canceled -= OnFocusingCanceled;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        // 1. 인벤토리 매니저에서 현재 들고 있는 아이템 정보를 가져옵니다.
        ItemData currentItem = inventoryManager.GetCurrentFocusedItem();

        // 2. 아이템이 존재하면, 그 아이템의 Use() 함수를 호출합니다.
        if (currentItem != null)
        {
            // Use 함수에 필요한 정보(장착 위치, 카메라 위치)를 넘겨줍니다.
            currentItem.Use(inventoryManager.equipPoint, cameraTransform);
        }
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        isHittingHead = Physics.CheckSphere(headCheck.position, headRadius, groundMask);

        ProcessInput();
        HandleTransparency();

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

        if (isHittingHead) isBoosting = false;
        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;
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

    private void ProcessInput()
    {
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpBufferCounter = jumpBufferTime;
    }

    // ▼▼▼ 추가된 함수: 점프 버튼을 뗐을 때 isBoosting을 false로 만듦 ▼▼▼
    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        isBoosting = false;
    }
    // ▲▲▲ 추가된 함수 ▲▲▲

    private void OnFocusingStarted(InputAction.CallbackContext context)
    {
        isAiming = true;
        SetMaterialsTransparent(true);
    }

    private void OnFocusingCanceled(InputAction.CallbackContext context)
    {
        isAiming = false;
        SetMaterialsTransparent(false);
    }

    private void HandleTransparency()
    {
        if (originalMaterials.Count == 0) return;
        float targetAlpha = isAiming ? transparentAlpha : 1f;
        foreach (Material mat in originalMaterials.Keys)
        {
            Color newColor = mat.color;
            float newAlpha = Mathf.Lerp(newColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
            newColor.a = newAlpha;
            mat.color = newColor;
        }
    }

    private void SetMaterialsTransparent(bool transparent)
    {
        foreach (KeyValuePair<Material, Color> entry in originalMaterials)
        {
            Material mat = entry.Key;
            Color originalColor = entry.Value;

            if (transparent)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = -1;
                mat.color = originalColor;
            }
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
        Vector3 camForward = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        Vector3 camRight = new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z).normalized;
        moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        if (canRotate)
        {
            Vector3 targetLookDirection = moveDirection;
            if (isAiming)
            {
                targetLookDirection = camForward;
            }
            if (targetLookDirection.magnitude >= 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetLookDirection, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
            }
        }

        float currentSpeed = isGrounded ? moveSpeed : moveSpeed * airMultiplier;
        Vector3 targetVelocity = moveDirection * currentSpeed;

        Vector3 smoothedVelocity = Vector3.SmoothDamp(
            new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z),
            new Vector3(targetVelocity.x, 0, targetVelocity.z),
            ref velocityRef,
            moveSmoothTime
        );

        rb.linearVelocity = new Vector3(smoothedVelocity.x, rb.linearVelocity.y, smoothedVelocity.z);
    }
    private void OnAttackStarted(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        ItemData item = inventoryManager.GetCurrentFocusedItem();
        if (item == null) return;
        item.BeginUse(inventoryManager.equipPoint, cameraTransform, this);
    }

    private void OnAttackCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        ItemData item = inventoryManager.GetCurrentFocusedItem();
        if (item != null) item.EndUse();

        // 워터링 정지 (있으면)
        var runtime = GetComponent<WateringCanRuntime>();
        if (runtime != null) runtime.StopWatering();
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

