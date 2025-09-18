using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Fireball : MonoBehaviour
{
    [Header("유도탄 설정")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody rb;
    private Transform player;
    private EnemyHealth enemyHealth; // ★★★ 자신의 체력 상태를 확인할 변수 추가 ★★★

    // ★★★ Start 대신 Awake로 변경하여 다른 스크립트보다 먼저 실행되도록 보장 ★★★
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyHealth = GetComponent<EnemyHealth>(); // ★★★ 시작할 때 EnemyHealth 컴포넌트를 찾아 저장 ★★★
        
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("씬에서 플레이어를 찾을 수 없어 파이어볼이 즉시 파괴됩니다.");
            Destroy(gameObject);
            return;
        }
        
        Destroy(gameObject, lifetime);
    }
    
    void FixedUpdate()
    {
        // 만약 추적할 플레이어가 없거나, 내가 이미 죽었다면 움직이지 않음
        if (player == null || (enemyHealth != null && enemyHealth.IsDead))
        {
            // 물리적 움직임을 확실히 멈춤
            if(rb != null) rb.linearVelocity = Vector3.zero; 
            return;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 targetVelocity = directionToPlayer * moveSpeed;
        
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * turnSpeed);

        if (rb.linearVelocity != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed));
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // ★★★ 핵심 수정: 충돌 시 가장 먼저 죽었는지 확인 ★★★
        // EnemyHealth 스크립트가 존재하고, 이미 죽은(IsDead) 상태라면 아무것도 하지 않고 즉시 종료
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            return;
        }

        // 살아있을 때만 아래 로직을 실행
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerCheckpoint playerCheckpoint = collision.gameObject.GetComponent<PlayerCheckpoint>();
            if (playerCheckpoint != null)
            {
                playerCheckpoint.Respawn();
            }
            Destroy(gameObject);
        }
    }
}