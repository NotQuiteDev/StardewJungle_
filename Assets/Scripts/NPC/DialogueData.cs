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

public enum ConditionType
{
    HasQuest   // 특정 퀘스트의 상태를 조건으로
}

// ## 추가 2: 선택지 표시 조건을 정의하는 클래스 ##
[System.Serializable]
public class ChoiceCondition
{
    public ConditionType conditionType;

    [Header("Quest 조건")]
    public QuestData requiredQuest;    // 확인할 퀘스트
    public QuestStatus requiredStatus; // 만족해야 하는 퀘스트 상태
}


[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public ChoiceActionType actionType;

    // ## 추가: actionType이 StartNewDialogue일 경우 연결할 DialogueData ##
    [Tooltip("다른 대화를 시작할 경우에만 이 필드를 채워주세요.")]
    public DialogueData nextDialogue;

    // ## 핵심 추가: 이 선택지가 보이려면 만족해야 하는 조건들 목록 ##
    [Header("선택지 표시 조건")]
    public ChoiceCondition[] conditions;


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