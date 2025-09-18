using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerAttack : MonoBehaviour
{
    public Collider attackCollider;
    
    [Header("공격 설정")]
    [SerializeField] private float attackBufferTime = 0.2f;

    private Animator animator;
    private bool isAttacking = false;
    private PlayerMovement playerMovement;
    private float attackBufferCounter;
    private List<Collider> hitEnemiesThisSwing;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        attackCollider.enabled = false;
        hitEnemiesThisSwing = new List<Collider>();
    }

    void Update()
    {
        if (attackBufferCounter > 0)
        {
            attackBufferCounter -= Time.deltaTime;
        }
        if (Input.GetMouseButtonDown(0))
        {
            attackBufferCounter = attackBufferTime;
        }
        if (!isAttacking && attackBufferCounter > 0)
        {
            attackBufferCounter = 0f;
            StartAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        playerMovement.canRotate = false;
        animator.SetTrigger("Attack");
    }
    
    // ★★★ 추가: 외부에서 호출할 리셋 함수 ★★★
    public void ResetAttackState()
    {
        isAttacking = false;
        attackBufferCounter = 0f;
        hitEnemiesThisSwing.Clear();
        attackCollider.enabled = false;
        // 애니메이션 트리거도 리셋하여 공격 애니메이션이 멈추도록 함
        if(animator != null) animator.ResetTrigger("Attack");
    }
    
    public void OnSwordHit(Collider other)
    {
        if (other.CompareTag("Enemy") && !hitEnemiesThisSwing.Contains(other))
        {
            hitEnemiesThisSwing.Add(other);
        }
    }
    public void EnableAttackCollider()
    {
        hitEnemiesThisSwing.Clear();
        attackCollider.enabled = true;
    }
    public void DisableAttackCollider()
    {
        attackCollider.enabled = false;
        
        if (hitEnemiesThisSwing.Count > 0)
        {
            Collider closestEnemy = hitEnemiesThisSwing.OrderBy(enemy => 
                Vector3.Distance(transform.position, enemy.transform.position)
            ).FirstOrDefault();

            if (closestEnemy != null)
            {
                if (closestEnemy.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
                {
                    enemyHealth.TakeDamage(transform.position);
                }
            }
        }
        playerMovement.canRotate = true;
    }
    public void FinishAttack()
    {
        isAttacking = false;
    }
}