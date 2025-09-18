// 파일 이름: PlayerCheckpoint.cs
using UnityEngine;
using System; // 'Action'을 사용하기 위해 추가

public class PlayerCheckpoint : MonoBehaviour
{
    // "플레이어가 리스폰했다"는 사실을 알리는 static 이벤트 선언
    public static event Action OnPlayerRespawn;

    [Header("시작 스폰 위치")]
    [SerializeField] private Transform initialSpawnPoint;

    [Header("사망 판정")]
    [SerializeField] private float deathYLevel = -20f;
    
    private Transform currentCheckpointSpawnPoint;
    private Rigidbody rb;
    private CameraController cameraController;
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        
        if (Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }
        else
        {
            Debug.LogError("씬에 메인 카메라가 없거나 'MainCamera' 태그가 설정되지 않았습니다.");
        }

        if (initialSpawnPoint != null)
        {
            currentCheckpointSpawnPoint = initialSpawnPoint;
        }
        else
        {
            Debug.LogWarning("Initial Spawn Point가 설정되지 않았습니다. 시작 위치를 현재 위치로 사용합니다.");
            GameObject initialPosObject = new GameObject("InitialSpawnPoint");
            initialPosObject.transform.position = transform.position;
            initialPosObject.transform.rotation = transform.rotation;
            currentCheckpointSpawnPoint = initialPosObject.transform;
        }
    }

    private void Update()
    {
        if (transform.position.y < deathYLevel)
        {
            Respawn();
        }
    }
    
    public void SetNewCheckpoint(Transform newSpawnPoint)
    {
        if (currentCheckpointSpawnPoint != newSpawnPoint)
        {
            currentCheckpointSpawnPoint = newSpawnPoint;
            Debug.Log("새로운 체크포인트 저장: " + newSpawnPoint.name);
        }
    }
    
    public void Respawn()
    {
        if (currentCheckpointSpawnPoint == null)
        {
            Debug.LogError("부활할 체크포인트가 지정되지 않았습니다!");
            return;
        }
        GameManager.instance.RecordDeath();
        
        // ★★★ 리스폰 신호를 가장 먼저 방송합니다 ★★★
        OnPlayerRespawn?.Invoke();
        
        GameObject[] leftoverProjectiles = GameObject.FindGameObjectsWithTag("Projectile");
        foreach (GameObject projectile in leftoverProjectiles)
        {
            Destroy(projectile);
        }
        
        EnemyWaveManager[] allManagers = FindObjectsOfType<EnemyWaveManager>();
        foreach (EnemyWaveManager manager in allManagers)
        {
            manager.ResetWave();
        }
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = currentCheckpointSpawnPoint.position;
        transform.rotation = currentCheckpointSpawnPoint.rotation;

        if (playerMovement != null)
        {
            playerMovement.canRotate = true;
            playerMovement.isAiming = false;
        }
        if (playerAttack != null)
        {
            playerAttack.ResetAttackState();
        }
        
        if (cameraController != null)
        {
            cameraController.SetRotation(currentCheckpointSpawnPoint.rotation);
        }
        
        Debug.Log("플레이어가 '" + currentCheckpointSpawnPoint.name + "' 체크포인트에서 부활했습니다.");
    }
}