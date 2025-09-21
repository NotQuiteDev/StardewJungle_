using UnityEngine;

// ## ItemDropInfo 구조체 수정: GameObject 대신 ItemData를 직접 사용 ##
[System.Serializable]
public struct ItemDropInfo
{
    public ItemData itemData; // 드랍할 아이템의 ScriptableObject
    public int minCount;
    public int maxCount;
}

public class MineableStone : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("아이템 드랍 테이블")]
    [SerializeField] private ItemDropInfo[] dropTable;

    // ## 추가: InventoryManager에 있는 드랍 아이템 '껍데기' 프리팹 연결 ##
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

        // ## 중요: 드랍 셸이 연결되었는지 시작 시 확인 ##
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

        // ## 드랍 로직 수정: InventoryManager와 동일한 방식으로 변경 ##
        if (itemDropShellPrefab != null)
        {
            foreach (var drop in dropTable)
            {
                if (drop.itemData == null) continue;

                int count = Random.Range(drop.minCount, drop.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.5f;
                    GameObject dropInstance = Instantiate(itemDropShellPrefab, spawnPos, Quaternion.identity);

                    // 생성된 드랍 셸에 아이템 정보 주입
                    var itemDrop = dropInstance.GetComponent<ItemDrop>();
                    if(itemDrop != null)
                    {
                        itemDrop.Initialize(drop.itemData, 1); // 셸 하나당 1개씩 드랍
                    }
                    
                    // (선택) 물리 효과로 아이템을 흩뿌리기
                    var rb = dropInstance.GetComponent<Rigidbody>();
                    if(rb != null)
                    {
                        Vector3 force = (Vector3.up + Random.insideUnitSphere) * 2f;
                        rb.AddForce(force, ForceMode.Impulse);
                    }
                }
            }
        }

        Destroy(gameObject);
    }
}