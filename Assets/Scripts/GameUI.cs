// 파일 이름: GameUI.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("UI 텍스트 연결")]
    public TextMeshProUGUI deathCountText;
    public TextMeshProUGUI interactionPromptText;

    [Header("털뭉치 UI 설정")]
    public Image[] furBallImages;
    public Sprite emptyFurBallSprite;
    public Sprite collectedFurBallSprite;

    // ★★★ 핵심 1: 아래 두 함수를 여기에 추가하세요. ★★★
    // 이 스크립트가 활성화될 때 GameManager에게 신호를 받겠다고 등록합니다.
    private void OnEnable()
    {
        GameManager.OnDeathCountChanged += UpdateDeathCountUI;
    }

    // 이 스크립트가 비활성화될 때 신호 받는 것을 그만둡니다. (메모리 누수 방지)
    private void OnDisable()
    {
        GameManager.OnDeathCountChanged -= UpdateDeathCountUI;
    }


    void Start()
    {
        if (GameManager.instance == null)
        {
            Debug.LogError("GameManager가 씬에 없습니다!");
            return;
        }
        
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }

        UpdateDeathCountUI();
        UpdateFurBallUI();
    }
 
    // (이하 모든 코드는 기존과 동일합니다. 수정할 필요 없습니다.)
    
    public void ShowInteractionPrompt(string text)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = text;
            interactionPromptText.gameObject.SetActive(true);
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
    
    public void UpdateDeathCountUI()
    {
        if (deathCountText != null)
        {
            deathCountText.text = "DEATH : " + GameManager.instance.deathCount;
        }
    }
    public void UpdateFurBallUI()
    {
        int collectedCount = GameManager.instance.furBallsCollected;
        for (int i = 0; i < furBallImages.Length; i++)
        {
            if (i < collectedCount)
            {
                furBallImages[i].sprite = collectedFurBallSprite;
            }
            else
            {
                furBallImages[i].sprite = emptyFurBallSprite;
            }
        }
    }
}