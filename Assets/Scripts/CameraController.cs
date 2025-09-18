using UnityEngine;
using UnityEngine.InputSystem; // ▼▼▼ Input System 사용을 위해 추가 ▼▼▼

public class CameraController : MonoBehaviour
{
    [Header("타겟 설정 (Target Settings)")]
    [SerializeField] private Transform target; // ▼▼▼ 직접 연결 방식으로 변경 ▼▼▼

    [Header("카메라 설정 (Camera Settings)")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, -5f);
    [SerializeField] private float mouseSensitivity = 100f;
    
    [Header("조준 시 설정 (Aiming Settings)")]
    [Tooltip("우클릭 조준 시 적용될 카메라 오프셋입니다.")]
    [SerializeField] private Vector3 aimOffset = new Vector3(0.7f, 1.8f, -2.5f);
    [Tooltip("일반 모드와 조준 모드 간의 전환 속도입니다.")]
    [SerializeField] private float transitionSpeed = 15f;

    [Header("카메라 충돌 설정 (Collision Settings)")]
    [Tooltip("카메라가 충돌을 감지할 레이어들을 선택합니다. 'Player' 레이어는 제외해야 합니다.")]
    [SerializeField] private LayerMask collisionLayers; 
    [Tooltip("충돌 감지에 사용할 가상의 구 반지름입니다. 카메라가 벽에서 살짝 떨어지게 합니다.")]
    [SerializeField] private float collisionRadius = 0.2f; 

    [Header("시야각 제한 (Pitch Clamp)")]
    [SerializeField] private float minY = -30f;
    [SerializeField] private float maxY = 60f;

    // --- 내부 변수 ---
    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 currentOffset;
    private bool isAiming = false; // ▼▼▼ 조준 상태를 저장할 변수

    // ▼▼▼ Input System 관련 변수 추가 ▼▼▼
    private InputSystem_Actions playerControls;
    private Vector2 lookInput;

    private void Awake()
    {
        // Input Actions 인스턴스 생성
        playerControls = new InputSystem_Actions();

        if (target == null)
        {
            Debug.LogError("타겟이 Inspector에 연결되지 않았습니다!", this.gameObject);
        }
    }

    private void OnEnable()
    {
        // Player 액션 맵 활성화 및 이벤트 구독
        playerControls.Player.Enable();
        playerControls.Player.Focusing.started += OnFocusingStarted;
        playerControls.Player.Focusing.canceled += OnFocusingCanceled;
    }

    private void OnDisable()
    {
        // Player 액션 맵 비활성화 및 이벤트 구독 해제
        playerControls.Player.Disable();
        playerControls.Player.Focusing.started -= OnFocusingStarted;
        playerControls.Player.Focusing.canceled -= OnFocusingCanceled;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
        
        currentOffset = offset;
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        // ▼▼▼ Look 액션 값 읽기 ▼▼▼
        lookInput = playerControls.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        // ▲▲▲ 수정된 부분 ▲▲▲

        rotationX += mouseX;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        transform.rotation = rotation;
    }

    private void HandlePosition()
    {
        // ▼▼▼ isAiming 변수를 사용하여 목표 오프셋 결정 ▼▼▼
        Vector3 targetOffset = isAiming ? aimOffset : offset;
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * transitionSpeed);
        // ▲▲▲ 수정된 부분 ▲▲▲

        Vector3 desiredPosition = target.position + transform.rotation * currentOffset;
        
        Vector3 castOrigin = target.position;
        Vector3 castDirection = (desiredPosition - castOrigin).normalized;
        float castDistance = Vector3.Distance(castOrigin, desiredPosition);

        RaycastHit hit;
        if (Physics.SphereCast(castOrigin, collisionRadius, castDirection, out hit, castDistance, collisionLayers))
        {
            transform.position = hit.point + hit.normal * collisionRadius;
        }
        else
        {
            transform.position = desiredPosition;
        }
    }
    
    // ▼▼▼ Input System 이벤트 핸들러 함수들 ▼▼▼
    private void OnFocusingStarted(InputAction.CallbackContext context)
    {
        isAiming = true;
    }

    private void OnFocusingCanceled(InputAction.CallbackContext context)
    {
        isAiming = false;
    }
    // ▲▲▲ 추가된 부분 ▲▲▲
    
    public void SetRotation(Quaternion newRotation)
    {
        Vector3 angles = newRotation.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }
}
