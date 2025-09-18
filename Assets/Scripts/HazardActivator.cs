using UnityEngine;

// 이 스크립트가 붙은 오브젝트는 반드시 Collider가 있어야 함
[RequireComponent(typeof(Collider))]
public class HazardActivator : MonoBehaviour
{
    [Header("작동시킬 위험 요소")]
    [Tooltip("이 트리거를 밟았을 때 작동시킬 MovingHazard를 지정합니다.")]
    [SerializeField] private MovingHazard targetHazard;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 밟았는지 확인
        if (other.CompareTag("Player"))
        {
            // 타겟이 지정되어 있는지 확인
            if (targetHazard != null)
            {
                // 타겟의 Activate() 함수를 호출하여 원격으로 작동시킴
                targetHazard.Activate();
            }
            else
            {
                Debug.LogWarning(gameObject.name + "에 타겟 위험 요소가 지정되지 않았습니다.");
            }
        }
    }
}