// 파일 이름: BossCatAI.cs
using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(EnemyHealth))]
public class BossCatAI : MonoBehaviour
{
    public static event Action OnBossSummon;

    public enum BossState { Sleeping, Following, Attacking, Retreating, Stunned }
    public BossState currentState;

    [Header("핵심 연결")]
    public Transform player;
    public EnemyHealth bossHealth;
    public GameObject laserBeamPrefab;
    public GameObject laserChargeEffectPrefab;
    public Transform laserFirePoint;
    
    [Header("소환 패턴 연결")]
    public Transform armRootTransform;
    
    [Header("근접 회전 패턴")]
    [Tooltip("회전하는 팔들의 부모 Transform 입니다.")]
    public Transform spinningArmsTransform;
    [Tooltip("공격 판정을 가질 팔의 콜라이더들입니다.")]
    public Collider[] armColliders;
    [Tooltip("팔 회전이 최대 속도까지 도달하는 시간입니다.")]
    [SerializeField] private float spinWindUpDuration = 2.5f;
    [Tooltip("팔의 시작 회전 속도 (초당 각도)")]
    [SerializeField] private float startArmSpinSpeed = 100f;
    [Tooltip("팔의 최대 회전 속도 (초당 각도)")]
    [SerializeField] private float maxArmSpinSpeed = 1000f;
    [Tooltip("최대 속도에서 보스 본체가 회전하는 속도")]
    [SerializeField] private float bodySpinSpeed = 180f;
    [Tooltip("회전하며 플레이어에게 다가가는 속도")]
    [SerializeField] private float spinAdvanceSpeed = 1.5f;
    [Tooltip("최대 속도로 회전 공격을 지속하는 시간")]
    [SerializeField] private float spinAttackDuration = 4f;

    private BossHealthUIController healthUIController;

    [Header("AI 핵심 설정")]
    [SerializeField] private float activationRange = 25f;
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float retreatDistance = 7f;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float retreatSpeed = 3f;
    [SerializeField] private float patternCooldown = 3f;
    [Tooltip("보스의 Y좌표가 이 값보다 낮아지면 추락사합니다.")]
    [SerializeField] private float deathYLevel = 17f;

    [Header("AI 안전 설정")]
    [SerializeField] private float fallCheckDistance = 5f;
    [Tooltip("안전 지형으로 인식할 레이어들을 선택합니다. 'Player'도 포함할 수 있습니다.")]
    [SerializeField] private LayerMask groundCheckLayerMask; // << 이 줄을 추가하세요!
    
    [Header("돌진 패턴 설정")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDuration = 1f;

    [Header("소환 패턴 설정")]
    [SerializeField] private float summonArmAnimDuration = 0.8f;
    [SerializeField] private float armIdleXRotation = -56f;
    [SerializeField] private float armRaisedXRotation = -170f;
    [SerializeField] private float summonPatternCooldown = 15f;

    [Header("레이저 패턴 설정")]
    [SerializeField] private float laserWindUpTime = 1.5f;
    [SerializeField] private float laserAttackDuration = 3f;
    [SerializeField] private float laserHorizontalTrackingSpeed = 5f;
    [SerializeField] private float laserStartAngle = 80f;
    [SerializeField] private float laserEndAngle = 20f;

    private bool canAct = true;
    private bool isActivated = false;
    private GameObject activeChargeEffect = null;
    private bool canSummon = true;

    void Awake()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (bossHealth == null) bossHealth = GetComponent<EnemyHealth>();
        if (healthUIController == null) healthUIController = FindObjectOfType<BossHealthUIController>();
    }

    private void OnEnable()
    {
        PlayerCheckpoint.OnPlayerRespawn += SelfDestruct;
    }

    private void OnDisable()
    {
        PlayerCheckpoint.OnPlayerRespawn -= SelfDestruct;
    }

    void Start()
    {
        currentState = BossState.Sleeping;
    }

    void Update()
    {
        if (player == null || bossHealth.IsDead)
        {
            if (activeChargeEffect != null) Destroy(activeChargeEffect);
            return;
        }

        if (transform.position.y < deathYLevel)
        {
            bossHealth.Die();
            return;
        }

        switch (currentState)
        {
            case BossState.Sleeping:
                CheckForPlayerActivation();
                break;
            case BossState.Following:
            case BossState.Retreating:
                HandleMovementState();
                break;
        }
    }

    void CheckForPlayerActivation()
    {
        if (!isActivated && Vector3.Distance(transform.position, player.position) <= activationRange)
        {
            isActivated = true;
            currentState = BossState.Following;
            if (healthUIController != null)
            {
                healthUIController.Setup(bossHealth);
            }
            else
            {
                Debug.LogWarning("BossCatAI: 씬에서 BossHealthUIController를 찾지 못했습니다.");
            }
        }
    }

    void HandleMovementState()
    {
        if (!canAct) return;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange)
        {
            StartCoroutine(ChooseActionRoutine());
        }
        else
        {
            currentState = BossState.Following;
            MoveTowardsPlayer();
        }
    }

    void MoveTowardsPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.position = Vector3.MoveTowards(transform.position, transform.position + dir, followSpeed * Time.deltaTime);
        RotateTowardsPlayer(5f);
    }

    void RetreatFromPlayer()
    {
        Vector3 dir = (transform.position - player.position).normalized;
        dir.y = 0;
        if (IsSafeToMove(dir))
        {
            transform.position = Vector3.MoveTowards(transform.position, transform.position + dir, retreatSpeed * Time.deltaTime);
        }
        RotateTowardsPlayer(5f);
    }

    void RotateTowardsPlayer(float speed)
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
    }

    private bool IsSafeToMove(Vector3 dir)
    {
        Vector3 start = transform.position + dir.normalized * fallCheckDistance;
        start.y += 1f;

        // Physics.Raycast에 groundCheckLayerMask를 추가합니다.
        if (Physics.Raycast(start, Vector3.down, 5f, groundCheckLayerMask))
        {
            Debug.DrawRay(start, Vector3.down * 5f, Color.green, 0.1f);
            return true;
        }
        else
        {
            Debug.DrawRay(start, Vector3.down * 5f, Color.red, 0.1f);
            return false;
        }
    }

    private IEnumerator ChooseActionRoutine()
    {
        canAct = false;
        currentState = BossState.Attacking;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float actionRoll = UnityEngine.Random.value;

        // --- 플레이어와의 거리에 따라 패턴 그룹 결정 ---
        if (distanceToPlayer < retreatDistance) // 플레이어가 가까우면
        {
            if (actionRoll < 0.5f) // 50% 확률로 후퇴 또는 돌진
            {
                Vector3 retreatDirection = (transform.position - player.position).normalized;
                if (IsSafeToMove(retreatDirection))
                { // 후퇴
                    currentState = BossState.Retreating;
                    float retreatTime = 3.0f;
                    float timer = 0;
                    while (timer < retreatTime && player != null)
                    {
                        RetreatFromPlayer();
                        timer += Time.deltaTime;
                        yield return null;
                    }
                }
                else
                { // 막혔으면 돌진
                    yield return StartCoroutine(ChargePatternRoutine());
                }
            }
            else // 50% 확률로 근접 회전 공격
            {
                yield return StartCoroutine(MeleeSpinPatternRoutine());
            }
        }
        else // 플레이어가 멀면
        {
            if (actionRoll < 0.5f && canSummon)
            { // 소환
                yield return StartCoroutine(SummonPatternRoutine());
            }
            else
            { // 레이저
                yield return StartCoroutine(LaserPatternRoutine());
            }
        }
        
        // --- 패턴 종료 후 방어 기동 ---
        Vector3 defensiveRetreatDir = (transform.position - player.position).normalized;
        if (!IsSafeToMove(defensiveRetreatDir))
        {
            Debug.Log("방어 기동: 구석에 몰려 돌진으로 탈출!");
            yield return StartCoroutine(ChargePatternRoutine());
        }

        // --- 다음 행동 쿨타임 ---
        currentState = BossState.Following;
        yield return new WaitForSeconds(patternCooldown);
        canAct = true;
    }

    private IEnumerator LaserPatternRoutine()
    {
        currentState = BossState.Attacking;
        Debug.Log("패턴: 레이저 조준!");
        if (laserChargeEffectPrefab != null) { activeChargeEffect = Instantiate(laserChargeEffectPrefab, laserFirePoint.position, laserFirePoint.rotation, laserFirePoint); }
        float windUpTimer = 0;
        while (windUpTimer < laserWindUpTime) { if (player != null) RotateTowardsPlayer(laserHorizontalTrackingSpeed); windUpTimer += Time.deltaTime; yield return null; }
        if (activeChargeEffect != null) Destroy(activeChargeEffect);
        
        Debug.Log("레이저 발사!");
        GameObject laserBeamObject = Instantiate(laserBeamPrefab, laserFirePoint.position, laserFirePoint.rotation);
        LaserBeam laser = laserBeamObject.GetComponent<LaserBeam>();
        if (laser != null) laser.Initialize(laserFirePoint);
        float attackTimer = 0;
        while (attackTimer < laserAttackDuration) { float currentAngleX = Mathf.Lerp(laserStartAngle, laserEndAngle, attackTimer / laserAttackDuration); laserFirePoint.localRotation = Quaternion.Euler(currentAngleX, 0, 0); attackTimer += Time.deltaTime; yield return null; }
        Debug.Log("레이저 종료!");
        if (laserBeamObject != null) Destroy(laserBeamObject);
    }
    
    private IEnumerator ChargePatternRoutine()
    {
        Debug.Log("패턴: 돌진!");
        currentState = BossState.Attacking;
        Vector3 targetPosition = player.position;
        Vector3 chargeDirection = (targetPosition - transform.position).normalized;
        chargeDirection.y = 0;
        float turnTimer = 0;
        while (turnTimer < 0.2f) { transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(chargeDirection), Time.deltaTime * 15f); turnTimer += Time.deltaTime; yield return null; }
        float chargeTimer = 0;
        while (chargeTimer < chargeDuration) { transform.position = Vector3.MoveTowards(transform.position, transform.position + chargeDirection, chargeSpeed * Time.deltaTime); chargeTimer += Time.deltaTime; yield return null; }
    }

    private IEnumerator SummonPatternRoutine()
    {
        canSummon = false;
        StartCoroutine(SummonCooldownRoutine());
        Debug.Log("패턴: 소환!");
        currentState = BossState.Attacking;
        Quaternion startRot = Quaternion.Euler(armIdleXRotation, 0, 0);
        Quaternion endRot = Quaternion.Euler(armRaisedXRotation, 0, 0);
        float timer = 0;
        while (timer < summonArmAnimDuration) { if(armRootTransform != null) armRootTransform.localRotation = Quaternion.Slerp(startRot, endRot, timer / summonArmAnimDuration); timer += Time.deltaTime; yield return null; }
        if(armRootTransform != null) armRootTransform.localRotation = endRot;
        Debug.Log("소환 신호 방송!");
        OnBossSummon?.Invoke();
        yield return new WaitForSeconds(1.0f);
        timer = 0;
        while (timer < summonArmAnimDuration) { if(armRootTransform != null) armRootTransform.localRotation = Quaternion.Slerp(endRot, startRot, timer / summonArmAnimDuration); timer += Time.deltaTime; yield return null; }
        if(armRootTransform != null) armRootTransform.localRotation = startRot;
    }

    private IEnumerator SummonCooldownRoutine()
    {
        yield return new WaitForSeconds(summonPatternCooldown);
        canSummon = true;
        Debug.Log("이제 다시 소환 가능!");
    }

    private IEnumerator MeleeSpinPatternRoutine()
    {
        Debug.Log("패턴: 근접 회전 공격!");
        currentState = BossState.Attacking;

        // --- 1단계: 팔 회전 가속 (콜라이더는 아직 꺼진 상태) ---
        float windUpTimer = 0f;
        while (windUpTimer < spinWindUpDuration)
        {
            float currentSpeed = Mathf.Lerp(startArmSpinSpeed, maxArmSpinSpeed, windUpTimer / spinWindUpDuration);
            if(spinningArmsTransform != null) spinningArmsTransform.Rotate(currentSpeed * Time.deltaTime, 0, 0, Space.Self);
            windUpTimer += Time.deltaTime;
            yield return null;
        }

        // --- 2단계: 가속 종료 후, 공격 콜라이더 활성화! ---
        Debug.Log("회전 공격 활성화!");
        foreach (Collider col in armColliders) { if(col != null) col.enabled = true; }

        // --- 3단계: 본체 회전 및 전진 공격 ---
        float attackTimer = 0f;
        while (attackTimer < spinAttackDuration)
        {
            if(spinningArmsTransform != null) spinningArmsTransform.Rotate(maxArmSpinSpeed * Time.deltaTime, 0, 0, Space.Self);
            transform.Rotate(0, bodySpinSpeed * Time.deltaTime, 0, Space.World);
            if(player != null) transform.position = Vector3.MoveTowards(transform.position, player.position, spinAdvanceSpeed * Time.deltaTime);
            
            attackTimer += Time.deltaTime;
            yield return null;
        }

        // --- 4단계: 패턴 종료 후, 공격 콜라이더 비활성화 ---
        Debug.Log("회전 공격 비활성화!");
        foreach (Collider col in armColliders) { if(col != null) col.enabled = false; }
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}