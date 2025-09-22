// QuestData.cs

using UnityEngine;

// ## 추가 1: 퀘스트의 진행 상태를 나타내는 Enum ##
public enum QuestStatus
{
    NotStarted, // 시작 전
    InProgress, // 진행 중
    Completed   // 완료
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 정보")]
    public string questID; // 퀘스트를 구분할 고유 ID
    public string questName;
    [TextArea(3, 5)]
    public string questDescription;

    // 나중에 여기에 퀘스트 목표, 보상 등의 정보를 추가할 수 있습니다.
}