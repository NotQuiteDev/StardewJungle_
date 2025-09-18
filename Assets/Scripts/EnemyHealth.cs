// 파일 이름: EnemyHealth.cs
using UnityEngine;
using System.Collections;
using System; // 이벤트를 사용하기 위해 추가

public class EnemyHealth : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] public float maxHealth = 5f; 
    public float currentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged;

    [Header("오브젝트 타입")]
    [Tooltip("체크하면 죽을 때 넘어지지 않고 즉시 사라집니다. (예: 투사체)")]
    [SerializeField] private bool isSimpleDestructible = false;

    [Header("피격 효과 설정")]
    public Renderer meshRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    [Header("죽음 처리 설정 (AI 전용)")]
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private float deathTorque = 2f;

    [Header("외부 시스템 연결")]
    public EnemyWaveManager waveManager;

    [Header("디버그 정보 (읽기 전용)")]
    [SerializeField]
    private float _debug_CurrentHealth;

    private EnemyAI enemyAI;
    private Material enemyMaterial;
    private Color originalColor;
    private Rigidbody rb;
    private bool isDead = false;

    public bool IsDead { get { return isDead; } }

    void Awake()
    {
        currentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody>();
        _debug_CurrentHealth = currentHealth;

        if (meshRenderer != null)
        {
            enemyMaterial = meshRenderer.material;
            originalColor = enemyMaterial.color;
        }
    }
    
    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(Vector3 playerPosition, float damage = 1f)
    {
        if (isDead || (enemyAI != null && enemyAI.currentState == EnemyAI.EnemyState.Stunned))
        {
            return;
        }

        currentHealth -= damage;
        _debug_CurrentHealth = currentHealth;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (enemyAI != null)
        {
            enemyAI.OnHit(playerPosition);
        }

        StartCoroutine(FlashOnHit());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashOnHit()
    {
        if (enemyMaterial != null)
        {
            enemyMaterial.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            if (!isDead)
            {
                enemyMaterial.color = originalColor;
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        currentHealth = 0;
        _debug_CurrentHealth = currentHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (waveManager != null)
        {
            waveManager.OnEnemyDied(this.gameObject);
        }

        if (isSimpleDestructible)
        {
            Destroy(gameObject);
        }
        else
        {
            StopAllCoroutines();
            if (enemyMaterial != null) enemyMaterial.color = flashColor;
            
            var bossAI = GetComponent<BossCatAI>();
            if (bossAI != null) bossAI.enabled = false;
            if (enemyAI != null) enemyAI.enabled = false;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.None;
                // ★★★ 바로 이 부분입니다! ★★★
                Vector3 sideTorque = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(0.8f, 1.2f)).normalized;
                rb.AddTorque(sideTorque * deathTorque, ForceMode.Impulse);
            }
            Destroy(gameObject, destroyDelay);
        }
    }
}