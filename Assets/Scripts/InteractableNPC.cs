using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ShopItem
{
    public ItemData item;
    public int price;
}

public class InteractableNPC : MonoBehaviour, IInteractable
{
    [Header("NPC 공통 설정")]
    [Tooltip("플레이어에게 표시될 상호작용 텍스트")]
    [SerializeField] private string interactionPrompt = "Talk";

    // ## 수정: 이 NPC가 사용할 DialogueData 에셋을 연결 ##
    [Header("대화 설정")]
    [SerializeField] private DialogueData dialogueData;

    // ## 추가: 외부에서 이 NPC의 DialogueData를 읽을 수 있도록 해주는 통로 ##
    public DialogueData DialogueInfo => dialogueData;

    // ## 추가: NPC 자신의 '대화 중' 상태 변수 ##
    private bool isInteracting = false;

    [Header("상점 NPC 전용 설정")]
    public List<ShopItem> shopItemList;

    // ## 삭제: 초상화는 이제 DialogueData에서 관리 ##
    // public Sprite npcPortrait;

    public string GetInteractionText()
    {
        // NPC 이름은 DialogueData에서 가져와 표시
        return $"{interactionPrompt} with {dialogueData.npcName}";
    }

    public void Interact()
    {
        // ## 수정: 이미 대화 중이면, 새로운 상호작용을 시작하지 않고 즉시 종료 ##
        if (isInteracting) return;

        // 대화를 시작하기 직전, 자신의 상태를 '대화 중'으로 변경
        isInteracting = true;
        DialogueManager.Instance.StartDialogue(dialogueData, this);
    }
    // ## 추가: 대화가 끝났을 때 호출될 함수 ##
    public void OnDialogueEnd()
    {
        // 자신의 상태를 '대화 가능'으로 되돌림
        isInteracting = false;
    }
}