using UnityEngine;

[System.Serializable]
public struct ItemDropInfo
{
    public ItemData itemData;
    public int minCount;
    public int maxCount;
    [Tooltip("이 아이템이 드랍될 확률 (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
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
    
    // ## 추가: 아이템 생성 높이 조절 변수 ##
    [Tooltip("아이템이 생성될 때의 Y축 높이 오프셋. 땅에 파묻히는 것을 방지합니다.")]
    [SerializeField] private float dropYOffset = 0.5f;
    
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
            // ## 수정: 드랍 기준 위치에 Y 오프셋을 먼저 더해줍니다. ##
            Vector3 baseSpawnPosition = transform.position + new Vector3(0, dropYOffset, 0);

            foreach (var drop in dropTable)
            {
                if (Random.value <= drop.dropChance)
                {
                    if (drop.itemData == null) continue;

                    int count = Random.Range(drop.minCount, drop.maxCount + 1);
                    for (int i = 0; i < count; i++)
                    {
                        // ## 수정: Y 오프셋이 적용된 위치 주변에 아이템을 흩뿌립니다. ##
                        Vector3 spawnPos = baseSpawnPosition + Random.insideUnitSphere * 0.5f;
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