using UnityEngine;
using System.Collections.Generic; // Dictionary를 사용하기 위해 추가

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
    [Tooltip("조준 시 캐릭터의 투명도입니다. (0: 완전 투명, 1: 완전 불투명)")]
    [SerializeField] [Range(0f, 1f)] private float transparentAlpha = 0.3f;
    [Tooltip("투명해지는 속도입니다.")]
    [SerializeField] private float fadeSpeed = 10f;

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
    
    // ▼▼▼ 수정된 부분: 여러 머티리얼을 관리하기 위한 Dictionary ▼▼▼
    private Dictionary<Material, Color> originalMaterials = new Dictionary<Material, Color>();
    // ▲▲▲ 수정된 부분 ▲▲▲
    
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

        // ▼▼▼ 수정된 부분: 자식 오브젝트의 모든 렌더러를 찾아 머티리얼 정보 저장 ▼▼▼
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // 각 렌더러의 모든 머티리얼을 순회하며 원본 색상 저장
            foreach (Material mat in renderer.materials)
            {
                if (!originalMaterials.ContainsKey(mat))
                {
                    originalMaterials.Add(mat, mat.color);
                }
            }
        }
        // ▲▲▲ 수정된 부분 ▲▲▲
    }

    private void Update()
    {
        // 상태 판정
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        isHittingHead = Physics.CheckSphere(headCheck.position, headRadius, groundMask);

        // 입력 처리
        GetInput();
        
        // 투명도 처리
        HandleTransparency();

        // 타이머 및 상태 초기화
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

        isAiming = Input.GetMouseButton(1);

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            isBoosting = false;
        }
        
        // ▼▼▼ 수정된 부분: 마우스 입력에 따라 렌더링 모드 즉시 변경 ▼▼▼
        if (Input.GetMouseButtonDown(1))
        {
            SetMaterialsTransparent(true);
        }

        if (Input.GetMouseButtonUp(1))
        {
            SetMaterialsTransparent(false);
        }
        // ▲▲▲ 수정된 부분 ▲▲▲
    }
    
    // ▼▼▼ 수정된 함수: 투명도 '값'만 부드럽게 변경 ▼▼▼
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
    // ▲▲▲ 수정된 함수 ▲▲▲
    
    // ▼▼▼ 새로 추가된 함수: 머티리얼의 렌더링 '모드'를 변경 ▼▼▼
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
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
                mat.color = originalColor; // 원래 색상으로 즉시 복구
            }
        }
    }
    // ▲▲▲ 새로 추가된 함수 ▲▲▲

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
        moveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;

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