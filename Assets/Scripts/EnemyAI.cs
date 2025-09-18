using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    // ... (상단 변수 선언 및 함수들은 이전과 동일) ...
    public enum EnemyState { Idle, Chasing, Stunned, AvoidingCliff }
    public EnemyState currentState;

    [Header("AI 설정")]
    [Tooltip("플레이어를 감지할 범위입니다.")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("플레이어를 향해 이동하는 속도입니다.")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("이 Y좌표보다 낮아지면 추락사합니다.")]
    [SerializeField] private float deathYLevel = -20f;
    
    [Header("절벽 감지")]
    [Tooltip("절벽 감지를 시작할 위치입니다. 보통 캐릭터의 앞쪽 발밑에 둡니다.")]
    [SerializeField] private Transform cliffCheck;
    [Tooltip("발밑으로 얼마나 멀리까지 땅을 확인할지 거리입니다.")]
    [SerializeField] private float cliffCheckDistance = 1.5f;
    [Tooltip("땅으로 인식할 레이어입니다.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("절벽을 회피하기 위해 뒤로 물러나는 시간입니다.")]
    [SerializeField] private float cliffAvoidanceTime = 0.5f;

    [Header("피격 설정")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float stunDuration = 0.5f;

    private Transform player;
    private Rigidbody rb;
    private EnemyHealth enemyHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyHealth = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Start()
    {
        currentState = EnemyState.Idle;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (currentState == EnemyState.Stunned || currentState == EnemyState.AvoidingCliff || player == null) return;
        
        if (transform.position.y < deathYLevel)
        {
            enemyHealth.Die();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            bool isGroundAhead = Physics.Raycast(cliffCheck.position, Vector3.down, cliffCheckDistance, groundLayer);
            
            if (isGroundAhead)
            {
                currentState = EnemyState.Chasing;
                MoveTowards(player.position);
            }
            else
            {
                StopMovement();
                StartCoroutine(AvoidCliffRoutine());
            }
        }
        else
        {
            currentState = EnemyState.Idle;
            StopMovement();
        }
    }
    
    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f));
        }
    }

    void StopMovement()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }

    public void OnHit(Vector3 playerPosition)
    {
        StartCoroutine(StunRoutine(playerPosition));
    }

    private IEnumerator StunRoutine(Vector3 playerPosition)
    {
        currentState = EnemyState.Stunned;
        
        Vector3 knockbackDir = (transform.position - playerPosition).normalized;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(stunDuration);

        currentState = EnemyState.Idle;
    }

    private IEnumerator AvoidCliffRoutine()
    {
        currentState = EnemyState.AvoidingCliff;

        Vector3 avoidDirection = -transform.forward;
        float timer = 0;
        while (timer < cliffAvoidanceTime)
        {
            rb.linearVelocity = new Vector3(avoidDirection.x * moveSpeed, rb.linearVelocity.y, avoidDirection.z * moveSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        StopMovement();
        currentState = EnemyState.Idle;
    }


    // ★★★ 핵심 수정: 충돌 시 가장 먼저 죽었는지 확인 ★★★
    private void OnCollisionEnter(Collision collision)
    {
        // 만약 EnemyHealth 스크립트가 없거나, 이미 죽은 상태라면 아무것도 하지 않고 즉시 종료
        if (enemyHealth == null || enemyHealth.IsDead)
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
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (cliffCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cliffCheck.position, cliffCheck.position + Vector3.down * cliffCheckDistance);
        }
    }
}