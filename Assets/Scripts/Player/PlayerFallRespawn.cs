using UnityEngine;

/// <summary>
/// 플레이어가 특정 Y좌표 이하로 떨어졌을 때 지정된 위치로 리스폰시키는 스크립트.
/// 플레이어 GameObject에 붙여주세요.
/// </summary>
public class PlayerFallRespawn : MonoBehaviour
{
    [Header("리스폰 설정")]
    [Tooltip("플레이어가 리스폰될 위치입니다. 빈 오브젝트를 생성하여 지정해주세요.")]
    [SerializeField] private Transform respawnPoint;

    [Tooltip("이 Y좌표보다 아래로 떨어지면 리스폰됩니다.")]
    [SerializeField] private float fallThresholdY = -20f;

    private Rigidbody rb; // PlayerMovement 스크립트가 Rigidbody를 사용하므로 캐싱합니다.

    private void Awake()
    {
        // 플레이어의 Rigidbody 컴포넌트를 미리 찾아둡니다.
        rb = GetComponent<Rigidbody>();

        if (respawnPoint == null)
        {
            Debug.LogError("리스폰 위치(Respawn Point)가 지정되지 않았습니다! 이 스크립트가 동작하려면 꼭 설정해주세요.", this.gameObject);
        }
    }

    void Update()
    {
        // 리스폰 지점이 설정되었고, 플레이어가 추락 기준점보다 아래로 떨어졌는지 확인합니다.
        if (respawnPoint != null && transform.position.y < fallThresholdY)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        // 1. 물리적 움직임(속도)을 즉시 멈춰서 추가적인 문제를 방지합니다.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. 지정된 리스폰 위치로 플레이어를 즉시 이동시킵니다.
        transform.position = respawnPoint.position;
        
        // 3. (선택 사항) 리스폰 지점의 회전값으로 플레이어의 방향도 초기화합니다.
        transform.rotation = respawnPoint.rotation;

        Debug.Log("플레이어가 추락하여 리스폰 지점으로 이동했습니다.");
    }
}