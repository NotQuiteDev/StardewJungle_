using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("타겟 설정 (Target Settings)")]
    [SerializeField] private string targetTag = "Player";
    private Transform target;

    [Header("카메라 설정 (Camera Settings)")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, -5f);
    [SerializeField] private float mouseSensitivity = 100f;
    
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
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationX += mouseX;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        
        Vector3 desiredPosition = target.position + rotation * offset;
        
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
        
        transform.rotation = rotation;
    }

    // ▼▼▼ 추가된 부분: 외부에서 카메라의 회전값을 강제로 설정하는 함수 ▼▼▼
    /// <summary>
    /// 카메라의 회전값을 새로운 회전값으로 즉시 설정합니다. (주로 플레이어 리스폰 시 호출됨)
    /// </summary>
    /// <param name="newRotation">새롭게 적용될 회전값</param>
    public void SetRotation(Quaternion newRotation)
    {
        // Quaternion을 오일러 각(Euler Angles)으로 변환하여
        // 이 스크립트가 사용하는 rotationX, rotationY 변수에 업데이트합니다.
        Vector3 angles = newRotation.eulerAngles;
        rotationX = angles.y; // 좌우 회전 (Yaw)
        rotationY = angles.x; // 상하 회전 (Pitch)
    }
}