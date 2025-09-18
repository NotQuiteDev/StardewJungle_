using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))] 
public class FireCatSpawner : MonoBehaviour
{
    [Header("소환 설정")]
    [Tooltip("소환할 파이어볼의 원본 프리팹입니다.")]
    [SerializeField] private GameObject fireballPrefab;
    [Tooltip("파이어볼이 생성될 위치입니다. (보통 입 앞의 빈 오브젝트)")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("파이어볼을 소환하는 시간 간격입니다. (초)")]
    [SerializeField] private float spawnInterval = 3f;

    [Header("플레이어 감지")]
    [Tooltip("플레이어가 이 범위 안에 들어오면 소환을 시작합니다.")]
    [SerializeField] private float detectionRange = 15f;
    [Tooltip("플레이어가 이 범위 안으로 들어오면 소환을 '멈춥니다'.")]
    [SerializeField] private float minSpawnRange = 3f;

    private Transform player;
    private bool isSpawning = false;
    private EnemyHealth enemyHealth;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyHealth = GetComponent<EnemyHealth>();

        if (fireballPrefab == null || spawnPoint == null)
        {
            Debug.LogError(gameObject.name + "의 FireCatSpawner에 Prefab 또는 Spawn Point가 설정되지 않았습니다!");
            this.enabled = false;
        }
        
        if (player == null)
        {
            Debug.LogWarning("씬에서 플레이어를 찾을 수 없어 FireCatSpawner가 작동하지 않습니다.");
            this.enabled = false;
        }
    }

    void Update()
    {
        if (player == null || (enemyHealth != null && enemyHealth.IsDead))
        {
            if (isSpawning)
            {
                StopAllCoroutines();
                isSpawning = false;
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange && distanceToPlayer > minSpawnRange)
        {
            if (!isSpawning)
            {
                StartCoroutine(SpawnRoutine());
                isSpawning = true;
            }
        }
        else
        {
            if (isSpawning)
            {
                StopAllCoroutines();
                isSpawning = false;
            }
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // ★★★ 핵심 수정 1: 루프에 진입하기 전에 한 프레임 기다립니다. (안정성 향상) ★★★
        yield return null;

        while (true)
        {
            // ★★★ 핵심 수정 2: 소환하기 전에 "먼저" 기다립니다. ★★★
            yield return new WaitForSeconds(spawnInterval);
            
            // 기다리는 동안 플레이어가 범위를 벗어나거나 내가 죽었을 수 있으므로, 다시 한번 상태를 확인합니다.
            if (enemyHealth != null && enemyHealth.IsDead)
            {
                // 내가 죽었다면 코루틴을 완전히 종료합니다.
                yield break;
            }

            // 모든 조건을 통과했다면 파이어볼을 발사합니다.
            Instantiate(fireballPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((enemyHealth == null || !enemyHealth.IsDead) && collision.gameObject.CompareTag("Player"))
        {
            PlayerCheckpoint playerCheckpoint = collision.gameObject.GetComponent<PlayerCheckpoint>();
            if (playerCheckpoint != null)
            {
                //playerCheckpoint.Respawn();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minSpawnRange);
    }
}