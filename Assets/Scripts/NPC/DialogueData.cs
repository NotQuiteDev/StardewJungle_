// DialogueData.cs

using UnityEngine;

// ## 수정: 새로운 대화를 시작하는 옵션 추가 ##
public enum ChoiceActionType
{
    DoNothing,      // 아무것도 안 함
    OpenShop,       // 상점 열기
    OpenUpgradeUI,  // 대장간 UI 열기
    StartNewDialogue // 다른 대화 시작
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public ChoiceActionType actionType;

    // ## 추가: actionType이 StartNewDialogue일 경우 연결할 DialogueData ##
    [Tooltip("다른 대화를 시작할 경우에만 이 필드를 채워주세요.")]
    public DialogueData nextDialogue;
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "NPC/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("NPC 정보")]
    public string npcName;
    public Sprite npcPortrait;

    [Header("대화 내용")]
    [TextArea(3, 5)]
    public string[] dialogueLines;

    [Header("선택지")]
    public DialogueChoice[] choices;
}