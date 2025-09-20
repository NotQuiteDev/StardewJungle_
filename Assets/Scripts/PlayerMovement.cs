using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    #region 변수 (Variables)

    // ## 추가된 부분: 움직임 잠금 상태 ##
    private bool isMovementLocked = false;

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
    private bool isAttacking = false;

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
        attackAction = playerControls.Player.Attack;

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
        playerControls.Player.Jump.performed += OnJumpPerformed;
        playerControls.Player.Jump.canceled += OnJumpCanceled;
        attackAction.started += OnAttackStarted;
        attackAction.canceled += OnAttackCanceled;
        attackAction.performed += OnAttackPerformed;
        playerControls.Player.Focusing.started += OnFocusingStarted;
        playerControls.Player.Focusing.canceled += OnFocusingCanceled;
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
        playerControls.Player.Jump.performed -= OnJumpPerformed;
        playerControls.Player.Jump.canceled -= OnJumpCanceled;
        attackAction.started -= OnAttackStarted;
        attackAction.canceled -= OnAttackCanceled;
        attackAction.performed -= OnAttackPerformed;
        playerControls.Player.Focusing.started -= OnFocusingStarted;
        playerControls.Player.Focusing.canceled -= OnFocusingCanceled;
    }

    private void Update()
    {
        if (isMovementLocked) return; // 잠금 상태면 실행 중지

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        isHittingHead = Physics.CheckSphere(headCheck.position, headRadius, groundMask);

        ProcessInput();
        HandleTransparency(); // <<< 이 함수가 누락되었었습니다.

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

        if (!attackAction.IsPressed())
        {
            var hoeRuntime = GetComponent<TillingHoeRuntime>();
            if (hoeRuntime != null) hoeRuntime.StopTilling();
        }
    }

    private void FixedUpdate()
    {
        if (isMovementLocked) return; // 잠금 상태면 실행 중지

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpInitialSpeed, rb.linearVelocity.z);
            isBoosting = true;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        MovePlayer(); // <<< 이 함수가 누락되었었습니다.
        HandleJump(); // <<< 이 함수가 누락되었었습니다.
    }

    #endregion


    #region 커스텀 함수 (Custom Methods)

    private void ProcessInput()
    {
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();
    }
    
    // ==========================================================
    // ## 아래에 누락되었던 함수 정의들이 모두 포함되었습니다. ##
    // ==========================================================
    
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (isMovementLocked) return;
        ItemData currentItem = inventoryManager.GetCurrentFocusedItem();
        if (currentItem == null) return;
        if (currentItem is WateringCanData || currentItem is TillingHoeData) return;
        currentItem.Use(inventoryManager.equipPoint, cameraTransform);
    }
    
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isMovementLocked) return;
        jumpBufferCounter = jumpBufferTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (isMovementLocked) return;
        isBoosting = false;
    }

    private void OnFocusingStarted(InputAction.CallbackContext context)
    {
        if (isMovementLocked) return;
        isAiming = true;
        SetMaterialsTransparent(true); // <<< 이 함수가 누락되었었습니다.
    }

    private void OnFocusingCanceled(InputAction.CallbackContext context)
    {
        if (isMovementLocked) return;
        isAiming = false;
        SetMaterialsTransparent(false); // <<< 이 함수가 누락되었었습니다.
    }
    
    private void OnAttackStarted(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (isMovementLocked) return;
        isAttacking = true;
        ItemData item = inventoryManager.GetCurrentFocusedItem();
        if (item == null) return;
        item.BeginUse(inventoryManager.equipPoint, cameraTransform, this);
    }

    private void OnAttackCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (isMovementLocked) return;
        isAttacking = false;
        ItemData item = inventoryManager.GetCurrentFocusedItem();
        if (item != null) item.EndUse();
        var waterRuntime = GetComponent<WateringCanRuntime>();
        if (waterRuntime != null) waterRuntime.StopWatering();
        var hoeRuntime = GetComponent<TillingHoeRuntime>();
        if (hoeRuntime != null) hoeRuntime.StopTilling();
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
        Vector3 camRight   = new Vector3(cameraTransform.right.x,  0f, cameraTransform.right.z).normalized;
        
        moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        if (canRotate)
        {
            Vector3 targetLookDirection = (isAiming || isAttacking) ? camForward : moveDirection;
            if (targetLookDirection.sqrMagnitude >= 0.01f)
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

    #endregion

    #region 외부 제어 함수 (Public Control Methods)

    public void LockMovement()
    {
        isMovementLocked = true;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        moveInput = Vector2.zero;
        isAttacking = false;
        isAiming = false;
    }

    public void UnlockMovement()
    {
        isMovementLocked = false;
        
        if (rb != null)
        {
            rb.isKinematic = false;
        }
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