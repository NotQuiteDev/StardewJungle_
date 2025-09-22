using UnityEngine;

// 상점 열기, 새 대화 시작 등의 '큰 흐름'을 결정
public enum ChoiceActionType
{
    DoNothing,      // 아무것도 안 함 (결과만 실행)
    OpenShop,       // 상점 열기
    OpenUpgradeUI,  // 대장간 UI 열기
    StartNewDialogue // 다른 대화 시작
}

// 퀘스트 상태, 아이템 소지 여부 등의 '조건'을 명시
public enum ConditionType
{
    HasQuest,
    HasItem,
    HasMoney
}

// 퀘스트 시작/완료, 아이템 회수 등의 '결과'를 명시
public enum ChoiceResultType
{
    DoNothing,
    StartQuest,
    CompleteQuest,
    TakeItem,
    TakeMoney
}

// ## 여기가 중요! ##
// 아래 클래스들에 DialogueManager가 필요로 하는 모든 변수가 포함되어 있습니다.
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;

    // ## 오류 수정: 이 줄들이 없으면 에러가 발생합니다 ##
    public ChoiceActionType actionType;
    [Tooltip("다른 대화를 시작할 경우에만 이 필드를 채워주세요.")]
    public DialogueData nextDialogue;
    //#####################################################

    [Header("선택지 표시 조건")]
    public ChoiceCondition[] conditions;

    [Tooltip("체크하면, 위 조건들을 만족하지 못했을 때 선택지를 숨기는 대신 비활성화 상태로 보여줍니다.")]
    public bool showAsDisabledIfNotMet = false;

    [Header("선택지 결과")]
    public ChoiceResult[] results;
}

[System.Serializable]
public class ChoiceCondition
{
    public ConditionType conditionType;

    [Header("Quest 조건")]
    public QuestData requiredQuest;
    public QuestStatus requiredStatus;

    [Header("Item 조건")]
    public ItemData requiredItem;
    public int requiredItemCount = 1;

    [Header("Money 조건")]
    public int requiredMoney;
}

[System.Serializable]
public class ChoiceResult
{
    public ChoiceResultType resultType;

    [Header("Quest 관련")]
    public QuestData targetQuest;

    [Header("Item/Money 관련")]
    public ItemData itemToTake;
    public int itemCountToTake = 1;
    public int moneyToTake;
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
