using UnityEngine;
using System.Collections.Generic;

public class BlacksmithNPC : MonoBehaviour, IInteractable
{
    [Header("NPC 정보")]
    [SerializeField] private string npcName = "Blacksmith";
    [Tooltip("UI에 표시될 NPC의 얼굴 그림(초상화)")]
    public Sprite npcPortrait;

    [Header("대화 설정")]
    [SerializeField] private DialogueData dialogueData;

    [Header("업그레이드 레시피")]
    [Tooltip("이 대장장이가 제공하는 업그레이드 목록")]
    [SerializeField] private List<UpgradeRecipeData> availableRecipes;

    public string GetInteractionText() => $"Talk to {npcName}";

    // ## 추가: NPC 자신의 '대화 중' 상태 변수 ##
    private bool isInteracting = false;

    public void Interact()
    {
        // ## 수정: 이미 대화 중이면, 새로운 상호작용을 시작하지 않고 즉시 종료 ##
        if (isInteracting) return;

        // 대화를 시작하기 직전, 자신의 상태를 '대화 중'으로 변경
        isInteracting = true;
        DialogueManager.Instance.StartDialogue(dialogueData, this); // dialogueData 변수가 필요합니다.
    }
    // ## 추가: 대화가 끝났을 때 호출될 함수 ##
    public void OnDialogueEnd()
    {
        // 자신의 상태를 '대화 가능'으로 되돌림
        isInteracting = false;
    }

    public List<UpgradeRecipeData> GetAvailableRecipes()
    {
        return availableRecipes;
    }
}