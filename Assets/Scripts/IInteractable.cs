using UnityEngine;

public interface IInteractable
{
    // "이 규칙을 따르는 모든 오브젝트는 Interact() 라는 기능을 반드시 가져야 한다"는 약속
    void Interact();

    // "이 규칙을 따르는 모든 오브젝트는 상호작용 텍스트를 반환하는 기능을 가져야 한다"는 약속
    string GetInteractionText(); 

        // ## 추가: 대화가 끝났을 때 DialogueManager가 호출해줄 함수 ##
    void OnDialogueEnd();
}