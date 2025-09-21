using UnityEngine;
using UnityEngine.Events;

// ## 추가: 선택지가 어떤 종류의 행동을 하는지 정의하는 enum ##
public enum ChoiceActionType
{
    DoNothing,      // 아무것도 안 함
    OpenShop,       // 상점 열기
    OpenUpgradeUI   // 대장간 업그레이드 UI 열기
    // 여기에 나중에 StartQuest, GiveItem 등을 추가할 수 있습니다.
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public ChoiceActionType actionType; // UnityEvent 대신 이 enum을 사용합니다.
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