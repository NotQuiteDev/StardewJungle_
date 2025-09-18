using UnityEngine;

public enum MoveAxis { X, Y, Z }
public enum MovementMode { Continuous, Triggered }

[RequireComponent(typeof(Collider))]
public class MovingHazard : MonoBehaviour
{
    [Header("작동 방식 선택")]
    [Tooltip("Continuous: 계속 왕복 운동 / Triggered: 지정된 트리거에 의해서만 한 번 작동")]
    [SerializeField] private MovementMode mode = MovementMode.Continuous;

    [Header("이동 설정")]
    [Tooltip("오브젝트가 왕복 운동할 축을 선택합니다.")]
    [SerializeField] private MoveAxis axis = MoveAxis.Z;
    [SerializeField] private float moveDistance = 5f;
    [SerializeField] private float speed = 2f;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool isMoving = false;
    private float moveTimer = 0f; // 진행도(progress) 대신 시간(timer)으로 더 명확하게 관리

    private void Start()
    {
        startPoint = transform.position;
        endPoint = startPoint + GetDirectionVector() * moveDistance;

        // ▼▼▼ 추가점: 값이 잘못 설정되었을 때 경고를 띄워주는 안전장치 ▼▼▼
        if (mode == MovementMode.Triggered && (speed <= 0 || moveDistance <= 0))
        {
            Debug.LogError(gameObject.name + "의 MovingHazard: Triggered 모드에서는 Speed와 Move Distance가 0보다 커야 합니다!", gameObject);
            // 에디터를 멈추고 싶다면 아래 줄의 주석을 해제하세요.
            // UnityEditor.EditorApplication.isPlaying = false; 
        }
    }

    private void Update()
    {
        if (mode == MovementMode.Continuous)
        {
            transform.position = Vector3.Lerp(startPoint, endPoint, Mathf.PingPong(Time.time * speed, 1f));
        }
        else // mode == MovementMode.Triggered
        {
            if (isMoving)
            {
                // 이동에 걸리는 총 시간 계산
                float duration = moveDistance / speed;

                // 타이머 증가
                moveTimer += Time.deltaTime;

                // 현재 진행도 (시간 / 총 시간) 계산
                float progress = moveTimer / duration;

                transform.position = Vector3.Lerp(startPoint, endPoint, progress);

                // 만약 이동이 끝났다면
                if (moveTimer >= duration)
                {
                    transform.position = startPoint;
                    isMoving = false;
                }
            }
        }
    }

    public void Activate()
    {
        if (mode != MovementMode.Triggered) return;
        
        transform.position = startPoint;
        moveTimer = 0f; // 타이머 초기화
        isMoving = true;

        Debug.Log(gameObject.name + "이(가) 트리거에 의해 활성화되었습니다.");
    }

    // (이 아래 부분은 변경 없음)
    #region 기존 함수들
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCheckpoint player = other.GetComponent<PlayerCheckpoint>();
            if (player != null)
            {
                player.Respawn();
                Debug.Log("플레이어가 위험 요소(" + gameObject.name + ")에 닿아 리스폰됩니다.");
            }
        }
    }

    private Vector3 GetDirectionVector()
    {
        switch (axis)
        {
            case MoveAxis.X: return Vector3.right;
            case MoveAxis.Y: return Vector3.up;
            case MoveAxis.Z: return Vector3.forward;
            default: return Vector3.forward;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 gizmoStartPoint = Application.isPlaying ? startPoint : transform.position;
        Vector3 gizmoEndPoint = gizmoStartPoint + GetDirectionVector() * moveDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoStartPoint, 0.3f);
        Gizmos.DrawWireSphere(gizmoEndPoint, 0.3f);
        Gizmos.DrawLine(gizmoStartPoint, gizmoEndPoint);
    }
#endif
    #endregion
}