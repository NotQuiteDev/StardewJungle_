// 파일 이름: BossSummoner.cs
using UnityEngine;

public class BossSummoner : MonoBehaviour
{
    [Header("소환 설정")]
    [Tooltip("소환할 적의 Prefab을 지정합니다.")]
    [SerializeField] private GameObject enemyPrefabToSummon;
    
    [Tooltip("적이 소환될 위치들을 지정합니다.")]
    [SerializeField] private Transform[] summonPoints;

    [Tooltip("소환 시 나타날 파티클 효과입니다. (선택 사항)")]
    [SerializeField] private GameObject summonEffectPrefab;

    [Header("소환 안전 설정")]
    [Tooltip("이 반경(미터) 안에 다른 콜라이더가 있으면 소환하지 않습니다.")]
    [SerializeField] private float safeSummonRadius = 3f;

    // ★★★ 1. 장애물로 간주할 레이어들을 선택할 LayerMask 변수 추가 ★★★
    [Tooltip("장애물로 인식할 레이어들을 선택하세요. (예: Player, Enemy 등) Ground는 제외해야 합니다.")]
    [SerializeField] private LayerMask obstacleLayerMask;


    private void OnEnable()
    {
        BossCatAI.OnBossSummon += HandleSummon;
    }

    private void OnDisable()
    {
        BossCatAI.OnBossSummon -= HandleSummon;
    }

    private void HandleSummon()
    {
        Debug.Log("소환 신호를 받았습니다! 몬스터를 소환합니다.");

        if (enemyPrefabToSummon == null) { Debug.LogError("소환할 적 Prefab이 지정되지 않았습니다!"); return; }
        if (summonPoints.Length == 0) { Debug.LogError("소환 위치가 하나도 지정되지 않았습니다!"); return; }

        foreach (Transform point in summonPoints)
        {
            // ★★★ 2. OverlapSphere에 LayerMask를 적용하여 특정 레이어만 검사 ★★★
            // 이제 obstacleLayerMask에 포함된 레이어의 콜라이더만 감지합니다.
            Collider[] collidersInArea = Physics.OverlapSphere(point.position, safeSummonRadius, obstacleLayerMask);
            
            if (collidersInArea.Length == 0)
            {
                if (summonEffectPrefab != null)
                {
                    Instantiate(summonEffectPrefab, point.position, point.rotation);
                }
                Instantiate(enemyPrefabToSummon, point.position, point.rotation);
                Debug.Log(point.name + " 위치에 소환 성공!");
            }
            else
            {
                Debug.Log(point.name + " 위치는 안전하지 않아 소환을 취소합니다. (검출된 오브젝트: " + collidersInArea[0].name + ")");
            }
        }
    }
}