// QuestManager.cs

using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // ## 핵심: 모든 퀘스트의 현재 상태를 저장하는 Dictionary ##
    // Key: 퀘스트 원본 데이터 (QuestData)
    // Value: 해당 퀘스트의 진행 상태 (QuestStatus)
    private Dictionary<QuestData, QuestStatus> questStatuses = new Dictionary<QuestData, QuestStatus>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지되도록 설정
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 특정 퀘스트의 현재 상태를 알려주는 함수
    public QuestStatus GetQuestStatus(QuestData quest)
    {
        // 만약 Dictionary에 퀘스트 정보가 등록된 적이 없다면,
        // 그 퀘스트는 아직 시작 전(NotStarted) 상태이므로 기본값으로 돌려줌
        if (questStatuses.TryGetValue(quest, out QuestStatus status))
        {
            return status;
        }
        return QuestStatus.NotStarted;
    }

    // 퀘스트의 상태를 변경하는 함수
    public void UpdateQuestStatus(QuestData quest, QuestStatus newStatus)
    {
        questStatuses[quest] = newStatus;
        Debug.Log($"퀘스트 '{quest.questName}'의 상태가 '{newStatus}'(으)로 변경되었습니다.");
        // 여기에 퀘스트 상태 변경 시 UI 업데이트 등의 로직을 추가할 수 있습니다.
    }
}