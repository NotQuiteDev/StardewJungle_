using UnityEngine;

public class GameManagerLoader : MonoBehaviour
{
    // Awake는 Start보다 먼저 실행됩니다.
    void Awake()
    {
        // 만약 씬에 GameManager(지휘관)가 없다면
        if (GameManager.instance == null)
        {
            Debug.Log("GameManager가 없어서 Resources 폴더에서 로드합니다.");
            // Resources 폴더에서 "GameManager" 프리팹을 찾아서 씬에 생성한다.
            Instantiate(Resources.Load<GameObject>("GameManager"));
        }
    }
}