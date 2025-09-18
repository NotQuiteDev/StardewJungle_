using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("타겟 설정 (Target Settings)")]
    [SerializeField] private string targetTag = "Player";
    private Transform target;

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

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 currentOffset; // ▼▼▼ 추가된 부분: 부드러운 전환을 위한 현재 오프셋 변수

    void Start()
    {
        GameObject targetObject = GameObject.FindWithTag(targetTag);
        if (targetObject != null)
        {
            target = targetObject.transform;
        }
        else
        {
            Debug.LogError("'" + targetTag + "' 태그를 가진 타겟을 찾을 수 없습니다! 플레이어 오브젝트에 태그를 설정했는지 확인하세요.");
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
        
        currentOffset = offset; // ▼▼▼ 추가된 부분: 현재 오프셋 초기화
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleRotation();
        HandlePosition();
    }

    /// <summary>
    /// 마우스 입력에 따른 카메라 회전을 처리합니다.
    /// </summary>
    void HandleRotation()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationX += mouseX;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        transform.rotation = rotation;
    }

    /// <summary>
    /// 타겟 추적 및 카메라 충돌을 포함한 위치를 처리합니다.
    /// </summary>
    void HandlePosition()
    {
        // ▼▼▼ 수정된 부분: 우클릭 상태에 따라 목표 오프셋 결정 ▼▼▼
        Vector3 targetOffset = Input.GetMouseButton(1) ? aimOffset : offset;
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
    
    public void SetRotation(Quaternion newRotation)
    {
        Vector3 angles = newRotation.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }
}