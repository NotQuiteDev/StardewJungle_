using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // ## 추가: 외부에서 현재 대화 진행 상태를 확인할 수 있는 통로 ##

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

    // ## 추가: 대화 시작 프레임의 입력 충돌을 막기 위한 '깃발' ##
    private bool blockInputForOneFrame = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        dialoguePanel.SetActive(false);
        currentDialogueLines = new Queue<string>();

        // Input Action을 찾아서 연결하는 부분은 그대로 둡니다.
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions.FindAction("Interact");
            // ## 디버그 추가: Interact 액션을 찾았는지 확인 ##
            if (interactAction != null)
            {
                Debug.Log("DialogueManager: 'Interact' 액션을 성공적으로 찾았습니다.");
            }
            else
            {
                Debug.LogError("DialogueManager: 'Interact' 라는 이름의 Input Action을 찾을 수 없습니다! Input Actions 에셋을 확인해주세요.");
            }
        }
        else
        {
            Debug.LogError("DialogueManager: PlayerInput 컴포넌트를 씬에서 찾을 수 없습니다!");
        }
    }

    // ## 삭제: OnEnable, OnDisable, OnInteractPerformed 함수는 더 이상 사용하지 않습니다. ##
    
    // ## 부활: Update 함수에서 직접 입력을 감지하는 방식으로 변경 ##
    private void Update()
    {
        // ## 수정: '깃발'이 세워져 있으면, 이번 프레임은 입력을 무시하고 깃발을 내립니다. ##
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

        // ## 수정: 대화가 시작될 때 '깃발'을 세웁니다. ##
        blockInputForOneFrame = true;

        // ## 참고: 이전에 시간 정지를 임시로 풀었다면, 다시 활성화해야 합니다. ##
        GameManager.Instance.EnterUIMode(); 

        dialoguePanel.SetActive(true);
        choicesPanel.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(true);

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
        if (currentDialogueLines.Count == 0)
        {
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
        dialogueText.gameObject.SetActive(false);
        choicesPanel.gameObject.SetActive(true);
        foreach (Transform child in choicesPanel) { Destroy(child.gameObject); }

        foreach (var choice in currentDialogueData.choices)
        {
            GameObject choiceButtonGO = Instantiate(choiceButtonPrefab, choicesPanel);
            var buttonText = choiceButtonGO.GetComponentInChildren<TextMeshProUGUI>();
            var button = choiceButtonGO.GetComponent<Button>();

            buttonText.text = choice.choiceText;
            button.onClick.AddListener(() => {
                PerformChoiceAction(choice.actionType);
            });
        }
    }
    
    private void PerformChoiceAction(ChoiceActionType actionType)
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);

        // ## 핵심 수정: 어떤 선택지를 고르든, NPC의 상태를 먼저 '대화 가능'으로 되돌려줍니다. ##
        currentInteractor?.OnDialogueEnd();

        switch (actionType)
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

        // ## 수정: 대화가 끝났음을 NPC에게 알려줌 ##
        // ?.는 currentInteractor가 null이 아닐 때만 함수를 호출하는 안전장치
        currentInteractor?.OnDialogueEnd();

        GameManager.Instance.EnterGameplayMode();
    }
}