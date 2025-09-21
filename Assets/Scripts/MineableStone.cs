using UnityEngine;

// ## ItemDropInfo 구조체에 dropChance 추가 ##
[System.Serializable]
public struct ItemDropInfo
{
    public ItemData itemData;
    public int minCount;
    public int maxCount;
    
    [Tooltip("이 아이템이 드랍될 확률 (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)] // 인스펙터에서 0과 1 사이를 조절하는 슬라이더가 생깁니다.
    public float dropChance; 
}

public class MineableStone : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("아이템 드랍 테이블")]
    [SerializeField] private ItemDropInfo[] dropTable;

    [Header("드랍 프리팹")]
    [Tooltip("바닥에 생성될 아이템 드랍 셸 프리팹 (ItemDrop 스크립트가 있는 것)")]
    [SerializeField] private GameObject itemDropShellPrefab;
    
    [Header("이펙트")]
    [SerializeField] private GameObject destructionEffectPrefab;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        if (itemDropShellPrefab == null)
        {
            Debug.LogError("Item Drop Shell Prefab이 MineableStone에 연결되지 않았습니다!", this.gameObject);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        if (itemDropShellPrefab != null)
        {
            foreach (var drop in dropTable)
            {
                // ## 드랍 확률 체크 로직 추가 ##
                // 0과 1 사이의 랜덤 숫자를 뽑아서, 설정한 dropChance보다 작거나 같으면 통과 (드랍 성공)
                if (Random.value <= drop.dropChance)
                {
                    if (drop.itemData == null) continue;

                    int count = Random.Range(drop.minCount, drop.maxCount + 1);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.5f;
                        GameObject dropInstance = Instantiate(itemDropShellPrefab, spawnPos, Quaternion.identity);

                        var itemDrop = dropInstance.GetComponent<ItemDrop>();
                        if(itemDrop != null)
                        {
                            itemDrop.Initialize(drop.itemData, 1);
                        }
                        
                        var rb = dropInstance.GetComponent<Rigidbody>();
                        if(rb != null)
                        {
                            Vector3 force = (Vector3.up + Random.insideUnitSphere) * 2f;
                            rb.AddForce(force, ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        Destroy(gameObject);
    }
}