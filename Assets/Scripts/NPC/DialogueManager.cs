// DialogueManager.cs

using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image npcPortraitImage;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choicesPanel;
    [SerializeField] private GameObject choiceButtonPrefab;

    private Queue<string> currentDialogueLines;
    private DialogueData currentDialogueData;
    private bool isDialogueActive = false;
    public bool IsDialogueActive => isDialogueActive;
    private IInteractable currentInteractor;
    private InputAction interactAction;
    private bool blockInputForOneFrame = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        dialoguePanel.SetActive(false);
        currentDialogueLines = new Queue<string>();

        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions.FindAction("Interact");
            if (interactAction == null)
            {
                Debug.LogError("DialogueManager: 'Interact' 라는 이름의 Input Action을 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("DialogueManager: PlayerInput 컴포넌트를 씬에서 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        if (blockInputForOneFrame)
        {
            blockInputForOneFrame = false;
            return;
        }

        if (isDialogueActive && !choicesPanel.gameObject.activeSelf)
        {
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                DisplayNextLine();
            }
        }
    }

    public void StartDialogue(DialogueData data, IInteractable interactor)
    {
        isDialogueActive = true;
        currentDialogueData = data;
        currentInteractor = interactor;
        blockInputForOneFrame = true;

        GameManager.Instance.EnterUIMode();

        dialoguePanel.SetActive(true);
        choicesPanel.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(true); // 대화 텍스트는 항상 보이도록 설정

        npcPortraitImage.sprite = data.npcPortrait;
        npcNameText.text = data.npcName;

        currentDialogueLines.Clear();
        foreach (string line in data.dialogueLines)
        {
            currentDialogueLines.Enqueue(line);
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // ## 핵심 수정: 마지막 대사일 경우, 대사를 표시하고 바로 선택지를 띄웁니다. ##
        if (currentDialogueLines.Count == 1 && currentDialogueData.choices.Length > 0)
        {
            string lastLine = currentDialogueLines.Dequeue();
            dialogueText.text = lastLine;
            ShowChoices(); // 마지막 대사를 표시한 후 바로 선택지 표시
            return;
        }

        if (currentDialogueLines.Count == 0)
        {
            // 선택지가 있지만 대사가 없는 경우 바로 선택지 표시
            if (currentDialogueData.choices.Length > 0)
            {
                ShowChoices();
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        string line = currentDialogueLines.Dequeue();
        dialogueText.text = line;
    }

    private void ShowChoices()
    {
        choicesPanel.gameObject.SetActive(true);
        foreach (Transform child in choicesPanel) { Destroy(child.gameObject); }

        // ## 핵심 수정: 버튼을 만들기 전에 조건 확인 로직을 추가 ##
        foreach (var choice in currentDialogueData.choices)
        {
            // 이 선택지의 모든 조건이 충족되었는지 확인합니다.
            if (AreConditionsMet(choice)) 
            {
                // 조건이 충족된 선택지만 버튼으로 만듭니다.
                GameObject choiceButtonGO = Instantiate(choiceButtonPrefab, choicesPanel);
                var buttonText = choiceButtonGO.GetComponentInChildren<TextMeshProUGUI>();
                var button = choiceButtonGO.GetComponent<Button>();

                buttonText.text = choice.choiceText;
                button.onClick.AddListener(() => {
                    PerformChoiceAction(choice);
                });
            }
        }
    }

    // ## 핵심 추가: 선택지의 조건들을 확인하는 새로운 함수 ##
// ## 핵심 수정: Quest 조건만 확인하도록 함수 내부를 간소화합니다. ##
private bool AreConditionsMet(DialogueChoice choice)
{
    foreach (var condition in choice.conditions)
    {
        // 이제 조건 타입이 HasQuest 하나뿐이므로 switch 문이 필요 없습니다.
        QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(condition.requiredQuest);

        // 현재 상태가 조건과 일치하지 않으면, 즉시 false를 반환합니다.
        if (currentStatus != condition.requiredStatus)
        {
            return false;
        }
    }

    // 모든 퀘스트 조건을 통과했으면 true를 반환합니다.
    return true;
}

    // ## 수정: 파라미터로 ChoiceActionType 대신 DialogueChoice 전체를 받습니다. ##
    private void PerformChoiceAction(DialogueChoice choice)
    {
        // 새로운 대화를 시작하는 경우, 대화창을 닫지 않고 바로 새 대화를 시작합니다.
        if (choice.actionType == ChoiceActionType.StartNewDialogue)
        {
            // ## 핵심 추가: 새로운 대화 데이터가 할당되어 있다면 다음 대화를 시작합니다. ##
            if (choice.nextDialogue != null)
            {
                // 현재 대화창의 내용을 새 대화 내용으로 교체합니다.
                StartDialogue(choice.nextDialogue, currentInteractor);
            }
            else
            {
                Debug.LogWarning("다음 대화가 지정되지 않았지만 StartNewDialogue 액션이 호출되었습니다. 대화를 종료합니다.");
                EndDialogue();
            }
            return; // 아래의 로직을 실행하지 않고 함수 종료
        }

        // 그 외의 액션들은 기존처럼 대화창을 닫고 각자의 행동을 수행합니다.
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        currentInteractor?.OnDialogueEnd();

        switch (choice.actionType)
        {
            case ChoiceActionType.OpenShop:
                var shopNpc = currentInteractor as InteractableNPC;
                if (shopNpc != null) { ShopUIManager.Instance.OpenShop(shopNpc); }
                break;
            case ChoiceActionType.OpenUpgradeUI:
                var blacksmithNpc = currentInteractor as BlacksmithNPC;
                if (blacksmithNpc != null) { UpgradeUIManager.Instance.OpenUpgradeUI(blacksmithNpc); }
                break;
            case ChoiceActionType.DoNothing:
                GameManager.Instance.EnterGameplayMode();
                break;
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        currentInteractor?.OnDialogueEnd();
        GameManager.Instance.EnterGameplayMode();
    }
}