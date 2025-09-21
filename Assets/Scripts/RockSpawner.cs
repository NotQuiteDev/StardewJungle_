using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ## 변수 이름을 Chance로 변경하여 명확하게 만듦 ##
[System.Serializable]
public struct SpawnableObject
{
    public GameObject prefab;
    [Tooltip("스폰될 확률 (%). 다른 아이템과의 상대적인 비율이며, 총합이 100이 아니어도 됩니다.")]
    public float chancePercent; // Weight -> Chance로 이름 변경
}

public class RockSpawner : MonoBehaviour
{
    [Header("감지 및 스폰 설정")]
    [Tooltip("이 반경 내의 돌 개수를 확인하고, 이 반경 내에 새로 스폰합니다.")]
    [SerializeField] private float checkAndSpawnRadius = 20f;
    [Tooltip("이 반경 내에 유지할 돌의 최대 개수입니다.")]
    [SerializeField] private int maxObjects = 15;
    [Tooltip("몇 초마다 돌이 부족한지 확인할지 설정합니다.")]
    [SerializeField] private float spawnCheckInterval = 5f;
    [Tooltip("돌을 감지할 레이어입니다. ('Interactable')")]
    [SerializeField] private LayerMask objectLayer;

    [Header("스폰 목록 및 확률")]
    [Tooltip("스폰 가능한 오브젝트 목록과 각 오브젝트의 스폰 확률(%)을 설정합니다.")]
    [SerializeField] private List<SpawnableObject> spawnableObjects;

    private float totalChance; // ## 변수 이름 변경 ##

    private void Start()
    {
        // 스폰 확률의 총합을 미리 계산해 둡니다.
        CalculateTotalChance();
        
        StartCoroutine(SpawnRoutine());
    }

    // ## 함수 이름 변경 ##
    private void CalculateTotalChance()
    {
        totalChance = 0f;
        foreach (var spawnable in spawnableObjects)
        {
            totalChance += spawnable.chancePercent;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnCheckInterval);

            int currentObjectCount = CountNearbyObjects();

            if (currentObjectCount < maxObjects)
            {
                SpawnRandomObject();
            }
        }
    }

    private int CountNearbyObjects()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, checkAndSpawnRadius, objectLayer);
        
        int count = 0;
        foreach (var col in colliders)
        {
            if (col.GetComponent<MineableStone>() != null)
            {
                count++;
            }
        }
        return count;
    }

    private void SpawnRandomObject()
    {
        if (spawnableObjects.Count == 0 || totalChance <= 0) return;

        // 1. 전체 확률 총합 내에서 랜덤한 지점을 선택
        float randomPoint = Random.Range(0, totalChance);
        GameObject selectedPrefab = null;

        // 2. 랜덤 지점이 어떤 아이템의 확률 범위에 속하는지 확인
        foreach (var spawnable in spawnableObjects)
        {
            if (randomPoint <= spawnable.chancePercent)
            {
                selectedPrefab = spawnable.prefab;
                break;
            }
            randomPoint -= spawnable.chancePercent;
        }
        
        if (selectedPrefab == null) return;

        // 3. 스폰 위치 결정
        Vector2 randomCirclePoint = Random.insideUnitCircle * checkAndSpawnRadius;
        
        Vector3 spawnPosition = new Vector3(
            transform.position.x + randomCirclePoint.x,
            transform.position.y,
            transform.position.z + randomCirclePoint.y
        );

        // 4. 오브젝트 생성
        Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, checkAndSpawnRadius);
    }
}