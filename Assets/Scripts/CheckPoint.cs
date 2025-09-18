using UnityEngine;

// 이 스크립트가 붙은 오브젝트는 반드시 Collider가 있어야 함
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("플레이어 부활 위치")]
    [Tooltip("이 체크포인트에 해당하는 부활 지점입니다. 비워두면 이 오브젝트의 위치를 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Header("옵션")]
    [Tooltip("체크하면 한 번만 활성화됩니다.")]
    [SerializeField] private bool activateOnce = true;

    [Header("색상 설정")]
    [Tooltip("비활성화 상태일 때의 색상입니다.")]
    [SerializeField] private Color deactivatedColor = Color.yellow;
    [Tooltip("활성화된 후의 색상입니다.")]
    [SerializeField] private Color activatedColor = Color.green;

    private bool hasBeenActivated = false;
    private Renderer objectRenderer; 

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
        }

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
            if (objectRenderer == null)
            {
                Debug.LogWarning(gameObject.name + " 에서 Renderer 컴포넌트를 찾을 수 없습니다! 색상을 변경할 수 없습니다.");
            }
        }
    }

    private void Start()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = deactivatedColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activateOnce && hasBeenActivated)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerCheckpoint playerCheckpoint = other.GetComponent<PlayerCheckpoint>();

            if (playerCheckpoint != null)
            {
                playerCheckpoint.SetNewCheckpoint(spawnPoint);
                hasBeenActivated = true;
                
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = activatedColor;
                }
                
                Debug.Log(gameObject.name + " 체크포인트가 활성화되었습니다.");
            }
        }
    }

    // ▼▼▼ 씬(Scene) 뷰에 방향 안내 기즈모(Gizmo)를 그리는 함수 (이 부분은 유지) ▼▼▼
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            // 스폰 위치에 반투명한 파란색 구를 그립니다.
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);

            // 스폰 방향을 가리키는 파란색 화살표를 그립니다.
            Gizmos.color = Color.blue;
            Vector3 from = spawnPoint.position;
            Vector3 to = spawnPoint.position + spawnPoint.forward * 1.5f; // 화살표 길이 1.5
            Gizmos.DrawLine(from, to);
            
            // 화살표 머리 부분을 그립니다.
            Vector3 right = Quaternion.LookRotation(spawnPoint.forward) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(spawnPoint.forward) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
            Gizmos.DrawRay(to, right * 0.5f);
            Gizmos.DrawRay(to, left * 0.5f);
        }
    }
#endif

    // CycleSpawnDirection() 함수는 삭제되었습니다.
}