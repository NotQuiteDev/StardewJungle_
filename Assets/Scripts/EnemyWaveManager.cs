using UnityEngine;
using System.Collections.Generic;

// ★★★ 핵심: 스폰할 적의 정보(프리팹과 위치)를 하나로 묶는 클래스 ★★★
// [System.Serializable]을 붙여야 인스펙터 창에서 보입니다.
[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;    // 스폰할 적의 프리팹
    public Transform spawnPoint;      // 스폰될 위치
}

public class EnemyWaveManager : MonoBehaviour
{
    [Header("웨이브 설정")]
    [Tooltip("이 웨이브에서 스폰할 적들의 목록을 설정합니다.")]
    public List<EnemySpawnInfo> enemyWaveSetup; // ★★★ 새로운 설정 방식 ★★★

    [Tooltip("모든 적을 처치했을 때 활성화할 오브젝트입니다.")]
    public GameObject objectToActivateOnClear;

    // 현재 씬에 살아있는 적들을 추적하는 리스트
    private List<GameObject> aliveEnemies = new List<GameObject>();

    void Start()
    {
        // 게임이 시작되면 웨이브를 스폰합니다.
        SpawnWave();
    }

    // 웨이브를 스폰하는 함수
    void SpawnWave()
    {
        // 혹시 남아있을지 모를 이전 적들을 모두 파괴하고 리스트를 비웁니다.
        foreach (GameObject enemy in aliveEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        aliveEnemies.Clear();

        // 설정된 목록을 바탕으로 적들을 스폰합니다.
        foreach (EnemySpawnInfo info in enemyWaveSetup)
        {
            if (info.enemyPrefab != null && info.spawnPoint != null)
            {
                // 프리팹을, 지정된 스폰 위치에, 지정된 회전값으로 생성합니다.
                GameObject newEnemy = Instantiate(info.enemyPrefab, info.spawnPoint.position, info.spawnPoint.rotation);
                
                // 생성된 적을 살아있는 적 리스트에 추가합니다.
                aliveEnemies.Add(newEnemy);

                // 생성된 적의 EnemyHealth 스크립트에 이 매니저를 자동으로 연결해줍니다.
                EnemyHealth health = newEnemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.waveManager = this;
                }
            }
        }
        Debug.Log(aliveEnemies.Count + "명의 적으로 웨이브를 시작합니다.");
    }

    // EnemyHealth 스크립트가 적이 죽을 때 호출할 함수
    public void OnEnemyDied(GameObject deadEnemy)
    {
        aliveEnemies.Remove(deadEnemy);

        if (aliveEnemies.Count <= 0)
        {
            Debug.Log("웨이브 클리어! 오브젝트를 활성화합니다.");
            if (objectToActivateOnClear != null)
            {
                objectToActivateOnClear.SetActive(true);
            }
        }
    }

    // 플레이어가 죽었을 때 호출될 리셋 함수
    public void ResetWave()
    {
        Debug.Log("플레이어 사망. 웨이브를 리셋합니다.");
        // 간단하게 웨이브 스폰 함수를 다시 호출하여 모든 것을 리셋합니다.
        SpawnWave();
    }
}