using UnityEngine;

public class FurBallCollectible : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 부딪힌 것이 플레이어라면
        if (other.CompareTag("Player"))
        {
            // 1. GameManager에 털뭉치 획득 개수를 1 늘린다.
            GameManager.instance.furBallsCollected++;

            // 2. 씬에 있는 GameUI를 찾아서 UI를 업데이트하라고 명령한다.
            FindObjectOfType<GameUI>().UpdateFurBallUI();

            // 3. 아이템을 파괴해서 사라지게 한다.
            Destroy(gameObject);
        }
    }
}