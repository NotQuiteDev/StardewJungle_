using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        // GameManager를 찾아서, 모든 데이터를 초기화하라고 명령합니다.
        if (GameManager.instance != null)
        {
            GameManager.instance.ResetAllGameData();
        }

        SceneManager.LoadScene("Stage1"); // Stage1 씬 로드
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}