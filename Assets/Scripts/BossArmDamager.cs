// 파일 이름: BossArmDamager.cs
using UnityEngine;

public class BossArmDamager : MonoBehaviour
{
    // OnCollisionEnter는 non-Trigger 콜라이더끼리 부딪혔을 때 호출됩니다.
    private void OnCollisionEnter(Collision collision)
    {
        // 1. 부딪힌 오브젝트의 태그가 "Player"인지 확인합니다.
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("보스 팔 공격이 플레이어에게 적중!");
            
            // 2. 부딪힌 플레이어 오브젝트에서 PlayerCheckpoint 스크립트를 찾습니다.
            PlayerCheckpoint playerCheckpoint = collision.gameObject.GetComponent<PlayerCheckpoint>();

            // 3. 스크립트를 찾았다면, 그 스크립트의 Respawn() 함수를 호출합니다.
            if (playerCheckpoint != null)
            {
                playerCheckpoint.Respawn();
            }
        }
    }
}