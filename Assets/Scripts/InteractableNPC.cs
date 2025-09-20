using UnityEngine;

// IInteractable 인터페이스를 사용하기 때문에,
// 플레이어의 기존 상호작용 시스템이 이 NPC를 자동으로 감지할 겁니다.
public class InteractableNPC : MonoBehaviour, IInteractable
{
    [Header("NPC 상호작용 설정")]
    [Tooltip("플레이어에게 표시될 상호작용 텍스트 (예: Open Shop, Talk)")]
    [SerializeField] private string interactionPrompt = "Interact";

    /// <summary>
    /// 플레이어의 UI에 표시될 텍스트를 반환하는 함수입니다.
    /// </summary>
    public string GetInteractionText()
    {
        return interactionPrompt;
    }

    /// <summary>
    /// 플레이어가 E키를 눌렀을 때 호출되는 메인 함수입니다.
    /// </summary>
    public void Interact()
    {
        // 지금은 디버그 로그만 출력합니다.
        // TODO: 나중에 이 부분에 상점 UI를 여는 코드를 추가할 겁니다.
        Debug.Log($"'{gameObject.name}' NPC와 상호작용했습니다. 액션: {interactionPrompt}");

        // 여기에 나중에 ShopUIManager.Instance.OpenShop(this); 같은 코드가 들어가겠지.
    }
}