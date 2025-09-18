using UnityEngine;
using UnityEngine.SceneManagement;

// IInteractable 규칙을 따른다고 선언
public class StagePortal : MonoBehaviour, IInteractable
{
    [Header("포탈 설정")]
    [Tooltip("이 포탈을 작동시켰을 때 불러올 씬의 이름입니다.")]
    [SerializeField] private string sceneNameToLoad;
    [Tooltip("플레이어가 가까이 왔을 때 화면에 표시될 안내 문구입니다.")]
    [SerializeField] [TextArea] private string interactionPrompt = "E키를 눌러 다음 지역으로 이동";

    // 규칙 1: Interact() 기능을 반드시 구현해야 함
    public void Interact()
    {
        // 씬 이름이 비어있지 않다면 해당 씬을 불러온다
        if (!string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.Log(sceneNameToLoad + " 씬을 불러옵니다...");
            SceneManager.LoadScene(sceneNameToLoad);
        }
        else
        {
            Debug.LogWarning("불러올 씬 이름이 지정되지 않았습니다!");
        }
    }

    // 규칙 2: GetInteractionText() 기능을 반드시 구현해야 함
    public string GetInteractionText()
    {
        return interactionPrompt;
    }
}